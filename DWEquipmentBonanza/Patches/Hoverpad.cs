using DWEquipmentBonanza.MonoBehaviours;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWEquipmentBonanza.Patches
{
#if BELOWZERO
    [HarmonyPatch(typeof(Hoverpad))]
    public class HoverpadPatches
    {
        [HarmonyPatch(nameof(Hoverpad.Start))]
        [HarmonyPostfix]
        public static void PostStart(Hoverpad __instance)
        {
            __instance.gameObject.EnsureComponent<HoverpadUpdater>();
        }
    }
#endif
}
