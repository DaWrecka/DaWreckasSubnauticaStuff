using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace DWEquipmentBonanza.Patches
{
	[HarmonyPatch(typeof(Plantable))]
	internal class Plantables
	{
		/*[HarmonyPatch(nameof(Plantable.Spawn))]
		[HarmonyPrefix]
		public static bool Prefix(ref Plantable __instance, ref GameObject __result)
		{
			Common.Log.LogDebug($"Patches.Plantables.Postfix running for Plantable with plantTechType '{__instance.plantTechType.AsString()}'");
			if (__instance.plantTechType == TechType.SnowStalkerPlant)
				__instance.size = Plantable.PlantSize.Large;

			return true;
		}*/
	}

	[HarmonyPatch(typeof(Planter))]
	internal class Planters
	{
		/*
		[HarmonyPrefix]
		[HarmonyPatch("IsAllowedToAdd")]
		public static void IsAllowedToAdd(Pickupable pickupable, bool verbose)
		{
			Plantable component = pickupable.GetComponent<Plantable>();
			if (component != null)
			{
				if (component.plantTechType == TechType.SnowStalkerPlant)
					component.size = Plantable.PlantSize.Large;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch("AddItem", new Type[] { typeof(InventoryItem) })]
		public static void AddItem(InventoryItem item)
		{
			Plantable component = item.item.GetComponent<Plantable>();
			if (component != null && component.plantTechType == TechType.SnowStalkerPlant)
				component.size = Plantable.PlantSize.Large;
		}
		*/
	}
}
