using CombinedItems;
using CombinedItems.VehicleModules;
using CombinedItems.MonoBehaviours;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using QModManager.Utility;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace CombinedItems.Patches
{
    [HarmonyPatch(typeof(Hoverbike))]
    public class HoverbikePatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PostStart(Hoverbike __instance)
        {
            HoverbikeUpdater component = __instance.gameObject.EnsureComponent<HoverbikeUpdater>();
            component.Initialise(ref __instance);
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void PreUpdate(Hoverbike __instance)
        {
            __instance.gameObject.EnsureComponent<HoverbikeUpdater>()?.PreUpdate(__instance);
        }

        [HarmonyPatch("OnUpgradeModuleChange", new Type[] { typeof(int), typeof(TechType), typeof(bool) })]
        [HarmonyPostfix]
        public static void PostUpgradeModuleChange(Hoverbike __instance, int slotID, TechType techType, bool added)
        {
            __instance.gameObject.EnsureComponent<HoverbikeUpdater>()?.PostUpgradeModuleChange(slotID, techType, added, __instance);
        }

        [HarmonyPatch("PhysicsMove")]
        [HarmonyPrefix]
        public static void PrePhysicsMove(Hoverbike __instance)
        {
            __instance.gameObject.EnsureComponent<HoverbikeUpdater>()?.PrePhysicsMove(__instance);
        }
    }
}
