using BepInEx;
using Common;
using CustomiseYourStorage.Configuration;
using HarmonyLib;
#if NAUTILUS
using Nautilus.Handlers;
#else
using SMLHelper.V2.Handlers;
#endif
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if QMM
	using QModManager.API.ModLoading;
	using Logger = QModManager.Utility.Logger;
#endif

namespace CustomiseYourStorage
{
#if BEPINEX
    [BepInPlugin(GUID, pluginName, version)]
    [BepInProcess("Subnautica.exe")]
    [BepInDependency("com.snmodding.nautilus")]
    public class CustomiseStoragePlugin : BaseUnityPlugin
    {
#elif QMM
    [QModCore]
	public static class CustomiseStoragePlugin
    {
#endif
        #region[Declarations]
        public const string
            MODNAME = "CustomiseStorage",
            AUTHOR = "dawrecka",
            GUID = "com." + AUTHOR + "." + MODNAME;
        private const string pluginName = "Customise Your Storage";
        internal const string version = "1.0.0.3";
        #endregion

        private static readonly Harmony harmony = new Harmony(GUID);
        
		internal static DWStorageConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWStorageConfig>();
		//internal static readonly DWStorageConfigNonSML config = DWStorageConfigNonSML.LoadConfig(Path.Combine(new string[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json" }));

		// The actual TechTypes used for the blacklist, populated at post-patch time.
		internal static HashSet<TechType> StorageBlacklist = new HashSet<TechType>();

		internal static List<string> stringBlacklist = new List<string>()
		// If the TechType is on this list, don't do anything.
		// Initially-used for the aquariums - the free-standing base Aquarium, and the Seatruck Aquarium Module.
		// This is because there is a fixed limit of 8 fish that can be made visible in the aquarium, and the code assumes it will never go over this limit.
		// If it does - because you've increased the size of the storage container and added a ninth fish - a null reference exception is thrown.
		// The values *could* likely be changed, certainly decreased, but if the result is more than 8 slots, hello Mr Null Reference.
		// Similarly, we exclude the planters because they, too, have visuals associated with them that are likely to go ka-ka if we expand the storage.
		{
#if SN1
			"EscapePod.StorageContainer", // We've given the DropPod special treatment, so we're keeping it for the Lifepod.
					// This might seem counter-intuitive, blacklisting the Escape Pod locker when we could easily customise it the same way as any other container,
					// but the BZ version of this mod was developed before the SN1 version, so the Droppod code was implemented before the SN1 version of the mod.
#elif BELOWZERO
			"SeaTruckAquariumModule",
#endif
			"BagEquipment1",
			"BagEquipment2",
			"BagEquipment3",
			"Aquarium",
			"FarmingTray",
			"PlanterBox",
			"PlanterPot",
			"PlanterPot2",
			"PlanterPot3",
			"PlanterShelf",
			"AutoSorter",
			"AutosortTarget",
			"AutosortTargetStanding",
			"DockedVehicleStorageAccess"
		};


#if QMM
		[QModPatch]
#endif
		public static void Awake()
		{
		}

#if QMM
		[QModPostPatch]
#endif
		public void Start()
		{
            var assembly = Assembly.GetExecutingAssembly();
            config.Init();
            harmony.PatchAll(assembly);
			Log.InitialiseLog(GUID);
            
			foreach (string s in stringBlacklist)
			{
				TechType tt = TechTypeUtils.GetTechType(s);
				if (tt != TechType.None)
				{
					if(!StorageBlacklist.Contains(tt))
						StorageBlacklist.Add(tt);
				}
				else
					Log.LogDebug($"Could not find TechType for string '{s}' in blacklist. Is associated mod installed?");
			}
		}
	}
}
