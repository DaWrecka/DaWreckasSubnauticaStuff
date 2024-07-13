using Main = DWEquipmentBonanza.DWEBPlugin;
using DWEquipmentBonanza.MonoBehaviours;
using System.Collections.Generic;
using UnityEngine;
using System;
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
#if SN1
    using RecipeData = SMLHelper.V2.Crafting.TechData;
#endif
#endif

namespace DWEquipmentBonanza.VehicleModules
{
    public abstract class ExosuitChargerModule<Y> : VehicleChargerModule<Y> where Y : MonoBehaviour
    {
        public override EquipmentType EquipmentType => EquipmentType.ExosuitModule;
        protected override TechType templateType => TechType.ExosuitThermalReactorModule;

#if NAUTILUS
        public override void FinalisePrefab(CustomPrefab prefab)
        {
            base.FinalisePrefab(prefab);
            ExosuitUpdater.AddChargerType(this.TechType, ChargerWeight);
        }
#else
        protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();
            ExosuitUpdater.AddChargerType(this.TechType, ChargerWeight);
        }
#endif

        public ExosuitChargerModule(string classID,
            string friendlyName,
            string description) : base(classID, friendlyName, description)
        {
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
#if SN1
                        new Ingredient(TechType.PrecursorKey_Purple, 1)
#elif BELOWZERO
                        new Ingredient(TechType.RadioTowerPPU, 1)
#endif
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
