using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    class HoverbikeWaterTravelModule : HoverbikeUpgradeBase<HoverbikeWaterTravelModule>
    {
        //private GameObject prefab;
        protected override TechType spriteTemplate => TechType.CyclopsHullModule3; // Placeholder

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
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                         // but it can still be instantiated. [unlike with SetActive(false)]
            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }

        public HoverbikeWaterTravelModule() : base("HoverbikeWaterTravelModule", "Water Travel Module", "Increases the power of the Snowfox's hover pads, allowing travel over water in exchange for increased energy consumption.")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
            };
        }
    }
#endif
}
