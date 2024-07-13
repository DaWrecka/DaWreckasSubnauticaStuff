using BepInEx;
using Common;
using HarmonyLib;
#if QMM
using QModManager.API.ModLoading;
#endif
#if NAUTILUS
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using Nautilus.Handlers;
#else
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Handlers;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace UnlockCustomisation
{
#if BEPINEX
    [BepInPlugin(GUID, pluginName, version)]
    [BepInProcess("Subnautica.exe")]
    public class UnlockCustomisationPlugin : BaseUnityPlugin
    {
#elif QMM
    [QModCore]
	public static class UnlockCustomisationPlugin
    {
#endif
#region[Declarations]
        public const string
            MODNAME = "UnlockCustomisation",
            AUTHOR = "dawrecka",
            GUID = "com." + AUTHOR + "." + MODNAME;
        private const string pluginName = "Unlock Customisation";
        public const string version = "1.0.0.0";
#endregion

        private static readonly Harmony harmony = new Harmony(GUID);
        public static UCConfig config { get; } = OptionsPanelHandler.RegisterModOptions<UCConfig>();
        private static readonly Assembly myAssembly = Assembly.GetExecutingAssembly();

#if QMM
        [QModPatch]
#endif
        public static void Start()
        {
            harmony.PatchAll(myAssembly);
            config.Patch();
        }
    }

}
