using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SkyApplierWorkaround
{
	[HarmonyPatch(typeof(SkyApplier))]
	public class SkyApplierPatches
	{
		[HarmonyPatch(nameof(SkyApplier.HasMoved))]
		[HarmonyPrefix]
		public static bool PreHasMoved(SkyApplier __instance)
		{
			if(__instance == null)
				return false;

			if (__instance.gameObject == null)
			{
				Log.LogWarning($"Object {__instance.gameObject.name} has faulty SkyApplier! gameObject == null");
				return false;
			}

			if (__instance.transform == null)
			{
				Log.LogWarning($"Object {__instance.gameObject.name} has faulty SkyApplier! transform == null");
				return false;
			}

			if (__instance.applyPosition == Vector3.zero)
			{
				__instance.applyPosition = __instance.transform.position;
			}

			return true;
		}
	}
}
