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
using Ingredient = CraftData.Ingredient;
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
    internal class HoverbikeSpeedModule : HoverbikeUpgradeBase<HoverbikeSpeedModule>
    {
        private const float speedMultiplier = 1.5f;
        private const float cooldownMultiplier = 0.5f;
        private const float powerConsumptionModifier = 2f;
        private const int maxStack = 1;

        //private GameObject prefab;
        protected override TechType spriteTemplate => TechType.SeaTruckUpgradeAfterburner; // Placeholder
        protected override TechType prefabTemplate => TechType.HoverbikeJumpModule;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.ComputerChip, 1),
                        new Ingredient(TechType.Polyaniline, 1)
                    }
                )
            };
        }
        protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();
            HoverbikeUpdater.AddEfficiencyMultiplier(this.TechType, powerConsumptionModifier);
            HoverbikeUpdater.AddMovementModifier(this.TechType, speedMultiplier, cooldownMultiplier, maxStack);
        }

        public HoverbikeSpeedModule() : base("HoverbikeSpeedModule", "Snowfox Speed Module", "Increases Snowfox speed, but also significantly increases power consumption. Does not stack.")
        {
        }
    }
#endif
}
