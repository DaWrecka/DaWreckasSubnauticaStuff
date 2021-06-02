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
    internal class HoverbikeSpeedModule : HoverbikeUpgradeBase<HoverbikeSpeedModule>
    {
        private const float speedMultiplier = 1.5f;
        private const float cooldownMultiplier = 0.5f;
        private const float powerConsumptionModifier = 2f;
        private const int maxStack = 1;

        //private GameObject prefab;
        protected override TechType spriteTemplate => TechType.SeaTruckUpgradeAfterburner; // Placeholder
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.ComputerChip, 1),
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

        public HoverbikeSpeedModule() : base("HoverbikeSpeedModule", "Snowfox Speed Module", "Increases Snowfox speed, but also significantly increases power consumption. Does not stack.")
        {
            OnFinishedPatching += () =>
            {
                HoverbikeUpdater.AddEfficiencyMultiplier(this.TechType, powerConsumptionModifier);
                HoverbikeUpdater.AddMovementModifier(this.TechType, speedMultiplier, cooldownMultiplier, maxStack);
                Main.AddModTechType(this.TechType);
            };
        }
    }
}
