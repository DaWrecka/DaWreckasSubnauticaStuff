using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.Patches
{
#if BELOWZERO
	[HarmonyPatch]
	public class HorsepowerPatches
	{
		private static Dictionary<TechType, float> speedMultipliers = new();

		public static bool RegisterHorsepowerModifier(TechType module, float value)
		{
			if (speedMultipliers.ContainsKey(module))
				return false;

			speedMultipliers.Add(module, value);
			return true;
		}

		[HarmonyPatch(typeof(SeaTruckSegment))]
		[HarmonyPatch(nameof(SeaTruckSegment.GetAttachedWeight))]
		[HarmonyPostfix]
		public static void PostGetAttachedWeight(SeaTruckSegment __instance, ref float __result)
		{
			if (!__instance.isMainCab)
				return;

			float mult = 1f;
			Equipment modules = (__instance.motor != null && __instance.motor.upgrades != null ? __instance.motor.upgrades.modules : null);
			if(modules != null)
				foreach (var kvp in speedMultipliers)
				{
					if (modules.GetCount(kvp.Key) > 0)
						mult = Mathf.Min(mult, kvp.Value);
				}

			__result *= mult;
		}

		/*public static void PostGetBonusGetSpeedMultiplierBonus(MonoBehaviour __instance, ref float __result, float speedBoosterCount)
		{
			GameObject vehicle = __instance?.gameObject;
			if (vehicle == null)
			{
				Log.LogDebug($"PostGetBonusGetSpeedMultiplierBonus(): Could not find GameObject for component {__instance.name}({__instance.GetInstanceID()})");
				return;
			}

			Equipment modules = vehicle.GetComponent<SeaTruckUpgrades>()?.modules;
			if (modules == null)
			{
				Log.LogDebug($"PostGetBonusGetSpeedMultiplierBonus(): Could not find Equipment component for GameObject {vehicle.name}({vehicle.GetInstanceID()})");
				return;
			}

			float Multiplier = 1f;
			foreach (var kvp in speedMultipliers)
			{
				if (modules.GetCount(kvp.Key) > 0)
				{
					Log.LogDebug($"PostGetBonusGetSpeedMultiplierBonus(): Got multiplier of {kvp.Value} for equipped module {kvp.Key}");
					Multiplier = Mathf.Max(Multiplier, kvp.Value);
				}
			}
			Log.LogDebug($"PostGetBonusGetSpeedMultiplierBonus(): Applying multiplier of {Multiplier} to original result {__result}");
			__result *= Multiplier;
		}*/
	}
#endif
}
