using Common;
using PowerOverYourPower.Patches;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
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
    [QModCore]
    public class Main
    {
        private static Assembly myAssembly = Assembly.GetExecutingAssembly();
        private static string ModPath = Path.GetDirectoryName(myAssembly.Location);
        internal static string AssetsFolder = Path.Combine(ModPath, "Assets");

        public const string version = "0.5.0.0";
        public const string modName = "PowerOverYourPower";
        internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();

        internal static void AddModTechType(TechType tech, GameObject prefab = null)
        {
            TechTypeUtils.AddModTechType(tech, prefab);
        }

        public static TechType GetModTechType(string key)
        {
            return TechTypeUtils.GetModTechType(key);
        }

        [QModPatch]
        public static void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
            Batteries.PostPatch();
        }

        [QModPostPatch]
        internal static void PostPatch()
        {
        }
    }
}
