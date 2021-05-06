using CustomBatteries.API;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FuelCells
{
    [QModCore]
    public class Main
    {
        private static Assembly myAssembly = Assembly.GetExecutingAssembly();
        private static string ModPath = Path.GetDirectoryName(myAssembly.Location);
        internal static string AssetsFolder = Path.Combine(ModPath, "Assets");

        public const string version = "0.5.0.0";
        public const string modName = "FuelCells";
        private const int batteryCap = 400;
        private const int cellCap = (int)(batteryCap * 2.25f);

        [QModPatch]
        public static void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
            var nBattery = new CbBattery // Calling the CustomBatteries API to patch this item as a Battery
            {
                EnergyCapacity = batteryCap,
                ID = "SmallFuelCell",
                Name = "Small Fuel Cell",
                FlavorText = "Small hydrogen fuel cell, longer-lasting drop-in substitute for standard Alterra batteries.",
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
                EnergyCapacity = cellCap,
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
        }

    }
}
