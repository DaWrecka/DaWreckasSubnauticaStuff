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
		private static HashSet<TechType> chipRecharges = new HashSet<TechType>(); // Hashset of TechTypes which are recharge recipes, and not actually chips themselves.
		private static HashSet<TechType> chipRechargeables = new HashSet<TechType>(); // HashSet of the chips which can be recharged. At present, only one such chip is planned, but this allows for expansion
		private static float lastChipCharge; // Charge of the battery in the last-consumed chip.
		public static bool IsChip(TechType tt) => chipTechTypes.Contains(tt);
		public static bool IsChipRecharge(TechType tt) => chipRecharges.Contains(tt);
		public static bool IsRechargeableChip(TechType tt) => chipRechargeables.Contains(tt);
		internal static void AddChip(TechType newChip, bool bRechargeable = false)
		{
			if (bRechargeable)
			{
				if (!chipRechargeables.Contains(newChip))
					chipRechargeables.Add(newChip);
			}

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

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddOrSwap), new Type[] { typeof(InventoryItem), typeof(Equipment), typeof(string) })]
		public static bool PreAddOrSwap(ref bool __result, InventoryItem itemA, Equipment equipmentB, string slotB)
		{
			if (itemA == null || !itemA.CanDrag(true) || equipmentB == null)
				return true;
			Pickupable item = itemA.item;
			if (item == null)
				return true;
			TechType techType = item.GetTechType();
			bool isBattery = IsChip(techType);
			
			if (isBattery)
			{
				IItemsContainer container = itemA.container;
				if (container == null)
					return true;
				Equipment equipment = container as Equipment;
				bool flag = equipment != null;
				string empty = string.Empty;
				if (flag && !equipment.GetItemSlot(item, ref empty))
					return true;
				EquipmentType equipmentType = EquipmentType.BatteryCharger;
				if (string.IsNullOrEmpty(slotB))
					equipmentB.GetCompatibleSlot(equipmentType, out slotB);
				if (string.IsNullOrEmpty(slotB))
					return true;
				if (container == equipmentB && empty == slotB)
					return true;
				EquipmentType slotBType = Equipment.GetSlotType(slotB);
				if (slotBType != EquipmentType.BatteryCharger)
					return true;
				else // Else, we're trying to plug a battery or powercell to its charger
				{
					InventoryItem inventoryItem = equipmentB.RemoveItem(slotB, false, true);
					if (inventoryItem == null)
					{
						if (equipmentB.AddItem(slotB, itemA, false))
						{
							__result = true;
							return false;
						}
					}
					else if (equipmentB.AddItem(slotB, itemA, false))
					{
						if ((flag && equipment.AddItem(empty, inventoryItem, false)) || (!flag && container.AddItem(inventoryItem)))
						{
							__result = true;
							return false;
						}
						if (flag)
							equipment.AddItem(empty, itemA, true);
						else
							container.AddItem(itemA);
						equipmentB.AddItem(slotB, inventoryItem, true);
					}
					else
						equipmentB.AddItem(slotB, inventoryItem, true);
					__result = false;
					return false;
				}
			}
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddOrSwap), new Type[] { typeof(InventoryItem), typeof(Equipment), typeof(string) })]
		internal static void PostAddOrSwap(InventoryItem itemA, Equipment equipmentB, string slotB, Inventory __instance, ref bool __result)
		{
			//Log.LogDebug($"InventoryPatches.PostAddOrSwap(): itemA = {itemA.ToString()}, equipmentB = {equipmentB.ToString()}, equipmentB._label = {equipmentB._label}, slotB = {slotB}, __result = {__result}");
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddOrSwap), new Type[] { typeof(InventoryItem), typeof(IItemsContainer) })]
		internal static void PostAddOrSwap(InventoryItem itemA, IItemsContainer containerB, Inventory __instance, ref bool __result)
		{
			//Log.LogDebug($"InventoryPatches.PostAddOrSwap(): itemA = {itemA.ToString()}, containerB = {containerB.ToString()}, containerB.label = {containerB.label}, __result = {__result}");
		}
	}
}
