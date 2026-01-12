using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelCells.Patches
{
#if BELOWZERO
	[HarmonyPatch(typeof(TechTypeExtensions))]
	public static class TechTypeExtensionsPatches
	{
		[HarmonyPatch(nameof(TechTypeExtensions.IsObsolete))]
		[HarmonyPrefix]
		[HarmonyPriority(Priority.First)]
		public static bool PreIsObsolete(ref bool __result)
		{
			__result = false;
			return false;
		}
	}
#endif
}
