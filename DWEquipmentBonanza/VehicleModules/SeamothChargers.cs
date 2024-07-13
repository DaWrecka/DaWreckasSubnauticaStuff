using System.Collections.Generic;
using UnityEngine;
using DWEquipmentBonanza.MonoBehaviours;
using Main = DWEquipmentBonanza.DWEBPlugin;
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
#if SN1
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

#if NAUTILUS
        protected override TechType templateType => TechType.SeamothSolarCharge;
        protected override string templateClassId => string.Empty;

        public override void FinalisePrefab(CustomPrefab prefab)
        {
            base.FinalisePrefab(prefab);
            SeamothUpdater.AddChargerType(this.TechType, ChargerWeight);
        }
#endif

        public SeamothChargerModule(string classID,
            string friendlyName,
            string description) : base(classID, friendlyName, description)
        {
#if !NAUTILUS
            OnFinishedPatching += () =>
            {
                SeamothUpdater.AddChargerType(this.TechType, ChargerWeight);
            };
#endif
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
                        new Ingredient(TechType.PrecursorKey_Purple, 1)
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
