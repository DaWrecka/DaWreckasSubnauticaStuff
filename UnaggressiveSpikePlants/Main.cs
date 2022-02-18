using HarmonyLib;
using QModManager.API.ModLoading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UnaggressiveSpikePlants
{
    [QModCore]
    public static class Main
    {
        [QModPatch]
        public static void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }

    [HarmonyPatch]
    public static class RangeTargeterPatch
    {
        [HarmonyPatch(typeof(RangeTargeter), nameof(RangeTargeter.IsTargetValid))]
        [HarmonyPostfix]
        public static void PostIsTargetValid(RangeTargeter __instance, ref bool __result, IEcoTarget ecoTarget)
        {
            if (__instance?.gameObject?.GetComponent<SpikePlant>() != null && ecoTarget?.GetGameObject()?.GetComponent<Player>() != null)
                __result = false;
        }
    }
}
