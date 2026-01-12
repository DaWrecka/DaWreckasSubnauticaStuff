using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stacking.Patches
{
	[HarmonyPatch(typeof(InventoryItem))]
	internal class PickupablePatches
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(Pickupable))]
		public static void PostConstructor(Pickupable __instance)
		{
			TechType itemType = __instance.GetTechType();
			if(StackablesPlugin.blacklist.Contains(itemType))
				return;

			if (StackablesPlugin.GetStackLimit(itemType) < 2)
				return;

			__instance.gameObject.EnsureComponent<Stackable>();
		}

	}
}
