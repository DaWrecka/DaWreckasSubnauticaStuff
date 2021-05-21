using Common;
using CustomiseYourStorage_BZ.Configuration;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Logger = QModManager.Utility.Logger;

namespace CustomiseYourStorage_BZ
{
	[QModCore]
	public class Main
	{
		internal const string version = "1.0.0.2";

		internal static DWStorageConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWStorageConfig>();
		//internal static readonly DWStorageConfigNonSML config = DWStorageConfigNonSML.LoadConfig(Path.Combine(new string[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json" }));

		// The actual TechTypes used for the blacklist, populated at post-patch time.
		internal static List<TechType> StorageBlacklist = new List<TechType>();

		internal static List<string> stringBlacklist = new List<string>()
		// If the TechType is on this list, don't do anything.
		// Initially-used for the aquariums - the free-standing base Aquarium, and the Seatruck Aquarium Module.
		// This is because there is a fixed limit of 8 fish that can be made visible in the aquarium, and the code assumes it will never go over this limit.
		// If it does - because you've increased the size of the storage container and added a ninth fish - a null reference exception is thrown.
		// The values *could* likely be changed, certainly decreased, but if the result is more than 8 slots, hello Mr Null Reference.
		// Similarly, we exclude the planters because they, too, have visuals associated with them that are likely to go ka-ka if we expand the storage.
		{
#if BELOWZERO
			"SeaTruckAquariumModule",
#endif
			"Aquarium",
			"FarmingTray",
			"PlanterBox",
			"PlanterPot",
			"PlanterPot2",
			"PlanterPot3",
			"PlanterShelf",
			"AutoSorter",
			"AutosortTarget",
			"AutosortTargetStanding"
		};


		[QModPatch]
		public static void Load()
		{
			var assembly = Assembly.GetExecutingAssembly();
			config.Init();
			new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
		}

		[QModPostPatch]
		public void PostPatch()
		{
			/* Enable the Storage Module. It's only useful for the Exosuit, but it still works, and it can turn the Exosuit's storage locker from "useless" to "useful", so I don't know why UWE disabled it.
			 *
			 * This block of code became unnecessary with the Seaworthy update, late Feb 2021, which re-enabled the Storage Module. The above comment has been retained for historical purposes.
			 * 
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
			LanguageHandler.SetTechTypeTooltip(TechType.VehicleStorageModule, "A small storage locker. Expands Exosuit storage capacity.");
			CraftDataHandler.SetTechData(TechType.VehicleStorageModule, storageModuleData);
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.SeamothUpgrades, TechType.VehicleStorageModule, new string[] { "ExosuitModules" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.VehicleStorageModule, new string[] { "Upgrades", "ExosuitUpgrades" });
			KnownTechHandler.SetAnalysisTechEntry(TechType.Exosuit, new TechType[] { TechType.VehicleStorageModule });*/
			foreach (string s in stringBlacklist)
			{
				TechType tt = TechTypeUtils.GetTechType(s);
				if (tt != TechType.None)
					StorageBlacklist.Add(tt);
				else
					Logger.Log(Logger.Level.Debug, $"Could not find TechType for string '{s}' in blacklist. Is associated mod installed?");
			}
		}
	}
}
