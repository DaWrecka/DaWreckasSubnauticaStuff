using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using UnityEngine;
using DWEquipmentBonanza.MonoBehaviours;
using SMLHelper.V2.Utility;
using System.IO;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    public abstract class SeatruckChargerModule<Y> : VehicleChargerModule<Y> where Y : MonoBehaviour
    {
        public override EquipmentType EquipmentType => EquipmentType.SeaTruckModule;
        //public override string[] StepsToFabricatorTab => new string[] { "SeaTruckUpgrade" };

        public SeatruckChargerModule(string classID,
            string friendlyName,
            string description) : base(classID, friendlyName, description)
        {
            OnFinishedPatching += () =>
            {
                SeaTruckUpdater.AddChargerType(this.TechType, ChargerWeight);
                //SeaTruckUpgradesPatches.AddMaxModuleOverride(this.TechType, MaxSolarModules);
            };
        }
    }

    public class SeaTruckSolarModule : SeatruckChargerModule<VehicleSolarChargerMk1>
    {
        protected override TechType template => TechType.SeaTruckUpgradeEnergyEfficiency;
        
        
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

        public SeaTruckSolarModule(string classID = "SeatruckSolarModule",
            string friendlyName = "SeaTruck Solar Charger",
            string description = "Recharges SeaTruck power cells in sunlight. Limited stacking ability.") : base(classID, friendlyName, description)
        {
            OnFinishedPatching += () =>
            {
            };
        }
    }

    public class SeatruckSolarModuleMk2 : SeatruckChargerModule<VehicleSolarChargerMk2>
    {
        protected override TechType template => TechType.SeaTruckUpgradeEnergyEfficiency;
        protected override float ChargerWeight => 1.5f;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("SeatruckSolarModule"), 1),
                        new Ingredient(TechType.Battery, 2),
                        new Ingredient(TechType.WiringKit, 1)
                    }
                )
            };
        }

        public SeatruckSolarModuleMk2() : base("SeatruckSolarModuleMk2", "Seatruck Solar Charger Mk2", "Recharges SeaTruck power cells in sunlight, and has an internal backup battery. Limited stacking ability.")
        {
            OnFinishedPatching += () =>
            {
            };
        }
    }
#endif
}
