using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace CombinedItems.VehicleModules
{
    internal class ExosuitSprintModule : Equipable
    {
        public override EquipmentType EquipmentType => EquipmentType.ExosuitModule;

        public override QuickSlotType QuickSlotType => QuickSlotType.Passive;

        public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;

        public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;

        public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;

        private GameObject prefab;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Titanium, 3),
                        new Ingredient(TechType.Nickel, 2),
                        new Ingredient(TechType.Kyanite, 1),
                        new Ingredient(TechType.Lubricant, 1),
                        new Ingredient(TechType.WiringKit, 1),
                        new Ingredient(TechType.HydraulicFluid, 1)
                    }
                )
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ExosuitThermalReactorModule);
                yield return task;
                prefab = GameObject.Instantiate<GameObject>(task.GetResult());
            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.ExosuitJetUpgradeModule);
        }

        public ExosuitSprintModule() : base("ExosuitSprintModule", "Exosuit Sprint Module", "A hydraulic system that allows the Exosuit's jump jets to angle for horizontal travel.")
        {
            OnFinishedPatching += () =>
            {
            };
        }
    }
}
