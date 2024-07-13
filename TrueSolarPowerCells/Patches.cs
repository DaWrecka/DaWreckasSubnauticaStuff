using Common;
using HarmonyLib;

namespace TrueSolarPowerCells
{
    [HarmonyPatch]
    internal class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSource), "Start")]
        internal static void PrePowerSourceStart(PowerSource __instance)
        {
            Log.LogDebug($"Checking gameObject {__instance.gameObject.GetInstanceID()}");
            if (__instance.gameObject != null && __instance.gameObject.TryGetComponent<RegeneratePowerSource>(out RegeneratePowerSource component))
            {
                Log.LogDebug($"gameObject has a RegeneratePowerSource");
                SolarRegeneratePowerSource realSolarPower = __instance.gameObject.EnsureComponent<SolarRegeneratePowerSource>();
                realSolarPower.powerSource = __instance;
                component.regenerationAmount = 0f;
                component.regenerationThreshhold = TSPCPlugin.config.regenerationThreshold;
            }
            else
            {
                //Log.LogDebug($"gameObject has no RegeneratePowerSource");
            }
        }
    }
}
