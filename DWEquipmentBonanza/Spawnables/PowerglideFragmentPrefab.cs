using Main = DWEquipmentBonanza.DWEBPlugin;
using DWEquipmentBonanza.Equipables;
using Common;
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
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UWE;
using Random = UnityEngine.Random;
using DWEquipmentBonanza.MonoBehaviours;
using Nautilus.Assets.Gadgets;

namespace DWEquipmentBonanza.Spawnables
{
	internal class PowerglideFragmentPrefab : Spawnable
	{
#if NAUTILUS
		protected override TechType templateType => TechType.SeaglideFragment;
		protected override string templateClassId => string.Empty;
		private float scanTime => 3f;
		private int fragmentsToScan => 3;
		private bool destroyOnScan => true;
		public override void FinalisePrefab(CustomPrefab prefab)
		{
			base.FinalisePrefab(prefab);
			ScanningGadget scanningGadget = GadgetExtensions.CreateFragment(prefab, Main.GetModTechType("DWEBPowerglide"), scanTime, fragmentsToScan, destroyAfterScan: destroyOnScan, isFragment: true);
		}
#else
		private static GameObject prefab;
#endif
	   
		public PowerglideFragmentPrefab() : base("PowerglideFragment", "Powerglide Fragment", "Damaged Powerglide")
		{
			//Console.WriteLine($"{this.ClassID} constructing");
#if LEGACY
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
			};
#endif
		}

		public override List<LootDistributionData.BiomeData> BiomesToSpawnIn => GetBiomeDistribution();

		public override WorldEntityInfo EntityInfo => new WorldEntityInfo() { cellLevel = LargeWorldEntity.CellLevel.Medium, classId = ClassID, localScale = Vector3.one, prefabZUp = false, slotType = EntitySlot.Type.Small, techType = this.TechType };

		public List<LootDistributionData.BiomeData> GetBiomeDistribution()
		{
			return new List<LootDistributionData.BiomeData>()
			{
#if SN1
				new LootDistributionData.BiomeData(){ biome = BiomeType.Dunes_TechSite, count = 1, probability = 0.04f },
				new LootDistributionData.BiomeData(){ biome = BiomeType.Mountains_TechSite, count = 1, probability = 0.08f },
				new LootDistributionData.BiomeData(){ biome = BiomeType.SeaTreaderPath_TechSite, count = 1, probability = 0.04f },
				new LootDistributionData.BiomeData(){ biome = BiomeType.SparseReef_Techsite, count = 1, probability = 0.04f },
				new LootDistributionData.BiomeData(){ biome = BiomeType.UnderwaterIslands_TechSite, count = 1, probability = 0.04f },
#elif BELOWZERO
				new LootDistributionData.BiomeData(){ biome = BiomeType.LilyPads_Deep_Grass, count = 1, probability = 0.01f },
				new LootDistributionData.BiomeData(){ biome = BiomeType.LilyPads_Deep_Ground, count = 1, probability = 0.02f },
				new LootDistributionData.BiomeData(){ biome = BiomeType.MiningSite_Ground, count = 1, probability = 0.01f },
				new LootDistributionData.BiomeData(){ biome = BiomeType.PurpleVents_Deep_Pool_Voxel, count = 1, probability = 0.01f },
				//new LootDistributionData.BiomeData(){ biome = BiomeType.TwistyBridges_Deep_Ground, count = 1, probability = 0.1f },
				new LootDistributionData.BiomeData(){ biome = BiomeType.TwistyBridges_Cave_Ground, count = 1, probability = 0.01f },
				//new LootDistributionData.BiomeData(){ biome = BiomeType.TwistyBridges_Deep_ThermalVentArea_Ground, count = 1, probability = 0.1f }
#endif
			};
		}

#if NAUTILUS
		public override void ModPrefab(GameObject gameObject)
		{
			base.ModPrefab(gameObject);
			gameObject = PreparePrefab(gameObject);
		}

#elif SN1
		public override GameObject GetGameObject()
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
			if (prefab == null)
			{
				prefab = PreparePrefab(CraftData.GetPrefabForTechType(TechType.SeaglideFragment));
			}

			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
			return prefab;
		}

#elif BELOWZERO
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.SeaglideFragment);
				yield return task;

				prefab = PreparePrefab(task.GetResult());
			}

			gameObject.Set(prefab);
		}
#endif

		private GameObject PreparePrefab(GameObject thisPrefab)
		{
			if (thisPrefab == null)
			{
				Log.LogError("PowerlideFragmentPrefab.PreparePrefab called with null prefab!");
				return null;
			}

#if NAUTILUS
			var obj = thisPrefab;
#else
			var obj = GameObject.Instantiate(thisPrefab);
#endif

			MeshRenderer[] meshRenderers = obj.GetAllComponentsInChildren<MeshRenderer>();
			SkinnedMeshRenderer[] skinnedMeshRenderers = obj.GetAllComponentsInChildren<SkinnedMeshRenderer>();
			Color powerGlideColour = PowerglideBehaviour.PowerGlideColour;

			foreach (MeshRenderer mr in meshRenderers)
			{
				// MeshRenderers have the third-person mesh, apparently?
				if (mr.name.Contains("SeaGlide_01_damaged"))
				{
					mr.material.color = powerGlideColour;
				}
			}

			foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
			{
				if (smr.name.Contains("SeaGlide_geo"))
				{
					smr.material.color = powerGlideColour;
				}
			}

			PrefabIdentifier prefabIdentifier = obj.EnsureComponent<PrefabIdentifier>();
			prefabIdentifier.ClassId = this.ClassID;
			obj.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			obj.EnsureComponent<TechTag>().type = this.TechType;

			Pickupable pickupable = obj.EnsureComponent<Pickupable>();
			pickupable.isPickupable = false;

			ResourceTracker resourceTracker = obj.EnsureComponent<ResourceTracker>();
			resourceTracker.prefabIdentifier = prefabIdentifier;
			typeof(ResourceTracker).GetField("techType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(resourceTracker, this.TechType);
			//resourceTracker.techType = this.TechType;
			resourceTracker.overrideTechType = TechType.Fragment;
			resourceTracker.techType = this.TechType;
			resourceTracker.rb = obj.GetComponent<Rigidbody>();
			resourceTracker.pickupable = pickupable;

#if !NAUTILUS
			ModPrefabCache.AddPrefab(obj, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
															  // but it can still be instantiated. [unlike with SetActive(false)]
#endif

			return obj;
		}
	}
}
