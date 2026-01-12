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
#if BEPINEX
	using BepInEx;
	using BepInEx.Logging;
#elif QMM
using Logger = QModManager.Utility.Logger;
#endif
using DWEquipmentBonanza.MonoBehaviours;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
	internal class HoverbikeMobilityUpgrade : HoverbikeUpgradeBase<HoverbikeMobilityUpgrade>
	{
		private const float speedMultiplier = 1.3f;
		private const float cooldownMultiplier = 0.5f;
		private const float efficiencyModifier = 0.8f;
		private const int maxStack = 1;
		private const int upgradePriority = 2;

		public override float CraftingTime => 10f;
		protected override TechType spriteTemplate => TechType.SeaTruckUpgradeHorsePower; // Placeholder
		protected override TechType templateType => TechType.HoverbikeJumpModule;


		//private GameObject prefab;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.HoverbikeJumpModule, 1),
						new Ingredient(Main.GetModTechType("HoverbikeEngineEfficiencyModule"), 1),
						new Ingredient(Main.GetModTechType("HoverbikeSpeedModule"), 1),
						new Ingredient(Main.GetModTechType("HoverbikeWaterTravelModule"), 1),
						new Ingredient(TechType.AdvancedWiringKit, 1)
					}
				)
			};
		}

		protected override void OnFinishedPatch()
		{
			base.OnFinishedPatch();
			HoverbikeUpdater.AddEfficiencyMultiplier(this.TechType, efficiencyModifier, upgradePriority, maxStack);
			HoverbikeUpdater.AddMovementModifier(this.TechType, speedMultiplier, cooldownMultiplier, upgradePriority, maxStack);
			//Main.AddModTechType(this.TechType);
		}

		public HoverbikeMobilityUpgrade() : base("HoverbikeMobilityUpgrade", "Snowfox Mobility Upgrade", "Allows Snowfox to jump, travel on water, and provides a modest bonus to speed, without increasing power consumption. Does not stack with Speed Module or Efficiency Upgrade.")
		{
		}
	}
#endif
}
