using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelCells.Patches
{
    [HarmonyPatch(typeof(Pickupable))]
    internal class Pickupables
    {
        [HarmonyPatch(nameof(Pickupable.Awake))]
        [HarmonyPostfix]
        internal static void PostAwake(Pickupable __instance)
        {
            if (__instance.gameObject != null && __instance.gameObject.TryGetComponent<Battery>(out Battery b))
                Batteries.AddPendingBattery(ref b);
        }
    }
}
