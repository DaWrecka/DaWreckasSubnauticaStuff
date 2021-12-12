using DWEquipmentBonanza.Equipables;
using DWEquipmentBonanza.Patches;
using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

#if SUBNAUTICA_STABLE
using Common.Interfaces;
#endif

namespace DWEquipmentBonanza.MonoBehaviours
{
	public class DiverPerimeterDefenceBehaviour : MonoBehaviour,
#if SUBNAUTICA_STABLE
		IInventoryDescriptionSN1,
#elif BELOWZERO
		IInventoryDescription,
#endif
		IBattery,
		ICraftTarget,
		ISerializationCallbackReceiver
	{
		private static Dictionary<TechType, int> maxDischarges = new Dictionary<TechType, int>();
		private static Dictionary<TechType, bool> destroyWhenDischarged = new Dictionary<TechType, bool>();

		protected const float JuicePerDischarge = 100f; // Units of energy consumed by a perimeter discharge.
		protected static int MaxDischargeCheat = 0;
		[SerializeField]
		protected float _charge;
		[SerializeField]
		protected TechType techType;
		protected Pickupable thisPickup;
		protected bool bDestroyWhenEmpty;// If true, the chip is destroyed when empty. If false, the chip is just empty and can possibly be recharged
		protected int _maxDischarges;
		protected virtual int MaxDischarges
		{
			get
			{
				if (_maxDischarges <= 0)
					_maxDischarges = 1;

				return _maxDischarges;
			}
		}
		public TechType ChipTechType { get; protected set; }
		public static readonly Gradient gradient = new Gradient
		{
			colorKeys = new GradientColorKey[]
			{
				new GradientColorKey(new Color(0.8745099f, 0.2509804f, 0.1490196f, 1f), 0f),
				new GradientColorKey(new Color(1f, 0.8196079f, 0f, 1f), 0.5f),
				new GradientColorKey(new Color(0.5803922f, 0.8705883f, 0f, 1f), 1f)
			},
			alphaKeys = new GradientAlphaKey[]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};
		public float charge
		{
			get { return _charge; }
			set { _charge = Mathf.Clamp(value, 0f, capacity); }
		}
		public float capacity
		{
			get
			{
				return MaxDischarges * JuicePerDischarge;
			}
		}
		protected virtual float DischargeDamage
		{
			get { return 10f; }
		}
		protected virtual string brokenTechString
		{
			get
			{ return "DiverPerimeterDefenceChip_Broken"; }
		}
		protected virtual int MaxCharge
		{
			get { return 1; }
		}

		public void RuntimeDischargeCheat(int Cheat)
		{
			MaxDischargeCheat = Cheat;
		}

		public void Awake()
		{
			if (thisPickup == null && gameObject.TryGetComponent<Pickupable>(out Pickupable component))
				thisPickup = component;
		}

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			if (this.techType == TechType.None)
			{
				Log.LogError($"DiverPerimeterDefenceBehaviour deserialised with null TechType!");
				return;
			}

			(int discharges, bool bDestroy) returnValue = GetChipData(this.techType);

			if (returnValue.discharges > 1)
				this._maxDischarges = returnValue.discharges;

			this.bDestroyWhenEmpty = returnValue.bDestroy;
			if (thisPickup == null && gameObject.TryGetComponent<Pickupable>(out Pickupable pickupable))
				thisPickup = pickupable;
		}

		public void OnProtoSerialize(ProtobufSerializer serializer)
		{
		}

		public void OnProtoDeserialize(ProtobufSerializer serializer)
		{
		}

		internal static void AddChipData(TechType chip, int maxDischargeValue, bool bDestroy)
		{
			Log.LogDebug($"DiverPerimeterDefenceBehaviour.AddChipData: chip = {chip.AsString()}, maxDischargeValue = {maxDischargeValue}, bDestroy = {bDestroy}");

			maxDischarges[chip] = maxDischargeValue;
			destroyWhenDischarged[chip] = bDestroy;
		}

		internal static (int discharges, bool bDestroy) GetChipData(TechType chip)
		{
			//int discharges;
			//bool bDestroy = destroyWhenDischarged.GetOrDefault(chip, false);
			(int discharges, bool bDestroy) returnValue = (maxDischarges.GetOrDefault(chip, -1), destroyWhenDischarged.GetOrDefault(chip, false));

			Log.LogDebug($"DiverPerimeterDefenceBehaviour.GetChipData({chip.AsString()}): got values of {returnValue.ToString()}");

			return returnValue;
		}

		internal void Initialise(TechType newTechType)
		{
			Log.LogDebug($"DiverPerimeterDefenceBehaviour.Initialise(): existing chip TechType {this.techType.AsString()}, passed TechType {newTechType.AsString()}");
			if (this.techType == TechType.None)
				this.OnCraftEnd(newTechType);
		}

		// Returns true if discharge occurred, false otherwise
		internal bool Discharge(GameObject attacker)
		{
			if (this.charge < 1)
			{
				Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): chip TechType {techType.AsString()} battery is dead");
				return false;
			}

			LiveMixin mixin = attacker.GetComponent<LiveMixin>();
			if (mixin == null)
			{
				Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): chip TechType {techType.AsString()} could not find LiveMixin component on attacker");
				return false;
			}

			Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): chip TechType {techType.AsString()} discharging");
			mixin.TakeDamage(DischargeDamage, gameObject.transform.position, DamageType.Electrical, gameObject);
			this.charge = Mathf.Max(this.charge - JuicePerDischarge, 0f);
			Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): Discharged, available charge now {this.charge}");
			if (this.charge < 1f)
			{
				if (bDestroyWhenEmpty)
				{
					Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): bDestroyWhenEmpty = true, destroying chip");
					CoroutineHost.StartCoroutine(AddBrokenChipAndDestroy());
				}
			}
			return true;
		}

		protected IEnumerator AddBrokenChipAndDestroy()
		{
			if (!bDestroyWhenEmpty)
				yield break;

			Equipment e = Inventory.main.equipment;
			e.RemoveItem(thisPickup != null ? thisPickup : gameObject.GetComponent<Pickupable>());
			//TaskResult<GameObject> result = new TaskResult<GameObject>();
			yield return AddInventoryAsync(Main.GetModTechType(brokenTechString)); //, result);
			GameObject.Destroy(gameObject);
			yield break;
		}

		protected IEnumerator AddInventoryAsync(TechType techType, IOut<GameObject> result = null)
		{
#if SUBNAUTICA_STABLE
			GameObject go = CraftData.InstantiateFromPrefab(techType, false);
#elif BELOWZERO
			TaskResult<GameObject> instResult = new TaskResult<GameObject>();
			yield return CraftData.InstantiateFromPrefabAsync(techType, instResult, false);

			GameObject go = instResult.Get();
#endif
			Pickupable component = go?.GetComponent<Pickupable>();
			if (component != null)
				Inventory.main.ForcePickup(component);
			else
			{
				Log.LogError($"DiverPerimeterDefenceBehaviour.AddInventoryAsync(): Failed to instantiate inventory item for TechType {techType.AsString()}");
			}

			if(result != null)
				result.Set(go);
			yield break;
		}

		protected IEnumerator AddBattery(TechType techType, float setCharge = 0f)
		{
			if (techType == TechType.None)
				yield break;

			TaskResult<GameObject> result = new TaskResult<GameObject>();
			yield return AddInventoryAsync(techType, result);

			//IBattery component = result.Get().GetComponent<IBattery>();

			GameObject resultObject = result.Get();
			if (resultObject != null && resultObject.TryGetComponent<IBattery>(out IBattery component))
			{
				if (setCharge == 0f)
					component.charge = 0f;
				// if the passed value for charge is less than or equal to 1, use it as a multiplier
				else if (setCharge <= 1f)
					component.charge = component.capacity * Mathf.Clamp01(setCharge);
				else
					component.charge = setCharge;
				InventoryPatches.ResetBatteryCache();
			}
			yield break;
		}

		public void OnCraftEnd(TechType craftedTechType)
		{
			(int discharges, bool bDestroy) returnValue = GetChipData(craftedTechType);

			if (returnValue.discharges < 1)
			{
				Log.LogError($"DiverPerimeterDefenceBehaviour.OnCraftEnd(): craftedTechType of {craftedTechType.AsString()} returned invalid data! This is not a chip.");
				return;
			}

			this.techType = craftedTechType;
			this._maxDischarges = returnValue.discharges;
			this.bDestroyWhenEmpty = returnValue.bDestroy;

			TechType battery = InventoryPatches.GetCachedBattery();
			float cachedCharge = InventoryPatches.GetCachedCharge();
			Log.LogDebug($"DiverPerimeterDefenceBehaviour.OnCraftEnd({craftedTechType.AsString()}): battery = {battery.AsString()}, cachedCharge = {cachedCharge}");
			if (battery != TechType.None)
			{
				if (cachedCharge >= 0f)
				{
					if (cachedCharge <= 1f)
						this.charge = this.capacity * cachedCharge;
					else
						this.charge = cachedCharge;
				}
				CoroutineHost.StartCoroutine(AddBattery(battery, 0f));
			}
			else
			{
				this.charge = this.capacity;
			}
			Log.LogDebug($"DiverPerimeterDefenceBehaviour.OnCraftEnd({craftedTechType.AsString()}): completed");
		}

		public string GetChargeValueText()
		{
			Log.LogDebug("DiverPerimeterDefenceBehaviour.GetChargeValueText() begin");

			int numShots = Mathf.FloorToInt(this._charge / JuicePerDischarge);
			int maxShots = Mathf.FloorToInt(this.capacity / JuicePerDischarge);
			float num = numShots / maxShots;
			//return Language.main.GetFormat<string, float, int, float>("BatteryCharge", ColorUtility.ToHtmlStringRGBA(gradient.Evaluate(num)), num, numShots, maxShots);
			Log.LogDebug("DiverPerimeterDefenceBehaviour.GetChargeValueText() ending");
			return Language.main.GetFormat<string, float, int, int>("<color=#{0}>{1,4}u ({2}/{3})</color>", ColorUtility.ToHtmlStringRGBA(gradient.Evaluate(num)), Mathf.Floor(this.charge), numShots, maxShots);
		}

		public string GetInventoryDescription()
		{
			//Log.LogDebug("DiverPerimeterDefenceBehaviour.GetInventoryDescription() begin");
			string arg0 = ""; // "Diver Perimeter Defence Chip";
			string arg1 = ""; // "Protects a diver from hostile fauna using electrical discouragement. Discharge damages the chip beyond repair.";
			if (this.techType != TechType.None)
			{
				arg0 = Language.main.Get(this.techType);
				arg1 = Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(this.techType));
			}
			//Log.LogDebug($"For techType of {this.techType.AsString()} got arg0 or '{arg0}' and arg1 of '{arg1}'");
			return string.Format("{0}\n", arg1);
		}
	}

}
