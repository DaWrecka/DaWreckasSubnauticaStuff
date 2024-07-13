using Common;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Handlers;
using RecipeData = Nautilus.Crafting.RecipeData;
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using RecipeData = SMLHelper.V2.Crafting.TechData;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if SN1
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace CustomiseOxygen
{
#if !NAUTILUS
	// Internal class to allow crafting a linked item and not the initial item, so that we can craft tank refills without adding a "Refilled Tank" item to the inventory as well.
	[Obsolete]
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
				CustomiseOxygenPlugin.bannedTech.Add(this.TechType);
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

#if ASYNC
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject prefab;

            if (!prefabs.TryGetValue(this.TechType, out prefab))
            {
                //var task = CraftData.GetPrefabForTechTypeAsync(TechType.Tank);
                var task = CraftData.GetPrefabForTechTypeAsync(thisBaseTank);
                yield return task;

                prefab = task.GetResult();
                //prefab.EnsureComponent<AutoRemover>();
                prefabs.Add(this.TechType, prefab);
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                         // but it can still be instantiated. [unlike with SetActive(false)]
            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }
#else
        public override GameObject GetGameObject()
		{
			GameObject prefab;

			if (!prefabs.TryGetValue(this.TechType, out prefab))
			{
				prefab = CraftData.GetPrefabForTechType(thisBaseTank)
					;
				//prefab.EnsureComponent<AutoRemover>();
				prefabs.Add(this.TechType, prefab);
				ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
														 // but it can still be instantiated. [unlike with SetActive(false)]
														 // Clarification: An inactive prefab can still be instantiated, but it will be instantiated as an inactive GameObject. Obviously, not that useful to us here.
														 // But since the prefab isn't *set* inactive, we can instantiate active objects with it, even though the prefab is effectively-inactive.
			}

			return prefab;
		}
#endif

        public override TechType RequiredForUnlock => GetRequiredUnlock(this.TechType);
	}
#endif
}
