﻿using System;
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
using CombinedItems.Patches;
using CombinedItems.MonoBehaviours;
using SMLHelper.V2.Handlers;
using Newtonsoft;
using Newtonsoft.Json;

namespace CombinedItems.Equipables
{
	public class DiverPerimeterDefenceChip_Broken : PdaItem
	{
		private static bool bWaiting;
		protected static TechType templateTechType => TechType.MapRoomHUDChip;
		public static Sprite icon { get; private set; }
		public static GameObject prefab { get; private set; }

		public DiverPerimeterDefenceChip_Broken() : base("DiverPerimeterDefenceChip_Broken", "Diver Perimeter System (damaged)", $"Protects a diver from hostile fauna using electrical discouragement.\n\nChip has been discharged and is non-functional.")
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		public override Vector2int SizeInInventory => new Vector2int(1, 1);
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Equipment;
		public override TechType RequiredForUnlock => TechType.Unobtanium;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData();
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
				var task = CraftData.GetPrefabForTechTypeAsync(TechType.MapRoomHUDChip);
				yield return task;

				prefab = task.GetResult();
			}

			gameObject.Set(GameObject.Instantiate(prefab));
		}
	}

	abstract public class DiverPerimeterDefenceChipItemBase<T> : Equipable
	{
		internal static Dictionary<TechType, int> MaxDischargeDict = new Dictionary<TechType, int>();
		public static Sprite icon { get; protected set; }
		public static GameObject prefab { get; protected set; }
		public static GameObject brokenPrefab { get; protected set; }

		private bool bWaiting;

		public virtual TechType templateTechType =>
#if SN1
			TechType.SeamothElectricalDefense;
#elif BZ
			TechType.SeaTruckUpgradePerimeterDefense;
#endif
		public override Vector2int SizeInInventory => new Vector2int(1, 1);
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Equipment;
		public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
		public override string[] StepsToFabricatorTab => new string[] { "Personal", "ChipEquipment" };
		public override float CraftingTime => 5f;
		public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
		public override EquipmentType EquipmentType => EquipmentType.Chip;
		protected virtual int MaxDischarges => 1;
		protected virtual bool bDestroyedOnDischarge => false;
		protected virtual List<TechType> RequiredTech => new List<TechType>();

		internal static void AddChipData(TechType chip, int MaxDischarges)
		{
			MaxDischargeDict[chip] = MaxDischarges;
		}

		/*internal static int GetMaxDischarges(TechType chip)
		{
			if (MaxDischargeDict.TryGetValue(chip, out int value))
			{
				return value;
			}

			return 1;
		}*/

		public DiverPerimeterDefenceChipItemBase(string classId,
			string friendlyName,
			string description) : base(classId, friendlyName, description)
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				InventoryPatches.AddChip(this.TechType, !this.bDestroyedOnDischarge);
				DiverPerimeterDefenceBehaviour.AddChipData(this.TechType, this.MaxDischarges, this.bDestroyedOnDischarge);
				if (RequiredTech.Count > 0)
				{
					Log.LogDebug($"{this.TechType.AsString()}.OnFinishedPatching(): Setting up CompoundTech with RequiredTech of:" + JsonConvert.SerializeObject(RequiredTech, Formatting.Indented));

					KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
					compound.techType = this.TechType;
					compound.dependencies = RequiredTech;
					Reflection.AddCompoundTech(compound);
				}
				CoroutineHost.StartCoroutine(this.PostPatchSetup());
			};
		}

		/*protected override RecipeData GetBlueprintRecipe()
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
		}*/

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
			DiverPerimeterDefenceBehaviour behaviour;
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.MapRoomHUDChip, verbose: true);
				yield return task;

				prefab = GameObject.Instantiate(task.GetResult());
				behaviour = prefab.EnsureComponent<DiverPerimeterDefenceBehaviour>();
				//behaviour.Initialise(this.MaxDischarges, this.bDestroyedOnDischarge);
				prefab.SetActive(false);
			}

			GameObject go = GameObject.Instantiate(prefab);
			behaviour = go.EnsureComponent<DiverPerimeterDefenceBehaviour>();
			behaviour.Initialise(this.TechType);
			//behaviour.Initialise(this.MaxDischarges, this.bDestroyedOnDischarge);
			//behaviour.SetBattery(battery);
			//behaviour.SetChipType(this.TechType);
			gameObject.Set(go);
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
			Log.LogDebug($"{this.TechType.AsString()}.PostPatchSetup(): sprite loaded, now waiting for chip slots");
			while (Inventory.main == null)
			{
				yield return new WaitForSecondsRealtime(0.5f);
			}
			while (Inventory.main.equipment == null)
			{
				yield return new WaitForSecondsRealtime(0.5f);
			}
			Log.LogDebug($"{this.TechType.AsString()}.PostPatchSetup(): retrieving available chip slots");
			Inventory.main.equipment.GetSlots(EquipmentType.Chip, Main.chipSlots);
			Log.LogDebug($"{this.TechType.AsString()}.PostPatchSetup(): completed");
		}

	}

	public class DiverPerimeterDefenceChipItem : DiverPerimeterDefenceChipItemBase<DiverPerimeterDefenceChipItem>
	{
		public DiverPerimeterDefenceChipItem(string classId = "DiverPerimeterDefenceChipItem",
			string friendlyName = "Diver Perimeter Defence System",
			string description = "Protects a diver from hostile fauna using electrical discouragement. Discharge damages the chip beyond repair.") : base(classId, friendlyName, description)
		{
			OnFinishedPatching += () =>
			{
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		public override EquipmentType EquipmentType => EquipmentType.Chip;
		public override string[] StepsToFabricatorTab => new string[] { "Personal", "ChipEquipment" };
		public override TechType RequiredForUnlock => TechType.Polyaniline;
		protected override bool bDestroyedOnDischarge => true;
		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.Battery, 1),
						new Ingredient(TechType.WiringKit, 1),
						new Ingredient(TechType.CopperWire, 1),
						new Ingredient(TechType.Polyaniline, 1)
					}
				)
			};
		}
	}

	public class DiverDefenceSystemMk2 : DiverPerimeterDefenceChipItemBase<DiverDefenceSystemMk2>
	{
		public DiverDefenceSystemMk2(string classId = "DiverDefenceSystemMk2",
			string friendlyName = "Diver Defence System Mk2",
			string description = "Protects a diver from hostile fauna using electrical discouragement. Can be recharged multiple times.") : base(classId, friendlyName, description)
		{
			OnFinishedPatching += () =>
			{
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		/*protected override List<TechType> RequiredTech => new List<TechType>()
		{
			TechType.RadioTowerPPU,
			Main.GetModTechType("DiverPerimeterDefenceChipItem")
		};*/


		protected override IEnumerator PostPatchSetup()
		{
			yield return base.PostPatchSetup();

			BatteryCharger.compatibleTech.Add(this.TechType);
			InventoryPatches.AddChipRecharge(this.TechType);
			yield break;
		}

		public override TechType RequiredForUnlock => Main.GetModTechType("DiverPerimeterDefenceChipItem");

		protected override int MaxDischarges => 1;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(Main.GetModTechType("DiverPerimeterDefenceChipItem"), 1),
						new Ingredient(TechType.RadioTowerPPU, 1),
						new Ingredient(TechType.Battery, 1)
					}
				)
			};
		}
	}

	public class DiverDefenceMk2_FromBrokenChip : Craftable
	{
		public DiverDefenceMk2_FromBrokenChip() : base("DiverDefenceMk2_FromBrokenChip", "Diver Defence System Mk2", "Protects a diver from hostile fauna using electrical discouragement. Can be recharged multiple times.")
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
			};
		}

        public override TechType RequiredForUnlock => Main.GetModTechType("DiverPerimeterDefenceChip_Broken");

        protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 0,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(Main.GetModTechType("DiverPerimeterDefenceChip_Broken"), 1),
						new Ingredient(TechType.RadioTowerPPU, 1),
						new Ingredient(TechType.Nickel, 1),
						new Ingredient(TechType.Battery, 1)
					}
				),
				LinkedItems = new List<TechType>()
				{
					Main.GetModTechType("DiverDefenceSystemMk2")
				}
			};
		}
	}

	public class DiverDefenceSystemMk3 : DiverPerimeterDefenceChipItemBase<DiverDefenceSystemMk3>
	{
		public DiverDefenceSystemMk3() : base("DiverDefenceSystemMk3", "Diver Defence System Mk3", "Protects a diver from hostile fauna using electrical discouragement. Can discharge multiple times per charge, and can be recharged multiple times.")
		{
		}

		protected override IEnumerator PostPatchSetup()
		{
			yield return base.PostPatchSetup();

			BatteryCharger.compatibleTech.Add(this.TechType);
			InventoryPatches.AddChipRecharge(this.TechType);
			yield break;
		}

		/*protected override List<TechType> RequiredTech => new List<TechType>()
		{
			TechType.PrecursorIonBattery,
			Main.GetModTechType("DiverDefenceSystemMk2")
		};*/


		public override TechType RequiredForUnlock => Main.GetModTechType("DiverDefenceSystemMk2");

		protected override int MaxDischarges => 5;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(Main.GetModTechType("DiverDefenceSystemMk2"), 1),
						new Ingredient(Main.GetModTechType("ShadowLeviathanSample"), 1),
						new Ingredient(TechType.Nickel, 1),
						new Ingredient(TechType.PrecursorIonBattery, 1)
					}
				)
			};
		}
	}
}
