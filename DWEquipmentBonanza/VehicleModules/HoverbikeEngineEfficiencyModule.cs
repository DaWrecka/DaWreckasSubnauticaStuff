using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using System.Collections;
using System.Collections.Generic;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#if SN1
	//using Ingredient = CraftData\.Ingredient;
#endif
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
#endif
using UnityEngine;
using DWEquipmentBonanza.MonoBehaviours;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
	internal class HoverbikeEngineEfficiencyModule : HoverbikeUpgradeBase<HoverbikeEngineEfficiencyModule>
	{
		private const float efficiencyModifier = 0.65f;
		private const int maxUpgrades = 2;
		protected override TechType spriteTemplate => TechType.SeaTruckUpgradeEnergyEfficiency; // Placeholder
		protected override TechType templateType => TechType.HoverbikeJumpModule;

		//private GameObject prefab;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.AdvancedWiringKit, 1),
						new Ingredient(TechType.Polyaniline, 1)
					}
				)
			};
		}

		protected override void OnFinishedPatch()
		{
			base.OnFinishedPatch();
			HoverbikeUpdater.AddEfficiencyMultiplier(this.TechType, efficiencyModifier, maxUpgrades: maxUpgrades);
		}

		public HoverbikeEngineEfficiencyModule() : base("HoverbikeEngineEfficiencyModule", "Snowfox Engine Efficiency Module", "Optimises Snowfox power use, reducing battery consumption by 35%. Stacks up to twice.")
		{
		}
	}
#endif
}
