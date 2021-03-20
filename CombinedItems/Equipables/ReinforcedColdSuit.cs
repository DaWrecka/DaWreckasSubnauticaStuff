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
using Logger = QModManager.Utility.Logger;

namespace CombinedItems.Equipables
{
    internal class ReinforcedColdGloves : Equipable
    {
        internal static TechType techType;
        public ReinforcedColdGloves() : base("ReinforcedColdGloves", "Reinforced Cold Gloves", "Reinforced insulating gloves provide physical protection and insulation from extreme temperatures.")
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuitGloves);
                CombinedItems.Reflection.AddColdResistance(this.TechType, System.Math.Max(10, coldResist));
                CombinedItems.Reflection.SetItemSize(this.TechType, 2, 2);
                Logger.Log(Logger.Level.Debug, $"Finished patching, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                Main.AddSubstitution(this.TechType, TechType.ColdSuitGloves);
                Main.AddSubstitution(this.TechType, TechType.ReinforcedGloves);
                ReinforcedColdGloves.techType = this.TechType;
                Main.AddModTechType(this.TechType);
            };
        }

        private GameObject prefab;

        public override EquipmentType EquipmentType => EquipmentType.Gloves;
        public override Vector2int SizeInInventory => new Vector2int(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>()
            };
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.ColdSuitGloves);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuitGloves, true);
                yield return task;

                prefab = task.GetResult();
                prefab.SetActive(false); // Keep the prefab inactive until we're done editing it.

                // Editing prefab

                prefab.SetActive(true);
            }

            GameObject go = GameObject.Instantiate(prefab);
            gameObject.Set(go);
        }
    }

    internal class InsulatedRebreather : Equipable
    {
        internal static TechType techType;

        public InsulatedRebreather() : base("InsulatedRebreather", "Insulated Rebreather", "Rebreather equipped with insulation helps slow the onset of hypothermia")
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuitHelmet);
                CombinedItems.Reflection.AddColdResistance(this.TechType, System.Math.Max(20, coldResist));
                CombinedItems.Reflection.SetItemSize(this.TechType, 2, 2);
                Logger.Log(Logger.Level.Debug, $"Finished patching, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                Main.AddSubstitution(this.TechType, TechType.ColdSuitHelmet);
                Main.AddSubstitution(this.TechType, TechType.Rebreather);
                InsulatedRebreather.techType = this.TechType;
                Main.AddModTechType(this.TechType);
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    TechType.Rebreather,
                    TechType.ColdSuit
                };
                Reflection.AddCompoundTech(compound);
            };
        }

        private GameObject prefab;
        public override EquipmentType EquipmentType => EquipmentType.Head;
        public override Vector2int SizeInInventory => new Vector2int(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        public override TechType RequiredForUnlock => TechType.FrozenCreatureAntidote;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { "SuitUpgrades" };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.ColdSuitHelmet, 1),
                    new Ingredient(TechType.Rebreather, 1)
                }
            };
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.ColdSuitHelmet);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuitGloves, true);
                yield return task;

                prefab = task.GetResult();
                prefab.SetActive(false); // Keep the prefab inactive until we're done editing it.

                // Editing prefab

                prefab.SetActive(true);
            }

            GameObject go = GameObject.Instantiate(prefab);
            gameObject.Set(go);
        }
    }

    internal class ReinforcedColdSuit : Equipable
    {
        internal static TechType techType;

        public ReinforcedColdSuit() : base("ReinforcedColdSuit", "Reinforced Cold Suit", "Reinforced, insulated diving suit providing physical protection and insulation from extreme temperatures.")
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
                CombinedItems.Reflection.AddColdResistance(this.TechType, System.Math.Max(50, coldResist));
                CombinedItems.Reflection.SetItemSize(this.TechType, 2, 3);
                ReinforcedColdSuit.techType = this.TechType;
                Logger.Log(Logger.Level.Debug, $"Finished patching, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                Main.AddSubstitution(this.TechType, TechType.ColdSuit);
                Main.AddSubstitution(this.TechType, TechType.ReinforcedDiveSuit);
                Main.AddModTechType(this.TechType);
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    TechType.ReinforcedDiveSuit,
                    TechType.ColdSuit
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
                Ingredients = new List<Ingredient>( new Ingredient[]
                    {
                        new Ingredient(TechType.ColdSuit, 1),
                        new Ingredient(TechType.ColdSuitGloves, 1),
                        new Ingredient(TechType.ReinforcedDiveSuit, 1),
                        new Ingredient(TechType.ReinforcedGloves, 1)
                    }
                ),
                LinkedItems = new List<TechType>()
                {
                    ReinforcedColdGloves.techType
                }
            };
        }

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

                prefab.SetActive(true);
            }

            GameObject go = GameObject.Instantiate(prefab);
            gameObject.Set(go);
        }
    }
}
