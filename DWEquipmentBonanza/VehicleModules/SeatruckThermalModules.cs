using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using DWEquipmentBonanza.MonoBehaviours;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    internal class SeatruckThermalModule : SeatruckChargerModule<VehicleThermalChargerMk1>
    {
        protected override TechType template => TechType.SeaTruckUpgradeEnergyEfficiency;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Sulphur, 2),
                        new Ingredient(TechType.Polyaniline, 2),
                        new Ingredient(TechType.WiringKit, 1)
                    }
                )
            };
        }

        public SeatruckThermalModule() : base("SeatruckThermalModule", "SeaTruck Thermal Charger", "Recharges SeaTruck power cells in hot zones. Limited stacking ability.")
        {
            OnFinishedPatching += () =>
            {
            };
        }
    }

    internal class SeatruckThermalModuleMk2 : SeatruckChargerModule<VehicleThermalChargerMk2>
    {
        protected override float ChargerWeight => 1.5f;
        protected override TechType template => TechType.SeaTruckUpgradeEnergyEfficiency;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("SeatruckThermalModule"), 1),
                        new Ingredient(TechType.Kyanite, 2),
                        new Ingredient(TechType.Battery, 2),
                        new Ingredient(TechType.AdvancedWiringKit, 1)
                    }
                )
            };
        }

        public SeatruckThermalModuleMk2() : base("SeatruckThermalModuleMk2", "SeaTruck Thermal Charger Mk2", "Recharges SeaTruck power cells in hot zones, and contains an internal backup battery. Limited stacking ability.")
        {
            OnFinishedPatching += () =>
            {
            };
        }
    }
#endif
}
