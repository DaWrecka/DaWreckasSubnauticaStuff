using DWEquipmentBonanza.MonoBehaviours;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWEquipmentBonanza.Patches
{
    [HarmonyPatch(typeof(HoverbikeHUD))]
    public class HoverbikeHUDPatches
    {
        public static bool bHasUpdater { get; private set; }

        [HarmonyPatch(nameof(HoverbikeHUD.Update))]
        [HarmonyPrefix]
        public static bool PreUpdate(HoverbikeHUD __instance)
        {
            HoverbikeUpdater updater = __instance.hoverbike.GetComponentInParent<HoverbikeUpdater>();
            bHasUpdater = updater != null;
            if (!bHasUpdater)
                return true;

            return updater.HUDUpdate(__instance);
        }

    }
}
