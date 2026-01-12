using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
#if SN1
//using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#elif BELOWZERO
#endif

namespace DWEquipmentBonanza.VehicleModules
{
	/*
	internal class ExosuitSprintModule : Equipable
	{
		public override EquipmentType EquipmentType => EquipmentType.ExosuitModule;
		public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
		public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
		public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
		public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
		public override CraftTree.Type FabricatorType => CraftTree.Type.None;
		public override string[] StepsToFabricatorTab => new string[] { "ExosuitModules" };
		public override float CraftingTime => 10f;
		public override Vector2int SizeInInventory => new Vector2int(1, 1);

		private static GameObject prefab;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
			};
		}

		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ExosuitThermalReactorModule);
				yield return task;
				prefab = GameObject.Instantiate<GameObject>(task.GetResult());
				ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
														 // but it can still be instantiated. [unlike with SetActive(false)]
			}

			gameObject.Set(GameObject.Instantiate(prefab));
		}

		protected override Sprite GetItemSprite()
		{
			return SpriteManager.Get(TechType.ExosuitJetUpgradeModule);
		}

		public ExosuitSprintModule() : base("ExosuitSprintModule", "Exosuit Sprint Module", "A hydraulic system that allows the Exosuit's jump jets to angle for horizontal travel.")
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
			};
		}
	}*/
}
