using CombinedItems.MonoBehaviours;
using CombinedItems.Patches;
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

namespace CombinedItems.Equipables
{
    internal class SuperSurvivalSuit : SurvivalSuitBase<SuperSurvivalSuit>
    {
        public SuperSurvivalSuit() : base("SuperSurvivalSuit", "Ultimate Survival Suit", "The ultimate in survival gear. Provides protection from extreme temperatures and physical harm, reduces the need for external sustenance, and dramatically improves the body's ability to retain water.")
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
                Reflection.AddColdResistance(this.TechType, System.Math.Max(55, coldResist));
                Reflection.SetItemSize(this.TechType, 2, 3);
                Log.LogDebug($"Finished patching {this.TechType.AsString()}, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");

                // the SurvivalSuit constructor will call AddModTechType already.
                // It has also been set up to add substitutions based on the value of the 'substitutions' property below,
                // as well as set up CompoundTech based on the value of CompoundDependencies
            };
        }

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
                return new TechType[] { TechType.ColdSuit, TechType.ReinforcedDiveSuit };
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
                    TechType.ColdSuit
                };
            }
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.ReinforcedDiveSuit, 1),
                        new Ingredient(Main.GetModTechType("SurvivalSuit"), 1),
                        new Ingredient(TechType.ColdSuit, 1),
                        new Ingredient(TechType.ColdSuitGloves, 1),
                        new Ingredient(TechType.ReinforcedGloves, 1)
                    }
                ),
                LinkedItems = new List<TechType>()
                {
                    Main.GetModTechType("ReinforcedColdGloves")
                }
            };
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.ColdSuit);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            Stillsuit s;
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuit, verbose: true);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                // Editing prefab
                if (prefab.TryGetComponent<Stillsuit>(out s))
                    GameObject.DestroyImmediate(s);
                prefab.EnsureComponent<SurvivalsuitBehaviour>();
                prefab.SetActive(false);
            }

            GameObject go = GameObject.Instantiate(prefab);
            if(go.TryGetComponent<Stillsuit>(out s))
                GameObject.DestroyImmediate(s);
            gameObject.Set(go);
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
        public override string[] StepsToFabricatorTab => new string[] { "SuitUpgrades" };

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.Stillsuit);
        }
    }

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
                        new Ingredient(TechType.ReinforcedGloves, 1)
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
}
