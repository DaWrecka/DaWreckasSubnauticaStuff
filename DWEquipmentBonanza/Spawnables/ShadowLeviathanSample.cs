using Main = DWEquipmentBonanza.DWEBPlugin;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Handlers;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
using Ingredient = CraftData.Ingredient;
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

namespace DWEquipmentBonanza.Spawnables
{
#if BELOWZERO
    internal class ShadowLeviathanSample : Spawnable
    {
        protected static Sprite sprite;
        protected static GameObject prefab;

        protected override Sprite GetItemSprite()
        {
            if (sprite == null || sprite == SpriteManager.defaultSprite)
            {
                sprite = SpriteManager.Get(TechType.FrozenCreatureAntidote);
            }

            return sprite;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.FrozenCreatureAntidote);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                if(prefab.TryGetComponent<CompleteGoalOnExamine>(out CompleteGoalOnExamine cgoe))
                    GameObject.DestroyImmediate(cgoe);
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                         // but it can still be instantiated. [unlike with SetActive(false)]
            }

            var obj = GameObject.Instantiate(prefab);
            gameObject.Set(obj);
        }

        public ShadowLeviathanSample()
            : base("ShadowLeviathanSample", "Shadow Leviathan Sample", "A sample of chitin and ichor from a deadly predator.")
        {
            //Console.WriteLine($"{this.ClassID} constructing");
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
                CraftDataHandler.SetHarvestOutput(TechType.ShadowLeviathan, this.TechType);
                CraftDataHandler.SetHarvestType(TechType.ShadowLeviathan, HarvestType.DamageAlive);
            };
        }
    }
#endif
}
