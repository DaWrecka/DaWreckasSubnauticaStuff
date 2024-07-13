using Common;
using PowerOverYourPower.Patches;
using HarmonyLib;
#if BEPINEX
    using BepInEx;
    using BepInEx.Logging;
#elif QMM
	using QModManager.API.ModLoading;
	using SMLHelper.V2.Handlers;
#endif
#if NAUTILUS
using Nautilus;
using Nautilus.Options;
using Nautilus.Handlers;
#else
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerOverYourPower
{
#if BEPINEX
    [BepInPlugin(GUID, pluginName, version)]
#if BELOWZERO
	[BepInProcess("SubnauticaZero.exe")]
#elif SN1
    [BepInProcess("Subnautica.exe")]
    public class POYPPlugin : BaseUnityPlugin
    {
#elif QMM
    [QModCore]
	public static class POYPPlugin
    {
#endif
        #region[Declarations]
        public const string
            MODNAME = "PowerOverYourPower",
            AUTHOR = "dawrecka",
            GUID = "com." + AUTHOR + "." + MODNAME;
        private const string pluginName = "Power Over Your Power";
        public const string version = "0.5.0.0";
        #endregion

        private static readonly Harmony harmony = new Harmony(GUID);
        private static Assembly myAssembly = Assembly.GetExecutingAssembly();
        private static string ModPath = Path.GetDirectoryName(myAssembly.Location);
        internal static string AssetsFolder = Path.Combine(ModPath, "Assets");

        internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();

        internal static void AddModTechType(TechType tech, GameObject prefab = null)
        {
            TechTypeUtils.AddModTechType(tech, prefab);
        }

        public static TechType GetModTechType(string key)
        {
            return TechTypeUtils.GetModTechType(key);
        }

#if QMM
        [QModPatch]
#endif
        public void Awake()
        {
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Batteries.PostPatch();
        }
    }
}
