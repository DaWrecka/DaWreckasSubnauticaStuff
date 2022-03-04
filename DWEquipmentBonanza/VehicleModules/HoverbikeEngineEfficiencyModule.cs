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
    internal class HoverbikeEngineEfficiencyModule : HoverbikeUpgradeBase<HoverbikeEngineEfficiencyModule>
    {
        private const float efficiencyModifier = 0.65f;
        private const int maxUpgrades = 2;
        protected override TechType spriteTemplate => TechType.SeaTruckUpgradeEnergyEfficiency; // Placeholder
        protected override TechType prefabTemplate => TechType.HoverbikeJumpModule;

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

        public HoverbikeEngineEfficiencyModule() : base("HoverbikeEngineEfficiencyModule", "Snowfox Engine Efficiency Module", "Optimises Snowfox power use, reducing battery consumption by 35%. Stacks up to twice.")
        {
            OnFinishedPatching += () =>
            {
                HoverbikeUpdater.AddEfficiencyMultiplier(this.TechType, efficiencyModifier, maxUpgrades: maxUpgrades);
                Main.AddModTechType(this.TechType);
            };
        }
    }
#endif
}
