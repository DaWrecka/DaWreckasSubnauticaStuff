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
    class HoverbikeWaterTravelModule : HoverbikeUpgradeBase<HoverbikeWaterTravelModule>
    {
        //private GameObject prefab;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.PrecursorIonCrystal, 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1),
                        new Ingredient(TechType.Magnetite, 1),
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
            return SpriteManager.Get(TechType.ExoHullModule1); // Placeholder
        }

        public HoverbikeWaterTravelModule() : base("HoverbikeWaterTravelModule", "Water Travel Module", "Increases the power of the Snowfox's hover pads, allowing travel over water in exchange for increased energy consumption.")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
            };
        }
    }
}
