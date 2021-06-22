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

namespace CombinedItems.Spawnables
{
    class ShadowLeviathanSample : Spawnable
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

                prefab = task.GetResult();
                GameObject.DestroyImmediate(prefab.GetComponent<CompleteGoalOnExamine>());
            }

            var obj = GameObject.Instantiate(prefab);
            gameObject.Set(obj);
        }

        public ShadowLeviathanSample()
            : base("ShadowLeviathanSample", "Shadow Leviathan Sample", "A sample of chitin and ichor from a deadly predator.")
        {
            /*sprite = Main.assetBundle.LoadAsset<Sprite>("BioPlasmaMK2");*/
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
                CraftDataHandler.SetHarvestOutput(TechType.ShadowLeviathan, this.TechType);
                CraftDataHandler.SetHarvestType(TechType.ShadowLeviathan, HarvestType.DamageAlive);
            };
        }
    }
}
