using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombinedItems.Patches
{
	[HarmonyPatch]
	internal static class InventoryPatches
	{
		private static TechType cachedBatteryType; // Filled in by ConsumeResourcesPrefix
		private static float cachedBatteryCharge; // Filled in by ConsumeResourcesPostfix
		private static HashSet<TechType> chipTechTypes = new HashSet<TechType>(); // HashSet of the TechTypes of all tiers of Diver Perimeter Defence Chips
		private static HashSet<TechType> chipRecharges = new HashSet<TechType>();
		private static float lastChipCharge; // Charge of the battery in the last-consumed chip.
		public static bool IsChip(TechType tt) => chipTechTypes.Contains(tt);
		public static bool IsChipRecharge(TechType tt) => chipRecharges.Contains(tt);
		internal static void AddChip(TechType newChip)
		{
			if (chipTechTypes.Contains(newChip))
				return;

			chipTechTypes.Add(newChip);
		}

		internal static void AddChipRecharge(TechType techType)
		{
			if (!chipRecharges.Contains(techType))
				chipRecharges.Add(techType);
		}

		internal static TechType GetCachedBattery()
		{
			return cachedBatteryType;
		}

		internal static float GetCachedCharge(bool bLastChip = false)
		{
			if (bLastChip)
				return lastChipCharge;

			return cachedBatteryCharge;
		}

		internal static void ResetBatteryCache()
		{
			cachedBatteryCharge = 0f;
			cachedBatteryType = TechType.None;
			lastChipCharge = 0f;
		}

		/*
		[HarmonyPatch(typeof(Inventory), nameof(Inventory.ConsumeResourcesForRecipe))]
		[HarmonyPrefix]
		internal static void ConsumeResourcesPrefix(TechType techType, uGUI_IconNotifier.AnimationDone endFunc = null)
		{
			Log.LogDebug($"InventoryPatches.ConsumeResourcesPrefix: techType = {techType.AsString()}");
			if (Main.compatibleBatteries.Contains(techType))
			{
				Log.LogDebug($"InventoryPatches.ConsumeResourcesPrefix: battery TechType is being consumed, caching TechType");
				cachedBatteryType = techType;
			}
		}

		[HarmonyPatch(typeof(Inventory), nameof(Inventory.ConsumeResourcesForRecipe))]
		[HarmonyPostfix]
		internal static void ConsumeResourcesPostfix(Inventory __instance, TechType techType, uGUI_IconNotifier.AnimationDone endFunc = null)
		{
			float lastRemovedBatteryCharge = __instance?.container == null ? -1f : __instance.container.lastRemovedBatteryCharge;
			if (lastRemovedBatteryCharge > 1f)
			{
				bool bIsChip = chipTechTypes.Contains(techType);
				Log.LogDebug($"InventoryPatches.ConsumeResourcesPostfix: found lastRemovedBatteryCharge of {lastRemovedBatteryCharge} and bIsChip: {bIsChip}");
				if (bIsChip)
					lastChipCharge = lastRemovedBatteryCharge;
				else
					cachedBatteryCharge = lastRemovedBatteryCharge;
			}
		}
		*/

		[HarmonyPatch(typeof(ItemsContainer), nameof(ItemsContainer.RemoveItem), new Type[] { typeof(TechType) })]
		[HarmonyPrefix]
		internal static void PreRemoveItem(ItemsContainer __instance, TechType techType)
		{
			Log.LogDebug($"InventoryPatches.PreRemoveItem: techType = {techType.AsString()}");
			if (Main.compatibleBatteries.Contains(techType))
			{
				Log.LogDebug($"InventoryPatches.RemoveItemPrefix: battery TechType is being consumed, caching TechType");
				cachedBatteryType = techType;
			}
		}

		[HarmonyPatch(typeof(ItemsContainer), nameof(ItemsContainer.RemoveItem), new Type[] { typeof(TechType) })]
		[HarmonyPostfix]
		internal static void PostRemoveItem(ItemsContainer __instance, TechType techType)
		{
			float lastRemovedBatteryCharge = __instance == null ? -1f : __instance.lastRemovedBatteryCharge;
			bool bIsChip = chipTechTypes.Contains(techType);
			Log.LogDebug($"InventoryPatches.PostRemoveItem: found lastRemovedBatteryCharge of {lastRemovedBatteryCharge} and bIsChip: {bIsChip}");
			if (lastRemovedBatteryCharge > 1f)
			{
				if (bIsChip)
					lastChipCharge = lastRemovedBatteryCharge;
				else
					cachedBatteryCharge = lastRemovedBatteryCharge;
			}
		}

	}
}
