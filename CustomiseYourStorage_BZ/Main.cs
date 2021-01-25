using CustomiseYourStorage_BZ.Configuration;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using System.Collections.Generic;
using System.Reflection;

namespace CustomiseYourStorage_BZ
{
    [QModCore]
    public class Main
    {
        internal const string version = "0.0.2.0";

        internal static DWStorageConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWStorageConfig>();

        internal static List<TechType> StorageBlacklist = new List<TechType>() // If the TechType is on this list, don't do anything.
        // Initially-used for the aquariums - the free-standing base Aquarium, and the Seatruck Aquarium Module.
        // This is because there is a fixed limit of 8 fish that can be made visible in the aquarium, and the code assumes it will never go over this limit.
        // If it does - because you've increased the size of the storage container and added a ninth fish - a null reference exception is thrown.
        // The values *could* likely be changed, certainly decreased, but if the result is more than 8 slots, hello Mr Null Reference.
        // Similarly, we exclude the planters because they, too, have visuals associated with them that are likely to go ka-ka if we expand the storage.
        {
            TechType.Aquarium,
            TechType.FarmingTray,
            TechType.PlanterBox,
            TechType.PlanterPot,
            TechType.PlanterPot2,
            TechType.PlanterPot3,
            TechType.PlanterShelf,
            TechType.SeaTruckAquariumModule
        };

        [QModPatch]
        public static void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
            config.Init();

            // Enable the Storage Module. It's only useful for the Exosuit, but it can turn the Exosuit's storage locker from "useless" to "useful", so I don't know why UWE disabled it.
            var storageModuleData = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                    {
                        new Ingredient(TechType.Lithium, 1),
                        new Ingredient(TechType.Titanium, 1)
                    }
            };

            LanguageHandler.SetTechTypeName(TechType.VehicleStorageModule, "Vehicle Storage Module");
            LanguageHandler.SetTechTypeTooltip(TechType.VehicleStorageModule, "A small storage locker. Exosuit compatible.");
            CraftDataHandler.SetTechData(TechType.VehicleStorageModule, storageModuleData);
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.SeamothUpgrades, TechType.VehicleStorageModule, new string[] { "ExosuitModules" });
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.VehicleStorageModule, new string[] { "Upgrades", "ExosuitUpgrades" });
        }
    }
}

