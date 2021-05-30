using CombinedItems.Equipables;
using CombinedItems.Patches;
using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace CombinedItems.MonoBehaviours
{
	public class DiverPerimeterDefenceBehaviour : MonoBehaviour, IInventoryDescription, IBattery, ICraftTarget
	{
		protected const float JuicePerDischarge = 100f; // Units of energy consumed by a perimeter discharge.
		protected static int MaxDischargeCheat = 0;
		protected float _charge;
		protected TechType techType;
		protected Pickupable thisPickup;
		protected virtual bool bDestroyWhenEmpty => true;// If true, the chip is destroyed when empty. If false, the chip is just empty and can possibly be recharged
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
			set { _charge = System.Math.Min(capacity, value); }
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
			if (thisPickup == null)
				thisPickup = gameObject.GetComponent<Pickupable>();
		}

		/*internal void SetChipType(TechType tt)
		{
			if (tt == TechType.None)
			{
				Log.LogError($"DiverPerimeterDefenceBehaviour.SetChipType() called with null TechType");
				return;
			}

			ChipTechType = tt;
		}*/

		internal void SetMaxDischarges(int newLimit)
		{
			_maxDischarges = newLimit;
		}

		// Returns true if discharge occurred, false otherwise
		internal bool Discharge(GameObject attacker)
		{
			if (this.charge < 1)
			{
				Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): battery is dead");
				return false;
			}

			LiveMixin mixin = attacker.GetComponent<LiveMixin>();
			if (mixin == null)
			{
				Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): could not find LiveMixin component on attacker");
				return false;
			}

			Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): Discharging");
			mixin.TakeDamage(DischargeDamage, gameObject.transform.position, DamageType.Electrical, gameObject);
			this.charge = Mathf.Max(this.charge - JuicePerDischarge, 0f);
			Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): Discharged, available charge now {this.charge}");
			if (this.charge < 1f)
			{
				Equipment e = Inventory.main.equipment;
				e.RemoveItem(thisPickup != null ? thisPickup : gameObject.GetComponent<Pickupable>());
				if (bDestroyWhenEmpty)
				{
					CoroutineHost.StartCoroutine(AddBrokenChipAndDestroy());
				}
			}
			return true;
		}

		protected IEnumerator AddBrokenChipAndDestroy()
		{
			TaskResult<GameObject> result = new TaskResult<GameObject>();
			yield return AddInventoryAsync(Main.GetModTechType(brokenTechString), result);
			GameObject.Destroy(gameObject);
			yield break;
		}

		protected IEnumerator AddInventoryAsync(TechType techType, IOut<GameObject> result)
		{
			TaskResult<GameObject> instResult = new TaskResult<GameObject>();
			yield return CraftData.InstantiateFromPrefabAsync(techType, instResult, false);

			GameObject go = instResult.Get();
			Pickupable component = (go != null ? go.GetComponent<Pickupable>() : null);
			if (component != null)
				Inventory.main.ForcePickup(component);
			else
			{
				Log.LogError($"DiverPerimeterDefenceBehaviour.AddInventoryAsync(): Failed to instantiate inventory item for TechType {techType.AsString()}");
			}

			result.Set(go);
			yield break;
		}

		protected IEnumerator AddBattery(TechType techType, float setCharge = 0f)
		{
			if (techType == TechType.None)
				yield break;

			TaskResult<GameObject> result = new TaskResult<GameObject>();
			yield return AddInventoryAsync(techType, result);

			IBattery component = result.Get().GetComponent<IBattery>();
			if (component != null)
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

		public void OnCraftEnd(TechType techType)
		{
			this.techType = techType;
			TechType battery = InventoryPatches.GetCachedBattery();
			float cachedCharge = InventoryPatches.GetCachedCharge();
			Log.LogDebug($"DiverPerimeterDefenceBehaviour.OnCraftEnd(): battery = {battery.AsString()}, cachedCharge = {cachedCharge}");
			if (battery != TechType.None)
			{
				if (cachedCharge > 0)
				{
					this.charge = cachedCharge;
				}
				CoroutineHost.StartCoroutine(AddBattery(battery, 0f));
			}
			else
			{
				this.charge = this.capacity;
			}
		}

		// Charge the internal battery using a provided battery.
		public void ChargeWithBattery(IBattery newBattery)
		{
			if (newBattery.charge > charge)
			{
				charge += newBattery.charge;
				newBattery.charge = 0f;
			}
		}

		public string GetChargeValueText()
		{
			int numShots = Mathf.FloorToInt(this._charge / JuicePerDischarge);
			int maxShots = Mathf.FloorToInt(this.capacity / JuicePerDischarge);
			float num = numShots / maxShots;
			//return Language.main.GetFormat<string, float, int, float>("BatteryCharge", ColorUtility.ToHtmlStringRGBA(gradient.Evaluate(num)), num, numShots, maxShots);
			return Language.main.GetFormat<string, float, int, int>("<color=#{0}>{1,4}u ({2}/{3})</color>", ColorUtility.ToHtmlStringRGBA(gradient.Evaluate(num)), Mathf.Floor(this.charge), numShots, maxShots);
		}

		public string GetInventoryDescription()
		{
			string arg0 = ""; // "Diver Perimeter Defence Chip";
			string arg1 = ""; // "Protects a diver from hostile fauna using electrical discouragement. Discharge damages the chip beyond repair.";
			if (this.techType != TechType.None)
			{
				arg0 = Language.main.Get(this.techType);
				arg1 = Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(this.techType));
			}
			return string.Format("{0}\n{1}\n", arg0, arg1);
		}
	}

}
