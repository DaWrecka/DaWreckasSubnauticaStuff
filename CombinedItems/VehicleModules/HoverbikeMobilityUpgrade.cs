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
    class HoverbikeMobilityUpgrade : HoverbikeUpgradeBase
    {
        private const float speedMultiplier = 1.3f;
        private const float cooldownMultiplier = 0.5f;
        private const float efficiencyModifier = 0.9f;
        private const int maxStack = 1;
        private const int upgradePriority = 2;

        private GameObject prefab;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.HoverbikeJumpModule, 1),
                        new Ingredient(Main.prefabHbEngineModule.TechType, 1),
                        new Ingredient(Main.prefabHbSpeedModule.TechType, 1),
                        new Ingredient(Main.prefabHbWaterTravelModule.TechType, 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1)
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
            return SpriteManager.Get(TechType.SeaTruckUpgradeHorsePower); // Placeholder
        }

        public HoverbikeMobilityUpgrade() : base("HoverbikeMobilityUpgrade", "Snowfox Mobility Upgrade", "Allows Snowfox to jump, travel on water, and provides a modest bonus to speed, without increasing power consumption. Does not stack with Speed Module or Efficiency Upgrade.")
        {
            OnFinishedPatching += () =>
            {
                HoverbikeUpdater.AddEfficiencyMultiplier(this.TechType, efficiencyModifier, upgradePriority, maxStack);
                HoverbikeUpdater.AddMovementModifier(this.TechType, speedMultiplier, cooldownMultiplier, upgradePriority, maxStack);
            };
        }
    }
}
