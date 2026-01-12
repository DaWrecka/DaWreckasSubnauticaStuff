using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace Stacking.Patches
{
	[HarmonyPatch(typeof(ItemsContainer))]
	public class ItemsContainerPatches
	{
		[HarmonyPatch(nameof(ItemsContainer.RemoveItem), new Type[] { typeof(Pickupable), typeof(bool) })]
		[HarmonyPrefix]

		/*
	public bool RemoveItem(Pickupable pickupable, bool forced = false)
	{
		if (!forced && !((IItemsContainer)this).AllowedToRemove(pickupable, true))
		{
			return false;
		}
		TechType techType = pickupable.GetTechType();
		ItemsContainer.ItemGroup itemGroup;
		if (this._items.TryGetValue(techType, out itemGroup))
		{
			List<InventoryItem> items = itemGroup.items;
			for (int i = 0; i < items.Count; i++)
			{
				InventoryItem inventoryItem = items[i];
				if (inventoryItem.item == pickupable)
				{
					items.RemoveAt(i);
					if (items.Count == 0)
					{
						this._items.Remove(techType);
					}
					inventoryItem.container = null;
					pickupable.onTechTypeChanged -= this.UpdateItemTechType;
					int count = this.count;
					this.count = count - 1;
					this.unsorted = true;
					this.NotifyRemoveItem(inventoryItem);
					return true;
				}
			}
		}
		return false;
	}

		 */
		public static bool PreRemoveItem(ItemsContainer __instance, bool __result, Pickupable pickupable, bool forced = false)
		{
			Stackable stacker = null;

			if (!pickupable.gameObject.TryGetComponent<Stackable>(out _))
				return true; // If the pickupable doesn't have a Stackable component that means it's not stackable, so we really don't have to do anything fancy, and can let the basic code do its thing

			if (!forced && !((IItemsContainer)__instance).AllowedToRemove(pickupable, true))
			{
				__result = false;
				return false;
			}
			TechType techType = pickupable.GetTechType();
			ItemsContainer.ItemGroup itemGroup;
			if (__instance._items.TryGetValue(techType, out itemGroup))
			{
				List<InventoryItem> items = itemGroup.items;
				for (int i = 0; i < items.Count; i++)
				{
					InventoryItem inventoryItem = items[i];
					if (inventoryItem.item == pickupable)
					{
						items.RemoveAt(i);
						if (items.Count == 0)
						{
							__instance._items.Remove(techType);
						}
						inventoryItem.container = null;
						pickupable.onTechTypeChanged -= __instance.UpdateItemTechType;
						int count = __instance.count;
						__instance.count = count - 1;
						__instance.unsorted = true;
						__instance.NotifyRemoveItem(inventoryItem);
						__result = true;
						return false;
					}
				}
			}

			__result = true;
			return false;
		}
	}
}
