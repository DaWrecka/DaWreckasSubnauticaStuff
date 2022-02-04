using DWEquipmentBonanza.MonoBehaviours;
using SMLHelper.V2.Crafting;
using System.Collections.Generic;
using UnityEngine;
#if SUBNAUTICA_STABLE
    using RecipeData = SMLHelper.V2.Crafting.TechData;
#endif

namespace DWEquipmentBonanza.VehicleModules
{
    public abstract class ExosuitChargerModule<Y> : VehicleChargerModule<Y> where Y : MonoBehaviour
    {
        public override EquipmentType EquipmentType => EquipmentType.ExosuitModule;
        public override string[] StepsToFabricatorTab => new string[] { "ExosuitModules" };
        protected override TechType template => TechType.ExosuitThermalReactorModule;

        public ExosuitChargerModule(string classID,
            string friendlyName,
            string description) : base(classID, friendlyName, description)
        {
            OnFinishedPatching += () =>
            {
                ExosuitUpdater.AddChargerType(this.TechType, ChargerWeight);
#if !BELOWZERO
                SeamothUpdater.AddChargerType(this.TechType, ChargerWeight);
#endif
            };
        }
    }

    public class ExosuitSolarModule : ExosuitChargerModule<VehicleSolarChargerMk1>
    {
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

        public ExosuitSolarModule(string classID = "ExosuitSolarModule",
            string friendlyName = "Exosuit Solar Charger",
            string description = "Recharges Exosuit power cells in sunlight. Limited stacking ability.")
            : base(classID, friendlyName, description)
        {
        }
    }

    public class ExosuitSolarModuleMk2 : ExosuitChargerModule<VehicleSolarChargerMk2>
    {
        protected override float ChargerWeight => 1.5f;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("ExosuitSolarModule"), 1),
                        new Ingredient(TechType.Battery, 2),
                        new Ingredient(TechType.WiringKit, 1)
                    }
                )
            };
        }

        public ExosuitSolarModuleMk2() : base("ExosuitSolarModuleMk2",
            "Exosuit Solar Charger Mk2", "Recharges Exosuit power cells in sunlight, and has an internal backup battery. Limited stacking ability.")
        {
        }
    }

    internal class ExosuitThermalModuleMk2 : ExosuitChargerModule<VehicleThermalChargerMk2>
    {
        protected override float ChargerWeight => 1.5f;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.ExosuitThermalReactorModule, 1),
                        new Ingredient(TechType.Nickel, 1),
                        new Ingredient(TechType.Battery, 2),
                        new Ingredient(TechType.AdvancedWiringKit, 1)
                    }
                )
            };
        }

        public ExosuitThermalModuleMk2() : base("ExosuitThermalModuleMk2",
            "Exosuit Thermal Charger Mk2", "Recharges Exosuit power cells in hot zones, and contains an internal backup battery. Limited stacking ability.")
        {
        }
    }

    public class ExosuitUnifiedChargerModule : ExosuitChargerModule<VehicleUnifiedCharger>
    {
        protected override float ChargerWeight => 2f;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("ExosuitSolarModuleMk2"), 1),
                        new Ingredient(Main.GetModTechType("ExosuitThermalModuleMk2"), 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1)
                    }
                )
            };
        }

        public ExosuitUnifiedChargerModule() : base("ExosuitUnifiedChargerModule",
             "Exosuit Unified Charger", "Recharges Exosuit power cells in sunlight or hot areas, and has an internal backup battery. Limited stacking ability.")
        {
        }
    }

}
