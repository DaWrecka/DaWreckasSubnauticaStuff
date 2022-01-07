using HarmonyLib;
using System;

namespace PrawnSuitArmSwitcher
{
#pragma warning disable IDE1006 // Naming Styles, because the name based on a subnautica class
    public static class uGUI_QuickSlots_HarmonyPatch
#pragma warning restore IDE1006 // Naming Styles
    {
        public static IQuickSlots QuickSlotsUpdateTarget = null;

        public static void Postfix(uGUI_QuickSlots __instance)
        {
            if (QuickSlotsUpdateTarget == null || __instance == null)
                return;

            try
            {
                AccessTools
                    .Method(typeof(uGUI_QuickSlots), "Init", new Type[] { typeof(IQuickSlots) })
                    .Invoke(__instance, new object[] { QuickSlotsUpdateTarget });
            }
            catch (Exception ex)
            {
                Logger.Log("Exception during uGUI_QuickSlots_HarmonyPatch.PostFix");
                throw ex;
            }
            finally
            {
                QuickSlotsUpdateTarget = null;
            }
        }
    }
}
