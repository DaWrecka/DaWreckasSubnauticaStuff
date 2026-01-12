//using SMLHelper.V2.Json;
//using SMLHelper.V2.Options;
//using SMLHelper.V2.Options.Attributes;
using System.Collections.Generic;
#if BEPINEX
	using BepInEx;
	using BepInEx.Logging;
#elif QMM
	using QModManager.API.ModLoading;
#endif
using System.IO;
#if LEGACY
using Oculus.Newtonsoft.Json;
using Oculus.Newtonsoft.Json.Converters;
using Oculus.Newtonsoft.Json.Serialization;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
#endif
using System.Reflection;
using Common;
using UnityEngine;
#if NAUTILUS
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using Nautilus.Handlers;
#else
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Handlers;
#endif

namespace CustomiseYourStorage.Configuration
{
	internal class DWStorageConfig : ConfigFile
	{
#if LEGACY
		private const string AdvancedInventoryAssembly = "AdvancedInventory";
#elif BEPINEX
		private const string AdvancedInventoryAssembly = "sn.advancedinventory.mod";
#elif BELOWZERO
		private const string AdvancedInventoryAssembly = "AdvancedInventory_BZ";
#endif

		private static readonly HashSet<string> heightSliders = new HashSet<string>() { "DroppodHeight", "ExosuitHeight", "ExosuitModuleHeight", "FiltrationHeight", "InvHeight", "BioreactorHeight", "CyclopsHeight" };
#if QMM
		private static bool bHasAdvancedInventory => QModManager.API.QModServices.Main.ModPresent(AdvancedInventoryAssembly);
#elif BEPINEX
		private static bool bHasAdvancedInventory => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(AdvancedInventoryAssembly);
#endif
		private static int MaxHeight => (bHasAdvancedInventory ? 12 : 8);
		private readonly Vector2int nullVector = new Vector2int(0, 0); // Used for quick comparison

		private Dictionary<string, Vector2int> defaultStorageSizes = new Dictionary<string, Vector2int>(System.StringComparer.OrdinalIgnoreCase)
		{
			{ "smalllocker.smalllocker", new Vector2int(5, 6) },
			{ "locker.locker", new Vector2int(6, 8) },
			{ "labtrashcan.labtrashcan", new Vector2int(3, 4) },
			{ "trashcans.trashcans", new Vector2int(4, 5) },
			{ "vehiclestoragemodule.seamothstoragemodule", new Vector2int(4, 4) },
#if BELOWZERO
			{ "quantumlocker.storagecontainer", new Vector2int(4, 4) },
			{ "recyclotron.recyclotron", new Vector2int(6, 4) },
			{ "coffeevendingmachine.coffeevendingmachine", new Vector2int(2, 1) },
			{ "fridge.fridge", new Vector2int(5, 7) },
			{ "seatruckfabricatormodule.storagecontainer (1)", new Vector2int(6, 2) },
			{ "seatruckstoragemodule.storagecontainer", new Vector2int(3, 5) },
			{ "seatruckstoragemodule.storagecontainer (1)", new Vector2int(6, 3) },
			{ "seatruckstoragemodule.storagecontainer (2)", new Vector2int(4, 3) },
			{ "seatruckstoragemodule.storagecontainer (3)", new Vector2int(4, 3) },
			{ "seatruckstoragemodule.storagecontainer (4)", new Vector2int(3, 5) },
			{ "spypenguin.inventory", new Vector2int(2, 2) },
			{ "exchanger.exchanger", new Vector2int(3, 2) },
			{ "supplydrop.supplydrop", new Vector2int(8, 8) },
#endif
			{ "smallstorage.storagecontainer", new Vector2int(4, 4) }
		};

		private Vector2int defaultLifepodLockerSize = new Vector2int(6, 8);
		public struct ExoConfigStruct
		{
			public int height;
			public int width;
			public int heightPerModule;

			public ExoConfigStruct(int width, int height, int heightPerModule)
			{
				this.height = height;
				this.width = width;
				this.heightPerModule = heightPerModule;
			}

			public override string ToString() => $"({width}, {height}) + {heightPerModule}";

			public bool IsNull()
			{
				return (this.height == 0 || this.width == 0 || this.heightPerModule == 0);
			}
		}

		public struct FiltrationConfigStruct
		{
			public int maxSalt;
			public int maxWater;
			public Vector2int containerSize;

			public override string ToString() => $"[maxSalt {maxSalt}, maxWater {maxWater}, size ({containerSize.ToString()})]";

			public bool IsNull()
			{
				return maxSalt < 1 || maxWater < 1 || containerSize.x < 1 || containerSize.y < 1;
			}

			public FiltrationConfigStruct(int maxSalt, int maxWater, Vector2int containerSize)
			{
				this.maxSalt = System.Math.Max(maxSalt, 1);
				this.maxWater = System.Math.Max(maxWater, 1);
				this.containerSize = containerSize;
			}
		}

		private ExoConfigStruct defaultExosuitConfig = new ExoConfigStruct(6, 4, 1);
		private FiltrationConfigStruct defaultFiltrationConfig = new FiltrationConfigStruct(2, 2, new Vector2int(2, 2));
		private Vector2int defaultInventorySize = new Vector2int(6, 8);
		private Vector2int defaultBioReactorSize = new Vector2int(4, 4);

		public Dictionary<string, Vector2int> StorageSizes;
		internal List<TechType> defaultLifepodLockerInventoryTypes = new List<TechType>();
		public bool useDropPodInventory = false;
		public List<string> defaultLifepodLockerInventory = new List<string>();
		public List<string> defaultBlueprintsToUnlock = new List<string>();
		private Dictionary<TechType, int> unlockedBlueprints = new Dictionary<TechType, int>(); // This set is used to tell whether or not we've unlocked a blueprint already, and therefore whether or not an entry is duplicated.

#if SN1
		private const string PodName = "LifePod";
#elif BELOWZERO
		private const string PodName = "Drop pod";
#endif

		//public Vector2int LifepodLockerSize = new Vector2int(0, 0);
		[Slider("LifePod locker width", 4, 8, DefaultValue = 4, Id = nameof(DroppodWidth), Step = 1f, 
			Tooltip = "Width of the " + PodName + " locker, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int DroppodWidth = 6;

		[Slider("LifePod locker height", 4, 8, DefaultValue = 6, Id = nameof(DroppodHeight),
			Step = 1f,
			Tooltip = "Width of the " + PodName + " lockerlocker, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int DroppodHeight = 8;

#if SN1
		[Slider("Cyclops locker width", 4, 8, DefaultValue = 3, Id = nameof(CyclopsWidth),
			Step = 1f,
			Tooltip = "Width of the Cyclops lockers, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int CyclopsWidth = 3;

		[Slider("Cyclops locker height", 4, 8, DefaultValue = 6, Id = nameof(CyclopsHeight),
			Step = 1f,
			Tooltip = "Height of the Cyclops lockers, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int CyclopsHeight = 6;
#endif

		//public ExoConfigStruct ExosuitConfig;
		[Slider("Exosuit locker width", 4, 8, DefaultValue = 6, Id = nameof(ExosuitWidth),
			Step = 1f,
			Tooltip = "Width of the Exosuit locker, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int ExosuitWidth = 6;
		[Slider("Exosuit locker height", 4, 8, DefaultValue = 4, Id = nameof(ExosuitHeight),
			Step = 1f,
			Tooltip = "Height of the Exosuit locker, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int ExosuitHeight = 4;
		[Slider("Exosuit storage module height", 1, 4, DefaultValue = 1, Id = nameof(ExosuitModuleHeight),
			Step = 1f,
			Tooltip = "Number of rows added to the Exosuit locker per Vehicle Storage Module installed"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int ExosuitModuleHeight = 1;

		//public FiltrationConfigStruct FiltrationConfig;
		[Slider("Filtration Machine width", 2, 8, DefaultValue = 2, Id = nameof(FiltrationWidth),
			Step = 1f,
			Tooltip = "Width of the Water Filtration Machine storage container, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int FiltrationWidth = 2;
		[Slider("Filtration Machine height", 2, 8, DefaultValue = 2, Id = nameof(FiltrationHeight),
			Step = 1f,
			Tooltip = "Height of the Water Filtration Machine storage container, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int FiltrationHeight = 2;
		[Slider("Filtration Machine max water", 2, 8, DefaultValue = 2, Id = nameof(FiltrationWater),
			Step = 1f,
			Tooltip = "Maximum number of water bottles that can be held by the Water Filtration Machine"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int FiltrationWater = 2;
		[Slider("Filtration Machine max salt", 2, 8, DefaultValue = 2, Id = nameof(FiltrationSalt),
			Step = 1f,
			Tooltip = "Maximum number of salt deposits that can be held by the Water Filtration Machine"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int FiltrationSalt = 2;

		//public Vector2int InventorySize = new Vector2int(0, 0);
		[Slider("Inventory width", 4, 8, DefaultValue = 6, Id = nameof(InvWidth),
			Step = 1f,
			Tooltip = "Base width of the inventory, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int InvWidth = 6;
		[Slider("Inventory height", 4, 8, DefaultValue = 8, Id = nameof(InvHeight),
			Step = 1f,
			Tooltip = "Base height of the inventory, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int InvHeight = 8;

		//public Vector2int BioreactorSize = new Vector2int(0, 0);
		[Slider("Bioreactor width", 3, 8, DefaultValue = 4, Id = nameof(BioreactorWidth),
			Step = 1f,
			Tooltip = "Width of the Bioreactor container, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int BioreactorWidth = 4;
		[Slider("Bioreactor height", 3, 8, DefaultValue = 4, Id = nameof(BioreactorHeight),
			Step = 1f,
			Tooltip = "Height of the Bioreactor container, in inventory units"), OnChange(nameof(OnSliderChange)), OnGameObjectCreated(nameof(GameOptionCreated))]
		public int BioreactorHeight = 4;

		public void GameOptionCreated(object sender, GameObjectCreatedEventArgs e)
		{
			if (heightSliders.Contains(e.Id))
			{
#if NAUTILUS
				GameObject go = e.Value as GameObject;
#else
				GameObject go = e.GameObject;
#endif
				GameObject slider = go.transform.Find("Slider").gameObject;
				slider.GetComponent<uGUI_SnappingSlider>().maxValue = MaxHeight;
			}
		}

		internal void OnSliderChange(SliderChangedEventArgs e)
		{
			switch (e.Id)
			{
				default:
					break;
			}
		}

		public bool TryGetModSize(string Identifier, out Vector2int newSize)
		{
			string lowID = Identifier.ToLower();
			Vector2int defaultSize;
			bool bHasDefault = defaultStorageSizes.TryGetValue(lowID, out defaultSize);
			if (bHasDefault)
			{
#if !RELEASE
				Log.LogDebug($"Found default values for ID {Identifier} using TryGetValue");
#endif
			}
			else
			{
				// Go through the defaults list manually; this *shouldn't* be necessary, as the dictionary has been declared with the IgnoreCase comparer, but
				// experience has not borne this out.

				foreach (KeyValuePair<string, Vector2int> kvp in defaultStorageSizes)
				{
					string key = kvp.Key.ToLower();
					if (key == lowID)
					{
						bHasDefault = true;
						defaultSize = kvp.Value;
#if !RELEASE
						Log.LogDebug($"Found default values for ID {Identifier} on manual review that were not found with TryGetValue");
#endif
						break;
					}
				}
			}

			newSize = new Vector2int(0, 0);
			if (StorageSizes.TryGetValue(Identifier, out newSize))
			{
#if !RELEASE
				Log.LogDebug($"Found configured values for ID {Identifier} using TryGetValue");
#endif
				if (bHasDefault)
				{
					// Return a value of true if the new value is different from default, and false if it's equal
					return !(newSize.Equals(defaultSize));
				}
				else
					return true;
			}
			else
			{
				foreach (KeyValuePair<string, Vector2int> kvp in StorageSizes)
				{
					if (lowID == kvp.Key.ToLower())
					{
						newSize = kvp.Value;
#if !RELEASE
						Log.LogDebug($"Found configured values for ID {Identifier} on manual review that were not found with TryGetValue");
#endif
						if (bHasDefault)
							return !(newSize.Equals(defaultSize));
						else
							return true;
					}
				}
			}

			// Couldn't find a value for this identifier, so:
#if !RELEASE
			Log.LogDebug("Could not find " + (bHasDefault ? "" : "default or ") + "configured values for the identifier " + Identifier);
#endif
			return false;
		}

		public bool AddContainer(string Id, int width, int height)
		{
			Vector2int size;

			if (TryGetModSize(Id, out size))
			{
				return false; // Already exists
			}

			size.x = width;
			size.y = height;
			StorageSizes.Add(Id, size);
			Save();
			return true;
		}

		/*public static DWStorageConfigNonSML LoadConfig(string LoadPath)
		{
			DWStorageConfigNonSML config = new DWStorageConfigNonSML();

			if (FileUtils.FileExists(LoadPath))
			{
				using (var reader = new StreamReader(LoadPath))
				{
					var serializer = new JsonSerializer();
					config = serializer.Deserialize(reader, typeof(DWStorageConfigNonSML)) as DWStorageConfigNonSML;
				}
			}
			return config;
		}*/


		public void Init()
		{
			bool bUpdated = false;
			if (StorageSizes == null || StorageSizes.Count < 1)
			{
				StorageSizes = new Dictionary<string, Vector2int>(defaultStorageSizes, System.StringComparer.OrdinalIgnoreCase);
				bUpdated = true;
			}

			/*if (LifepodLockerSize.x < 1 || LifepodLockerSize.y < 1)
			{
				LifepodLockerSize = defaultLifepodLockerSize;
				bUpdated = true;
			}

			if (ExosuitConfig.IsNull())
			{
				ExosuitConfig = defaultExosuitConfig;
				bUpdated = true;
			}

			if (BioreactorSize.x < 1 || BioreactorSize.y < 1)
			{
				BioreactorSize = defaultBioReactorSize;
				bUpdated = true;
			}

			if (InventorySize.x < 1 || InventorySize.y < 1)
			{
				InventorySize = defaultInventorySize;
				bUpdated = true;
			}*/

			// defaultLifepodLockerInventory is empty by default, so we actually want to act if it's not.
			if (defaultLifepodLockerInventory.Count > 0)
			{
				defaultLifepodLockerInventoryTypes.Clear(); // Doesn't hurt to be sure.
				foreach (string s in defaultLifepodLockerInventory)
				{
					TechType tt = TechTypeUtils.GetTechType(s);
					if (tt != TechType.None)
					{
						defaultLifepodLockerInventoryTypes.Add(tt);

					}
					else
					{
						Log.LogWarning($"Could not parse string '{s}' as TechType in defaultLifepodLockerInventory");
					}
				}
			}

			// Likewise for defaultBlueprintsToUnlock
			if (defaultBlueprintsToUnlock.Count > 0)
			{
				unlockedBlueprints.Clear();
				foreach (string s in defaultBlueprintsToUnlock)
				{
					TechType tt = TechTypeUtils.GetTechType(s);
					if (tt != TechType.None)
					{
						if (unlockedBlueprints.TryGetValue(tt, out int c))
						{
							c++;
							Log.LogWarning($"Entry {s} in defaultBlueprintsToUnlock appears more than once; entry has been found {c} times so far");
							unlockedBlueprints[tt] = c;
						}
						else
						{
							KnownTechHandler.UnlockOnStart(tt);
							unlockedBlueprints[tt] = 1;
						}
					}
					else
					{
						Log.LogWarning($"Could not parse string {s} as TechType in defaultBlueprintsToUnlock");
					}
				}
			}

			if (bUpdated)
				Save();
#if !RELEASE
			Log.LogDebug(bUpdated ? "Some values reset to defaults" : "All values present and correct");
#endif
		}
	}
}
