using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using QModManager.API.ModLoading;
//using SMLHelper.V2.Crafting;
using System.Reflection;
#if SUBNAUTICA_STABLE
#elif BELOWZERO || SUBNAUTICA_EXP
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
#endif
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace NoTriggerFullPlanters
{
    [QModCore]
    public class Main
    {
        [QModPatch]
        public static void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }

    [HarmonyPatch(typeof(StorageContainer))]
    public static class StorageContainerPatches
    {
        [HarmonyPatch(nameof(StorageContainer.OnHandClick))]
        [HarmonyPrefix]
        public static bool PreHandClick(StorageContainer __instance)
        {
            if (__instance.gameObject.GetComponent<Planter>() != null)
            {
                return __instance.container != null && __instance.container.HasRoomFor(1, 1);
            }

            return true;
        }

        [HarmonyPatch(nameof(StorageContainer.OnHandHover))]
        [HarmonyPrefix]
        public static bool PreHandHover(StorageContainer __instance, GUIHand hand)
        {
            if (!__instance.enabled)
            {
                return false;
            }

            // I've tried to order these checks in order from least-expensive to most-expensive, as I'm not sure whether this method is executed just once, when the hand first hovers over the planter,
            // or every frame.
            if (__instance.container == null)
                return true;

            if (__instance.container.HasRoomFor(1, 1))
                return true;

            Constructable component = __instance.gameObject.GetComponent<Constructable>();
            if (!component || component.constructed)
            {
                if (__instance.gameObject.GetComponent<Planter>() == null)
                    return true;

                HandReticle.main.SetInteractText(__instance.hoverText, string.Empty);
                HandReticle.main.SetIcon(HandReticle.IconType.HandDeny, 1f);
            }

            return false;
        }
    }
}