using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
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
        public const string version = "0.1.0.1";

        internal static Vibroblade prefabBlade1 = new Vibroblade();

        private static readonly Assembly myAssembly = Assembly.GetExecutingAssembly();
        private static readonly string modPath = Path.GetDirectoryName(myAssembly.Location);
        internal static readonly string AssetsFolder = Path.Combine(modPath, "Assets");

        [QModPatch]
        public static void Load()
        {
            //(new DiamondBladeRecipe()).Patch();
            var diamondBlade = new TechData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                    {
                        new Ingredient(TechType.Knife, 1),
                        new Ingredient(TechType.Diamond, 1)
                    }
                };
            CraftDataHandler.SetTechData(TechType.DiamondBlade, diamondBlade);
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.DiamondBlade, new string[] { "KnifeMenu" });

            prefabBlade1.Patch();
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }
}
