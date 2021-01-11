using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UpgradedBlades
{
    [QModCore]
    public class Main
    {
        public const string version = "0.1.0.0";

        internal static Vibroblade prefabBlade1 = new Vibroblade();

        private static readonly Assembly myAssembly = Assembly.GetExecutingAssembly();
        private static readonly string modPath = Path.GetDirectoryName(myAssembly.Location);
        internal static readonly string AssetsFolder = Path.Combine(modPath, "Assets");

        [QModPatch]
        public static void Load()
        {
            (new DiamondBladeRecipe()).Patch();
            prefabBlade1.Patch();

            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }
}
