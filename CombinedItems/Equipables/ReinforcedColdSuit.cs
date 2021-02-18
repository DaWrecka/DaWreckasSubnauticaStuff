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

namespace CombinedItems.ReinforcedColdSuitPrefabs
{
    internal class ReinforcedColdGloves : Equipable
    {
        internal static TechType techType;
        public ReinforcedColdGloves() : base("ReinforcedColdGloves", "Reinforced Cold Gloves", "Reinforced insulating gloves provide physical protection and insulation from extreme temperatures.")
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuitGloves);
                CombinedItems.Reflection.AddColdResistance(this.TechType, 10);
                CombinedItems.Reflection.SetItemSize(this.TechType, 2, 2);
                Logger.Log(Logger.Level.Debug, $"Finished patching, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                ReinforcedColdGloves.techType = this.TechType;
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
                CombinedItems.Reflection.AddColdResistance(this.TechType, 20);
                CombinedItems.Reflection.SetItemSize(this.TechType, 2, 2);
                Logger.Log(Logger.Level.Debug, $"Finished patching, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                InsulatedRebreather.techType = this.TechType;
            };
        }

        private GameObject prefab;
        public override EquipmentType EquipmentType => EquipmentType.Head;

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
                CombinedItems.Reflection.AddColdResistance(this.TechType, 50);
                CombinedItems.Reflection.SetItemSize(this.TechType, 2, 3);
                Logger.Log(Logger.Level.Debug, $"Finished patching, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                ReinforcedColdSuit.techType = this.TechType;
            };
        }

        private GameObject prefab;

        public override EquipmentType EquipmentType => EquipmentType.Body;

        public override TechType RequiredForUnlock => TechType.ReinforcedDiveSuit;

        public override Vector2int SizeInInventory => new Vector2int(2, 2);

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;

        public override string[] StepsToFabricatorTab => new string[] { "" };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>( new Ingredient[]
                    {
                        new Ingredient(TechType.ColdSuit, 1),
                        new Ingredient(TechType.ColdSuitGloves, 1),
                        new Ingredient(TechType.ColdSuitHelmet, 1),
                        new Ingredient(TechType.ReinforcedDiveSuit, 1),
                        new Ingredient(TechType.ReinforcedGloves, 1),
                        new Ingredient(TechType.Rebreather, 1)
                    }
                ),
                LinkedItems = new List<TechType>(new TechType[]
                    {
                        InsulatedRebreather.techType,
                        ReinforcedColdGloves.techType
                    }
                )
            
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
