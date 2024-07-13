using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
#if BEPINEX
using BepInEx;
using BepInEx.Logging;
#elif QMM
    using QModManager.API.ModLoading;
    using Logger = QModManager.Utility.Logger;
#endif
//using SMLHelper.V2.Crafting;
using System.Reflection;
using FMOD;
using static OVRPlugin;
#if SUBNAUTICA_LEGACY
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
#endif
using UnityEngine;

namespace NoTriggerFullPlanters
{
#if BEPINEX
    [BepInPlugin(GUID, pluginName, version)]
    [BepInProcess("Subnautica.exe")]
    public class NoTriggerPlanterPlugin : BaseUnityPlugin
    {
#elif QMM
    [QModCore]
	public static class NoTriggerPlanterPlugin
    {
        [QModPatch]
#endif
        public void Start()
        {
            var assembly = Assembly.GetExecutingAssembly();
           harmony.PatchAll(assembly);
        }

        #region[Declarations]
        public const string
            MODNAME = "NoPlanterTrigger",
            AUTHOR = "dawrecka",
            GUID = "com." + AUTHOR + "." + MODNAME;
        internal const string pluginName = "No Trigger Full Planters";
        public const string version = "1.1.0.0";
        #endregion

        private static readonly Harmony harmony = new Harmony(GUID);
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

#if LEGACY
                HandReticle.main.SetInteractText("RegenPowerCell", format, true, false, HandReticle.Hand.None);
#else
                //HandReticle.main.SetInteractText("RegenPowerCell", format, true, false, HandReticle.Hand.None);
                HandReticle.main.SetText(HandReticle.TextType.Hand, "RegenPowerCell", true);
#endif
#if LEGACY
                HandReticle.main.SetInteractText(__instance.hoverText, string.Empty);
#else
                HandReticle.main.SetText(HandReticle.TextType.Hand, __instance.hoverText, true);
#endif
                HandReticle.main.SetIcon(HandReticle.IconType.HandDeny, 1f);
            }

            return false;
        }
    }
}