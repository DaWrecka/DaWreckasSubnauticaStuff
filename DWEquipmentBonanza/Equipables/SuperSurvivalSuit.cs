﻿using DWEquipmentBonanza.MonoBehaviours;
using DWEquipmentBonanza.Patches;
using Common;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;
using UWE;
#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
    using Object = UnityEngine.Object;
#elif BELOWZERO
#endif

namespace DWEquipmentBonanza.Equipables
{
    internal class SuperSurvivalSuit : SurvivalSuitBase<SuperSurvivalSuit>
    {
        public SuperSurvivalSuit() : base(classId: "SuperSurvivalSuit",
            friendlyName: "Ultimate Survival Suit",
#if SUBNAUTICA_STABLE
            Description: "The ultimate in survival gear. Provides protection from extreme temperatures, corrosive substances and physical harm, and reduces the need for external sustenance."
#elif BELOWZERO
            Description: "The ultimate in survival gear. Provides protection from extreme temperatures and physical harm, and reduces the need for external sustenance."
#endif
            )
        {
            Log.LogDebug($"SuperSurvivalSuit(): constructor begin");
            OnFinishedPatching += () =>
            {
                Log.LogDebug($"SuperSurvivalSuit(): OnFinishedPatching begin");
                Reflection.AddCompoundTech(this.TechType, new List<TechType>()
                {
#if SUBNAUTICA_STABLE
                    TechType.RadiationSuit,
#elif BELOWZERO
                    TechType.ColdSuit,
#endif
                    Main.StillSuitType,
                    TechType.ReinforcedDiveSuit
                });

#if SUBNAUTICA_STABLE
                Main.AddSubstitution(this.TechType, TechType.RadiationSuit);
                Main.AddDiveSuit(this.TechType, 8000f, 0.50f, 40f);
                /*Main.DamageResistances[this.TechType] = new List<Main.DamageInfo>()
                {
                    {
                        new Main.DamageInfo(DamageType.Acid, -0.6f)
                    }
                };*/
                Main.AddDamageResist(this.TechType, DamageType.Acid, 0.6f);
                Log.LogDebug($"Finished patching {this.TechType.AsString()}");
#elif BELOWZERO
                int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
                Reflection.AddColdResistance(this.TechType, System.Math.Max(55, coldResist));
                Reflection.SetItemSize(this.TechType, 2, 3);
                Log.LogDebug($"Finished patching {this.TechType.AsString()}, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
#endif

                // the SurvivalSuit constructor will call AddModTechType already.
                // It has also been set up to add substitutions based on the value of the 'substitutions' property below,
                // as well as set up CompoundTech based on the value of CompoundDependencies
                Log.LogDebug($"SuperSurvivalSuit(): OnFinishedPatching end");
            };
            Log.LogDebug($"SuperSurvivalSuit(): constructor end");
        }

        public override bool UnlockedAtStart => false;
        public override EquipmentType EquipmentType => EquipmentType.Body;
        [Obsolete]
        protected override float SurvivalCapOverride => 200f;
        protected override float maxDepth => 8000f;
        protected override float breathMultiplier => 0.50f;
        protected override float minTempBonus => 40f;
#if SUBNAUTICA_STABLE
        protected override float DeathRunDepth => -1f;
#elif BELOWZERO
        protected override TechType prefabTechType => TechType.ColdSuit;
#endif

        protected override TechType[] substitutions => new TechType[] {
#if BELOWZERO
                    TechType.ColdSuit,
#endif
                    TechType.ReinforcedDiveSuit
                };

        protected override List<TechType> CompoundDependencies => new List<TechType>()
                {
                    TechType.ReinforcedDiveSuit,
                    Main.StillSuitType,
#if SUBNAUTICA_STABLE
                    TechType.RadiationSuit
#elif BELOWZERO
                    TechType.ColdSuit,
#endif
                };

        protected override RecipeData GetBlueprintRecipe()
        {
#if SUBNAUTICA_STABLE
            if (Main.HasNitrogenMod())
            {
                return new RecipeData()
                {
                    craftAmount = 1,
                    Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("NitrogenBrineSuit3"), 1),
                        new Ingredient(Main.GetModTechType("SurvivalSuit"), 1),
                    })
                };
            }
#endif


            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(Main.GetModTechType("SurvivalSuit"), 1),
#if SUBNAUTICA_STABLE
                    new Ingredient(Main.GetModTechType("AcidSuit"), 1),
#elif BELOWZERO
                    new Ingredient(TechType.ReinforcedDiveSuit, 1),
                    new Ingredient(TechType.ReinforcedGloves, 1),
                    new Ingredient(TechType.ColdSuit, 1),
                    new Ingredient(TechType.ColdSuitGloves, 1),
#endif
                }),
#if BELOWZERO
                LinkedItems = new List<TechType>()
                {
                    Main.GetModTechType("ReinforcedColdGloves")
                }
#endif
            };
        }

        protected override Sprite GetItemSprite()
        {
#if SUBNAUTICA_STABLE
            return SpriteManager.Get(Main.StillSuitType);
#elif BELOWZERO
            return SpriteManager.Get(TechType.ColdSuit);
#endif
        }

        /*public override GameObject GetGameObject()
        {
            Log.LogDebug($"SuperSurvivalSuit.GetGameObject(): begin");
            if (prefab == null)
            {
                Log.LogDebug($"SuperSurvivalSuit.GetGameObject(): caching new prefab");
                prefab = InstantiateAndModifyGameObject(CraftData.GetPrefabForTechType(Main.StillSuitType));
            }
            else
            {
                Log.LogDebug($"SuperSurvivalSuit.GetGameObject(): using existing prefab");
            }

            Log.LogDebug($"SuperSurvivalSuit.GetGameObject(): done");
            return prefab;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
            Stillsuit s;
            if (prefab == null)
            {
                Log.LogDebug("SuperSurvivalSuit.GetGameObjectAsync(): Caching new prefab");
#if SUBNAUTICA_STABLE
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(Main.StillSuitType, verbose: true);
#elif BELOWZERO
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuit, verbose: true);
#endif
                yield return task;

                prefab = InstantiateAndModifyGameObject(task.GetResult());
            }
            else
            {
                Log.LogDebug("SuperSurvivalSuit.GetGameObjectAsync(): Using existing prefab");
            }

            GameObject go = GameObject.Instantiate(prefab);
            if(go.TryGetComponent<Stillsuit>(out s))
                GameObject.DestroyImmediate(s);
            gameObject.Set(go);
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
        }

        private GameObject InstantiateAndModifyGameObject(GameObject thisPrefab)
        {
            MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
            GameObject obj = GameObject.Instantiate(thisPrefab);
            Log.LogDebug($"SuperSurvivalSuit.InstantiateAndModifyGameObject(): prefab instantiated, modifying");
            // Editing prefab
#if SUBNAUTICA_STABLE
            Stillsuit s = obj.GetComponent<Stillsuit>();
            if(s != null)
#elif BELOWZERO
            if (obj.TryGetComponent<Stillsuit>(out Stillsuit s))
#endif
                GameObject.DestroyImmediate(s);
            obj.EnsureComponent<SurvivalsuitBehaviour>();
            Log.LogDebug($"SuperSurvivalSuit.InstantiateAndModifyGameObject(): prefab modified, invoking ModPrefabCache.AddPrefab()");
            ModPrefabCache.AddPrefab(obj); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.

            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
            return obj;
        }*/
    }

    internal abstract class SurvivalSuitBlueprint : Craftable
    {
        public SurvivalSuitBlueprint(string classId) : base(classId,
                    "Ultimate Survival Suit",
                    "The ultimate in survival gear. Provides protection from extreme temperatures and physical harm, and reduces the need for external sustenance.")
        {
            OnFinishedPatching += () =>
            {
            };
        }
        public override TechType RequiredForUnlock => Main.GetModTechType("SuperSurvivalSuit");
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(Main.StillSuitType);
        }
    }

    internal class SurvivalSuitBlueprint_FromReinforcedSurvival : SurvivalSuitBlueprint
    {
        public SurvivalSuitBlueprint_FromReinforcedSurvival() : base("SurvivalSuitBlueprint_FromReinforcedSurvival")
        {
            OnFinishedPatching += () =>
            {
                Reflection.AddCompoundTech(this.TechType, new List<TechType>()
                {
                    Main.StillSuitType,
#if SUBNAUTICA_STABLE
                    TechType.RadiationSuit,
#elif BELOWZERO
                    TechType.ColdSuit,
#endif
                    TechType.ReinforcedDiveSuit
                });
            };
        }

        public override bool UnlockedAtStart => false;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("ReinforcedSurvivalSuit"), 1),
                        new Ingredient(TechType.ReinforcedGloves, 1),
#if SUBNAUTICA_STABLE
                        new Ingredient(TechType.HydrochloricAcid, 1),
                        new Ingredient(TechType.CreepvinePiece, 2),
                        new Ingredient(TechType.Aerogel, 1),
                        new Ingredient(TechType.RadiationGloves, 1),
                        new Ingredient(TechType.RadiationHelmet, 1),
                        new Ingredient(TechType.RadiationSuit, 1),
#elif BELOWZERO
                        new Ingredient(TechType.ColdSuit, 1),
                        new Ingredient(TechType.ColdSuitGloves, 1),
#endif
                    }
                ),
                LinkedItems = new List<TechType>()
                {
                    Main.GetModTechType("SuperSurvivalSuit"),
#if SUBNAUTICA_STABLE
                    Main.GetModTechType("AcidGloves"),
#elif BELOWZERO
                    Main.GetModTechType("ReinforcedColdGloves")
#endif
                }
            };
        }
    }

#if BELOWZERO
        internal class SurvivalSuitBlueprint_FromReinforcedCold : SurvivalSuitBlueprint
    {
        public SurvivalSuitBlueprint_FromReinforcedCold() : base("SurvivalSuitBlueprint_FromReinforcedCold")
        {
            OnFinishedPatching += () =>
            {
                Reflection.AddCompoundTech(this.TechType, new List<TechType>()
                {
                    Main.StillSuitType,
                    TechType.ColdSuit,
                    TechType.ReinforcedDiveSuit
                });
            };
        }

		public override bool UnlockedAtStart => false;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("ReinforcedColdSuit"), 1),
                        new Ingredient(Main.GetModTechType("SurvivalSuit"), 1)
                    }
                ),
                LinkedItems = new List<TechType>()
                {
                    Main.GetModTechType("SuperSurvivalSuit")
                }
            };
        }
    }

    internal class SurvivalSuitBlueprint_FromSurvivalCold : SurvivalSuitBlueprint
    {
        public SurvivalSuitBlueprint_FromSurvivalCold() : base("SurvivalSuitBlueprint_FromSurvivalCold")
        {
            OnFinishedPatching += () =>
            {
                Reflection.AddCompoundTech(this.TechType, new List<TechType>()
                {
                    TechType.ReinforcedDiveSuit,
                    Main.StillSuitType,
                    TechType.ColdSuit
                });
            };
        }

		public override bool UnlockedAtStart => false;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("SurvivalColdSuit"), 1),
                        new Ingredient(TechType.ReinforcedGloves, 1),
                        new Ingredient(TechType.ColdSuitGloves, 1),
                        new Ingredient(TechType.ReinforcedDiveSuit, 1)
                    }
                ),
                LinkedItems = new List<TechType>()
                {
                    Main.GetModTechType("SuperSurvivalSuit"),
                    Main.GetModTechType("ReinforcedColdGloves")
                }
            };
        }
    }
#endif
}
