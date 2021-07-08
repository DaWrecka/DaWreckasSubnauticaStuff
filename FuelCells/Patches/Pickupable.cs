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
#if SUBNAUTICA_STABLE
            Battery b = __instance.gameObject?.GetComponent<Battery>();
            if(b != null)
#elif BELOWZERO
            if (__instance.gameObject != null && __instance.gameObject.TryGetComponent<Battery>(out Battery b))
#endif
                Batteries.AddPendingBattery(ref b);
        }
    }
}
