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

namespace CombinedItems.Equipables
{
    public class SurvivalSuit : Equipable
    {
        public SurvivalSuit(string classId = "SurvivalSuit",
                string friendlyName = "Survival Suit",
                string Description = "Enhanced survival suit provides passive replenishment of calories and fluids, reducing the need for external sources of sustenance.") : base(classId, friendlyName, Description)
        {
            OnFinishedPatching += () =>
            {
                Main.AddSubstitution(this.TechType, TechType.Stillsuit);
                Main.AddModTechType(this.TechType);
            };
        }

        private GameObject prefab;

        public override EquipmentType EquipmentType => EquipmentType.Body;
        public override TechType RequiredForUnlock => TechType.FrozenCreatureAntidote;
        public override Vector2int SizeInInventory => new Vector2int(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { "SuitUpgrades" };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Stillsuit, 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1),
                        new Ingredient(TechType.JellyPlant, 1),
                        new Ingredient(TechType.KelpRootPustule, 1)
                    }
                ),
                LinkedItems = new List<TechType>()
            };
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.Stillsuit);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.Stillsuit, true);
                yield return task;

                prefab = task.GetResult();
                prefab.SetActive(false); // Keep the prefab inactive until we're done editing it.

                // Editing prefab
                GameObject.Destroy(prefab.GetComponent<Stillsuit>());
                prefab.EnsureComponent<SurvivalsuitBehaviour>();

                prefab.SetActive(true);
            }

            GameObject go = GameObject.Instantiate(prefab);
            gameObject.Set(go);
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
                Main.AddSubstitution(this.TechType, TechType.Stillsuit);
                Main.AddSubstitution(this.TechType, TechType.ReinforcedDiveSuit);
                Main.AddModTechType(this.TechType);
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    TechType.ReinforcedDiveSuit,
                    TechType.Stillsuit
                };
                Reflection.AddCompoundTech(compound);
            };
        }

        private GameObject prefab;

        public override EquipmentType EquipmentType => EquipmentType.Body;
        public override TechType RequiredForUnlock => TechType.FrozenCreatureAntidote;
        public override Vector2int SizeInInventory => new Vector2int(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { "SuitUpgrades" };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Stillsuit, 1),
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

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.Stillsuit, true);
                yield return task;

                prefab = task.GetResult();
                prefab.SetActive(false); // Keep the prefab inactive until we're done editing it.

                // Editing prefab
                GameObject.Destroy(prefab.GetComponent<Stillsuit>());
                prefab.EnsureComponent<SurvivalsuitBehaviour>();

                prefab.SetActive(true);
            }

            GameObject go = GameObject.Instantiate(prefab);
            gameObject.Set(go);
        }
    }

    internal class SuperSurvivalSuit : Equipable
    {
        public SuperSurvivalSuit() : base("SuperSurvivalSuit", "Ultimate Survival Suit", "The ultimate in survival gear. Provides protection from extreme temperatures and physical harm, and reduces the need for external sustenance.")
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
                Reflection.AddColdResistance(this.TechType, System.Math.Max(55, coldResist));
                Reflection.SetItemSize(this.TechType, 2, 3);
                ReinforcedColdSuit.techType = this.TechType;
                Log.LogDebug($"Finished patching, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                Main.AddSubstitution(this.TechType, TechType.ColdSuit);
                Main.AddSubstitution(this.TechType, TechType.ReinforcedDiveSuit);
                Main.AddModTechType(this.TechType);
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

        private GameObject prefab;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Body;
        public override TechType RequiredForUnlock => TechType.FrozenCreatureAntidote; // This is a deliberate choice; there doesn't seem to be a way to prevent a blueprint from unlocking at the start without setting this.
        public override Vector2int SizeInInventory => new Vector2int(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.ColdSuit);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuit, true);
                yield return task;

                prefab = task.GetResult();
                prefab.SetActive(false); // Keep the prefab inactive until we're done editing it.

                // Editing prefab
                prefab.EnsureComponent<SurvivalsuitBehaviour>();

                prefab.SetActive(true);
            }

            GameObject go = GameObject.Instantiate(prefab);
            gameObject.Set(go);
        }
    }

    internal abstract class SurvivalSuitBlueprint : Craftable
    {
        public SurvivalSuitBlueprint(string classId,
            string friendlyName = "Ultimate Survival Suit",
            string Description = "The ultimate in survival gear. Provides protection from extreme temperatures and physical harm, and reduces the need for external sustenance.") : base(classId, friendlyName, Description)
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

    internal class SurvivalSuitBlueprint_BaseSuits : SurvivalSuitBlueprint
    {
        public SurvivalSuitBlueprint_BaseSuits() : base("SurvivalSuitBlueprint_BaseSuits")
        {
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.ReinforcedDiveSuit, 1),
                        new Ingredient(TechType.Stillsuit, 1),
                        new Ingredient(TechType.ColdSuit, 1)
                    }
                ),
                LinkedItems = new List<TechType>()
                {
                    Main.GetModTechType("SuperSurvivalSuit")
                }
            };
        }
    }

    internal class SurvivalSuitBlueprint_FromReinforcedSurvival : SurvivalSuitBlueprint
    {
        public SurvivalSuitBlueprint_FromReinforcedSurvival() : base("SurvivalSuitBlueprint_FromReinforcedSurvival")
        { }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("ReinforcedSurvivalSuit"), 1),
                        new Ingredient(TechType.ColdSuit, 1)
                    }
                ),
                LinkedItems = new List<TechType>()
                {
                    Main.GetModTechType("SuperSurvivalSuit")
                }
            };
        }
    }

    internal class SurvivalSuitBlueprint_FromReinforcedCold : SurvivalSuitBlueprint
    {
        public SurvivalSuitBlueprint_FromReinforcedCold() : base("SurvivalSuitBlueprint_FromReinforcedSurvival")
        { }

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
}
