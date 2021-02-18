using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombinedItems.Patches
{
    class VehiclePatches
    {
        protected static float defaultForwardForce;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vehicle.Start))]
        public static void PostStart(Vehicle __instance)
        {
            if (__instance is Exosuit)
            {
                defaultForwardForce = __instance.forwardForce;
                Log.LogDebug($"VehiclePatches.Start(): Found Exosuit with forwardForce of {defaultForwardForce}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("ApplyPhysicsMove")]
        public static void PreApplyPhysicsMove(ref Vehicle __instance)
        {
            if (__instance is Exosuit exosuit)
            {
                bool bExosuitSprint = (exosuit.modules.GetCount(Main.prefabExosuitSprintModule.TechType) > 0 && GameInput.GetButtonDown(GameInput.Button.Sprint));
                exosuit.forwardForce = defaultForwardForce * (bExosuitSprint ? 2f : 1f); // These constants will likely be tweaked, but they're here for testing
                Log.LogDebug($"VehiclePatches.PreApplyPhysicsMove(): Applying forwardForce of {exosuit.forwardForce} to Exosuit with defaultForwardForce of {defaultForwardForce}");
            }
        }
    }
}
