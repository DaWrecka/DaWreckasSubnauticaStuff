using HarmonyLib;
using Main = HabitatBuilderSpeed.BuilderSpeedPlugin;

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
			//Log.LogDebug($"Patching GetConstructInterval using multiplier of {multiplier}, with result of {__result}");
		}
	}
}
