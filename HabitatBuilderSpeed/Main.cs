using HabitatBuilderSpeed.Configuration;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using System.Reflection;

namespace HabitatBuilderSpeed
{
    [QModCore]
    public class Main
    {
        internal const string version = "0.1.2.0";

        internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();

        [QModPatch]
        public static void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }
}

