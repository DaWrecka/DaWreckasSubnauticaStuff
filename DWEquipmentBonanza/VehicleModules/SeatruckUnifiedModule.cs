using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;
using DWEquipmentBonanza.Patches;
using DWEquipmentBonanza.MonoBehaviours;
using System;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    public class SeatruckUnifiedChargerModule : SeatruckChargerModule<VehicleUnifiedCharger>
    {
        protected override TechType template => TechType.SeaTruckUpgradeEnergyEfficiency;
        protected override float ChargerWeight => 2f;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("SeatruckSolarModuleMk2"), 1),
                        new Ingredient(Main.GetModTechType("SeatruckThermalModuleMk2"), 1),
                        new Ingredient(TechType.RadioTowerPPU, 1)
                    }
                )
            };
        }

        public SeatruckUnifiedChargerModule() : base("SeatruckUnifiedChargerModule", "Seatruck Unified Charger", "Recharges SeaTruck power cells in sunlight or hot areas, and has an internal backup battery. Limited stacking ability.")
        {
            OnFinishedPatching += () =>
            {
            };
        }
    }
#endif
}
