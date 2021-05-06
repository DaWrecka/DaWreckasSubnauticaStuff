using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using SMLHelper.V2.Assets;
using System.Collections;
using UWE;
using Common;

namespace CombinedItems.Equipables
{
	public class DiverPerimeterDefenceBehaviour : MonoBehaviour, IInventoryDescription
	{
		private const float DischargeDamage = 10f;
		protected virtual bool bDestroyWhenEmpty {
			get { return true; } // If true, the chip is destroyed when empty. If false, the chip is just empty.
		} 
		private Pickupable thisPickup;

		protected virtual int MaxCharge {
			get { return 1; }
		}

		public TechType ChipTechType { get; private set; }
		private Battery battery;

		public void Awake()
		{
			if (battery == null)
				battery = gameObject.GetComponent<Battery>();
			if (thisPickup == null)
				thisPickup = gameObject.GetComponent<Pickupable>();
		}

		internal void SetBattery(Battery batt)
		{
			battery = batt;
			battery._charge = MaxCharge;
			battery._capacity = MaxCharge;
		}

		internal void SetChipType(TechType tt)
		{
			if (tt == TechType.None)
			{
				Log.LogError($"DiverPerimeterDefenceBehaviour.SetChipType() called with null TechType");
				return;
			}

			ChipTechType = tt;
		}

		// Returns true if discharge occurred, false otherwise
		internal bool Discharge(GameObject attacker)
		{
			if (battery == null)
			{
				Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): battery is null");
				return false;
			}

			if (battery.charge < 1)
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
			battery._charge -= 1;
			Log.LogDebug($"DiverPerimeterDefenceBehaviour.Discharge(): Discharged, available charges now {battery.charge}");
			if (battery._charge < 1)
			{
				Equipment e = Inventory.main.equipment;
				e.RemoveItem(thisPickup != null ? thisPickup : gameObject.GetComponent<Pickupable>());
				if (bDestroyWhenEmpty)
				{
					if (DiverPerimeterDefenceChipItem.brokenPrefab != null)
					{
						GameObject brokenChip = GameObject.Instantiate(DiverPerimeterDefenceChipItem.brokenPrefab);
						if (brokenChip != null)
							Inventory.main.ForcePickup(brokenChip.GetComponent<Pickupable>());
						else
						{
							Log.LogError($"DiverPerimeterDefenceBehaviour.Discharge(): Failed to instantiate broken chip");
						}
					}
					else
					{
						Log.LogError($"DiverPerimeterDefenceBehaviour.Discharge(): No prefab available for broken chip");
					}
					GameObject.Destroy(gameObject);
				}
			}
			return true;
		}

		public string GetInventoryDescription()
		{
			string arg0 = "Diver Perimeter Defence Chip";
			string arg1 = "Protects a diver from hostile fauna using electrical discouragement. Discharge damages the chip beyond repair.";
			//string arg2 = "";
			if (ChipTechType != TechType.None)
			{
				arg0 = Language.main.Get(ChipTechType);
				arg1 = Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(ChipTechType));
			}
			//arg2 = battery == null ? "DISCHARGED" : battery.GetChargeValueText();
			return string.Format("{0}\n{1}\n", arg0, arg1);
		}
	}

	public class DiverPerimeterDefenceChip_Broken : PdaItem
	{
		private static bool bWaiting;
		protected static TechType templateTechType => TechType.MapRoomHUDChip;
		public static Sprite icon { get; private set; }
		public static GameObject prefab { get; private set; }

		public DiverPerimeterDefenceChip_Broken() : base("DiverPerimeterDefenceChip_Broken", "Diver Perimeter Defence Chip", $"Protects a diver from hostile fauna using electrical discouragement.\n\nChip has been discharged and is non-functional.")
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				SMLHelper.V2.Handlers.PrefabHandler.Main.RegisterPrefab(this);
			};
		}

		public override Vector2int SizeInInventory => new Vector2int(1, 1);
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Equipment;
		public override TechType RequiredForUnlock => TechType.Polyaniline;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData();
			/*{
				craftAmount = 0,
				Ingredients = new List<Ingredient>()
			};*/
		}
		protected static IEnumerator PostPatchSetup()
		{
			if (bWaiting)
				yield break;

			bWaiting = true;

			while (bWaiting)
			{
				if (icon == null || icon == SpriteManager.defaultSprite)
				{
					icon = SpriteManager.Get(templateTechType);
				}
				else
					bWaiting = false;

				yield return new WaitForSecondsRealtime(0.5f);
			}
		}

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
			if (prefab == null)
			{
				var task = CraftData.GetPrefabForTechTypeAsync(TechType.MapRoomHUDChip);
				yield return task;

				prefab = task.GetResult();
			}

			gameObject.Set(GameObject.Instantiate(prefab));
        }
    }

	public class DiverPerimeterDefenceChipItem : Equipable
    {
		public static Sprite icon { get; private set; }
		public static GameObject prefab { get; private set; }
		public static GameObject brokenPrefab { get; private set; }

		protected Battery battery;
		private bool bWaiting;

		public virtual TechType templateTechType =>
#if SN1
			TechType.SeamothElectricalDefense;
#elif BZ
			TechType.SeaTruckUpgradePerimeterDefense;
#endif
		public DiverPerimeterDefenceChipItem() : base("DiverPerimeterDefenceChipItem", "Diver Perimeter Defence Chip", $"Protects a diver from hostile fauna using electrical discouragement. Discharge damages the chip beyond repair.")
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		public override EquipmentType EquipmentType => EquipmentType.Chip;
		public override Vector2int SizeInInventory => new Vector2int(1, 1);
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Equipment;
		public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
		public override string[] StepsToFabricatorTab => new string[] { "Personal", "Equipment" };
		public override float CraftingTime => 5f;
		public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
		public override TechType RequiredForUnlock => TechType.Polyaniline;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.WiringKit, 1),
						new Ingredient(TechType.CopperWire, 2),
						new Ingredient(TechType.Polyaniline, 1)
					}
				)
			};
		}

		protected virtual IEnumerator PostPatchSetup()
		{
			if (bWaiting)
				yield break;

			bWaiting = true;

			while (bWaiting)
			{
				if (icon == null || icon == SpriteManager.defaultSprite)
				{
					icon = SpriteManager.Get(templateTechType);
				}
				else
					bWaiting = false;

				yield return new WaitForSecondsRealtime(0.5f);
			}
			Inventory.main.equipment.GetSlots(EquipmentType.Chip, Main.chipSlots);
		}

		protected override Sprite GetItemSprite()
		{
			if (icon == null || icon == SpriteManager.defaultSprite)
			{
				icon = SpriteManager.Get(templateTechType);
			}
			return icon;
		}

		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.MapRoomHUDChip, true);
				yield return task;

				prefab = GameObject.Instantiate(task.GetResult());
				prefab.EnsureComponent<DiverPerimeterDefenceBehaviour>();
				battery = prefab.EnsureComponent<Battery>(); // The battery is pretty much entirely so the UI will display charge value in the inventory
															 //EnergyMixin energyMixin = prefab.EnsureComponent<EnergyMixin>();

				TechType brokeChip = Main.GetModTechType("DiverPerimeterDefenceChip_Broken");
				task = CraftData.GetPrefabForTechTypeAsync(brokeChip);
				yield return task;
				brokenPrefab = task.GetResult();
				if (brokenPrefab == null)
				{
					Log.LogError($"Could not get prefab for TechType {brokeChip.AsString()}");
				}
			}

			GameObject go = GameObject.Instantiate(prefab);
			battery = go.EnsureComponent<Battery>();
			DiverPerimeterDefenceBehaviour behaviour = go.EnsureComponent<DiverPerimeterDefenceBehaviour>();
			behaviour.SetBattery(battery);
			behaviour.SetChipType(this.TechType);
			gameObject.Set(go);
		}
	}
}
