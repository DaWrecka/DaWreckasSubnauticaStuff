using Common;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using UnityEngine;
using UWE;
using DWEquipmentBonanza.MonoBehaviours;
using DWEquipmentBonanza.Patches;
#if SUBNAUTICA_STABLE
    using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
    using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza.Equipables
{
    abstract public class SurvivalSuitBase<T> : Equipable
    {
        public SurvivalSuitBase(string classId,
                string friendlyName,
                string Description) : base(classId, friendlyName, Description)
        {
            OnFinishedPatching += () => OnFinishedPatch();
        }

        public override Vector2int SizeInInventory => new(2, 3);

        [Obsolete]
        protected virtual float SurvivalCapOverride { get; }
        protected abstract float maxDepth { get; }
        protected abstract float breathMultiplier { get; }
        protected abstract float minTempBonus { get; }
#if SUBNAUTICA_STABLE
        protected abstract float DeathRunDepth { get; }
#endif
        protected virtual TechType[] substitutions => new TechType[] { Main.StillSuitType };
        protected virtual TechType prefabTechType => Main.StillSuitType;
        protected virtual List<TechType> CompoundDependencies => new List<TechType>();
        protected static GameObject prefab;
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        protected virtual void OnFinishedPatch()
        {
            Main.AddModTechType(this.TechType);
            PlayerPatch.AddSurvivalSuit(this.TechType);
            //Main.AddSubstitution(this.TechType, Main.StillSuitType);
            foreach (TechType tt in substitutions)
            {
                Main.AddSubstitution(this.TechType, tt);
            }

            if (CompoundDependencies.Count > 0)
            {
                Reflection.AddCompoundTech(this.TechType, CompoundDependencies);
            }
            //SurvivalPatches.AddNeedsCapOverride(this.TechType, SurvivalCapOverride);
            Main.AddDiveSuit(this.TechType, maxDepth, breathMultiplier, minTempBonus);
        }

#if SUBNAUTICA_STABLE
        public override GameObject GetGameObject()
        {
            if (prefab == null)
            {
                prefab = PrepareGameObject(CraftData.GetPrefabForTechType(prefabTechType));
            }

            return prefab;
        }
#endif

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTechType, verbose: true);
                yield return task;

                prefab = PrepareGameObject(task.GetResult());
            }
            gameObject.Set(prefab);

            yield break;
        }

        protected virtual GameObject PrepareGameObject(GameObject prefab)
        {
            GameObject obj = GameObject.Instantiate<GameObject>(prefab);

            if (obj.TryGetComponent<Stillsuit>(out Stillsuit s))
                GameObject.DestroyImmediate(s);
            obj.EnsureComponent<SurvivalsuitBehaviour>();
            ModPrefabCache.AddPrefab(obj, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it;
                                                  // the prefab doesn't show up in the world [as with SetActive(false)] but because it's not been set inactive, it can instantiate active GameObjects immediately.

            return obj;
        }
    }

    public class SurvivalSuit : SurvivalSuitBase<SurvivalSuit>
    {
        public SurvivalSuit(string classId = "SurvivalSuit",
                string friendlyName = "Survival Suit",
                string Description = "Enhanced survival suit provides passive replenishment of calories and fluids, reducing the need for external sources of sustenance.") : base(classId, friendlyName, Description)
        {
        }

        protected override float maxDepth => 1300f;
        protected override float breathMultiplier => 0.90f;
        protected override float minTempBonus => 5f;
#if SUBNAUTICA_STABLE
        protected override float DeathRunDepth => 800f;
#endif
        protected override TechType[] substitutions => new TechType[] { Main.StillSuitType };
        public override EquipmentType EquipmentType => EquipmentType.Body;
        public override TechType RequiredForUnlock => Main.StillSuitType;
        public override Vector2int SizeInInventory => new(2, 2);

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.StillSuitType, 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1),
                        new Ingredient(TechType.JellyPlant, 1),
#if SUBNAUTICA_STABLE
                        new Ingredient(TechType.KooshChunk, 1)
#elif BELOWZERO
                        new Ingredient(TechType.KelpRootPustule, 1)
#endif
                    }
                ),
                LinkedItems = new List<TechType>()
                {
                    Main.GetModTechType("SurvivalSuit")
                }
            };
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(Main.StillSuitType);
        }
    }

    public class ReinforcedSurvivalSuit : SurvivalSuitBase<ReinforcedSurvivalSuit>
    {
        protected override float maxDepth => 1300f;
        protected override float breathMultiplier => 0.80f;
        protected override float minTempBonus => 35f;
#if SUBNAUTICA_STABLE
        protected override float DeathRunDepth => -1f;
#endif
        public ReinforcedSurvivalSuit(string classId = "ReinforcedSurvivalSuit",
                string friendlyName = "Reinforced Survival Suit",
                string Description = "Enhanced survival suit with reinforcing fibres provides passive primary needs reduction and protection from physical force and high temperatures") : base(classId, friendlyName, Description)
        {
        }

        protected override List<TechType> CompoundDependencies
        {
            get
            {
                return new List<TechType>()
                {
                    Main.StillSuitType,
                    TechType.ReinforcedDiveSuit
                };
            }
        }
        protected override TechType[] substitutions
        {
            get
            {
                return new TechType[] { Main.StillSuitType, TechType.ReinforcedDiveSuit };
            }
        }
        public override EquipmentType EquipmentType => EquipmentType.Body;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("SurvivalSuit"), 1),
                        new Ingredient(TechType.ReinforcedDiveSuit, 1),
                        new Ingredient(TechType.AramidFibers, 1)
                    }
                ),
                LinkedItems = new List<TechType>()
            };
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(Main.StillSuitType);
        }
    }


#if BELOWZERO
    public class SurvivalColdSuit : SurvivalSuitBase<SurvivalColdSuit>
    {
        public SurvivalColdSuit(string classId = "SurvivalColdSuit",
                string friendlyName = "Insulated Survival Suit",
                string Description = "Enhanced survival suit provides passive primary needs reduction and protection from extreme cold.") : base(classId, friendlyName, Description)
        {
        }

        protected override float maxDepth => 1300f;
        protected override float breathMultiplier => 0.80f;
        protected override float minTempBonus => 40f;
        protected override TechType[] substitutions
        {
            get
            {
                return new TechType[] { TechType.ColdSuit };
            }
        }

        public override EquipmentType EquipmentType => EquipmentType.Body;

        protected override List<TechType> CompoundDependencies
        {
            get
            {
                return new List<TechType>()
                {
                    Main.GetModTechType("SurvivalSuit"),
                    TechType.ColdSuit
                };
            }
        }

        protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();

            int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
            Reflection.AddColdResistance(this.TechType, System.Math.Max(55, coldResist));
            Reflection.SetItemSize(this.TechType, 2, 3);
            Log.LogDebug($"Finished patching {this.TechType.AsString()}, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("SurvivalSuit"), 1),
                        new Ingredient(TechType.ColdSuit, 1)
                    }
                ),
                LinkedItems = new List<TechType>()
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            Stillsuit s;
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuit, verbose: true);
                yield return task;

                prefab = task.GetResult();

                // Editing prefab
                if(prefab.TryGetComponent<Stillsuit>(out s))
                    GameObject.DestroyImmediate(s);
                prefab.EnsureComponent<SurvivalsuitBehaviour>();

                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                         // but it can still be instantiated. [unlike with SetActive(false)]
            }

            // Despite the component being removed from the prefab above, testing shows that the Survival Suits still add the water packs when they shouldn't.
            // So we're going to force-remove it here, to be safe.
            GameObject go = GameObject.Instantiate(prefab);
            if (go.TryGetComponent<Stillsuit>(out s))
                GameObject.DestroyImmediate(s);
            gameObject.Set(go);
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(Main.StillSuitType);
        }
    }
#endif
}
