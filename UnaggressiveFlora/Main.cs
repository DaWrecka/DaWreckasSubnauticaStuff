using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnaggressiveFlora
{
    [QModCore]
    public static class Main
    {
        [QModPatch]
        public static void Load()
        {
            if (QModServices.Main.ModPresent("UnaggressiveSpikePlants"))
            {
                throw new Exception("UnaggressiveFlora is a replacement for UnaggressiveSpikePlants, please remove UnaggressiveSpikePlants from your QMods directory");
            }

            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }

    [HarmonyPatch]
    public static class FloraPatches
    {
#if SN1
        [HarmonyPatch(typeof(RangeTargeter), nameof(RangeTargeter.IsTargetValid))]
        [HarmonyPostfix]
        public static void PostIsTargetValid(RangeTargeter __instance, ref bool __result, IEcoTarget ecoTarget)
        {
            // Not every RangeTargeter is on a SpikePlant, and we can't patch SpikePlant directly because it literally has no code of its own.
            // So, we need to check whether the thing doing the targeting it a SpikePlant before we negate the check.
            if (__instance?.gameObject?.GetComponent<SpikePlant>() != null && ecoTarget?.GetGameObject()?.GetComponent<Player>() != null)
                __result = false;
        }

#elif BELOWZERO
        [HarmonyPatch(typeof(SpikeyTrap), "IsValidTarget")]
        [HarmonyPrefix]
        public static bool PreIsTargetValid(SpikeyTrap __instance, ref bool __result, GameObject target)
        {
            if (target?.GetComponent<Player>() != null)
            {
                __result = false;
                return false;
            }

            return true;
        }
#endif
    }
}
