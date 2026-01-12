using BepInEx;
using HarmonyLib;
#if QMM
using QModManager.API;
using QModManager.API.ModLoading;
#endif

using System.Reflection;
using UnityEngine;

namespace UnaggressiveFlora
{
#if BEPINEX
	[BepInPlugin(GUID, pluginName, version)]

#if BELOWZERO
	[BepInProcess("SubnauticaZero.exe")]
#elif SN1
	[BepInProcess("Subnautica.exe")]
#endif
	public class UnaggressiveFlora : BaseUnityPlugin
	{
#elif QMM
	[QModCore]
	public static class UnaggressiveFlora
	{
#endif
		public const string
			MODNAME = "UnaggressiveFlora",
			AUTHOR = "dawrecka",
			GUID = "com." + AUTHOR + "." + MODNAME;
		private const string pluginName = "UnaggressiveFlora";
		public const string version = "1.22.0.0";
		public void Awake()
		{
#if QMM
			if (QModServices.Main.ModPresent("UnaggressiveSpikePlants"))
			{
				throw new Exception("UnaggressiveFlora is a replacement for UnaggressiveSpikePlants, please remove UnaggressiveSpikePlants from your QMods directory");
			}
#endif

			var assembly = Assembly.GetExecutingAssembly();
			new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
		}
	}

	[HarmonyPatch]
	public static class FloraPatches
	{
#if SN1
		[HarmonyPatch(typeof(RangeTargeter), nameof(RangeTargeter.IsTargetValid))]
		[HarmonyPrefix]
		public static bool PostIsTargetValid(RangeTargeter __instance, ref bool __result, IEcoTarget ecoTarget)
		{
			// Not every RangeTargeter is on a SpikePlant, and we can't patch SpikePlant directly because it literally has no code of its own.
			// So, we need to check whether the thing doing the targeting it a SpikePlant before we negate the check.
			if (__instance?.gameObject?.GetComponent<SpikePlant>() != null && ecoTarget?.GetGameObject()?.GetComponent<Player>() != null)
			{
				__result = false;
				return false;
			}

			return true;
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
