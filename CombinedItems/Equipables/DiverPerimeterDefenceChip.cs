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
using CombinedItems.Patches;
using CombinedItems.MonoBehaviours;
using SMLHelper.V2.Handlers;

namespace CombinedItems.Equipables
{
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
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		public override Vector2int SizeInInventory => new Vector2int(1, 1);
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Equipment;
		public override TechType RequiredForUnlock => TechType.Polyaniline;

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

	abstract public class DiverPerimeterDefenceChipItemBase : Equipable
	{
		internal static Dictionary<TechType, int> MaxDischargeDict = new Dictionary<TechType, int>();

		internal static void AddChipData(TechType chip, int MaxDischarges)
		{
			MaxDischargeDict[chip] = MaxDischarges;
		}

		internal static int GetMaxDischarges(TechType chip)
		{
			if (MaxDischargeDict.TryGetValue(chip, out int value))
			{
				return value;
			}

			return 1;
		}

		public DiverPerimeterDefenceChipItemBase(string classId,
			string friendlyName,
			string description) : base(classId, friendlyName, description)
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				InventoryPatches.AddChip(this.TechType);
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

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
		//public override string[] StepsToFabricatorTab => new string[] { "Personal", "ChipEquipment", this.TechType.AsString(false) };
		public override float CraftingTime => 5f;
		public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
		public override EquipmentType EquipmentType => EquipmentType.Chip;
		protected virtual int MaxDischarges => 1;

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
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.MapRoomHUDChip, true);
				yield return task;

				prefab = GameObject.Instantiate(task.GetResult());
				prefab.EnsureComponent<DiverPerimeterDefenceBehaviour>();

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
			DiverPerimeterDefenceBehaviour behaviour = go.EnsureComponent<DiverPerimeterDefenceBehaviour>();
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
			Log.LogDebug($"DiverPerimeterDefenceChipItemBase.PostPatchSetup(): sprite loaded, now waiting for chip slots");
			while (Inventory.main == null && Inventory.main.equipment == null)
			{
				yield return new WaitForSeconds(0.5f);
			}
			Log.LogDebug($"DiverPerimeterDefenceChipItemBase.PostPatchSetup(): retrieving available chip slots");
			Inventory.main.equipment.GetSlots(EquipmentType.Chip, Main.chipSlots);
			Log.LogDebug($"DiverPerimeterDefenceChipItemBase.PostPatchSetup(): completed");
		}

	}

	public class DiverPerimeterDefenceChipItem : DiverPerimeterDefenceChipItemBase
	{
		public DiverPerimeterDefenceChipItem(string classId = "DiverPerimeterDefenceChipItem",
			string friendlyName = "Diver Perimeter Defence Chip",
			string description = "Protects a diver from hostile fauna using electrical discouragement. Discharge damages the chip beyond repair.") : base(classId, friendlyName, description)
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				InventoryPatches.AddChip(this.TechType);
				BatteryCharger.compatibleTech.Add(this.TechType);
				/*
				Log.LogDebug($"DiverPerimeterDefenceChipItem.OnFinishedPatching, attempting to add tab node {classId} to fabricator");
				CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, classId, friendlyName, GetItemSprite(), new string[]
				{
					"Personal",
					"ChipRecharge"
				});
				// Create the recipes for recharging this chip, based on the batteries that are available.
				// The goal here is to support Custom Batteries as well as vanilla.
				foreach (TechType tt in Main.compatibleBatteries)
				{
					string recipeClassID = classId + "Recharge" + tt.AsString();
					Log.LogDebug($"DiverPerimeterDefenceChipItem.OnFinishedPatching, setting up recharge recipe for {classId} with recipeClassID {recipeClassID} and battery {tt.AsString()}");
					string battName = Language.main.Get(tt);
					Sprite sprite = SpriteManager.Get(tt);

					TechType newTechType = TechTypeHandler.AddTechType(recipeClassID, $"{friendlyName} Recharge ({battName})", $"{friendlyName} recharged using {battName}");
					KnownTechHandler.SetAnalysisTechEntry(this.TechType, new TechType[] { newTechType });
					SpriteHandler.RegisterSprite(newTechType, sprite);
					var techData = new RecipeData()
					{
						craftAmount = 0,
						Ingredients = new List<Ingredient>()
						{
							new Ingredient(tt, 1),
							new Ingredient(this.TechType, 1)
						}
					};
					techData.LinkedItems.Add(this.TechType);
					CraftDataHandler.SetTechData(newTechType, techData);
					CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, newTechType, new string[]
					{
						"Personal",
						"ChipRecharge",
						classId,
						newTechType.AsString()
					});
					InventoryPatches.AddChipRecharge(newTechType);
				}
				*/
			};
		}

		public override EquipmentType EquipmentType => EquipmentType.Chip;
		public override string[] StepsToFabricatorTab => new string[] { "Personal", "ChipEquipment" };
		public override TechType RequiredForUnlock => TechType.Polyaniline;
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

		private void OnPatchDone()
		{
		}

	}
}
