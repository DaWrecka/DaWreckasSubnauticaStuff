using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;
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
        protected override TechType prefabTemplate => TechType.HoverbikeJumpModule;


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

        public HoverbikeMobilityUpgrade() : base("HoverbikeMobilityUpgrade", "Snowfox Mobility Upgrade", "Allows Snowfox to jump, travel on water, and provides a modest bonus to speed, without increasing power consumption. Does not stack with Speed Module or Efficiency Upgrade.")
        {
            OnFinishedPatching += () =>
            {
                HoverbikeUpdater.AddEfficiencyMultiplier(this.TechType, efficiencyModifier, upgradePriority, maxStack);
                HoverbikeUpdater.AddMovementModifier(this.TechType, speedMultiplier, cooldownMultiplier, upgradePriority, maxStack);
                Main.AddModTechType(this.TechType);
            };
        }
    }
#endif
}
