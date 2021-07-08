using DWEquipmentBonanza.MonoBehaviours;
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
        public SuperSurvivalSuit() : base("SuperSurvivalSuit", "Ultimate Survival Suit", "The ultimate in survival gear. Provides protection from extreme temperatures and physical harm, reduces the need for external sustenance, and dramatically improves the body's ability to retain water.")
        {
            OnFinishedPatching += () =>
            {
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    TechType.Stillsuit,
#if SUBNAUTICA_STABLE
                    TechType.RadiationSuit,
#elif BELOWZERO
                    TechType.ColdSuit,
#endif
                    TechType.ReinforcedDiveSuit
                };
                Reflection.AddCompoundTech(compound);

#if SUBNAUTICA_STABLE
                Main.AddSubstitution(this.TechType, TechType.RadiationSuit);
                Main.NitroAddDiveSuit?.Invoke(null, new object[] { this.TechType, 8000f, 0.55f, 35f });
                Log.LogDebug($"Finished patching {this.TechType.AsString()}");
                Main.DamageResistances[this.TechType] = new List<Main.DamageInfo>()
                {
                    {
                        new Main.DamageInfo(DamageType.Acid, -0.6f)/*,
                        new DamageInfo(DamageType.Radiation, -0.70f)*/
                    }
                };
#elif BELOWZERO
                int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
                Reflection.AddColdResistance(this.TechType, System.Math.Max(55, coldResist));
                Reflection.SetItemSize(this.TechType, 2, 3);
                Log.LogDebug($"Finished patching {this.TechType.AsString()}, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
#endif

                // the SurvivalSuit constructor will call AddModTechType already.
                // It has also been set up to add substitutions based on the value of the 'substitutions' property below,
                // as well as set up CompoundTech based on the value of CompoundDependencies
            };
        }

        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override EquipmentType EquipmentType => EquipmentType.Body;
        protected override float SurvivalCapOverride
        {
            get
            {
                return 200f;
            }
        }

        protected override TechType[] substitutions
        {
            get
            {
                return new TechType[] {
#if BELOWZERO
                    TechType.ColdSuit,
#endif
                    TechType.ReinforcedDiveSuit
                };
            }
        }

        protected override List<TechType> CompoundDependencies
        {
            get
            {
                return new List<TechType>()
                {
                    TechType.ReinforcedDiveSuit,
                    TechType.Stillsuit,
#if SUBNAUTICA_STABLE
                    TechType.RadiationSuit
#elif BELOWZERO
                    TechType.ColdSuit,
#endif
                };
            }
        }

        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { "BodyMenu" };
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
                    new Ingredient(TechType.RadiationSuit, 1),
                    new Ingredient(Main.GetModTechType("AcidGloves"), 1)
#elif BELOWZERO
                    new Ingredient(TechType.ReinforcedDiveSuit, 1),
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
            return SpriteManager.Get(TechType.Stillsuit);
#elif BELOWZERO
            return SpriteManager.Get(TechType.ColdSuit);
#endif
        }

#if SUBNAUTICA_STABLE
        public override GameObject GetGameObject()
        {
            if (prefab == null)
            {
                prefab = InstantiateAndModifyGameObject(CraftData.GetPrefabForTechType(TechType.Stillsuit));
            }

            return prefab;
        }

#elif BELOWZERO
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            Stillsuit s;
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuit, verbose: true);
                yield return task;

                prefab = InstantiateAndModifyGameObject(task.GetResult());
            }

            GameObject go = GameObject.Instantiate(prefab);
            if(go.TryGetComponent<Stillsuit>(out s))
                GameObject.DestroyImmediate(s);
            gameObject.Set(go);
        }
#endif

        private GameObject InstantiateAndModifyGameObject(GameObject thisPrefab)
        {
            GameObject obj = GameObject.Instantiate(thisPrefab);
            // Editing prefab
            if (obj.TryGetComponent<Stillsuit>(out Stillsuit s))
                GameObject.DestroyImmediate(s);
            obj.EnsureComponent<SurvivalsuitBehaviour>();
            ModPrefabCache.AddPrefab(obj, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.

            return obj;
        }
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
        public override string[] StepsToFabricatorTab => new string[] { "BodyMenu" };

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.Stillsuit);
        }
    }

#if SUBNAUTICA_STABLE
#elif BELOWZERO
    internal class SurvivalSuitBlueprint_FromReinforcedSurvival : SurvivalSuitBlueprint
    {
        public SurvivalSuitBlueprint_FromReinforcedSurvival() : base("SurvivalSuitBlueprint_FromReinforcedSurvival")
        {
            OnFinishedPatching += () =>
            {
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    TechType.Stillsuit,
                    TechType.ColdSuit,
                    TechType.ReinforcedDiveSuit
                };
                Reflection.AddCompoundTech(compound);
            };
        }

        public override TechType RequiredForUnlock => TechType.Unobtanium;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("ReinforcedSurvivalSuit"), 1),
                        new Ingredient(TechType.ColdSuit, 1),
                        new Ingredient(TechType.ColdSuitGloves, 1),
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

    internal class SurvivalSuitBlueprint_FromReinforcedCold : SurvivalSuitBlueprint
    {
        public SurvivalSuitBlueprint_FromReinforcedCold() : base("SurvivalSuitBlueprint_FromReinforcedCold")
        {
            OnFinishedPatching += () =>
            {
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    TechType.Stillsuit,
                    TechType.ColdSuit,
                    TechType.ReinforcedDiveSuit
                };
                Reflection.AddCompoundTech(compound);
            };
        }

        public override TechType RequiredForUnlock => TechType.Unobtanium;
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
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    TechType.ReinforcedDiveSuit,
                    TechType.Stillsuit,
                    TechType.ColdSuit
                };
                Reflection.AddCompoundTech(compound);
            };
        }

        public override TechType RequiredForUnlock => TechType.Unobtanium;
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
