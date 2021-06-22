using FuelCells.Patches;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using Story;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FuelCells.Spawnables
{
#if BELOWZERO
    class CoralSample : Spawnable
    {
        protected static Sprite sprite;
        protected static GameObject prefab;

        protected override Sprite GetItemSprite()
        {
            if (sprite == null || sprite == SpriteManager.defaultSprite)
            {
                sprite = SpriteManager.Get(TechType.CoralChunk);
            }

            return sprite;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.CoralChunk);
                yield return task;

                prefab = task.GetResult();
            }

            var obj = GameObject.Instantiate(prefab);
            if(obj.TryGetComponent<LargeWorldEntity>(out LargeWorldEntity largeWorldEntity))
            {
                obj.EnsureComponent<WorldForces>().useRigidbody = largeWorldEntity.GetComponent<Rigidbody>();
            }
            gameObject.Set(obj);
        }

        public CoralSample()
            : base("CoralSample", "Coral Shelf Sample", "A sample of coral from the Twisty Bridges.")
        {
            /*sprite = Main.assetBundle.LoadAsset<Sprite>("BioPlasmaMK2");*/
            OnFinishedPatching += () =>
            {
                KnifePatches.AddHarvestable(TechType.TwistyBridgesCoralShelf, 100f);
                Main.AddModTechType(this.TechType);
                CraftDataHandler.SetHarvestOutput(TechType.TwistyBridgesCoralShelf, this.TechType);
                CraftDataHandler.SetHarvestType(TechType.TwistyBridgesCoralShelf, HarvestType.DamageAlive);
                CraftDataHandler.SetTechData(TechType.Bleach, new SMLHelper.V2.Crafting.RecipeData()
                {
                    craftAmount = 1,
                    Ingredients = new List<Ingredient>()
                {
                    new Ingredient(this.TechType, 1),
                    new Ingredient(TechType.Salt, 1)
                }
                });
                CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.Bleach, new string[] { "Resources", "BasicMaterials" });

                CraftDataHandler.SetTechData(TechType.DisinfectedWater, new SMLHelper.V2.Crafting.RecipeData()
                {
                    craftAmount = 2,
                    Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.Bleach, 1)
                }
                });
                CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.DisinfectedWater, new string[] { "Survival", "Water" });
                KnownTechHandler.SetAnalysisTechEntry(TechType.None, new TechType[] { TechType.Bleach });
                KnownTechHandler.SetAnalysisTechEntry(TechType.Bleach, new TechType[] { TechType.DisinfectedWater });
            };
        }
    }
#endif
}
