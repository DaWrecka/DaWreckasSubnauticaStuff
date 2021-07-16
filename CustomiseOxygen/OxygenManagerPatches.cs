using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace CustomiseOxygen
{
    [HarmonyPatch(typeof(OxygenManager))]
    internal static class OxygenManagerPatches
    {
        private static readonly FieldInfo sourcesInfo = typeof(OxygenManager).GetField("sources", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPrefix]
        [HarmonyPatch(nameof(OxygenManager.AddOxygen))]
        public static bool Prefix(ref OxygenManager __instance, float __result, float secondsToAdd)
        {
            if (Main.config.bManualRefill)
            {
                if (sourcesInfo == null)
                    return true; // Fail safe

#if SUBNAUTICA_STABLE
                List<Oxygen> oxySources = (List<Oxygen>)sourcesInfo.GetValue(__instance);
#elif BELOWZERO
                List<IOxygenSource> oxySources = (List<IOxygenSource>)sourcesInfo.GetValue(__instance);
#endif
                if (oxySources == null)
                    return true;

                float O2added = 0f;
                for (int i = 0; i < oxySources.Count; i++)
                {
#if SUBNAUTICA_STABLE
                    if (!oxySources[i].isPlayer)
#elif BELOWZERO
                    if (!oxySources[i].IsPlayer())
#endif
                        continue;

                    float num = oxySources[i].AddOxygen(secondsToAdd);
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

            return true;
        }
    }
}
