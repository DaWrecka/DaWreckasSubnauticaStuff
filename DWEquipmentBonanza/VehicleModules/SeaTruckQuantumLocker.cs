using DWEquipmentBonanza.MonoBehaviours;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    internal class SeaTruckQuantumLocker : SeaTruckUpgradeModule<SeaTruckQuantumLocker>
    {
        protected override TechType prefabTemplate => TechType.SeaTruckUpgradeHorsePower;
        public override TechType RequiredForUnlock => TechType.QuantumLocker;
        protected override TechType spriteTemplate => TechType.QuantumLocker;
        public override QuickSlotType QuickSlotType => QuickSlotType.Instant;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient(TechType.QuantumLocker, 1),
                    new Ingredient(TechType.PrecursorIonCrystal, 1),
                    new Ingredient(TechType.Titanium, 1)
                }
            };
        }

        protected override GameObject ModifyPrefab(GameObject prefab)
        {
            prefab = base.ModifyPrefab(prefab);
            prefab.EnsureComponent<VehicleQuantumLockerComponent>();
            return prefab;
        }

        public SeaTruckQuantumLocker() : base("SeaTruckQuantumLocker", "SeaTruck Quantum Locker", "Vehicle-equippable quantum locker")
        {
        }
    }
#endif
}
