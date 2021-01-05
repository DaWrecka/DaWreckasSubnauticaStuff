using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using System.Reflection;

namespace CustomiseYourScannables
{
    [QModCore]
    public static class Main
    {
        internal const string version = "0.0.1.0";
        private static Assembly myAssembly = Assembly.GetExecutingAssembly();
        internal static CYScanConfig config { get; } = OptionsPanelHandler.RegisterModOptions<CYScanConfig>();

        [QModPatch]
        public static void Load()
        {
            Harmony.CreateAndPatchAll(myAssembly, $"DaWrecka_{myAssembly.GetName().Name}");
            config.Init();
        }
    }
}
