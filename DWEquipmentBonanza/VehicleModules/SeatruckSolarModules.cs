using Main = DWEquipmentBonanza.DWEBPlugin;
using System.Collections.Generic;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Nautilus.Utility;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
using Ingredient = CraftData.Ingredient;
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
#endif
using UnityEngine;
using DWEquipmentBonanza.MonoBehaviours;
using System.IO;
using System;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    public abstract class SeatruckChargerModule<Y> : VehicleChargerModule<Y> where Y : MonoBehaviour
    {
        public override EquipmentType EquipmentType => EquipmentType.SeaTruckModule;
        //public override string[] StepsToFabricatorTab => new string[] { "SeaTruckUpgrade" };

        protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();
            SeaTruckUpdater.AddChargerType(this.TechType, ChargerWeight);
        }

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
            //Console.WriteLine($"{this.ClassID} constructing"); 
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
            //Console.WriteLine($"{this.ClassID} constructing");
        }
    }
#endif
}
