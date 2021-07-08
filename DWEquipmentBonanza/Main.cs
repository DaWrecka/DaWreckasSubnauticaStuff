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
using DWEquipmentBonanza.Equipables;
using DWEquipmentBonanza.Patches;
using DWEquipmentBonanza.VehicleModules;
using Common;
using UWE;
using UnityEngine;
using DWEquipmentBonanza.Spawnables;
using SMLHelper.V2.Assets;
using System.Collections;
using System.IO;
using SMLHelper.V2.Utility;
#if SUBNAUTICA_STABLE
	using Oculus.Newtonsoft.Json;
	using Oculus.Newtonsoft.Json.Serialization;
	using Oculus.Newtonsoft.Json.Converters;
#elif BELOWZERO
	using CustomDataboxes.API;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Serialization;
	using Newtonsoft.Json.Converters;
#endif

namespace DWEquipmentBonanza
{
	[QModCore]
	public class Main
	{
		internal static bool bVerboseLogging = true;
		internal static bool bLogTranspilers = false;
		internal const string version = "0.8.0.3";
		public static bool bInAcid = false; // Whether or not the player is currently immersed in acid
		public static List<string> playerSlots = new List<string>()
		{
			"Head",
			"Body",
			"Gloves",
			"Foots", // Seriously? 'Foots'?
            "Chip1",
			"Chip2",
			"Tank"
		};

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

		internal static List<string> _chipSlots = new List<string>();
		internal static List<string> chipSlots
		{
			get
			{
				if(_chipSlots.Count < 1 && Inventory.main?.equipment != null)
				{
					Inventory.main.equipment.GetSlots(EquipmentType.Chip, _chipSlots);
				}

				return _chipSlots;
			}
		}

		private static readonly Assembly myAssembly = Assembly.GetExecutingAssembly();
		private static readonly string modPath = Path.GetDirectoryName(myAssembly.Location);
		internal static readonly string AssetsFolder = Path.Combine(modPath, "Assets");

		private static readonly Type NitrogenMain = Type.GetType("NitrogenMod.Main, NitrogenMod", false, false);
		internal static readonly MethodInfo NitroAddDiveSuit = NitrogenMain?.GetMethod("AddDiveSuit", BindingFlags.Public | BindingFlags.Static);
		internal static readonly Texture2D glovesTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidGlovesskin.png"));
		internal static readonly Texture2D suitTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidSuitskin.png"));
		internal static readonly Texture2D glovesIllumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidGlovesillum.png"));
		internal static readonly Texture2D suitIllumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidSuitillum.png"));

		public static bool bUseNitrogenAPI; // If true, use the Nitrogen API instead of patching GetTechTypeInSlot. Overrides bNoPatchTechTypeInSlot.
		private static Dictionary<string, TechType> NitrogenTechtypes = new Dictionary<string, TechType>();

		internal static void AddSubstitution(TechType custom, TechType vanilla)
		{
			EquipmentPatch.AddSubstitution(custom, vanilla);
			PlayerPatch.AddSubstitution(custom, vanilla);
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
			TechTypeUtils.AddModTechType(tech, prefab);
		}

		public static TechType GetModTechType(string key)
		{
			return TechTypeUtils.GetModTechType(key);
		}

		internal static GameObject GetModPrefab(string key)
		{
			return TechTypeUtils.GetModPrefab(key);
		}

		public static int EquipmentGetCount(Equipment e, TechType[] techTypes)
		{
			int count = 0;
			foreach (TechType tt in techTypes)
			{
				if (tt == TechType.None)
					continue;

				count += e.GetCount(tt);
			}
			return count;
		}

		public static TechType GetNitrogenTechtype(string name)
		{
			TechType tt;
			if (NitrogenTechtypes.TryGetValue(name, out tt))
				return tt;

			if (SMLHelper.V2.Handlers.TechTypeHandler.TryGetModdedTechType(name, out tt))
				return tt;

			return TechType.None;
		}

		public static bool HasNitrogenMod()
		{
			return QModServices.Main.ModPresent("NitrogenMod");
		}

		internal struct DamageInfo
		{
			public DamageType damageType;
			public float damageMult;

			public DamageInfo(DamageType t, float m)
			{
				this.damageType = t;
				this.damageMult = m;
			}
		}

		/*internal struct DamageResistance
		{
			public TechType TechType;
			public DamageInfo[] damageInfoList;

			public DamageResistance(TechType tt, DamageInfo[] dil)
			{
				this.TechType = tt;
				this.damageInfoList = dil;
			}
		}*/

		// This particular system is not that useful, but it could be expanded to allow any sort of equipment type to reduce damage.
		// For example, you could add a chip that projects a sort of shield that protects from environmental damage, such as Acid, Radiation, Heat, Poison, or others.
		// Although the system would need to be extended to allow, say, a shield that drains a battery when resisting damage.
		//Interfaces would be the way I think, but I've not yet wrapped my brain around that.
		// BZ has a DamageModifier component available that does basically this.
		internal static Dictionary<TechType, List<DamageInfo>> DamageResistances = new Dictionary<TechType, List<DamageInfo>>();
		public static float ModifyDamage(TechType tt, float damage, DamageType type)
		{
			float baseDamage = damage;
			float damageMod = 0f;
			//Logger.Log(Logger.Level.Debug, $"Main.ModifyDamage called: tt = {tt.ToString()}, damage = {damage}; DamageType = {type}");
			//foreach (DamageResistance r in DamageResistances)
			if (DamageResistances.TryGetValue(tt, out List<DamageInfo> diList))
			{
				//Logger.Log(Logger.Level.Debug, $"Found DamageResistance with TechType: {r.TechType.ToString()}");
				foreach (DamageInfo d in diList)
				{
					if (d.damageType == type)
					{
						damageMod += baseDamage * d.damageMult;
						//Logger.Log(Logger.Level.Debug, $"Player has equipped armour of TechType {tt.ToString()}, base damage = {baseDamage}, type = {type}, modifying damage by {d.damageMult}x with result of {damageMod}");
					}
				}
			}
			return damageMod;
		}

		[QModPrePatch]
		public static void PrePatch()
		{
		}

		[QModPatch]
		public static void Load()
		{
			if (QModServices.Main.ModPresent("CombinedItems") || QModServices.Main.ModPresent("AcidProofSuit"))
			{
				throw new Exception("Equipment Bonanza is a replacement for Combined Items and Brine Suit. Remove those mods and try again.");
			}

#if SUBNAUTICA_STABLE
			Log.LogDebug("Checking for Nitrogen mod");
			bool bHasN2 = QModServices.Main.ModPresent("NitrogenMod");
			string sStatus = "Nitrogen mod " + (bHasN2 ? "" : "not ") + "present";
			Log.LogDebug(sStatus);

#elif BELOWZERO
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
			/*
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "DPDTier1", "Diver Perimeter Defence Chip", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal", "ChipRecharge" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "DPDTier2", "Diver Perimeter Defence Chip Mk2", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal", "ChipRecharge" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "DPDTier3", "Diver Perimeter Defence Chip Mk3", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal", "ChipRecharge" });
			*/
			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeSilentModule" });
			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeJumpModule" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeIceWormReductionModule, new string[] { "Upgrades", "HoverbikeUpgrades" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeJumpModule, new string[] { "Upgrades", "HoverbikeUpgrades" });
#endif

			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "ChipEquipment", "Chips", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "BodyMenu", "Suit Upgrades", SpriteManager.Get(TechType.Stillsuit));
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "ChipRecharge", "Chip Recharges", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal" });

			var prefabs = new List<Spawnable>() {
				//new ExosuitLightningClawPrefab(),
				new ExosuitLightningClawGeneratorModule(),
				new PowerglideFragmentPrefab(),
				new SurvivalSuit(),
				new PowerglideEquipable(),
				new ReinforcedSurvivalSuit(),
#if SUBNAUTICA_STABLE
                new AcidSuit(),
                new AcidGloves(),
                new AcidHelmet(),
                //new Blueprint_Suits(),
#elif BELOWZERO
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
				new ShadowLeviathanSample(),
				new SurvivalSuitBlueprint_FromReinforcedSurvival(),
#endif
				//new SurvivalSuitBlueprint_BaseSuits(),
				new DiverPerimeterDefenceChip_Broken(),
				new DiverPerimeterDefenceChipItem(),
				new DiverDefenceSystemMk2(),
				new DiverDefenceMk2_FromBrokenChip(),
				new DiverDefenceSystemMk3()
			};


#if SUBNAUTICA_STABLE
			if (bHasN2)
			{
				Log.LogDebug($"Main.Load(): Found NitrogenMod, adding Nitrogen prefabs");
				foreach (string sTechType in new List<string> { "reinforcedsuit2", "reinforcedsuit3", "rivereelscale", "lavalizardscale" })
				{
					if (SMLHelper.V2.Handlers.TechTypeHandler.TryGetModdedTechType(sTechType, out TechType tt))
					{
						NitrogenTechtypes.Add(sTechType, tt);
						bHasN2 = true;
					}
					else
					{
						Log.LogDebug($"Load(): Could not find TechType for Nitrogen class ID {sTechType}");
					}
				}
				//prefabSuitMk2 = new NitrogenBrineSuit2();
				//prefabSuitMk3 = new NitrogenBrineSuit3();
				prefabs.Add(new NitrogenBrineSuit2());
				prefabs.Add(new NitrogenBrineSuit3());
				prefabs.Add(new Blueprint_BrineMk1toMk2());
				prefabs.Add(new Blueprint_BrineMk2toMk3());
				//prefabs.Add(new Blueprint_BrineMk1toMk3());
				prefabs.Add(new Blueprint_ReinforcedMk2toBrineMk2());
				prefabs.Add(new Blueprint_ReinforcedMk3toBrineMk3());
			}
#endif

			// These may depend on Nitrogen 
			prefabs.Add(new SuperSurvivalSuit());


			foreach (Spawnable s in prefabs)
			{
				s.Patch();
			}

#if BELOWZERO
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


#elif SUBNAUTICA_STABLE
			Log.LogDebug($"Setting up DamageResistances list");
			Main.DamageResistances = new Dictionary<TechType, List<DamageInfo>> {
                // Gloves
                {
					TechTypeUtils.GetModTechType("AcidGloves"), new List<DamageInfo> {
						new DamageInfo(DamageType.Acid, -0.15f)/*,
                        new DamageInfo(DamageType.Radiation, -0.10f)*/
                    }
				},


                // Helmet
                {
					TechTypeUtils.GetModTechType("AcidHelmet"), new List<DamageInfo> {
						new DamageInfo(DamageType.Acid, -0.25f)/*,
                        new DamageInfo(DamageType.Radiation, -0.20f)*/
                    }
				},


            // Suit
                {
					TechTypeUtils.GetModTechType("AcidSuit"), new List<DamageInfo> {
						new DamageInfo(DamageType.Acid, -0.6f)/*,
                        new DamageInfo(DamageType.Radiation, -0.70f)*/
                    }
				}
			};
#endif
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
		private static readonly MethodInfo playerUpdateReinforcedSuitInfo = typeof(Player).GetMethod("UpdateReinforcedSuit", BindingFlags.NonPublic | BindingFlags.Instance);
#if BELOWZERO
		private static readonly MethodInfo addJsonPropertyInfo = typeof(CraftDataHandler).GetMethod("AddJsonProperty", BindingFlags.NonPublic | BindingFlags.Static);
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

		public static void PlayerCheckColdsuitGoal(Player player)
		{
			playerCheckColdsuitGoalInfo.Invoke(player, new object[] { });
		}
#endif

		public static void PlayerUpdateReinforcedSuit(Player player)
		{
			playerUpdateReinforcedSuitInfo.Invoke(player, new object[] { });
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
