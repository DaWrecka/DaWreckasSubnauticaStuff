using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWE;
using UnityEngine;
using Common;

namespace DWEquipmentBonanza.Patches
{
    [HarmonyPatch(typeof(Drillable))]
    public class DrillablePatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PostStart(Drillable __instance)
        {
            if (__instance.resources.Length < 1)
                return;

            if (__instance.gameObject.GetComponent<FloatersTarget>() != null)
                return;

            Log.LogDebug($"Found Drillable {__instance.name}");
            if (__instance.gameObject.TryGetComponent<LargeWorldEntity>(out LargeWorldEntity lwe))
            {
                lwe.cellLevel = LargeWorldEntity.CellLevel.Far; 
            }
#if SUBNAUTICA_STABLE
            __instance.kChanceToSpawnResources = Mathf.Max(DWConstants.newKyaniteChance, __instance.kChanceToSpawnResources);
#endif
        }
    }
}
