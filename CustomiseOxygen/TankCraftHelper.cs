﻿using Common;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomiseOxygen
{
    // Internal class to allow crafting a linked item and not the initial item, so that we can craft tank refills without adding a "Refilled Tank" item to the inventory as well.
    internal class TankCraftHelper : Craftable
    {
        internal static Dictionary<TechType, GameObject> prefabs = new Dictionary<TechType, GameObject>();
        internal static Dictionary<TechType, TechType> RequiredUnlockDict = new Dictionary<TechType, TechType>();
        internal TechType thisBaseTank;
        internal RecipeData myRecipe;
        //static string tankName => Language.main.Get(this.thisBaseTank);

        private static TechType GetRequiredUnlock(TechType key)
        {
            if (RequiredUnlockDict.TryGetValue(key, out TechType value))
                return value;

            Log.LogDebug($"Failed to find value in RequiredToUnlock dictionary for key {key.AsString()}");
            return TechType.None;
        }

        public TankCraftHelper(TechType baseTank) : base(baseTank.AsString(false) + "RefillHelper", "Refilled " + Language.main.Get(baseTank), "Refilled " + Language.main.Get(baseTank))
        {
            Log.LogDebug($"{baseTank.AsString(false) + "RefillHelper"} constructor initialising; this.TechType == {this.TechType.AsString()}");
            OnFinishedPatching += () =>
            {
                Log.LogDebug($"{this.TechType.AsString(false)} OnFinishedPatching begin");
                RequiredUnlockDict[this.TechType] = baseTank;
                thisBaseTank = baseTank;
                Main.bannedTech.Add(this.TechType);
                Log.LogDebug($"{this.TechType.AsString(false)} adding crafting node");
                CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, this.TechType, new string[] {
                        "Personal",
                        "TankRefill",
                        this.TechType.AsString(false)
                    });
                myRecipe = new RecipeData()
                {
                    craftAmount = 0,
                    Ingredients = new List<Ingredient>()
                    {
                        new Ingredient(baseTank, 1)
                    }
                };
                myRecipe.LinkedItems.Add(baseTank);
                Log.LogDebug($"{this.TechType.AsString(false)} OnFinishedPatching completed");
            };
        }

        public override string[] StepsToFabricatorTab => new string[] { "Personal", "TankRefill" };

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(thisBaseTank);
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            /*TechType baseTank;

            if (RequiredUnlockDict.TryGetValue(this.TechType, out baseTank))
            {
                return new RecipeData()
                {
                    craftAmount = 0,
                    Ingredients = new List<Ingredient>()
                    {
                        new Ingredient(baseTank, 1)
                    },
                    LinkedItems = new List<TechType>()
                    {
                        baseTank
                    }
                };
            }

            return new RecipeData();*/
            return myRecipe;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject prefab;

            if (!prefabs.TryGetValue(this.TechType, out prefab))
            {
                var task = CraftData.GetPrefabForTechTypeAsync(TechType.Tank);
                yield return task;

                prefab = task.GetResult();
                //prefab.EnsureComponent<AutoRemover>();
                prefabs.Add(this.TechType, prefab);
            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }

        public override TechType RequiredForUnlock => GetRequiredUnlock(this.TechType);
    }

}

