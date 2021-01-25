using HarmonyLib;
using SMLHelper.V2.Handlers;
using Logger = QModManager.Utility.Logger;

namespace HabitatBuilderSpeed.Patches
{
    [HarmonyPatch(typeof(Constructable), "GetConstructInterval")]
    internal class Constructable_GetConstructInterval
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result)
        {
            if (__result >= 1f)
                __result *= Main.config.builderMultiplier;
            //Logger.Log(Logger.Level.Debug, $"Patching GetConstructInterval using multiplier of {multiplier}, with result of {__result}");
        }
	}
}
