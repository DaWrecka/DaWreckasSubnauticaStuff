using Common;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace DWEquipmentBonanza.Patches
{
    [HarmonyPatch(typeof(uGUI_MainMenu))]
    public class uGUI_MainMenuPatches
    {
        [HarmonyPatch(nameof(uGUI_MainMenu.Start))]
        [HarmonyPostfix]
        public static void PostStart()
        {
			CoroutineHost.StartCoroutine(PostMenuCoroutine());
        }

		internal static IEnumerator PostMenuCoroutine()
		{
			var types = new List<TechType>()
			{
				TechType.DrillableAluminiumOxide,
				TechType.DrillableCopper,
				TechType.DrillableDiamond,
				TechType.DrillableGold,
				TechType.DrillableKyanite,
				TechType.DrillableLead,
				TechType.DrillableLithium,
				TechType.DrillableMagnetite,
				TechType.DrillableMercury,
				TechType.DrillableNickel,
				TechType.DrillableQuartz,
				TechType.DrillableSalt,
				TechType.DrillableSilver,
				TechType.DrillableSulphur,
				TechType.DrillableTitanium,
				TechType.DrillableUranium
			};
			var classIDs = new Dictionary<string, string>() // The keys in this dictionary are used only to allow the classID to be given some human-readable identifier.
			// These first two classIDs are special - Kyanite is the only drillable with two different prefabs
			{
				{ "DrillableKyanite", "4f441e53-7a9a-44dc-83a4-b1791dc88ffd" },
				{ "DrillableKyanite_Large", "853a9c5b-aba3-4d6b-a547-34553aa73fa9" },
				{ "DrillableSulphur", "697beac5-e39a-4809-854d-9163da9f997e" }
			};

			for (int i = 0; i < types.Count; i++)
			{
				TechType tt = types[i];
				string classid = CraftData.GetClassIdForTechType(tt);
				if (String.IsNullOrEmpty(classid))
				{
					Log.LogDebug($"PostMenuCoroutine(): Could not get classId for TechType {tt.AsString()}");
				}
				else
				{
					Log.LogDebug($"PostMenuCoroutine(): Retrieved classId '{classid}' for TechType {tt.AsString()}");
					classIDs.Add(tt.AsString(), classid);
				}
			}
				
			foreach(KeyValuePair<string, string> kvp in classIDs)
			{
				string classid = kvp.Value;
				string techType = kvp.Key;
				if (WorldEntityDatabase.TryGetInfo(classid, out var worldEntityInfo))
				{
					Log.LogDebug($"PostMenuCoroutine(): Setting WorldEntityDatabase CellLevel to VeryFar for classID '{classid}'");
					worldEntityInfo.cellLevel = LargeWorldEntity.CellLevel.VeryFar;

					WorldEntityDatabase.main.infos[classid] = worldEntityInfo;
				}

				//CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(tt);
				IPrefabRequest request = PrefabDatabase.GetPrefabAsync(classid);
				yield return request;

				if (request.TryGetPrefab(out GameObject prefab))
				{
					LargeWorldEntity lwe = prefab.GetComponent<LargeWorldEntity>();
					if (lwe != null)
					{
						lwe.cellLevel = LargeWorldEntity.CellLevel.VeryFar;
						Log.LogDebug($"PostMenuCoroutine(): CellLevel for object type {techType} updated to VeryFar");
					}
					else
					{
						Log.LogWarning($"PostMenuCoroutine(): Could not find LargeWorldEntity component in prefab for TechType {techType}");
					}
#if SUBNAUTICA_STABLE
					// Since we're here, make kyanite less troll-tastic.
					Drillable drillable = prefab.GetComponent<Drillable>();
					if (drillable != null && drillable.kChanceToSpawnResources < DWConstants.newKyaniteChance)
					{
						drillable.kChanceToSpawnResources = DWConstants.newKyaniteChance;
					}
#endif
				}
				else
				{
					Log.LogWarning($"PostMenuCoroutine(): Could not get prefab for TechType {techType}");
				}
			}

			Log.LogDebug("PostMenuCoroutine(): Processing vehicle defaults");
			types = new List<TechType>() {
#if SUBNAUTICA_STABLE
			TechType.Seamoth,
			TechType.Cyclops,
#elif BELOWZERO
			TechType.SeaTruck,
			TechType.Hoverbike,
#endif
			TechType.Exosuit,
			};

			for (int i = 0; i < types.Count; i++)
			{
				TechType tt = types[i];
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(tt);
				yield return task;

				GameObject prefab = task.GetResult();
				if (prefab != null)
				{
					LiveMixin mixin = prefab.GetComponent<LiveMixin>();
					if (mixin != null && mixin.data != null)
					{
						// This shouldn't be necessary, but sadly indications are to the contrary
						if (Main.defaultHealth.ContainsKey(tt))
							Log.LogWarning($"PostMenuCoroutine(): Default health value already recorded for TechType {tt.AsString()}");
						else
						{
							Main.defaultHealth.Add(tt, mixin.data.maxHealth);
							Log.LogDebug($"PostMenuCoroutine(): For TechType {tt.AsString()}, got default health of {mixin.data.maxHealth}");
						}
					}
					else
					{
						Log.LogDebug($"PostMenuCoroutine(): Failed to get LiveMixin for TechType {tt.AsString()}");
					}
				}
				else
				{
					Log.LogDebug($"PostMenuCoroutine(): Failed to get prefab for TechType {tt.AsString()}");
				}
			}

#if BELOWZERO
			// These are completely unrelated to DWEB at present, but are used for testing and, if successful, might be usable
			foreach (KeyValuePair<string, string> kvp in new Dictionary<string, string>()
			{
				{ "WorldEntities/Environment/Aurora/Aurora_ExtinguishableFire_Small", "14bbf7f0-4276-48bf-868b-317b366edd16" },
				{ "WorldEntities/Environment/Aurora/Aurora_ExtinguishableFire_Medium","3877d31d-37a5-4c94-8eef-881a500c58bc" },
				{ "WorldEntities/Environment/Aurora/Aurora_ExtinguishableFire_Medium_Tall", "afe53ea1-d2a8-4f76-8ffb-d41ff6046b52" }
			})
			{
				Log.LogDebug($"Trying known classID for prefab with SN1 path: '{kvp.Key}'");

				string classid = kvp.Value;
				IPrefabRequest request = PrefabDatabase.GetPrefabAsync(classid);
				yield return request;

				if (request.TryGetPrefab(out GameObject prefab))
				{
					Log.LogDebug($"    Successfully retrieved a prefab for the classID {classid}");
				}
				if (PrefabDatabase.TryGetPrefabFilename(classid, out string filename))
				{
					Log.LogDebug($"    Got a prefab filename of '{filename}' for the classID {classid}");
				}
			}
#endif

			yield break;
		}

		internal static IEnumerator ProcessPrefabCoroutine()
		{
			yield break;
		}

	}
}
