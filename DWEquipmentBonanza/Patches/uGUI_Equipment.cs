using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWEquipmentBonanza.Patches
{
    [HarmonyPatch(typeof(uGUI_Equipment))]
    internal class uGUI_EquipmentPatches
    {
        // Based pretty much entirely on DecorationsMod.uGUI_EquipmentFixer
        [HarmonyPrefix]
        [HarmonyPatch(nameof(uGUI_Equipment.CanSwitchOrSwap))]
        internal static bool PreCanSwitchOrSwap(uGUI_Equipment __instance, ref ItemAction __result, string slotB)
        {
            //Log.LogDebug($"uGUI_EquipmentPatches.PreCanSwitchOrSwap(): __result = {__result.ToString()}, slotB = {slotB}");

            if (!ItemDragManager.isDragging)
                return true;
            InventoryItem draggedItem = ItemDragManager.draggedItem;
            if (draggedItem == null)
                return true;
            Pickupable item = draggedItem.item;
            if (item == null)
                return true;
            TechType techType = item.GetTechType();
            if (InventoryPatches.IsChip(techType))
            {
                if (Equipment.GetSlotType(slotB) == EquipmentType.BatteryCharger)
                {
                    Equipment equipmentValue = __instance.equipment;
                    InventoryItem itemInSlot = equipmentValue.GetItemInSlot(slotB);
                    if (itemInSlot == null)
                    {
#if DEBUG_PLACE_TOOL
                        Logger.Log("DEBUG: CanSwitchOrSwap returns SWITCH battery for " + techType.AsString(false));
#endif
                        __result = ItemAction.Switch;
                        return false;
                    }
                    if (Inventory.CanSwap(draggedItem, itemInSlot))
                    {
#if DEBUG_PLACE_TOOL
                        Logger.Log("DEBUG: CanSwitchOrSwap returns SWAP battery for " + techType.AsString(false));
#endif
                        __result = ItemAction.Swap;
                        return false;
                    }
#if DEBUG_PLACE_TOOL
                    Logger.Log("DEBUG: CanSwitchOrSwap returns NONE battery for " + techType.AsString(false));
#endif
                    __result = ItemAction.None;
                    return false;
                }
            }

            return true;
        }
    }
}
