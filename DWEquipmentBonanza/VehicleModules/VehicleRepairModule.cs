using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DWEquipmentBonanza.MonoBehaviours;
using System.IO;
using Common.Utility;
using System;

#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Nautilus.Utility;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#if SN1
	//using Ingredient = CraftData\.Ingredient;
#endif
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
	#if SN1
		using RecipeData = SMLHelper.V2.Crafting.TechData;
	#endif
#endif
#if SN1
//using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza.VehicleModules
{
	internal class VehicleRepairModule : Equipable
	{
#if NAUTILUS
		protected override TechType templateType => TechType.VehiclePowerUpgradeModule;
		protected override string templateClassId => string.Empty;
#endif

		public override EquipmentType EquipmentType => EquipmentType.VehicleModule;
#if SN1
		public override QuickSlotType QuickSlotType => QuickSlotType.Toggleable;
#elif BELOWZERO
		public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
#endif
		public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
		public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
		public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
		public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
#if SN1
		public override string[] StepsToFabricatorTab => new string[] { "CommonModules" };
#elif BELOWZERO
		public override string[] StepsToFabricatorTab => new string[] { "ExosuitModules" };
#endif
		public override float CraftingTime => 5f;
		public override Vector2int SizeInInventory => new(1, 1);

		private static Sprite sprite;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.Kyanite, 2),
						new Ingredient(TechType.Polyaniline, 2),
						new Ingredient(TechType.WiringKit, 1)
					}
				)
			};
		}

#if NAUTILUS
		public override void ModPrefab(GameObject gameObject)
		{
			base.ModPrefab(gameObject);
			gameObject.name = ClassID;
			gameObject.EnsureComponent<VehicleRepairComponent>();
		}
#else
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			GameObject modPrefab;

			if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
			{
				//TaskResult<GameObject> prefabResult = new TaskResult<GameObject>();
				//yield return CraftData.InstantiateFromPrefabAsync(TechType.SeaTruckUpgradeEnergyEfficiency, prefabResult, false);
				//prefab = prefabResult.Get();

#if SN1
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.SeamothReinforcementModule);
#elif BELOWZERO
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.SeaTruckUpgradeEnergyEfficiency, true);
#endif
				yield return task;
				modPrefab = GameObjectUtils.InstantiateInactive(task.GetResult());

				modPrefab.name = ClassID;
				//prefab.EnsureComponent<VehicleRepairComponent>();
				// The code is handled by the SeatruckUpdater component, rather than anything here.
				//ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
														   // but it can still be instantiated. [unlike with SetActive(false)]
				TechTypeUtils.AddModTechType(this.TechType, modPrefab);
			}

			gameObject.Set(modPrefab);
		}
#endif

		protected override Sprite GetItemSprite()
		{
			sprite ??= ImageUtils.LoadSpriteFromFile(Path.Combine(Main.AssetsFolder, $"{ClassID}.png")); ;
			return sprite;
		}

		public VehicleRepairModule() : base("VehicleRepairModule", "Vehicle Repair Module", "Passively repairs damaged hull for modest energy cost; in active mode, rapidly repairs damage, but at significant energy cost")
		{
			//Console.WriteLine($"{this.ClassID} constructing");
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
#if SN1
				bool success = SeamothUpdater.AddRepairModuleType(this.TechType);
				bool successExo = ExosuitUpdater.AddRepairModuleType(this.TechType);
				Log.LogDebug(($"Finished patching {this.TechType.AsString()}, added successfully: Seamoth {success}, Exosuit {successExo}"));
#elif BELOWZERO
				bool success = SeaTruckUpdater.AddRepairModuleType(this.TechType);
				Log.LogDebug(($"Finished patching {this.TechType.AsString()}, added successfully: {success}"));
#endif
			};
		}
	}
}
