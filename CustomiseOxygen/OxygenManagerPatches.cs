using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace CustomiseOxygen
{
    [HarmonyPatch(typeof(OxygenManager), nameof(OxygenManager.AddOxygen))]
    internal static class OxygenManagerPatches
    {
        private static readonly FieldInfo sourcesInfo = typeof(OxygenManager).GetField("sources", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPrefix]
        public static bool Prefix(ref OxygenManager __instance, float __result, float secondsToAdd)
        {
            if (Main.config.bAllowAutoRefill)
                return true;

            if (sourcesInfo == null)
                return true; // Fail safe

            List<IOxygenSource> oxySources = (List<IOxygenSource>)sourcesInfo.GetValue(__instance);
            if (oxySources == null)
                return true;

            float O2added = 0f;
            for (int i = 0; i < oxySources.Count; i++)
            {
                if (!oxySources[i].IsPlayer())
                    continue;

                float num = oxySources[0].AddOxygen(secondsToAdd);
                secondsToAdd -= num;
                O2added += num;
                if (Utils.NearlyEqual(secondsToAdd, 0f, 1.401298E-45f))
                {
                    break;
                }
            }
            __result = O2added;

            return false;
        }
    }
}
