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
using Common;

namespace DWEquipmentBonanza.Equipables
{
#if BELOWZERO
    internal class ReinforcedColdGloves : Equipable
    {
        public ReinforcedColdGloves() : base("ReinforcedColdGloves", "Reinforced Cold Gloves", "Reinforced insulating gloves provide physical protection and insulation from extreme temperatures.")
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuitGloves);
                DWEquipmentBonanza.Reflection.AddColdResistance(this.TechType, System.Math.Max(10, coldResist));
                DWEquipmentBonanza.Reflection.SetItemSize(this.TechType, 2, 2);
                Log.LogDebug($"Finished patching {this.TechType.AsString()}, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                Main.AddSubstitution(this.TechType, TechType.ColdSuitGloves);
                Main.AddSubstitution(this.TechType, TechType.ReinforcedGloves);
                Main.AddModTechType(this.TechType);
            };
        }

        protected static GameObject prefab;

        public override EquipmentType EquipmentType => EquipmentType.Gloves;
        public override Vector2int SizeInInventory => new(2, 2);
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
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuitGloves, verbose: true);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
            }

            GameObject go = GameObject.Instantiate(prefab);
            gameObject.Set(go);
        }
    }

    internal class InsulatedRebreather : Equipable
    {
        public InsulatedRebreather() : base("InsulatedRebreather", "Insulated Rebreather", "Rebreather equipped with insulation helps slow the onset of hypothermia")
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuitHelmet);
                DWEquipmentBonanza.Reflection.AddColdResistance(this.TechType, System.Math.Max(20, coldResist));
                DWEquipmentBonanza.Reflection.SetItemSize(this.TechType, 2, 2);
                Log.LogDebug($"Finished patching {this.TechType.AsString()}, using source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                Main.AddSubstitution(this.TechType, TechType.ColdSuitHelmet);
                Main.AddSubstitution(this.TechType, TechType.Rebreather);
                Main.AddModTechType(this.TechType);
                Reflection.AddCompoundTech(this.TechType, new List<TechType>()
                {
                    TechType.Rebreather,
                    TechType.ColdSuit
                });
            };
        }

        protected static GameObject prefab;
        public override EquipmentType EquipmentType => EquipmentType.Head;
        public override Vector2int SizeInInventory => new(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

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
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuitGloves, verbose: true);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
            }

            GameObject go = GameObject.Instantiate(prefab);
            gameObject.Set(go);
        }
    }

    internal class ReinforcedColdSuit : Equipable
    {
        public ReinforcedColdSuit() : base("ReinforcedColdSuit", "Reinforced Cold Suit", "Reinforced, insulated diving suit providing physical protection and insulation from extreme temperatures.")
        {
            OnFinishedPatching += () =>
            {
                int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
                Reflection.AddColdResistance(this.TechType, System.Math.Max(50, coldResist));
                Reflection.SetItemSize(this.TechType, 2, 3);
                Log.LogDebug($"Finished patching {this.TechType.AsString()}, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
                Main.AddSubstitution(this.TechType, TechType.ColdSuit);
                Main.AddSubstitution(this.TechType, TechType.ReinforcedDiveSuit);
                Main.AddModTechType(this.TechType);
                Reflection.AddCompoundTech(this.TechType, new List<TechType>()
                {
                    TechType.ReinforcedDiveSuit,
                    TechType.ColdSuit
                });
            };
        }

        protected static GameObject prefab;

        public override EquipmentType EquipmentType => EquipmentType.Body;
        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override Vector2int SizeInInventory => new(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.ColdSuit, 1),
                        new Ingredient(TechType.ColdSuitGloves, 1),
                        new Ingredient(TechType.ReinforcedDiveSuit, 1),
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
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuit, verbose: true);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
            }

            GameObject go = GameObject.Instantiate(prefab);
            gameObject.Set(go);
        }
    }
#endif
}
