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

namespace CombinedItems.Spawnables
{
    internal class PowerglideFragmentPrefab : Spawnable
    {
        private static GameObject processedPrefab;
        // Slightly different colour for the fragments
        private const float PowerglideColourR = 0f;
        private const float PowerglideColourG = 1f;
        private const float PowerglideColourB = 0f;

        public PowerglideFragmentPrefab() : base("PowerglideFragment", "Powerglide Fragment", "Damaged Powerglide")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
            };
        }

        public override List<LootDistributionData.BiomeData> BiomesToSpawnIn => GetBiomeDistribution();

        public override WorldEntityInfo EntityInfo => new WorldEntityInfo() { cellLevel = LargeWorldEntity.CellLevel.Medium, classId = ClassID, localScale = Vector3.one, prefabZUp = false, slotType = EntitySlot.Type.Small, techType = TechType };

        public List<LootDistributionData.BiomeData> GetBiomeDistribution()
        {
            return new List<LootDistributionData.BiomeData>()
            {
                new LootDistributionData.BiomeData(){ biome = BiomeType.Glacier_Generic, count = 1, probability = 0.2f },
                new LootDistributionData.BiomeData(){ biome = BiomeType.GlacialBay, count = 1, probability = 0.2f },
                new LootDistributionData.BiomeData(){ biome = BiomeType.LilyPads_Deep_Grass, count = 1, probability = 0.2f },
                new LootDistributionData.BiomeData(){ biome = BiomeType.LilyPads_Deep_Ground, count = 1, probability = 0.2f },
                new LootDistributionData.BiomeData(){ biome = BiomeType.MiningSite_Ground, count = 1, probability = 0.2f },
                new LootDistributionData.BiomeData(){ biome = BiomeType.PurpleVents_Deep_Pool_Voxel, count = 1, probability = 0.2f },
                new LootDistributionData.BiomeData(){ biome = BiomeType.TwistyBridges_Deep_Ground, count = 1, probability = 0.1f },
                new LootDistributionData.BiomeData(){ biome = BiomeType.TwistyBridges_Cave_Ground, count = 1, probability = 0.1f },
                new LootDistributionData.BiomeData(){ biome = BiomeType.TwistyBridges_Deep_ThermalVentArea_Ground, count = 1, probability = 0.1f }
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (processedPrefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.SeaglideFragment);
                yield return task;

                GameObject prefab = GameObject.Instantiate(task.GetResult());
                prefab.SetActive(false);

                MeshRenderer[] meshRenderers = prefab.GetAllComponentsInChildren<MeshRenderer>();
                SkinnedMeshRenderer[] skinnedMeshRenderers = prefab.GetAllComponentsInChildren<SkinnedMeshRenderer>();
                Color powerGlideColour = new Color(PowerglideColourR, PowerglideColourG, PowerglideColourB);

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
            }

            gameObject.Set(processedPrefab);
        }
    }
}
