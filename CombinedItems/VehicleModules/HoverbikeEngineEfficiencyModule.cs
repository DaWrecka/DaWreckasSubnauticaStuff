using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;
using CombinedItems.MonoBehaviours;

namespace CombinedItems.VehicleModules
{
    class HoverbikeEngineEfficiencyModule : HoverbikeUpgradeBase
    {
        private const float efficiencyModifier = 0.65f;
        private const int maxUpgrades = 2;

        private GameObject prefab;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.AdvancedWiringKit, 1),
                        new Ingredient(TechType.Polyaniline, 1)
                    }
                )
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HoverbikeJumpModule);
                yield return task;
                prefab = GameObject.Instantiate<GameObject>(task.GetResult());
            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.SeaTruckUpgradeEnergyEfficiency); // Placeholder
        }

        public HoverbikeEngineEfficiencyModule() : base("HoverbikeEngineEfficiencyModule", "Snowfox Engine Efficiency Module", "Optimises Snowfox power use, reducing battery consumption by 35%. Stacks up to twice.")
        {
            OnFinishedPatching += () =>
            {
                HoverbikeUpdater.AddEfficiencyMultiplier(this.TechType, efficiencyModifier, maxUpgrades: maxUpgrades);
                Main.AddModTechType(this.TechType);
            };
        }
    }
}
