using FuelCells.Patches;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Handlers;
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
#endif
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
            if (sprite == null)
            {
                sprite = SpriteManager.Get(TechType.CoralChunk, null);
            }

            return sprite;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.CoralChunk);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                         // but it can still be instantiated. [unlike with SetActive(false)]
            }

            // In BZ, the coral chunk has faulty collision. We fix it here.
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
            OnFinishedPatching += () =>
            {
                KnifePatches.AddHarvestable(TechType.TwistyBridgesCoralShelf, 200f);
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
                KnownTechHandler.SetAnalysisTechEntry(this.TechType, new TechType[] { TechType.Bleach });
                KnownTechHandler.SetAnalysisTechEntry(TechType.Bleach, new TechType[] { TechType.DisinfectedWater });
                LanguageHandler.Main.SetTechTypeTooltip(TechType.Bleach, "NaClO. Sodium hypochlorite bleach. Sanitizing applications.");
            };
        }
    }
#endif
            }
