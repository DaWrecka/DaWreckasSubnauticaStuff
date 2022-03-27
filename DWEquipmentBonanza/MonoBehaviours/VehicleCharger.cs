using Common;
using Common.Interfaces;
using ProtoBuf;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Json;
using SMLHelper.V2.Json.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace DWEquipmentBonanza.MonoBehaviours
{
	public abstract class VehicleCharger : MonoBehaviour,
#if SUBNAUTICA_STABLE
		IInventoryDescriptionSN1,
#elif BELOWZERO
		IInventoryDescription,
#endif
		ISerializationCallbackReceiver
	{
		protected IBattery _cell;
		protected IBattery cell => _cell ??= gameObject.GetComponent<IBattery>();

		protected virtual float ThermalChargeRate => 0f;
		protected virtual float SolarChargeRate => 0f;

		[SerializeField]
		protected float _charge;
		public virtual float dischargeRate => 1f; // Maximum rate of discharge for any internal cell, measured in units/s
		private MonoBehaviour _parent;
		private MonoBehaviour parentVehicle
		{
			get => _parent;
			set
			{
				if (value is Vehicle)
					_parent = value;
#if BELOWZERO
				else if (value is SeaTruckUpgrades)
					_parent = value;
				else if (value is SeaTruckMotor stm)
					_parent = stm.upgrades;
#endif
			}
		}

		public float lastGenerated { get; protected set; }
		public float lastExcess { get; protected set; }
		public float lastSolarCharge { get; protected set; }
		public float lastThermalCharge { get; protected set; }
		private PrefabIdentifier prefabIdentifier => this.gameObject?.GetComponent<PrefabIdentifier>();
		protected string moduleId => prefabIdentifier?.id;
		protected virtual Dictionary<string, float> difficultyKeyedSolarChargeRates { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 0.75f },
			{ "Hard", 0.5f },
			{ "Ridiculous", 0.4f },
			{ "Insane", 0.2f }
		};
		protected virtual Dictionary<string, float> difficultyKeyedThermalChargeRates { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 0.75f },
			{ "Hard", 0.5f },
			{ "Ridiculous", 0.4f },
			{ "Insane", 0.2f }
		};

		public virtual void Init(MonoBehaviour vehicle)
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): begin");

			parentVehicle = vehicle;
		}

		public float ConsumeEnergy(float energy)
		{
			if (cell == null)
				return 0f;

			float consumption = Mathf.Min(cell.charge, energy);
			cell.charge -= consumption;
			return consumption;
		}

		public virtual void Generate(MonoBehaviour vehicle, float DeltaTime)
		{
			lastSolarCharge = SolarCharge(vehicle, DeltaTime);
			lastThermalCharge = ThermalCharge(vehicle, DeltaTime);
			float otherGeneration = OtherCharge(vehicle, DeltaTime);
			float generatedEnergy = lastGenerated = lastSolarCharge + lastThermalCharge + otherGeneration;

			lastExcess = 0f;
			float maxCellRecharge = ConsumeEnergy(DeltaTime * dischargeRate);
			generatedEnergy += maxCellRecharge;
#if BELOWZERO
			if (vehicle is SeaTruckMotor stm)
			{
				stm.relay.ModifyPower(generatedEnergy, out float modified);
				lastExcess = Mathf.Max(generatedEnergy - modified, 0f);
			}
			else if (vehicle is SeaTruckUpgrades stg)
			{
				stg.relay.ModifyPower(generatedEnergy, out float modified);
				lastExcess = Mathf.Max(generatedEnergy - modified, 0f);
			}
			else
#endif
			if (vehicle is Vehicle v)
			{
				float added = v.energyInterface.AddEnergy(generatedEnergy);
				lastExcess = Mathf.Max(generatedEnergy - v.energyInterface.AddEnergy(generatedEnergy), 0f);
			}

			if (cell != null && lastExcess > 0f)
			{
				cell.charge += lastExcess;
			}
		}

		protected virtual float SolarCharge(MonoBehaviour vehicle, float DeltaTime)
		{
			if (this.SolarChargeRate <= 0f)
				return 0f;

			DayNightCycle daynight = DayNightCycle.main;
			if (daynight == null)
			{
				return 0f;
			}

			return Constants.kSeamothSolarChargePerSecond * daynight.GetLocalLightScalar() * SolarChargeRate
				* Mathf.Clamp01((Constants.kMaxSolarChargeDepth + vehicle.gameObject.transform.position.y) / Constants.kMaxSolarChargeDepth)
				* DeltaTime;
		}

		protected virtual float ThermalCharge(MonoBehaviour vehicle, float DeltaTime)
		{
			if (ThermalChargeRate <= 0f)
				return 0f;

			WaterTemperatureSimulation waterSim = WaterTemperatureSimulation.main;
			if (waterSim == null)
				return 0f;

			return VehicleUpdater.thermalReactorCharge.Evaluate(waterSim.GetTemperature(vehicle.gameObject.transform.position)) * ThermalChargeRate * DeltaTime;
		}

		protected virtual float OtherCharge(MonoBehaviour vehicle, float DeltaTime)
		{
			// Any other form of charging goes here. We're not doing anything with this, but it's available as part of the interface.
			return 0f;
		}

		[ProtoBeforeSerialization]
		public void OnBeforeSerialize()
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): begin");

			if (cell == null)
			{
				Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): no battery cell found");
				return;
			}

			if (prefabIdentifier == null)
			{
				Log.LogError($"VehicleCharger {this.GetInstanceID()} could not get PrefabIdentifier!");
				return;
			}

			if (string.IsNullOrWhiteSpace(moduleId))
			{
				Log.LogError($"Invalid ID for object");
				return;
			}
			Log.LogDebug($"Saving charge value of {cell.charge} to disk for module ID of '{moduleId}'");
			Main.saveCache.AddModuleCharge(moduleId, cell.charge);
		}

		[ProtoBeforeDeserialization]
		public void OnBeforeDeserialize()
		{ }

		[ProtoAfterDeserialization]
		public void OnAfterDeserialize()
		{
			CoroutineHost.StartCoroutine(PostDeserialize());
		}

		public void OnDestroy()
		{
			Main.saveCache.UnregisterReceiver(this);
		}

		public virtual IEnumerator PostDeserialize()
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.name}): begin");
			Main.saveCache.RegisterReceiver(this);

			if (cell == null)
			{
				Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): no battery cell found");
				yield break;
			}

			// Get and restore existing module charge, if applicable
			if (string.IsNullOrEmpty(moduleId))
			{
				Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.name}) waiting for GameObject and/or PrefabIdentifier");
				while (string.IsNullOrEmpty(moduleId))
				{
					yield return new WaitForSecondsRealtime(0.2f);
				}
			}

			// Re-activate the charger, so that it continues to charge the vehicle after a game load
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.name}) Waiting for parentVehicle");
			Transform parent = (this.transform != null && this.transform.parent != null ? this.transform.parent: null);
			while (parent == null)
			{
				yield return new WaitForEndOfFrame();
				parent = parent = (this.transform != null && this.transform.parent != null ? this.transform.parent : null);
			}

			TechType parentType = CraftData.GetTechType(parent.gameObject);
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.name}) Got TechType of {parentType.AsString()}");
#if BELOWZERO
			if (parentType == TechType.SeaTruck)
			{
				string slot = string.Empty;
				SeaTruckUpgrades stg = parent.gameObject.GetComponent<SeaTruckUpgrades>();
				while (stg == null && parent != null)
				{
					parent = parent.parent;
					stg = parent.gameObject.GetComponent<SeaTruckUpgrades>();
					yield return new WaitForEndOfFrame();
				}

				if (stg == null)
				{
					Log.LogError($"Despite finding Seatruck as parent, couldn't get SeaTruckUpgrades component");
				}
				else
				{
					while (!stg.modules.GetItemSlot(this.gameObject.GetComponent<Pickupable>(), ref slot))
					{
						yield return new WaitForEndOfFrame();
					}
					InventoryItem item = stg.modules.GetItemInSlot(slot);

					stg.modules.NotifyEquip(slot, item);
				}
			}
			else
#endif
			if (parentType == TechType.Seamoth || parentType == TechType.Exosuit)
			{
				string slot = string.Empty;
				Vehicle v = parent.gameObject.GetComponent<Vehicle>();
				while (v == null && parent != null)
				{
					parent = parent.parent;
					v = parent.gameObject.GetComponent<Vehicle>();
					yield return new WaitForEndOfFrame();
				}

				if (v == null)
				{
					Log.LogError($"Despite finding vehicle as parent, couldn't get Vehicle component");
				}
				else
				{
					while (!v.modules.GetItemSlot(this.gameObject.GetComponent<Pickupable>(), ref slot))
					{
						yield return new WaitForEndOfFrame();
					}
					InventoryItem item = v.modules.GetItemInSlot(slot);

					v.modules.NotifyEquip(slot, item);
				}

			}

			yield break;
		}

		public string GetInventoryDescription()
		{
			List<string> args = new List<string>();
			string arg0 = ""; // "Diver Perimeter Defence Chip";
			string arg1 = ""; // "Protects a diver from hostile fauna using electrical discouragement. Discharge damages the chip beyond repair.";
			TechType thisType = CraftData.GetTechType(this.gameObject);
			if (thisType != TechType.None)
			{
				arg0 = Language.main.Get(thisType);
				arg1 = Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(thisType));
			}
			//Log.LogDebug($"For techType of {this.techType.AsString()} got arg0 or '{arg0}' and arg1 of '{arg1}'");
			args.Add(string.Format("{0}\n", arg1));
			if (SolarChargeRate > 0f)
			{
				args.Add(string.Format("Solar energy rate: {0}", lastSolarCharge));
			}
			if (ThermalChargeRate > 0f)
			{
				args.Add(string.Format("Thermal energy rate: {0}", lastThermalCharge));
			}

			return String.Join("\n", args);
		}
	}

	public class VehicleThermalChargerMk1 : VehicleCharger
	{
        protected override Dictionary<string, float> difficultyKeyedSolarChargeRates { get; } = new Dictionary<string, float>();
		protected override Dictionary<string, float> difficultyKeyedThermalChargeRates { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 0.75f },
			{ "Hard", 0.5f },
			{ "Ridiculous", 0.4f },
			{ "Insane", 0.2f }
		};

		protected override float ThermalChargeRate => difficultyKeyedThermalChargeRates.GetOrDefault(Main.config.ChargeDifficulty, 0f);
	}

	public class VehicleSolarChargerMk1 : VehicleCharger
	{
		protected override Dictionary<string, float> difficultyKeyedSolarChargeRates { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 0.75f },
			{ "Hard", 0.5f },
			{ "Ridiculous", 0.4f },
			{ "Insane", 0.2f }
		};
		protected override Dictionary<string, float> difficultyKeyedThermalChargeRates { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 0.75f },
			{ "Hard", 0.5f },
			{ "Ridiculous", 0.4f },
			{ "Insane", 0.2f }
		};
		protected override float ThermalChargeRate => 0f;
		protected override float SolarChargeRate => difficultyKeyedSolarChargeRates.GetOrDefault(Main.config.ChargeDifficulty, 0f);
	}

	public abstract class VehicleChargerMk2 : VehicleCharger, IBattery
	{
		protected virtual float baseCapacity => 20f;
		protected abstract Dictionary<string, float> difficultyTypedCapacityMultipliers { get; }

		public float charge
		{
			get { return _charge; }
			set { _charge = Mathf.Clamp(value, 0f, capacity); }
		}
		public float capacity => baseCapacity * (difficultyTypedCapacityMultipliers != null ? difficultyTypedCapacityMultipliers.GetOrDefault(Main.config.ChargeDifficulty, 0f) : 0f);

		public string GetChargeValueText()
		{
			float num = this._charge / this.capacity;
#if SUBNAUTICA_STABLE
			return Language.main.GetFormat<float, int, float>("BatteryCharge", num, Mathf.RoundToInt(this._charge), this.capacity);
#elif BELOWZERO
			return Language.main.GetFormat<string, float, int, float>("BatteryCharge", ColorUtility.ToHtmlStringRGBA(Battery.gradient.Evaluate(num)), num, Mathf.RoundToInt(this._charge), this.capacity);
#endif

		}

        public override IEnumerator PostDeserialize()
        {
			yield return base.PostDeserialize();
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.name}): begin");

			if (Main.saveCache.TryGetModuleCharge(moduleId, out float charge))
			{
				Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.name}) Retrieved charge value of {charge} from disk for module ID of '{moduleId}'");
				cell.charge = Mathf.Min(charge, this.capacity);
			}
			else
				Log.LogWarning($"VehicleCharger.OnAfterDeserialize({this.name}) Failed to retrieve charge value from disk for module ID of '{moduleId}'; is this a new module?");

			yield break;
        }

        public override void Init(MonoBehaviour vehicle)
		{
			//this.cell = this;
			base.Init(vehicle);
			//ErrorMessage.AddMessage($"VehicleChargerMk2.Init: this.cell = {this.cell.ToString()}");
		}
	}

	public class VehicleThermalChargerMk2 : VehicleChargerMk2
	{
		private Dictionary<string, float> capacityMultipliers { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 5f },
			{ "Hard", 4f },
			{ "Ridiculous", 3f },
			{ "Insane", 2f }
		};
		protected override Dictionary<string, float> difficultyTypedCapacityMultipliers => capacityMultipliers;
		protected override float ThermalChargeRate => difficultyKeyedThermalChargeRates.GetOrDefault(Main.config.ChargeDifficulty, 0f);
		protected override float SolarChargeRate => 0f;
		protected override float baseCapacity => 20f;
	}

	public class VehicleSolarChargerMk2 : VehicleChargerMk2
	{
		private Dictionary<string, float> capacityMultipliers { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 6f },
			{ "Hard", 4f },
			{ "Ridiculous", 3f },
			{ "Insane", 2f }
		};
		protected override Dictionary<string, float> difficultyTypedCapacityMultipliers => capacityMultipliers;
		protected override float ThermalChargeRate => 0f;
		protected override float SolarChargeRate => difficultyKeyedSolarChargeRates.GetOrDefault(Main.config.ChargeDifficulty, 0f);
	}
	public class VehicleUnifiedCharger : VehicleChargerMk2
	{
		protected override float baseCapacity => 20f;
		private Dictionary<string, float> capacityMultipliers { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 6f },
			{ "Hard", 4f },
			{ "Ridiculous", 3f },
			{ "Insane", 2f }
		};
		protected override Dictionary<string, float> difficultyTypedCapacityMultipliers => capacityMultipliers;
		protected override Dictionary<string, float> difficultyKeyedSolarChargeRates { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 1f },
			{ "Hard", 0.75f },
			{ "Ridiculous", 0.55f },
			{ "Insane", 0.3f }
		};
		protected override Dictionary<string, float> difficultyKeyedThermalChargeRates { get; } = new Dictionary<string, float>()
		{
			{ "Easy", 1f },
			{ "Hard", 0.75f },
			{ "Ridiculous", 0.55f },
			{ "Insane", 0.3f }
		};
		protected override float ThermalChargeRate => difficultyKeyedThermalChargeRates.GetOrDefault(Main.config.ChargeDifficulty, 0f);
		protected override float SolarChargeRate => difficultyKeyedSolarChargeRates.GetOrDefault(Main.config.ChargeDifficulty, 0f);
	}
}
