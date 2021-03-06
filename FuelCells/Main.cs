﻿using Common;
using CustomBatteries.API;
using CustomBatteries.Items;
using FuelCells.Patches;
using FuelCells.Spawnables;
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

namespace FuelCells
{
    [QModCore]
    public class Main
    {
        public const string BatteryCraftTab = "BatteryTab";
        public const string PowCellCraftTab = "PowCellTab";
        public const string ElecCraftTab = "Electronics";
        public const string ResCraftTab = "Resources";

        public static readonly string[] BatteryCraftPath = new[] { ResCraftTab, BatteryCraftTab };
        public static readonly string[] PowCellCraftPath = new[] { ResCraftTab, PowCellCraftTab };

        private static Assembly myAssembly = Assembly.GetExecutingAssembly();
        private static string ModPath = Path.GetDirectoryName(myAssembly.Location);
        internal static string AssetsFolder = Path.Combine(ModPath, "Assets");

        public const string version = "0.5.0.0";
        public const string modName = "FuelCells";
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
#if BELOWZERO
            (new CoralSample()).Patch();
#endif
            config.OnLoad();
            Batteries.PostPatch();

            var nBattery = new CbBattery // Calling the CustomBatteries API to patch this item as a Battery
            {
                EnergyCapacity = (int)config.smallFuelCellCap,
                ID = "SmallFuelCell",
                Name = "Small Fuel Cell",
                FlavorText = "Small hydrogen fuel cell, higher-capacity drop-in substitute for standard Alterra batteries.",
                CraftingMaterials = { TechType.Battery, TechType.Polyaniline, TechType.DisinfectedWater, TechType.Magnetite },
                UnlocksWith = TechType.Polyaniline,
                CustomIcon = ImageUtils.LoadSpriteFromFile(Path.Combine(AssetsFolder, "fuelcell.png")),
                CBModelData = new CBModelData
                {
                    CustomTexture = ImageUtils.LoadTextureFromFile(Path.Combine(AssetsFolder, "fuelcell_01.png")),
                    CustomIllumMap = ImageUtils.LoadTextureFromFile(Path.Combine(AssetsFolder, "fuelcell_01_illum.png")),
                    CustomSpecMap = ImageUtils.LoadTextureFromFile(Path.Combine(AssetsFolder, "fuelcell_01_spec.png")),
                    CustomIllumStrength = 0.95f,
                    UseIonModelsAsBase = false
                },
            };
            nBattery.Patch();

            var nPowercell = new CbPowerCell // Calling the CustomBatteries API to patch this item as a Power Cell
            {
                EnergyCapacity = (int)config.cellCap,
                ID = "FuelCell",
                Name = "Fuel Cell",
                FlavorText = "Full-sized hydrogen fuel cell, higher-capacity drop-in substitute for standard Alterra power cells.",
                CraftingMaterials = { TechType.PowerCell, TechType.Polyaniline, TechType.DisinfectedWater, TechType.Magnetite },
                UnlocksWith = nBattery.TechType,
                CustomIcon = ImageUtils.LoadSpriteFromFile(Path.Combine(AssetsFolder, "largefuelcell.png")),
                CBModelData = new CBModelData
                {
                    CustomTexture = ImageUtils.LoadTextureFromFile(Path.Combine(AssetsFolder, "large_fuel_cell_01.png")),
                    CustomIllumMap = ImageUtils.LoadTextureFromFile(Path.Combine(AssetsFolder, "large_fuel_cell_01_illum.png")),
                    CustomSpecMap = ImageUtils.LoadTextureFromFile(Path.Combine(AssetsFolder, "large_fuel_cell_01_spec.png")),
                    UseIonModelsAsBase = true,
                    CustomIllumStrength = 1.2f
                },
            };
            nPowercell.Patch();

            /*CraftDataHandler.SetTechData(TechType.LithiumIonBattery, new SMLHelper.V2.Crafting.RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.Lithium, 1),
                    new Ingredient(TechType.Copper, 1),
                    new Ingredient(TechType.GenericRibbon, 2)
                }
            });
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.LithiumIonBattery, BatteryCraftPath);
            KnownTechHandler.SetAnalysisTechEntry(TechType.Lithium, new TechType[] { TechType.LithiumIonBattery });*/

            float LithiumCapacity;
            if (!config.BatteryValues.TryGetValue("LithiumIonBattery", out LithiumCapacity))
            {
                LithiumCapacity = 200f;
            }

            var cbLithiumBattery = new CbBattery
            {
                EnergyCapacity = (int)LithiumCapacity,
                ID = "CbLithiumBattery",
                Name = "Lithium-Ion Battery",
                FlavorText = "High-capacity mobile power source",
                CraftingMaterials = {
                    TechType.Lithium, TechType.Copper,
#if SUBNAUTICA_STABLE
                    TechType.AcidMushroom, TechType.AcidMushroom
#elif BELOWZERO
                    TechType.GenericRibbon, TechType.GenericRibbon
#endif
                },
                UnlocksWith = TechType.Lithium
            };
            cbLithiumBattery.Patch();

            var cbLithiumCell = new CbPowerCell
            {
                EnergyCapacity = (int)(LithiumCapacity * 2),
                ID = "LithiumPowerCell",
                Name = "Lithium-Ion Power Cell",
                FlavorText = "Lithium-ion mobile power source",
                CraftingMaterials = { cbLithiumBattery.TechType, cbLithiumBattery.TechType, TechType.Silicone },
                UnlocksWith = TechType.Lithium
            };
            cbLithiumCell.Patch();
        }

        [QModPostPatch]
        internal static void PostPatch()
        {
        }
    }
}
