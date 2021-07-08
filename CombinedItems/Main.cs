using HarmonyLib;
using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using QModManager.API;
using QModManager.API.ModLoading;
using CombinedItems.Equipables;
using CombinedItems.Patches;
using CombinedItems.VehicleModules;
using Common;
using UWE;
using UnityEngine;
using CombinedItems.Spawnables;
using SMLHelper.V2.Assets;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System.Collections;
using CustomDataboxes.API;

namespace CombinedItems
{
	[QModCore]
	public class Main
	{
		internal static bool bVerboseLogging = true;
		internal static bool bLogTranspilers = false;
		internal const string version = "0.8.0.3";
		internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();

		private static readonly Type CustomiseOxygen = Type.GetType("CustomiseOxygen.Main, CustomiseOxygen", false, false);
		private static readonly MethodInfo CustomOxyAddExclusionMethod = CustomiseOxygen?.GetMethod("AddExclusion", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo CustomOxyAddTankMethod = CustomiseOxygen?.GetMethod("AddTank", BindingFlags.Public | BindingFlags.Static);
		//private static readonly PropertyInfo compatibleTechInfo = typeof(BatteryCharger).GetProperty("compatibleTech", BindingFlags.NonPublic | BindingFlags.Static);
		//private static HashSet<TechType> compatibleBatteries => (HashSet<TechType>)compatibleTechInfo.GetValue(null);
		internal static HashSet<TechType> compatibleBatteries => BatteryCharger.compatibleTech;

		//internal static InsulatedRebreather prefabInsulatedRebreather = new InsulatedRebreather();
		//internal static ReinforcedColdSuit prefabReinforcedColdSuit = new ReinforcedColdSuit();
		//internal static ReinforcedColdGloves prefabReinforcedColdGloves = new ReinforcedColdGloves();
		//internal static HighCapacityBooster prefabHighCapacityBooster = new HighCapacityBooster();
		//internal static ExosuitLightningClawPrefab prefabLightningClaw = new ExosuitLightningClawPrefab();
		//internal static ExosuitSprintModule prefabExosuitSprintModule = new ExosuitSprintModule();
		//internal static HoverbikeWaterTravelModule prefabHbWaterTravelModule = new HoverbikeWaterTravelModule();
		//internal static HoverbikeSolarChargerModule prefabHbSolarCharger = new HoverbikeSolarChargerModule();
		//internal static HoverbikeStructuralIntegrityModule prefabHbHullModule = new HoverbikeStructuralIntegrityModule();
		//internal static HoverbikeEngineEfficiencyModule prefabHbEngineModule = new HoverbikeEngineEfficiencyModule();
		//internal static HoverbikeSpeedModule prefabHbSpeedModule = new HoverbikeSpeedModule();
		//internal static HoverbikeMobilityUpgrade prefabHbMobility = new HoverbikeMobilityUpgrade();
		//internal static PowerglideEquipable prefabPowerglide = new PowerglideEquipable();
		//internal static ExosuitLightningClawGeneratorModule ExoLightningGenerator = new ExosuitLightningClawGeneratorModule();
		//internal static PowerglideFragmentPrefab powerglideFrag = new PowerglideFragmentPrefab();
		//internal static SurvivalSuit prefabSurvivalSuit = new SurvivalSuit();
		//internal static ReinforcedSurvivalSuit prefabReinforcedSurvivalSuit = new ReinforcedSurvivalSuit();
		private static readonly Dictionary<string, TechType> ModTechTypes = new Dictionary<string, TechType>(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, GameObject> ModPrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

		private static readonly Assembly myAssembly = Assembly.GetExecutingAssembly();
		internal static List<string> chipSlots = new List<string>();

		internal static void AddSubstitution(TechType custom, TechType vanilla)
		{
			Patches.EquipmentPatch.AddSubstitution(custom, vanilla);
			Patches.PlayerPatch.AddSubstitution(custom, vanilla);
		}

		internal static void AddCustomOxyExclusion(TechType excludedTank, bool bExcludeMultipliers, bool bExcludeOverride)
		{
			if (CustomOxyAddExclusionMethod != null)
				CustomOxyAddExclusionMethod.Invoke(null, new object[] { excludedTank, bExcludeMultipliers, bExcludeOverride });
		}

		internal static void AddCustomOxyTank(TechType tank, float capacity, Sprite icon = null)
		{
			if (CustomOxyAddTankMethod != null)
				CustomOxyAddTankMethod.Invoke(null, new object[] { tank, capacity, icon });
		}

		internal static void AddModTechType(TechType tech, GameObject prefab = null)
		{
			/*string key = tech.AsString(true);
			if (!ModTechTypes.ContainsKey(key))
			{
				ModTechTypes.Add(key, tech);
			}
			if (prefab != null)
			{
				ModPrefabs[key] = prefab;
			}*/
			TechTypeUtils.AddModTechType(tech, prefab);
		}

		public static TechType GetModTechType(string key)
		{
			/*string lowerKey = key.ToLower();
			TechType tt;
			if(ModTechTypes.TryGetValue(lowerKey, out tt))
				return tt;

			return TechTypeUtils.GetTechType(key);*/
			return TechTypeUtils.GetModTechType(key);
		}

		internal static GameObject GetModPrefab(string key)
		{
			/*string lowerKey = key.ToLower();
			GameObject modPrefab;
			if (ModPrefabs.TryGetValue(lowerKey, out modPrefab))
				return modPrefab;

			return null;*/
			return TechTypeUtils.GetModPrefab(key);
		}

		[QModPrePatch]
		public static void PrePatch()
		{
			/*foreach (TechType tt in new List<TechType>()
				{
					TechType.Battery,
					TechType.LithiumIonBattery,
					TechType.PowerCell,
					TechType.PrecursorIonBattery,
					TechType.PrecursorIonPowerCell
				})
			{
				string classId = CraftData.GetClassIdForTechType(tt);
				if (PrefabDatabase.TryGetPrefabFilename(classId, out string PrefabFilename))
				{
					Common.Log.LogDebug($"Got prefab filename '{PrefabFilename}' for classId '{classId}' for TechType {tt.AsString()}");
					AddressablesUtility.LoadAsync<GameObject>(PrefabFilename).Completed += (x) =>
					{
						GameObject gameObject1 = x.Result;
						Battery component = gameObject1?.GetComponent<Battery>();
						if (component != null)
							Batteries.AddPendingBattery(ref component);
					};
				}
				else
					Log.LogError($"Could not get prefab for classId '{classId}' for TechType {tt.AsString()}");
			}*/
		}

		[QModPatch]
		public static void Load()
		{
			// Knives
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "KnifeUpgrades", "Knife Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "workbench_knifemenu"));
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "HeatBlade" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.HeatBlade, new string[] { "KnifeUpgrades" });

			// Tanks
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "HighCapacityTank" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "ModTanks", "Tank Upgrades", SpriteManager.Get(TechType.HighCapacityTank));
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.HighCapacityTank, new string[] { "ModTanks" });

			// Fins menu
			CraftDataHandler.SetTechData(TechType.UltraGlideFins, new SMLHelper.V2.Crafting.RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>()
				{
					new Ingredient(TechType.Fins, 1),
					new Ingredient(TechType.Silicone, 2),
					new Ingredient(TechType.Titanium, 1),
					new Ingredient(TechType.Lithium, 1)
				}
			});
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "FinUpgrades", "Fin Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "workbench_finsmenu"));
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "SwimChargeFins" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.SwimChargeFins, new string[] { "FinUpgrades" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.UltraGlideFins, new string[] { "FinUpgrades " });
			// Test purposes, may be changed to a databox before release
			KnownTechHandler.SetAnalysisTechEntry(TechType.SwimChargeFins, new TechType[] { TechType.UltraGlideFins});

			// Seatruck Upgrades
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "SeaTruckWBUpgrades", "Seatruck Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "fabricator_seatruckupgrades"));
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "SeaTruckUpgradeHull2" });
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "SeaTruckUpgradeHull3" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.SeaTruckUpgradeHull2, new string[] { "SeaTruckWBUpgrades" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.SeaTruckUpgradeHull3, new string[] { "SeaTruckWBUpgrades" });

			// Exosuit Upgrades
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "ExoUpgrades", "Exosuit Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "workbench_exosuitmenu"));
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "ExoHullModule2" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.ExoHullModule2, new string[] { "ExoUpgrades" });

			// Now our custom stuff
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "SuitUpgrades", "Suit Upgrades", SpriteManager.Get(TechType.Stillsuit));
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "ChipEquipment", "Chips", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "ChipRecharge", "Chip Recharges", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "DPDTier1", "Diver Perimeter Defence Chip", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal", "ChipRecharge" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "DPDTier2", "Diver Perimeter Defence Chip Mk2", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal", "ChipRecharge" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "DPDTier3", "Diver Perimeter Defence Chip Mk3", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal", "ChipRecharge" });

			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeSilentModule" });
			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeJumpModule" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeIceWormReductionModule, new string[] { "Upgrades", "HoverbikeUpgrades" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeJumpModule, new string[] { "Upgrades", "HoverbikeUpgrades" });

			foreach (Spawnable s in new List<Spawnable>() {
				new ExosuitLightningClawPrefab(),
				//new ExosuitSprintModule(),
				//new ExosuitLightningClawGeneratorModule(),
				new PowerglideFragmentPrefab(),
				new SurvivalSuit(),
#if BELOWZERO
				new InsulatedRebreather(),
				new ReinforcedColdSuit(),
				new ReinforcedColdGloves(),
				new HighCapacityBooster(),
				new SurvivalColdSuit(),
				new SurvivalSuitBlueprint_FromReinforcedCold(),
				new SurvivalSuitBlueprint_FromSurvivalCold(),
				new HoverbikeMobilityUpgrade(),
				new SeatruckSolarModule(),
				new SeatruckThermalModule(),
				new SeaTruckSonarModule(),
#endif
				new ReinforcedSurvivalSuit(),
				new PowerglideEquipable(),
				new SuperSurvivalSuit(),
				//new SurvivalSuitBlueprint_BaseSuits(),
				new SurvivalSuitBlueprint_FromReinforcedSurvival(),
				new ShadowLeviathanSample(),
				new DiverPerimeterDefenceChip_Broken(),
				new DiverPerimeterDefenceChipItem(),
				new DiverDefenceSystemMk2(),
				new DiverDefenceMk2_FromBrokenChip(),
				new DiverDefenceSystemMk3(),
			})
			{
				s.Patch();
			}

			Databox powerglideDatabox = new Databox()
			{
				DataboxID = "PowerglideDatabox",
				PrimaryDescription = PowerglideEquipable.friendlyName + " Databox",
				SecondaryDescription = PowerglideEquipable.description,
				TechTypeToUnlock = GetModTechType("PowerglideEquipable"),
				CoordinatedSpawns = new Dictionary<Vector3, Vector3>()
				{
					{ new Vector3(285f, -242.07f, -1299f), new Vector3(344f, 3.77f, 14f) }
				}
			};
			powerglideDatabox.Patch();

			var harmony = new Harmony($"DaWrecka_{myAssembly.GetName().Name}");
			harmony.PatchAll(myAssembly);
		}

		[QModPostPatch]
		public static void PostPatch()
		{
#if BELOWZERO
			Sprite hoverbike = SpriteManager.Get(SpriteManager.Group.Pings, "Hoverbike");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "HoverbikeUpgrades", "Snowfox Upgrades", hoverbike, new string[] { "Upgrades" });
			foreach (Spawnable s in new List<Spawnable>() {
				new HoverbikeWaterTravelModule(),
				new HoverbikeSolarChargerModule(),
				new HoverbikeStructuralIntegrityModule(),
				new HoverbikeEngineEfficiencyModule(),
				new HoverbikeSelfRepairModule(),
				new HoverbikeDurabilitySystem(),
				new HoverbikeSpeedModule(),
			})
			{
				s.Patch();
			}


			//Batteries.PostPatch();
			LanguageHandler.SetLanguageLine("SeamothWelcomeAboard", "Welcome aboard captain.");
#endif
		}
	}

	[HarmonyPatch]
	public class Reflection
	{
#if BELOWZERO
		private static readonly MethodInfo addJsonPropertyInfo = typeof(CraftDataHandler).GetMethod("AddJsonProperty", BindingFlags.NonPublic | BindingFlags.Static);
		private static readonly MethodInfo playerUpdateReinforcedSuitInfo = typeof(Player).GetMethod("UpdateReinforcedSuit", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo playerCheckColdsuitGoalInfo = typeof(Player).GetMethod("CheckColdsuitGoal", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
		private static readonly FieldInfo knownTechCompoundTech = typeof(KnownTech).GetField("compoundTech", BindingFlags.NonPublic | BindingFlags.Static);
		private static List<KnownTech.CompoundTech> pendingCompoundTech = new List<KnownTech.CompoundTech>();
		private static bool bProcessingCompounds;

#if BELOWZERO
		public static void AddJsonProperty(TechType techType, string key, JsonValue newValue)
		{
			addJsonPropertyInfo.Invoke(null, new object[] { techType, key, newValue });
		}

		public static void AddColdResistance(TechType techType, int newValue)
		{
			//AddJsonProperty(techType, "coldResistance", new JsonValue(newValue));
			CraftDataHandler.SetColdResistance(techType, newValue);
		}
#endif

		public static void SetItemSize(TechType techType, int width, int height)
		{
			AddJsonProperty(techType, "itemSize", new JsonValue
				{
					{
						TechData.propertyX,
						new JsonValue(width)
					},
					{
						TechData.propertyY,
						new JsonValue(height)
					}
				}
			);
		}

		public static void PlayerUpdateReinforcedSuit(Player player)
		{
			playerUpdateReinforcedSuitInfo.Invoke(player, new object[] { });
		}
		public static void PlayerCheckColdsuitGoal(Player player)
		{
			playerCheckColdsuitGoalInfo.Invoke(player, new object[] { });
		}

		public static void AddCompoundTech(KnownTech.CompoundTech compound, bool bForce = false)
		{
			if(compound != null)
				pendingCompoundTech.Add(compound);
			//CoroutineHost.StartCoroutine(ProcessPendingCompounds(bForce));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(KnownTech), nameof(KnownTech.Initialize))]
		public static void PostKnownTechInit()
		{
			Log.LogDebug("Reflection.PostKnownTechInit() executing");
			CoroutineHost.StartCoroutine(ProcessPendingCompounds(true));
		}

		private bool KnownTechInitialised()
		{
			return (knownTechCompoundTech.GetValue(null) != null);
		}

		private static IEnumerator ProcessPendingCompounds(bool bForce = false)
		{
			if (bProcessingCompounds)
			{
				if (bForce)
					Log.LogDebug("ProcessPendingCompounds executing: forced");
				else
					yield break;
			}

			if (pendingCompoundTech.Count < 1)
			{
				bProcessingCompounds = false;
				yield break;
			}

			bProcessingCompounds = true;

			int tries = 0;
			while (pendingCompoundTech.Count > 0)
			{
				List<KnownTech.CompoundTech> compounds = (List<KnownTech.CompoundTech>)knownTechCompoundTech.GetValue(null);
				Log.LogDebug($"Attempting to process pending compound tech: pendingCompoundTech.Count == {pendingCompoundTech.Count}, attempt {++tries}");
				if (compounds != null)
				{
					Log.LogDebug("Successfully retrieved KnownTech.compoundTech: Now processing pendingCompoundTech");
					for (int i = pendingCompoundTech.Count - 1; i >= 0; i--)
					{
						compounds.Add(pendingCompoundTech[i]);
						pendingCompoundTech.RemoveAt(i);
					}
				}
				else
				{
					Log.LogDebug($"KnownTech.compoundTech could not be retrieved");
				}
				yield return new WaitForSecondsRealtime(2f);
			}

			bProcessingCompounds = false;
		}
	}
}
