using DWEquipmentBonanza.MonoBehaviours;
using SMLHelper.V2.Crafting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    class HoverbikeQuantumLocker : HoverbikeUpgradeBase<HoverbikeQuantumLocker>
    {
        public override TechType RequiredForUnlock => TechType.QuantumLocker;
        protected override TechType spriteTemplate => TechType.QuantumLocker;
        public override QuickSlotType QuickSlotType => QuickSlotType.Instant;
        protected override TechType prefabTemplate => TechType.HoverbikeJumpModule;

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

        protected override GameObject ModifyPrefab(GameObject original)
        {
            prefab = base.ModifyPrefab(original);
            prefab.EnsureComponent<VehicleQuantumLockerComponent>();
            return prefab;
        }

        public HoverbikeQuantumLocker() : base("HoverbikeQuantumLocker", "Snowfox Quantum Locker", "Quantum Locker that can be fitted to the Snowfox. Activate with C.")
        {
        }
    }
#endif
}
