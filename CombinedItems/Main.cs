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

namespace CombinedItems
{
	[QModCore]
	public class Main
	{
		internal static bool bVerboseLogging = true;
		internal static bool bLogTranspilers = true;
		internal const string version = "0.8.0.3";
		internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();

		private static readonly Type CustomiseOxygen = Type.GetType("CustomiseOxygen.Main, CustomiseOxygen", false, false);
		private static readonly MethodInfo CustomOxyAddExclusionMethod = CustomiseOxygen?.GetMethod("AddExclusion", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo CustomOxyAddTankMethod = CustomiseOxygen?.GetMethod("AddTank", BindingFlags.Public | BindingFlags.Static);

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
			string key = tech.AsString(true);
			if (!ModTechTypes.ContainsKey(key))
			{
				ModTechTypes.Add(key, tech);
			}
			if (prefab != null)
			{
				ModPrefabs[key] = prefab;
			}
		}

		public static TechType GetModTechType(string key)
		{
			string lowerKey = key.ToLower();
			TechType tt;
			if(ModTechTypes.TryGetValue(lowerKey, out tt))
				return tt;

			return TechTypeUtils.GetTechType(key);
		}

		internal static GameObject GetModPrefab(string key)
		{
			string lowerKey = key.ToLower();
			GameObject modPrefab;
			if (ModPrefabs.TryGetValue(lowerKey, out modPrefab))
				return modPrefab;

			return null;
		}

		[QModPatch]
		public static void Load()
		{
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "SuitUpgrades", "Suit Upgrades", SpriteManager.Get(TechType.Stillsuit));
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "HoverbikeUpgrades", "Snowfox Upgrades", SpriteManager.Get(TechType.Hoverbike), new string[] { "Upgrades" });
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "HighCapacityTank" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.HighCapacityTank, new string[] { "ModTanks" });
			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeSilentModule" });
			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeJumpModule" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeIceWormReductionModule, new string[] { "Upgrades", "HoverbikeUpgrades" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeJumpModule, new string[] { "Upgrades", "HoverbikeUpgrades" });

			foreach (Spawnable s in new List<Spawnable>() {
				new InsulatedRebreather(),
				new ReinforcedColdSuit(),
				new ReinforcedColdGloves(),
				new HighCapacityBooster(),
				new ExosuitLightningClawPrefab(),
				new ExosuitSprintModule(),
				new HoverbikeWaterTravelModule(),
				new HoverbikeSolarChargerModule(),
				new HoverbikeStructuralIntegrityModule(),
				new HoverbikeEngineEfficiencyModule(),
				new HoverbikeSpeedModule(),
				new ExosuitLightningClawGeneratorModule(),
				new PowerglideFragmentPrefab(),
				new SurvivalSuit(),
				new SurvivalColdSuit(),
				new ReinforcedSurvivalSuit(),
				new HoverbikeMobilityUpgrade(),
				new PowerglideEquipable(),
				new SuperSurvivalSuit(),
				new SurvivalSuitBlueprint_BaseSuits(),
				new SurvivalSuitBlueprint_FromReinforcedSurvival(),
				new SurvivalSuitBlueprint_FromReinforcedCold(),
				new SurvivalSuitBlueprint_FromSurvivalCold(),
				new DiverPerimeterDefenceChip_Broken(),
				new DiverPerimeterDefenceChipItem(),
			})
			{
				s.Patch();
			}

			new Harmony($"DaWrecka_{myAssembly.GetName().Name}").PatchAll(myAssembly);
		}

		[QModPrePatch]
		public static void PrePatch()
		{
			// Preston's Plant becomes 2x2 instead of 1x1
			//WorldEntities/Flora/Expansion/Shared/vegetable_plant_01_fruit.prefab
			/*
			CraftDataHandler.SetItemSize(TechType.SnowStalkerPlant, new Vector2int(2, 2)); // this affects the inventory only, or more specifically how much space the plant requires in a planter.
				// It does nothing about the actual growing size of the plant; other methods are required for that.
				// We need to know the prefab ahead of time; there is no known method to get the following prefab filename from the plant's TechType. The methods we can use for batteries
				// won't work for Preston's Plant, and likely other plantables too.
			AddressablesUtility.LoadAsync<GameObject>("WorldEntities/Flora/Expansion/Shared/vegetable_plant_01_fruit.prefab").Completed += (x) =>
			{
				GameObject gameObject1 = x.Result;
				Plantable component = gameObject1?.GetComponent<Plantable>();
				if (component != null)
					component.size = Plantable.PlantSize.Large;
			};*/


			foreach (TechType tt in new List<TechType>()
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
			}
		}

		[QModPostPatch]
		public static void PostPatch()
		{
			//CraftDataHandler.SetBackgroundType(prefabLightningClaw.TechType, CraftData.BackgroundType.ExosuitArm);
			//CraftDataHandler.SetBackgroundType(prefabExosuitSprintModule.TechType, CraftData.BackgroundType.Normal);
			CraftDataHandler.SetBackgroundType(GetModTechType("ExosuitSprintModule"), CraftData.BackgroundType.Normal);

			// This is test code
			//string PrefabFilename;
			//if it works, the following changes are made;

			Batteries.PostPatch();
			LanguageHandler.SetLanguageLine("SeamothWelcomeAboard", "Welcome aboard captain.");
			//Reflection.PostPatch();
		}
	}

	[HarmonyPatch]
	public class Reflection
	{
		private static readonly MethodInfo addJsonPropertyInfo = typeof(CraftDataHandler).GetMethod("AddJsonProperty", BindingFlags.NonPublic | BindingFlags.Static);
		private static readonly MethodInfo playerUpdateReinforcedSuitInfo = typeof(Player).GetMethod("UpdateReinforcedSuit", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo playerCheckColdsuitGoalInfo = typeof(Player).GetMethod("CheckColdsuitGoal", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly FieldInfo knownTechCompoundTech = typeof(KnownTech).GetField("compoundTech", BindingFlags.NonPublic | BindingFlags.Static);
		private static List<KnownTech.CompoundTech> pendingCompoundTech = new List<KnownTech.CompoundTech>();
		private static bool bProcessingCompounds;

		public static void AddJsonProperty(TechType techType, string key, JsonValue newValue)
		{
			addJsonPropertyInfo.Invoke(null, new object[] { techType, key, newValue });
		}

		public static void AddColdResistance(TechType techType, int newValue)
		{
			//AddJsonProperty(techType, "coldResistance", new JsonValue(newValue));
			CraftDataHandler.SetColdResistance(techType, newValue);
		}

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
