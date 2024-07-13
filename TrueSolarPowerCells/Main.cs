using BepInEx;
using HarmonyLib;
#if QMM
using QModManager.API.ModLoading;
#endif

#if NAUTILUS
using Nautilus.Handlers;
#else
using SMLHelper.V2.Handlers;
#endif
using System.Reflection;
using Common;

namespace TrueSolarPowerCells
{
#if BEPINEX
    [BepInPlugin(GUID, pluginName, version)]
    [BepInProcess("Subnautica.exe")]
    [BepInDependency("com.snmodding.nautilus")]
    public class TSPCPlugin : BaseUnityPlugin
    {
#elif QMM
    [QModCore]
	public static class TSPCPlugin
    {
#endif
        public const string
            MODNAME = "TrueSolarPowerCells",
            AUTHOR = "dawrecka",
            GUID = "com." + AUTHOR + "." + MODNAME;
        private const string pluginName = "TrueSolarPowerCells";
        public const string version = "1.2.0.0";

        internal static Config config { get; } = OptionsPanelHandler.RegisterModOptions<Config>();

#if QMM
        [QModPatch]
#endif

        public void Start()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony(GUID).PatchAll(assembly);
            Log.InitialiseLog(GUID);
            Log.LogInfo("True Solar Power Cells initialised");
        }
    }
}
