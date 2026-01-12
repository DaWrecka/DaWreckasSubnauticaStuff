using DWEquipmentBonanza;
using DWEquipmentBonanza.VehicleModules;
using DWEquipmentBonanza.MonoBehaviours;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using UWE;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace DWEquipmentBonanza.MonoBehaviours
{
	public abstract class VehicleUpdater : MonoBehaviour, ISerializationCallbackReceiver
	{
		[SerializeField]
		protected HashSet<VehicleCharger> activeChargers = new();
		protected VehicleRepairComponent repairComponent;

		protected const float ChargerWeightLimit = 4.5f; // Maximum value of connected chargers. If a new charger would take the weight above this limit, it will not be allowed.
		protected static Dictionary<TechType, float> ChargerWeights = new()
		{
			{ TechType.SeamothSolarCharge, 1f },
			{ TechType.ExosuitThermalReactorModule, 1f }
		};
		protected float chargerWeightCumulative = 0f;
		protected const float InvokeInterval = 0.5f;
		protected int repairSlotID = -1;
		protected bool bRepairActiveMode = false;
		protected bool bInitialised;
		public static HashSet<TechType> repairModuleTechTypes { get; protected set; }

		private MonoBehaviour _parent;
		public static AnimationCurve thermalReactorCharge;

		protected MonoBehaviour parentVehicle
		{
			get => _parent;
			set
			{
#if BELOWZERO
				if (value is SeaTruckUpgrades || value is SeaTruckMotor)
					_parent = value;
				else
#endif
				if (value is Vehicle)
					_parent = value;
				else if (value == null)
					_parent = null;
			}
		}

		public static bool AddRepairModuleType(TechType newRepairModule)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			if (newRepairModule == TechType.None)
			{
				//Log.LogError($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}(): attempt to add invalid repair module");
				return false;
			}

			repairModuleTechTypes ??= new HashSet<TechType>();

			//Log.LogError($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}(): attempting to register repair module TechType {newRepairModule.AsString()}");
			return repairModuleTechTypes.Add(newRepairModule);
		}

		public virtual void OnBeforeSerialize() { }

		public virtual void OnAfterDeserialize()
		{
			CoroutineHost.StartCoroutine(OnAfterDeserializeCoroutine());
		}

		protected virtual IEnumerator OnAfterDeserializeCoroutine()
		{
			/*System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): waiting for Vehicle");

			Vehicle v = parentVehicle as Vehicle;
			while (v == null)
			{
				yield return new WaitForSecondsRealtime(0.1f);
				v = parentVehicle as Vehicle;
			}

			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): waiting for Equipment");
			ICollection<string> slots = v.modules?.equipment?.Keys;
			while (slots == null || slots.Count < 1)
			{
				yield return new WaitForEndOfFrame();
				slots = v.modules?.equipment?.Keys;
			}

			foreach (string s in slots)
			{
				InventoryItem i = v.modules.GetItemInSlot(s);
				if (i?.item != null && ChargerWeights.ContainsKey(i.item.GetTechType()))
				{
					Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): re-equipping item in slot '{s}', TechType {i.item.GetTechType()}");
					OnEquipModule(s, i);
				}

				yield return new WaitForEndOfFrame();
			}
			*/
			yield break;
		}

		public void Awake()
		{
			repairModuleTechTypes ??= new();
		}

		public virtual void Initialise(ref MonoBehaviour vehicle)
		{
			repairModuleTechTypes ??= new();

			if (vehicle != null && vehicle is Vehicle V)
			{
				if (parentVehicle == vehicle)
				{
					Log.LogError($"Initialise called multiple times for vehicle! Vehicle name {vehicle.name}, vehicle ID {vehicle.GetInstanceID()})");
				}
				else
				{
					System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
					MethodBase callingMethod = new StackFrame(1).GetMethod();
					//Log.LogDebug($"{thisMethod.ReflectedType.Name}({vehicle.name}, {vehicle.GetInstanceID()}).{thisMethod.Name}() executing, invoked by: '{callingMethod.ReflectedType.Name}.{callingMethod.Name}'");

					parentVehicle = vehicle;
					V.onToggle -= OnToggle;
					V.onSelect -= OnSelect;
					V.modules.onEquip -= OnEquipModule;
					V.modules.onUnequip -= OnUnequipModule;
					V.modules.isAllowedToAdd -= IsAllowedToAdd;

					V.onToggle += OnToggle;
					V.onSelect += OnSelect;
					V.modules.onEquip += OnEquipModule;
					V.modules.onUnequip += OnUnequipModule;
					V.modules.isAllowedToAdd += IsAllowedToAdd;
					bInitialised = true;

					//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({vehicle.name}) end");
				}
			}
		}

		protected virtual void OnEquipModule(string slot, InventoryItem item)
		{
			//MethodBase thisMethod = MethodBase.GetCurrentMethod();
			//MethodBase callingMethod = new StackFrame(1).GetMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}({this.GetInstanceID()}).{thisMethod.Name}({slot}, item TechType: {item.item.GetTechType().AsString()}, item ID {item.item.GetInstanceID()}) executing, invoked by: '{callingMethod.ReflectedType.Name}.{callingMethod.Name}'");

			Pickupable pickup = item.item;
			TechType moduleType = pickup.GetTechType();
			//ErrorMessage.AddMessage($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}(): item TechType {moduleType.AsString()}");
			if (ChargerWeights.TryGetValue(moduleType, out float weight))
			{
				//ErrorMessage.AddMessage($"Attempting to find VehicleCharger component");
				VehicleCharger component = pickup.gameObject.GetComponent<VehicleCharger>();
				if (component != null && activeChargers.Add(component))
				{
					// HashSet.Add() will only return true if the component isn't already in the set, so we know this code will only run if this VehicleCharger is being newly-added.
					if (activeChargers.Count == 1) // This will only invoke if this is the first charger added; this prevents the timer from running multiple times consecutively
						base.InvokeRepeating("UpdateRecharge", InvokeInterval, InvokeInterval);
					chargerWeightCumulative += weight;
					//ErrorMessage.AddMessage($"Adding {moduleType.AsString()} to active chargers list");
					component.Init(parentVehicle);
				}
			}
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
		}

		protected virtual void OnUnequipModule(string slot, InventoryItem item)
		{
			//string memberName = new StackFrame(1)?.GetMethod().Name;
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}({this.GetInstanceID()}).{thisMethod.Name}({slot}, item TechType: {item.item.GetTechType().AsString()}, item ID {item.item.GetInstanceID()}) base executing, invoked by: '{memberName}'");
			Pickupable pickup = item.item;
		
			TechType moduleType = pickup.GetTechType();
			//ErrorMessage.AddMessage($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slot}, {item.item.name}): item TechType {moduleType.AsString()}");
			if (ChargerWeights.TryGetValue(moduleType, out float weight))
			{
				chargerWeightCumulative = System.Math.Max(chargerWeightCumulative - weight, 0);
				//ErrorMessage.AddMessage($"Attempting to find VehicleCharger component");
				VehicleCharger component = pickup.gameObject.GetComponent<VehicleCharger>();
				if (component != null && activeChargers.Contains(component))
				{
					//ErrorMessage.AddMessage($"Removing {moduleType.AsString()} from active chargers list");
					activeChargers.Remove(component);
				}
				if (activeChargers.Count < 1)
					base.CancelInvoke("UpdateRecharge");
			}
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slot}, {item.item.name}) end");
		}

		protected virtual void OnToggle(int slotID, bool state)
		{
			if (slotID == repairSlotID)
			{
				repairComponent ??= gameObject.EnsureComponent<VehicleRepairComponent>();
				repairComponent.SetActiveState(state);
				ErrorMessage.AddMessage("Repair module state: " + (state ? "active" : "passive"));
			}
		}

		protected virtual void OnSelect(int slotID)
		{
#if BELOWZERO
#endif
		}
		internal virtual void PostOverrideAcceleration(ref Vector3 acceleration) { }
		internal virtual void ApplyPhysicsMove() { }

		public virtual int GetModuleCount(TechType techType)
		{
			if (parentVehicle is Vehicle V)
				return V.modules.GetCount(techType);

			return 0;
		}
		public virtual bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
		{
			TechType tt = CraftData.GetTechType(pickupable.gameObject);
			if (tt != TechType.None)
			{
				bool allowed = this.AllowedToAdd(tt, out _, out string m);
				if (!String.IsNullOrEmpty(m))
					ErrorMessage.AddMessage(m);

				return allowed;
			}

			return true;
		}

		public virtual bool AllowedToAdd(TechType tt, out bool bOverride, out string message)
		{
			// Determines whether or not a module of the specified TechType should be allowed to be added.
			// If bOverride == true, the return value of this method will take priority 
			// if bOverride == false, then the return value of this method isn't necessarily ignored, but another method could override it.
			message = "";

			if (ChargerWeights.TryGetValue(tt, out float weight))
			{
				bOverride = true;

				if (chargerWeightCumulative + weight > ChargerWeightLimit)
				{
					message = "Too much electrical load to support this module";
					return false;
				}

				return true;
			}

			bOverride = false;
			return true;
		}

		protected virtual void OnDestroy()
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
			if (parentVehicle is Vehicle V)
			{
				V.onToggle -= OnToggle;
				V.onSelect -= OnSelect;
				V.modules.onEquip -= OnEquipModule;
				V.modules.onUnequip -= OnUnequipModule;
				V.modules.isAllowedToAdd -= IsAllowedToAdd;
			}
			parentVehicle = null;

			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
		}

		public static bool AddChargerType(TechType chargerType, float chargerWeight)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({chargerType.AsString()}, {chargerWeight}) executing");
			if (ChargerWeights.ContainsKey(chargerType))
			{
				//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() returning false");
				return false;
			}

			ChargerWeights.Add(chargerType, chargerWeight);

			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() returning true");
			return true;
		}

		protected virtual void Start()
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");

			if (thermalReactorCharge is null && PrefabDatabase.TryGetPrefabFilename(CraftData.GetClassIdForTechType(TechType.Exosuit), out string RCFilename))
			{
#if ASYNC
				AddressablesUtility.LoadAsync<GameObject>(RCFilename).Completed += (x) =>
				{
					GameObject gameObject1 = x.Result;
					Exosuit exosuit = gameObject1?.GetComponent<Exosuit>();
					thermalReactorCharge = exosuit?.thermalReactorCharge;
				};
#else
				var gameObject1 = Resources.Load<GameObject>(RCFilename);
				Exosuit exosuit = gameObject1?.GetComponent<Exosuit>();
				thermalReactorCharge = exosuit?.thermalReactorCharge;
#endif
			}


			MonoBehaviour vehicle = this.gameObject.GetComponent<Vehicle>();
			if (vehicle != null)
				this.Initialise(ref vehicle);
#if BELOWZERO
			else
			{
				MonoBehaviour stg = this.gameObject.GetComponent<SeaTruckUpgrades>();
				if (stg != null)
					this.Initialise(ref stg);
			}
#endif
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
		}

		internal virtual void PostNotifySelectSlot(MonoBehaviour instance, int slotID) { }

		internal virtual void PreUpdate(MonoBehaviour instance = null) { }

		// Not to be confused with Unity's Update, though it is related
		internal virtual void OnUpdate(MonoBehaviour instance = null) { }

		internal virtual void PostUpdate(MonoBehaviour instance = null) { }

		internal virtual void PreUpdateEnergy(MonoBehaviour instance = null) { }

		internal virtual void PostUpdateEnergy(MonoBehaviour instance = null) { }

		internal virtual void PostUpgradeModuleChange(int slotID, TechType techType, bool added, MonoBehaviour instance)
		{
			if (repairModuleTechTypes.Contains(techType))
			{
				this.repairComponent ??= gameObject.EnsureComponent<VehicleRepairComponent>();
				this.repairSlotID = (added ? slotID : -1);
				this.repairComponent.SetEnabled(added, instance);
				this.repairComponent.SetActiveState(false);
			}

			// We do some charger handling in this method, because this method is invoked for each equipped module following a game load.
			// We only check for added == true because OnUnequipModule still works for unequipping, as that can only meaningfully happen during gameplay.
			if (added)
			{
				if (ChargerWeights.ContainsKey(techType))
				{
					Vehicle v = gameObject.GetComponent<Vehicle>();
					string slot = v.slotIDs[slotID];
					InventoryItem item = v.GetSlotItem(slotID);

					// I could move this code into this method, but I'd already written the methods as OnEquipModule/OnUnequipModule by the time I realised the advantages of this approach.
					OnEquipModule(slot, item);
				}
			}
		}

		internal virtual void PostUpgradeModuleUse(MonoBehaviour instance, TechType tt, int slotID)
		{
			//Log.LogDebug($"{this.name}.PostUpgradeModuleUse({tt.AsString()} begin");
			if (repairModuleTechTypes.Contains(tt))
			{
				bool state = (repairComponent ??= gameObject.EnsureComponent<VehicleRepairComponent>()).ToggleActiveState(instance);
				ErrorMessage.AddMessage("Repair module state: " + (state ? "active" : "passive"));
			}
			//Log.LogDebug($"{this.name}.PostUpgradeModuleUse({tt.AsString()} end");
		}

		internal virtual bool PreQuickSlotIsToggled(MonoBehaviour instance, ref bool result, int slotID)
		{
			if (slotID == repairSlotID && repairComponent != null)
			{
				result = repairComponent.GetIsActive();
				return false;
			}
			return true;
		}

		protected virtual void UpdateRecharge()
		{
			if (activeChargers.Count > 0)
			{
				foreach (var charger in activeChargers)
					charger.Generate(parentVehicle, InvokeInterval);
			}
			else
			{
				base.CancelInvoke("UpdateRecharge");
			}
		}

		public virtual void PostOnPilotEnd() { }
	}
}
