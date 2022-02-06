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

			for (int i = 0; i < types.Count; i++)
			{
				TechType tt = types[i];
				string classid = CraftData.GetClassIdForTechType(tt);
				if (String.IsNullOrEmpty(classid))
				{
					Log.LogDebug($"PostMenuCoroutine(): Could not get classId for TechType {tt.AsString()}");
				}
				else if (WorldEntityDatabase.TryGetInfo(classid, out var worldEntityInfo))
				{
					worldEntityInfo.cellLevel = LargeWorldEntity.CellLevel.VeryFar;

					WorldEntityDatabase.main.infos[classid] = worldEntityInfo;
				}

				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(tt);
				yield return task;

				GameObject prefab = task.GetResult();
				if (prefab != null)
				{
					LargeWorldEntity lwe = prefab.GetComponent<LargeWorldEntity>();
					if (lwe != null)
					{
						lwe.cellLevel = LargeWorldEntity.CellLevel.VeryFar;
						Log.LogDebug($"PostMenuCoroutine(): CellLevel for TechType {tt.AsString()} updated to Far");
					}
					else
					{
						Log.LogWarning($"PostMenuCoroutine(): Could not find LargeWorldEntity component in prefab for TechType {tt.AsString()}");
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
					Log.LogWarning($"PostMenuCoroutine(): Could not get prefab for TechType {tt.AsString()}");
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
						Main.defaultHealth.Add(tt, mixin.data.maxHealth);
						Log.LogDebug($"PostMenuCoroutine(): For TechType {tt.AsString()}, got default health of {mixin.data.maxHealth}");
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

			yield break;
		}

		internal static IEnumerator ProcessPrefabCoroutine()
		{
			yield break;
		}

	}
}
