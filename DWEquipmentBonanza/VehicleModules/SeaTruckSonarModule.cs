using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using System.Collections;
using System.Collections.Generic;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Nautilus.Utility;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#if SN1
	//using Ingredient = CraftData\.Ingredient;
#endif
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
#endif
using UnityEngine;
#if BEPINEX
	using BepInEx;
	using BepInEx.Logging;
#elif QMM
using Logger = QModManager.Utility.Logger;
#endif

using Common.Utility;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
	internal class SeaTruckSonarModule : SeaTruckUpgradeModule<SeaTruckSonarModule>
	{
		internal const float EnergyCost = 1f;

		public override EquipmentType EquipmentType => EquipmentType.SeaTruckModule;
		public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
		public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
		public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
		public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
		public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
		public override string[] StepsToFabricatorTab => new string[] { "SeaTruckUpgrade" };
		public override float CraftingTime => 10f;
		public override Vector2int SizeInInventory => new Vector2int(1, 1);
		protected override TechType templateType => TechType.SeaTruckUpgradeEnergyEfficiency;
		protected override TechType spriteTemplate => TechType.SeamothSonarModule;

#if NAUTILUS
#else
		protected override GameObject ModifyPrefab(GameObject prefab)
		{
			ModPrefabCache.AddPrefab(prefab, false);
			return prefab;
		}

		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			GameObject modPrefab;

			if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.SeaTruckUpgradeEnergyEfficiency);
				yield return task;
				modPrefab = GameObjectUtils.InstantiateInactive(task.GetResult());
				// The code is handled by the SeatruckUpdater component, rather than anything here.
				ModPrefabCache.AddPrefab(modPrefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
															// but it can still be instantiated. [unlike with SetActive(false)]
			}

			gameObject.Set(modPrefab);
		}
#endif

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.Magnetite, 2),
						new Ingredient(TechType.CopperWire, 1)
					}
				)
			};
		}

		protected override void OnFinishedPatch()
		{
			base.OnFinishedPatch();
			CraftDataHandler.SetEnergyCost(this.TechType, EnergyCost);
		}

		public SeaTruckSonarModule() : base("SeaTruckSonarModule", "SeaTruck Sonar Module", "A dedicated system for detecting and displaying topographical data on the HUD.")
		{
		}
	}
#endif
}
