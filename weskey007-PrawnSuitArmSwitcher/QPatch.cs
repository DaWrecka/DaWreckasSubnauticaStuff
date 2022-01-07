// Prawn Suit Arm Switcher, by weskey007
// Modified to be buildable for BelowZero
// also buildable for SN1, but it was easier to make this specific copy of the project work with both configurations than make it only work with BZ

using HarmonyLib;
using QModManager.API.ModLoading;

namespace PrawnSuitArmSwitcher
{
    [QModCore]
    public static class QPatch
    {
        [QModPatch]
        public static void Patch()
        {
            Logger.Log("Patching...");
            var harmony = new Harmony("weskey.subnautica.showavailableitems.mod");

            var exosuitSlotKeyDown_Original = AccessTools.Method(typeof(Exosuit), "SlotKeyDown");
            var exosuitSlotKeyDown_Prefix = AccessTools.Method(typeof(ExosuitSlotKeyDown_HarmonyPatch), "Prefix");

            var uGUI_QuickSlots_Original = AccessTools.Method(typeof(uGUI_QuickSlots), "Update");
            var uGUI_QuickSlots_Postfix = AccessTools.Method(typeof(uGUI_QuickSlots_HarmonyPatch), "Postfix");


            harmony.Patch(exosuitSlotKeyDown_Original, new HarmonyMethod(exosuitSlotKeyDown_Prefix), null);
            harmony.Patch(uGUI_QuickSlots_Original, null, new HarmonyMethod(uGUI_QuickSlots_Postfix));

            Logger.Log("Patching complete");
        }
    }
}
