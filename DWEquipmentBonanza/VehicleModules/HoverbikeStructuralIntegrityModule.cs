using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using System.Collections;
using System.Collections.Generic;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#if SN1
	//using Ingredient = CraftData\.Ingredient;
#endif
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
#endif
using UnityEngine;
#if BEPINEX
	using BepInEx;
	using BepInEx.Logging;
#elif QMM
using Logger = QModManager.Utility.Logger;
#endif
using DWEquipmentBonanza.MonoBehaviours;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
	internal class HoverbikeStructuralIntegrityModule : HoverbikeUpgradeBase<HoverbikeStructuralIntegrityModule>
	{
		//private GameObject prefab;
		public const string description = "Consumes energy to reduce damage taken by the Snowfox by half. Does not stack.";
		protected override TechType spriteTemplate => TechType.VehicleArmorPlating; // Placeholder
		protected override TechType templateType => TechType.HoverbikeJumpModule;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.PrecursorIonCrystal, 1),
						new Ingredient(TechType.AdvancedWiringKit, 1),
						new Ingredient(TechType.Magnetite, 1),
						new Ingredient(TechType.Polyaniline, 1)
					}
				)
			};
		}

#if !NAUTILUS
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HoverbikeJumpModule);
				yield return task;
				prefab = GameObject.Instantiate<GameObject>(task.GetResult());
				//prefab.EnsureComponent<HoverbikeStructuralIntegrityModifier>();
				ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
														 // but it can still be instantiated. [unlike with SetActive(false)]
			}

			gameObject.Set(prefab);
		}
#endif

		/*protected override Sprite GetItemSprite()
		{
			return SpriteManager.Get(TechType.VehicleArmorPlating); // Placeholder
		}*/

		public HoverbikeStructuralIntegrityModule() : base("HoverbikeStructuralIntegrityModule", "Structural Integrity Field", description)
		{
		}
	}
#endif
	}
