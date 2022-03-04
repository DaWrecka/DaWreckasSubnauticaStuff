using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

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
