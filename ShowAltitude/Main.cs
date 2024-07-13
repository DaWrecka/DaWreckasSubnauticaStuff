using BepInEx;
using Common;
using HarmonyLib;
#if QMM
using QModManager.API.ModLoading;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UWE;

namespace ShowAltitude
{
#if BEPINEX
    [BepInPlugin(GUID, pluginName, version)]
#if BELOWZERO
	[BepInProcess("SubnauticaZero.exe")]
#elif SN1
    [BepInProcess("Subnautica.exe")]
    public class ShowAltitudePlugin: BaseUnityPlugin
    {
#elif QMM
    [QModCore]
	public static class ShowAltitudePlugin
    {
#endif
        #region[Declarations]
        public const string
            MODNAME = "ShowAltitudePlugin",
            AUTHOR = "dawrecka",
            GUID = "com." + AUTHOR + "." + MODNAME;
        private const string pluginName = "Show Altitude";
        internal const string version = "1.0.0.0";
        #endregion

        private static readonly Harmony harmony = new Harmony(GUID);

#if QMM
        [QModPatch]
#endif
        public void Awake()
        {
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }
    }
}
