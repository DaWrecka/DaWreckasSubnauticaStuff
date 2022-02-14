using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using UnityEngine;
using DWEquipmentBonanza.MonoBehaviours;
#if SUBNAUTICA_STABLE
    using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
    using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza.VehicleModules
{
#if !BELOWZERO
    public abstract class SeamothChargerModule<Y> : VehicleChargerModule<Y> where Y : MonoBehaviour
    {
        public override EquipmentType EquipmentType => EquipmentType.SeamothModule;
        public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
        public override string[] StepsToFabricatorTab => new string[] { "SeamothModules" };
        protected override TechType template => TechType.SeamothSolarCharge;

        public SeamothChargerModule(string classID,
            string friendlyName,
            string description) : base(classID, friendlyName, description)
        {
            OnFinishedPatching += () =>
            {
                SeamothUpdater.AddChargerType(this.TechType, ChargerWeight);
            };
        }
    }

    public class SeamothSolarModuleMk2 : SeamothChargerModule<VehicleSolarChargerMk2>
    {
        protected override float ChargerWeight => 1.5f;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.SeamothSolarCharge, 1),
                        new Ingredient(TechType.Battery, 2),
                        new Ingredient(TechType.WiringKit, 1)
                    }
                )
            };
        }

        public SeamothSolarModuleMk2() : base("SeamothSolarModuleMk2",
            "Seamoth Solar Charger Mk2", "Recharges Seamoth power cell in sunlight, and has an internal backup battery. Limited stacking ability.")
        {
        }
    }

    internal class SeamothThermalModule : SeamothChargerModule<VehicleThermalChargerMk1>
    {
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.CrashPowder, 2),
                        new Ingredient(TechType.Polyaniline, 2),
                        new Ingredient(TechType.WiringKit, 1)
                    }
                )
            };
        }

        public SeamothThermalModule() : base("SeamothThermalModule", "Seamoth Thermal Charger", "Recharges Seamoth power cells in hot zones. Limited stacking ability.")
        {
        }
    }

    internal class SeamothThermalModuleMk2 : SeamothChargerModule<VehicleThermalChargerMk2>
    {
        protected override float ChargerWeight => 1.5f;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("SeamothThermalModule"), 1),
                        new Ingredient(TechType.Kyanite, 2),
                        new Ingredient(TechType.Battery, 2),
                        new Ingredient(TechType.AdvancedWiringKit, 1)
                    }
                )
            };
        }

        public SeamothThermalModuleMk2() : base("SeamothThermalModuleMk2", "Seamoth Thermal Charger Mk2", "Recharges Seamoth power cell in hot zones, and contains an internal backup battery. Limited stacking ability.")
        {
        }
    }

    public class SeamothUnifiedChargerModule : SeamothChargerModule<VehicleUnifiedCharger>
    {
        protected override float ChargerWeight => 2f;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("SeamothSolarModuleMk2"), 1),
                        new Ingredient(Main.GetModTechType("SeamothThermalModuleMk2"), 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1)
                    }
                )
            };
        }

        public SeamothUnifiedChargerModule() : base("SeamothUnifiedChargerModule",
             "Seamoth Unified Charger", "Recharges Seamoth power cell in sunlight or hot areas, and has an internal backup battery. Limited stacking ability.")
        {
        }
    }
#endif
                    }
