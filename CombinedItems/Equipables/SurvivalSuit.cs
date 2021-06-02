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
using CombinedItems.MonoBehaviours;
using CombinedItems.Patches;

namespace CombinedItems.Equipables
{
    abstract public class SurvivalSuitBase<T> : Equipable
    {
        public SurvivalSuitBase(string classId,
                string friendlyName,
                string Description) : base(classId, friendlyName, Description)
        {
            OnFinishedPatching += () =>
            {
                //Main.AddSubstitution(this.TechType, TechType.Stillsuit);
                foreach (TechType tt in substitutions)
                {
                    Main.AddSubstitution(this.TechType, tt);
                }
                Main.AddModTechType(this.TechType);
                PlayerPatch.AddSurvivalSuit(this.TechType);
                //SurvivalPatches.AddNeedsCapOverride(this.TechType, SurvivalCapOverride);
            };
        }

        protected virtual float SurvivalCapOverride
        {
            get
            {
                return 150f;
            }
        }

        protected virtual TechType[] substitutions
        {
            get
            {
                return new TechType[] { TechType.Stillsuit };
            }
        }

        protected static GameObject prefab;

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.Stillsuit, true);
                yield return task;

                prefab = task.GetResult();
                //prefab.SetActive(false); // Keep the prefab inactive until we're done editing it.

                // Editing prefab
                GameObject.Destroy(prefab.GetComponent<Stillsuit>());
                prefab.EnsureComponent<SurvivalsuitBehaviour>();

                prefab.SetActive(true);
            }

            // Despite the component being removed from the prefab above, testing shows that the Survival Suits still add the water packs.
            // So we're going to force-remove the Stillsuit component here, to be safe.
            GameObject go = GameObject.Instantiate(prefab);
            Stillsuit still = go.GetComponent<Stillsuit>();
            if (still != null)
                GameObject.DestroyImmediate(still);
            gameObject.Set(go);
        }
    }

    public class SurvivalSuit : SurvivalSuitBase<SurvivalSuit>
    {
        public SurvivalSuit(string classId = "SurvivalSuit",
                string friendlyName = "Survival Suit",
                string Description = "Enhanced survival suit provides passive replenishment of calories and fluids, reducing the need for external sources of sustenance. Also improves bodily water capacity.") : base(classId, friendlyName, Description)
        {
            OnFinishedPatching += () =>
            {
            };
        }

        protected override TechType[] substitutions
        {
            get
            {
                return new TechType[] { TechType.Stillsuit };
            }
        }
        public override EquipmentType EquipmentType => EquipmentType.Body;
        public override TechType RequiredForUnlock => TechType.Stillsuit;
        public override Vector2int SizeInInventory => new Vector2int(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { "SuitUpgrades" };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Stillsuit, 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1),
                        new Ingredient(TechType.JellyPlant, 1),
                        new Ingredient(TechType.KelpRootPustule, 1)
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
            return SpriteManager.Get(TechType.Stillsuit);
        }
    }

    public class ReinforcedSurvivalSuit : SurvivalSuit
    {
        public ReinforcedSurvivalSuit(string classId = "ReinforcedSurvivalSuit",
                string friendlyName = "Reinforced Survival Suit",
                string Description = "Enhanced survival suit with reinforcing fibres provides passive primary needs reduction and protection from physical force and high temperatures") : base(classId, friendlyName, Description)
        {
            OnFinishedPatching += () =>
            {
                //Main.AddSubstitution(this.TechType, TechType.Stillsuit);
                //Main.AddSubstitution(this.TechType, TechType.ReinforcedDiveSuit);
                //Main.AddModTechType(this.TechType);
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    TechType.ReinforcedDiveSuit,
                    Main.GetModTechType("SurvivalSuit")
                };
                Reflection.AddCompoundTech(compound);
            };
        }

        protected override TechType[] substitutions
        {
            get
            {
                return new TechType[] { TechType.Stillsuit };
            }
        }

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
            return SpriteManager.Get(TechType.Stillsuit);
        }
    }


    public class SurvivalColdSuit : SurvivalSuit
    {
        public SurvivalColdSuit(string classId = "SurvivalColdSuit",
                string friendlyName = "Insulated Survival Suit",
                string Description = "Enhanced survival suit provides passive primary needs reduction and protection from extreme cold.") : base(classId, friendlyName, Description)
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
                Reflection.AddColdResistance(this.TechType, System.Math.Max(55, coldResist));
                Reflection.SetItemSize(this.TechType, 2, 3);
                Log.LogDebug($"Finished patching, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                //Main.AddSubstitution(this.TechType, TechType.Stillsuit);
                //Main.AddSubstitution(this.TechType, TechType.ColdSuit);
                //Main.AddModTechType(this.TechType);
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    Main.GetModTechType("SurvivalSuit"),
                    TechType.ColdSuit
                };
                Reflection.AddCompoundTech(compound);
            };
        }

        protected override TechType[] substitutions
        {
            get
            {
                return new TechType[] { TechType.ColdSuit };
            }
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
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuit, true);
                yield return task;

                prefab = task.GetResult();
                //prefab.SetActive(false); // Keep the prefab inactive until we're done editing it.

                // Editing prefab
                //GameObject.Destroy(prefab.GetComponent<Stillsuit>());
                prefab.EnsureComponent<SurvivalsuitBehaviour>();

                prefab.SetActive(true);
            }

            // Despite the component being removed from the prefab above, testing shows that the Survival Suits still add the water packs when they should.
            // So we're going to force-remove it here, to be safe.
            GameObject go = GameObject.Instantiate(prefab);
            Stillsuit still = go.GetComponent<Stillsuit>();
            if (still != null)
                GameObject.DestroyImmediate(still);
            gameObject.Set(go);
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.Stillsuit);
        }
    }
}
