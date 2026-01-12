using FuelCells.Patches;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Handlers;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using RecipeData = SMLHelper.V2.Crafting.RecipeData
#endif
using Story;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FuelCells.Spawnables
{
#if BELOWZERO

	class CoralSample : Spawnable
	{
		protected static Sprite sprite;
		protected static GameObject prefab;
#if NAUTILUS
		protected override string templateClassId => String.Empty;
		protected override TechType templateType => TechType.CoralChunk;
		protected virtual TechType spriteTemplate => TechType.CoralChunk;

#else

		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.CoralChunk);
				yield return task;

				prefab = GameObject.Instantiate(task.GetResult());
				ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
														 // but it can still be instantiated. [unlike with SetActive(false)]
			}

			// In BZ, the coral chunk has faulty collision. We fix it here.
			var obj = GameObject.Instantiate(prefab);
			if(obj.TryGetComponent<LargeWorldEntity>(out LargeWorldEntity largeWorldEntity))
			{
				obj.EnsureComponent<WorldForces>().useRigidbody = largeWorldEntity.GetComponent<Rigidbody>();
			}
			gameObject.Set(obj);
		}
#endif

		protected override Sprite GetItemSprite()
		{
			if (sprite == null)
			{
				sprite = SpriteManager.Get(TechType.CoralChunk, null);
			}

			return sprite;
		}

		protected void SetTechData(TechType tt, RecipeData recipeData)
		{
#if NAUTILUS
			CraftDataHandler.SetRecipeData(tt, recipeData);
#else
			CraftDataHandler.SetTechData(tt, recipeData);
#endif
		}

		public CoralSample()
			: base("CoralSample", "Coral Shelf Sample", "A sample of coral from the Twisty Bridges.")
		{
			OnFinishedPatching += () =>
			{
				KnifePatches.AddHarvestable(TechType.TwistyBridgesCoralShelf, 200f);
				FuelCellsPlugin.AddModTechType(this.TechType);
				CraftDataHandler.SetHarvestOutput(TechType.TwistyBridgesCoralShelf, this.TechType);
				CraftDataHandler.SetHarvestType(TechType.TwistyBridgesCoralShelf, HarvestType.DamageAlive);
				SetTechData(TechType.Bleach, new RecipeData()
				{
					craftAmount = 1,
					Ingredients = new List<Ingredient>()
				{
					new Ingredient(this.TechType, 1),
					new Ingredient(TechType.Salt, 1)
				}
				});
				CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.Bleach, new string[] { "Resources", "BasicMaterials" });
				CraftTreeHandler.AddCraftingNode(CraftTree.Type.SeaTruckFabricator, TechType.Bleach, new string[] { "Resources", "BasicMaterials" });

				SetTechData(TechType.DisinfectedWater, new RecipeData()
				{
					craftAmount = 2,
					Ingredients = new List<Ingredient>()
				{
					new Ingredient(TechType.Bleach, 1)
				}
				});
				CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.DisinfectedWater, new string[] { "Survival", "Water" });
				CraftTreeHandler.AddCraftingNode(CraftTree.Type.SeaTruckFabricator, TechType.DisinfectedWater, new string[] { "Survival", "Water" });
				KnownTechHandler.SetAnalysisTechEntry(this.TechType, new TechType[] { TechType.Bleach });
				KnownTechHandler.SetAnalysisTechEntry(TechType.Bleach, new TechType[] { TechType.DisinfectedWater });
				LanguageHandler.SetTechTypeTooltip(TechType.Bleach, "NaClO. Sodium hypochlorite bleach. Sanitizing applications.");
			};
		}
	}
#endif
}
