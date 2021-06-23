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
    internal class HoverbikeSolarChargerModule : HoverbikeUpgradeBase<HoverbikeSolarChargerModule>
    {
        //private GameObject prefab;
        protected override TechType spriteTemplate => TechType.SeamothSolarCharge;
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

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HoverbikeJumpModule);
                yield return task;
                prefab = GameObject.Instantiate<GameObject>(task.GetResult());
                prefab.SetActive(false);
            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }

        public HoverbikeSolarChargerModule() : base("HoverbikeSolarChargerModule", "Snowfox Solar Charger", "Recharges the Snowfox's battery while in sunlight. Does not stack.")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
            };
        }
    }
}
