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

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    internal class HoverbikeSolarChargerModule : HoverbikeUpgradeBase<HoverbikeSolarChargerModule>
    {
        //private GameObject prefab;
        protected override TechType spriteTemplate => TechType.SeamothSolarCharge;
        protected override TechType prefabTemplate => TechType.HoverbikeJumpModule;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.AdvancedWiringKit, 1),
                        new Ingredient(TechType.EnameledGlass, 1)
                    }
                )
            };
        }

        public HoverbikeSolarChargerModule() : base("HoverbikeSolarChargerModule", "Snowfox Solar Charger", "Recharges the Snowfox's battery while in sunlight. Does not stack.")
        {
        }
    }
#endif
}
