using DWEquipmentBonanza.Equipables;
using Common;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UWE;
using Random = UnityEngine.Random;
using DWEquipmentBonanza.MonoBehaviours;

namespace DWEquipmentBonanza.Spawnables
{
    internal class PowerglideFragmentPrefab : Spawnable
    {
        private static GameObject processedPrefab;

        public PowerglideFragmentPrefab() : base("PowerglideFragment", "Powerglide Fragment", "Damaged Powerglide")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
            };
        }

        public override List<LootDistributionData.BiomeData> BiomesToSpawnIn => GetBiomeDistribution();

        public override WorldEntityInfo EntityInfo => new WorldEntityInfo() { cellLevel = LargeWorldEntity.CellLevel.Medium, classId = ClassID, localScale = Vector3.one, prefabZUp = false, slotType = EntitySlot.Type.Small, techType = this.TechType };

        public List<LootDistributionData.BiomeData> GetBiomeDistribution()
        {
            return new List<LootDistributionData.BiomeData>()
            {
#if SUBNAUTICA_STABLE
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

#if SUBNAUTICA_STABLE
        public override GameObject GetGameObject()
        {
            if (processedPrefab == null)
            {
                processedPrefab = ModifyInstantiatedPrefab(CraftData.InstantiateFromPrefab(TechType.SeaglideFragment));
            }

            return processedPrefab;
        }
#elif BELOWZERO
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (processedPrefab == null)
            {
                TaskResult<GameObject> result = new TaskResult<GameObject>();
                IEnumerator enumerator = CraftData.InstantiateFromPrefabAsync(TechType.SeaglideFragment, result, false);
                yield return enumerator;

                processedPrefab = ModifyInstantiatedPrefab(result.Get());
            }

            gameObject.Set(processedPrefab);
        }
#endif

        private GameObject ModifyInstantiatedPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                Log.LogError("ModifyInstantiatedPrefab called with null prefab!");
                return null;
            }

            MeshRenderer[] meshRenderers = prefab.GetAllComponentsInChildren<MeshRenderer>();
            SkinnedMeshRenderer[] skinnedMeshRenderers = prefab.GetAllComponentsInChildren<SkinnedMeshRenderer>();
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

            PrefabIdentifier prefabIdentifier = prefab.GetComponent<PrefabIdentifier>();
            prefabIdentifier.ClassId = this.ClassID;
            prefab.GetComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
            prefab.EnsureComponent<TechTag>().type = this.TechType;

            Pickupable pickupable = prefab.GetComponent<Pickupable>();
            pickupable.isPickupable = false;

            ResourceTracker resourceTracker = prefab.EnsureComponent<ResourceTracker>();
            resourceTracker.prefabIdentifier = prefabIdentifier;
            typeof(ResourceTracker).GetField("techType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(resourceTracker, this.TechType);
            //resourceTracker.techType = this.TechType;
            resourceTracker.overrideTechType = TechType.Fragment;
            resourceTracker.rb = prefab.GetComponent<Rigidbody>();
            resourceTracker.pickupable = pickupable;

            prefab.SetActive(true);
            processedPrefab = prefab;
            ModPrefabCache.AddPrefab(processedPrefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                              // but it can still be instantiated. [unlike with SetActive(false)]

            return prefab;
        }
    }
}
