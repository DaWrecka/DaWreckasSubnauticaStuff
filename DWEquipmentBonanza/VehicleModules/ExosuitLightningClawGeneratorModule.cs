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
	#if SN1
		using RecipeData = SMLHelper.V2.Crafting.TechData;
	#endif
#endif
using UnityEngine;
using System;
#if SN1
//using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza.VehicleModules
{
	internal class ExosuitLightningClawGeneratorModule : Equipable
	{
#if NAUTILUS
		protected override TechType templateType => TechType.ExosuitThermalReactorModule;
		protected override string templateClassId => string.Empty;
#endif
		public override EquipmentType EquipmentType => EquipmentType.ExosuitModule;
		public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
		public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
		public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
		public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
		public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
		public override string[] StepsToFabricatorTab => new string[] { "ExosuitModules" };
		public override float CraftingTime => 10f;
		public override Vector2int SizeInInventory => new Vector2int(1, 2);

		//private static GameObject prefab;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.Polyaniline, 1),
						new Ingredient(TechType.WiringKit, 1),
						new Ingredient(TechType.Battery, 1),
						new Ingredient(TechType.AluminumOxide, 1)
					}
				)
			};
		}

#if NAUTILUS
#elif ASYNC
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
			if (prefab == null)
			{
				CoroutineTask<GameObject> instResult = CraftData.GetPrefabForTechTypeAsync(TechType.ExosuitThermalReactorModule);
				yield return instResult;
				prefab = PreparePrefab(instResult.GetResult());
			}

			gameObject.Set(prefab);

			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
		}
#else
		public override GameObject GetGameObject()
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
			if (prefab == null)
			{
				prefab = PreparePrefab(CraftData.GetPrefabForTechType(TechType.ExosuitThermalReactorModule));
			}
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
			
			return prefab;
		}
#endif

		protected GameObject PreparePrefab(GameObject prefab)
		{
			GameObject obj = GameObject.Instantiate<GameObject>(prefab);

			// Editing prefab

// Finalise prefab
#if NAUTILUS
#else
			ModPrefabCache.AddPrefab(obj, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
													 // but it can still be instantiated. [unlike with SetActive(false)]
#endif
			return obj;
		}

		protected override Sprite GetItemSprite()
		{
#if SN1
			return SpriteManager.Get(TechType.SeamothElectricalDefense);
#elif BELOWZERO
			return SpriteManager.Get(TechType.SeaTruckUpgradePerimeterDefense);
#endif
		}

		public ExosuitLightningClawGeneratorModule() : base("ExosuitLightningClawGeneratorModule", "Exosuit Lightning Claw Generator", "An electrical pulse generator which ties into the Exosuit's claw arm, electrocuting anything struck by it.")
		{
			//Console.WriteLine($"{this.ClassID} constructing");
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
			};
		}
	}
}
