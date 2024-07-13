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
            LargeWorldEntity.CellLevel target = LargeWorldEntity.CellLevel.VeryFar;

            //if (__instance.resources.Length < 1)
            //    return;

            if (__instance.gameObject.GetComponent<FloatersTarget>() != null)
            {
                target = LargeWorldEntity.CellLevel.Medium;
                return;
            }
            else if (__instance.resources.Length < 1)
                return;

            Log.LogDebug($"Found Drillable {__instance.name}");
            if (__instance.gameObject.TryGetComponent<LargeWorldEntity>(out LargeWorldEntity lwe))
            {
                lwe.cellLevel = target; 
            }
#if LEGACY && SN1
            __instance.kChanceToSpawnResources = Mathf.Max(DWConstants.newKyaniteChance, __instance.kChanceToSpawnResources);
#endif
        }
    }
}
