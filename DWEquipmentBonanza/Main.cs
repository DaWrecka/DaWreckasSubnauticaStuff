using HarmonyLib;
using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using CustomDataboxes.API;
using SMLHelper.V2.Json.Attributes;
using SMLHelper.V2.Json;
using DWEquipmentBonanza.MonoBehaviours;
#if SUBNAUTICA_STABLE
	using Sprite = Atlas.Sprite;
	using Oculus.Newtonsoft.Json;
	using Oculus.Newtonsoft.Json.Serialization;
	using Oculus.Newtonsoft.Json.Converters;
#elif BELOWZERO
#endif

namespace DWEquipmentBonanza
{
	[FileName("DWEquipmentBonanza")]
	[Serializable]
	internal class DWDataFile : SaveDataCache
	{
		private HashSet<ISerializationCallbackReceiver> activeReceivers = new HashSet<ISerializationCallbackReceiver>();
		public Dictionary<string, float> ModuleCharges = new Dictionary<string, float>();

		internal void Init()
		{
			if (ModuleCharges == null)
				ModuleCharges = new Dictionary<string, float>();
			//IngameMenuHandler.RegisterOnLoadEvent(OnLoad);
			//IngameMenuHandler.RegisterOnSaveEvent(OnSave);
			this.OnStartedSaving += (object sender, JsonFileEventArgs e) =>
			{
				DWDataFile data = e.Instance as DWDataFile;
				data.OnSave();
			};
		}

		internal void OnLoad()
		{
		}

		internal void OnSave()
		{
			foreach (var c in activeReceivers)
			{
				c.OnBeforeSerialize();
			}
		}

		public void AddModuleCharge(string UUID, float charge)
		{
			ModuleCharges[UUID] = charge;
		}

		public bool TryGetModuleCharge(string UUID, out float charge)
		{
			if (string.IsNullOrWhiteSpace(UUID))
			{
				Log.LogError($"TryGetModuleCharge() called with null or blank key!");
				charge = -1f;
				return false;
			}

			return ModuleCharges.TryGetValue(UUID, out charge);
		}

		// ISerializationCallbackReceiver is supposed to receive callbacks when game saving begins, but it doesn't seem to be working properly.
		// Worse, they seem to receive OnBeforeSerialize() callbacks at the start of the *loading* process.
		// Here, such receivers register themselves so that the SaveDataCache can make *sure* that they get a call.
		public bool RegisterReceiver(ISerializationCallbackReceiver v)
		{
			if (v == null || activeReceivers.Contains(v))
				return false;

			activeReceivers.Add(v);
			return true;
		}

		public bool UnregisterReceiver(ISerializationCallbackReceiver v)
		{
			if (v != null && activeReceivers.Contains(v))
			{
				activeReceivers.Remove(v);
				return true;
			}

			return false;
		}
	}

	[QModCore]
	public class Main
	{
		internal static bool bVerboseLogging = true;
		internal static bool bLogTranspilers = false;
		internal const string version = "0.9.0.0";
#if SUBNAUTICA_STABLE
		public static bool bInAcid = false; // Whether or not the player is currently immersed in acid
#endif
		/*public static List<string> playerSlots = new List<string>()
		{
			"Head",
			"Body",
			"Gloves",
			"Foots", // Seriously? 'Foots'?
			"Chip1",
			"Chip2",
			"Tank"
		};*/
		public static HashSet<string> playerSlots => Equipment.slotMapping.Keys.ToHashSet<string>();

		internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();
		internal static DWDataFile saveCache { get; private set; }

		private static readonly Type CustomiseOxygen = Type.GetType("CustomiseOxygen.Main, CustomiseOxygen", false, false);
		private static readonly MethodInfo CustomOxyAddExclusionMethod = CustomiseOxygen?.GetMethod("AddExclusion", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo CustomOxyAddTankMethod = CustomiseOxygen?.GetMethod("AddTank", BindingFlags.Public | BindingFlags.Static);
		//private static readonly PropertyInfo compatibleTechInfo = typeof(BatteryCharger).GetProperty("compatibleTech", BindingFlags.NonPublic | BindingFlags.Static);
		//private static HashSet<TechType> compatibleBatteries => (HashSet<TechType>)compatibleTechInfo.GetValue(null);
		internal static HashSet<TechType> compatibleBatteries => BatteryCharger.compatibleTech;

		//private static readonly Dictionary<string, TechType> ModTechTypes = new Dictionary<string, TechType>(StringComparer.OrdinalIgnoreCase);
		//private static readonly Dictionary<string, GameObject> ModPrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
		internal static readonly Dictionary<TechType, float> defaultHealth = new Dictionary<TechType, float>();

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
		private static readonly MethodInfo NitroAddDiveSuit = NitrogenMain?.GetMethod("AddDiveSuit", BindingFlags.Public | BindingFlags.Static);
		internal static readonly Texture2D glovesTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidGlovesskin.png"));
		internal static readonly Texture2D suitTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidSuitskin.png"));
		internal static readonly Texture2D glovesIllumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidGlovesillum.png"));
		internal static readonly Texture2D suitIllumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidSuitillum.png"));

		private static readonly bool bCustomOxygenMode = QModServices.Main.ModPresent("CustomiseOxygen");

		public static bool bUseNitrogenAPI; // If true, use the Nitrogen API instead of patching GetTechTypeInSlot. Overrides bNoPatchTechTypeInSlot.
		private static Dictionary<string, TechType> NitrogenTechtypes = new Dictionary<string, TechType>();
		internal static GameObject HighCapacityTankPrefab;
		internal static TechType StillSuitType
		{
			get
			{
#if SUBNAUTICA_STABLE
				return TechType.Stillsuit;

#elif BELOWZERO
				return TechType.WaterFiltrationSuit;

#endif
			}
		}

		private static Dictionary<TechType, float> suitTemperatureResistance = new Dictionary<TechType, float>();

		internal static void AddSubstitution(TechType custom, TechType vanilla)
		{
			EquipmentPatch.AddSubstitution(custom, vanilla);
			PlayerPatch.AddSubstitution(custom, vanilla);
		}

		internal static void AddCustomOxyExclusion(TechType excludedTank, bool bExcludeMultipliers, bool bExcludeOverride)
		{
			if (CustomOxyAddExclusionMethod != null)
				CustomOxyAddExclusionMethod.Invoke(null, new object[] { excludedTank, bExcludeMultipliers, bExcludeOverride });
			else
			{
				if(bCustomOxygenMode)
					Log.LogError($"Could not get Custom Oxygen AddExclusion method");
			}
		}

		internal static void AddCustomOxyTank(TechType tank, float capacity, Sprite icon = null, bool bUnlockAtStart = false)
		{
			if (CustomOxyAddTankMethod != null)
				CustomOxyAddTankMethod.Invoke(null, new object[] { tank, capacity, icon, bUnlockAtStart });
			else
			{
				if (bCustomOxygenMode)
					Log.LogError($"Could not get Custom Oxygen AddTank method");
			}
		}

		internal static void AddModTechType(TechType tech, GameObject prefab = null)
		{
			TechTypeUtils.AddModTechType(tech, prefab);
		}

		public static TechType GetModTechType(string key)
		{
			return TechTypeUtils.GetModTechType(key);
		}

		public static void AddDiveSuit(TechType diveSuit, float depth = 0f, float breathMultiplier = 1f, float minTempBonus = 0f)
		{
			if (NitroAddDiveSuit != null)
				NitroAddDiveSuit.Invoke(null, new object[] { diveSuit, depth, breathMultiplier, minTempBonus });
			else
			{
				suitTemperatureResistance[diveSuit] = minTempBonus;
			}
		}

		internal static float GetDiveSuitTempBonus(TechType suit)
		{
			return suitTemperatureResistance.GetOrDefault(suit, 0f);
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
				if (tt != TechType.None)
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

		/*public struct DamageMod
		{
			public DamageType damageType;
			public float damageMult;

			public DamageMod(DamageType t, float m)
			{
				this.damageType = t;
				this.damageMult = m;
			}
		}*/

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
		// BZ has a DamageModifier component available that does basically this, but it's unclear to what extent, if any, it works in SN1.
		//private static Dictionary<TechType, List<DamageMod>> DamageResistances = new Dictionary<TechType, List<DamageMod>>();
		public static Dictionary<TechType, Dictionary<DamageType, float> > DamageResistances = new Dictionary<TechType, Dictionary<DamageType, float> >();

		public static void AddDamageResist(TechType tt, DamageType damageType, float damageMult)
		{
			Log.LogDebug($"Main.AddDamageResist(): TechType = {tt.AsString()}, damageType = {damageType.ToString()}, damageMult = {damageMult}");
			if (DamageResistances.TryGetValue(tt, out Dictionary<DamageType, float> DamageModifiers))
			{
				if (DamageModifiers.TryGetValue(damageType, out float modifier))
				{
					Log.LogDebug($"AddDamageResist(): Tried to add modifier for DamageType {damageType.ToString()} to TechType {tt.AsString()} more than once; old value {modifier}.");
				}
				else
					DamageModifiers.Add(damageType, damageMult);
			}
			else
			{
				DamageResistances.Add(tt, new Dictionary<DamageType, float>()
				{
					{ damageType, damageMult }
				});
			}
		}

		public static float ModifyDamage(TechType tt, float damage, DamageType type)
		{
			float baseDamage = damage;
			float damageMod = 0f;
			Log.LogDebug($"Main.ModifyDamage called: tt = {tt.ToString()}, damage = {damage}; DamageType = {type}");
			//foreach (DamageResistance r in DamageResistances)
			if (DamageResistances.TryGetValue(tt, out Dictionary<DamageType, float> diList))
			{
				if (diList.TryGetValue(type, out float mult))
				{
					Log.LogDebug($"Got damage multiplier of {mult}");
					damageMod += baseDamage * mult;
				}
			}
			Log.LogDebug($"DamageMod = {damageMod}");
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
			//string sStatus = "Nitrogen mod " + (bHasN2 ? "" : "not ") + "present";
			Log.LogDebug("Nitrogen mod " + (bHasN2 ? "" : "not ") + "present");

#elif BELOWZERO
			// We're going to try and remove crafting nodes from the root of the workbench menu and move them into tabs.
			// Knives
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.KnifeMenuPath, "Knife Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "workbench_knifemenu"));
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "HeatBlade" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.HeatBlade, new string[] { DWConstants.KnifeMenuPath });

			// Tanks
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "HighCapacityTank" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.TankMenuPath, "Tank Upgrades", SpriteManager.Get(TechType.HighCapacityTank));
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.PlasteelTank, new string[] { DWConstants.TankMenuPath });
			CraftDataHandler.SetTechData(TechType.PlasteelTank, new SMLHelper.V2.Crafting.RecipeData()
				{
					craftAmount = 1,
					Ingredients = new List<Ingredient>()
					{
						new Ingredient(TechType.DoubleTank, 1),
						new Ingredient(TechType.Silicone, 2),
						new Ingredient(TechType.Titanium, 1),
						new Ingredient(TechType.Lithium, 1)
					}
				}
			);
			 CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.HighCapacityTank, new string[] { DWConstants.TankMenuPath });
			KnownTechHandler.SetAnalysisTechEntry(TechType.HighCapacityTank, new TechType[] { TechType.PlasteelTank });

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
				}
			);
			KnownTechHandler.SetAnalysisTechEntry(TechType.SwimChargeFins, new TechType[] { TechType.UltraGlideFins }); 
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.UltraGlideFins, new string[] { DWConstants.FinsMenuPath });
			
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.FinsMenuPath, "Fin Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "workbench_finsmenu"));
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "SwimChargeFins" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.SwimChargeFins, new string[] { DWConstants.FinsMenuPath });

			// Seatruck Upgrades
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.SeatruckMenuPath, "Seatruck Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "fabricator_seatruckupgrades"));
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "SeaTruckUpgradeHull2" });
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "SeaTruckUpgradeHull3" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.SeaTruckUpgradeHull2, new string[] { DWConstants.SeatruckMenuPath });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.SeaTruckUpgradeHull3, new string[] { DWConstants.SeatruckMenuPath });

			// Exosuit Upgrades
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "ExosuitMenu", "Exosuit Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "workbench_exosuitmenu"));
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "ExoHullModule2" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.ExoHullModule2, new string[] { "ExosuitMenu" });

			// Now our custom stuff
			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeSilentModule" });
			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeJumpModule" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeIceWormReductionModule, new string[] { "Upgrades", "HoverbikeUpgrades" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeJumpModule, new string[] { "Upgrades", "HoverbikeUpgrades" });
#endif

			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.BodyMenuPath, "Suit Upgrades", SpriteManager.Get(Main.StillSuitType));
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, DWConstants.ChipsMenuPath, "Chips", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal" });
			//CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "ChipRecharge", "Chip Recharges", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal" });

			var prefabs = new List<Spawnable>() {
				//new ExosuitLightningClawPrefab(),
#if SUBNAUTICA_STABLE
				new AcidGloves(),
				new AcidHelmet(),
				new AcidSuit(),
				//new Blueprint_Suits(),
				new SeamothSolarModuleMk2(),
				new SeamothThermalModule(),
				new SeamothThermalModuleMk2(),
				new SeamothUnifiedChargerModule(),
#elif BELOWZERO
				new InsulatedRebreather(),
				new ReinforcedColdSuit(),
				new ReinforcedColdGloves(),
				new HighCapacityBooster(),
				new SurvivalColdSuit(),
				new SurvivalSuitBlueprint_FromReinforcedCold(),
				new SurvivalSuitBlueprint_FromSurvivalCold(),
				new HoverbikeMobilityUpgrade(),
				new SeaTruckSolarModule(),
				new SeatruckSolarModuleMk2(),
				new SeatruckThermalModule(),
				new SeatruckThermalModuleMk2(),
				new SeatruckUnifiedChargerModule(),
				new SeaTruckSonarModule(),
				new ShadowLeviathanSample(),
				new SurvivalSuitBlueprint_FromReinforcedSurvival(),
				new IonBoosterTank(),
				new SeatruckRepairModule(),
#endif
				new DiverPerimeterDefenceChip_Broken(),
				new DiverPerimeterDefenceChipItem(),
				new DiverDefenceSystemMk2(),
				new DiverDefenceMk2_FromBrokenChip(),
				new DiverDefenceSystemMk3(),
				new PowerglideFragmentPrefab(),
				new SurvivalSuit(),
				new PowerglideEquipable(),
				new ReinforcedSurvivalSuit(),
				new ExosuitLightningClawGeneratorModule(),
				new Vibroblade(),
				new DWUltraGlideSwimChargeFins(),
				new PlasteelHighCapTank(),
				new ExosuitSolarModule(),
				new ExosuitSolarModuleMk2(),
				new ExosuitThermalModuleMk2(),
				new ExosuitUnifiedChargerModule(),
				new VehicleRepairModule()
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
				prefabs.Add(new NitrogenBrineSuit2());
				prefabs.Add(new NitrogenBrineSuit3());
				prefabs.Add(new Blueprint_ReinforcedMk2toBrineMk2());
				prefabs.Add(new Blueprint_ReinforcedMk3toBrineMk3());
			}
#endif

			// These may depend on Nitrogen, or they may not; but if they do they must be loaded afterwards.
			prefabs.Add(new SuperSurvivalSuit());


			foreach (Spawnable s in prefabs)
			{
				s.Patch();
			}

			Databox powerglideDatabox = new Databox()
			{
				DataboxID = "PowerglideDatabox",
				PrimaryDescription = PowerglideEquipable.friendlyName + " Databox",
				SecondaryDescription = PowerglideEquipable.description,
				TechTypeToUnlock = GetModTechType("PowerglideEquipable"),
#if SUBNAUTICA_STABLE
				CoordinatedSpawns = new List<Spawnable.SpawnLocation>()
				{
					new Spawnable.SpawnLocation(new Vector3(-1407.51f, -332.47f, 740.66f), new Vector3(6.93f, 275.67f, 0.00f)),
					//new Spawnable.SpawnLocation(new Vector3(-1384.79f, -330.18f, 718.84f), new Vector3(1.22f, 194.60f, 357.64f))
				}
#elif BELOWZERO
				CoordinatedSpawns = new List<Spawnable.SpawnLocation>()
				{
					new Spawnable.SpawnLocation(new Vector3(285f, -242.07f, -1299f), new Vector3(344f, 3.77f, 14f))
				}
#endif
			};
			powerglideDatabox.Patch();

			saveCache = SaveDataHandler.Main.RegisterSaveDataCache<DWDataFile>();
			saveCache.Init();

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
			CoroutineHost.StartCoroutine(PostPatchCoroutine());
		}

		internal static IEnumerator PostPatchCoroutine()
		{
			foreach (TechType tt in new HashSet<TechType>() {
			TechType.Exosuit,
#if SUBNAUTICA_STABLE
			TechType.Seamoth,
			TechType.Cyclops,
#elif BELOWZERO
			TechType.SeaTruck,
			TechType.Hoverbike
#endif
			})
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(tt);
				yield return task;

				GameObject prefab = task.GetResult();
				if (prefab != null)
				{
					LiveMixin mixin = prefab.GetComponent<LiveMixin>();
					if (mixin?.data != null)
					{
						Main.defaultHealth.Add(tt, mixin.data.maxHealth);
						Log.LogDebug($"For TechType {tt.AsString()}, got default health of {mixin.data.maxHealth}");
					}
					else
					{
						Log.LogDebug($"Failed to get LiveMixin for TechType {tt.AsString()}");
					}
				}
				else
				{
					Log.LogDebug($"Failed to get prefab for TechType {tt.AsString()}");
				}
			}

			foreach (TechType tt in new HashSet<TechType>()
			{
				TechType.DrillableAluminiumOxide,
				TechType.DrillableCopper,
				TechType.DrillableDiamond,
				TechType.DrillableGold,
				TechType.DrillableKyanite,
				TechType.DrillableLead,
				TechType.DrillableLithium,
				TechType.DrillableMagnetite,
				TechType.DrillableMercury,
				TechType.DrillableNickel,
				TechType.DrillableQuartz,
				TechType.DrillableSalt,
				TechType.DrillableSilver,
				TechType.DrillableSulphur,
				TechType.DrillableTitanium,
				TechType.DrillableUranium
			})
			{
				Log.LogInfo($"Fixing Cell Level for TechType {tt.AsString()}");
				var classid = CraftData.GetClassIdForTechType(tt);
				if (WorldEntityDatabase.TryGetInfo(classid, out var worldEntityInfo))
				{
					worldEntityInfo.cellLevel = LargeWorldEntity.CellLevel.VeryFar;

					WorldEntityDatabase.main.infos[classid] = worldEntityInfo;
				}

				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(tt);
				yield return task;

				GameObject prefab = task.GetResult();
				if (prefab != null)
				{
					LargeWorldEntity lwe = prefab.GetComponent<LargeWorldEntity>();
					if (lwe != null)
					{
						lwe.cellLevel = LargeWorldEntity.CellLevel.VeryFar;
						Log.LogDebug($"CellLevel for TechType {tt.AsString()} updated to Far");
					}
					else
					{
						Log.LogWarning($"Could not find LargeWorldEntity component in prefab for TechType {tt.AsString()}");
					}
#if SUBNAUTICA_STABLE
					// Since we're here, make kyanite less troll-tastic.
					Drillable drillable = prefab.GetComponent<Drillable>();
					if (drillable != null && drillable.kChanceToSpawnResources < DWConstants.newKyaniteChance)
					{
						drillable.kChanceToSpawnResources = DWConstants.newKyaniteChance;
					}
#endif
				}
				else
				{
					Log.LogWarning($"Could not get prefab for TechType {tt.AsString()}");
				}
			}

			yield break;
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
		private static Dictionary<TechType, List<TechType> > pendingCompoundTech = new Dictionary<TechType, List<TechType>>();
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

		public static void AddCompoundTech(TechType techType, List<TechType> dependencies, bool bForce = false)
		{
			if (techType == TechType.None)
			{
				Log.LogError($"AddCompoundTech called with TechType.None");
				return;
			}

			if (dependencies == null || dependencies.Count < 1)
			{
				Log.LogError($"AddCompoundTech called with TechType {techType.AsString()} but null or zero-length dependencies list.");
				return;
			}

			if (pendingCompoundTech.ContainsKey(techType))
			{
				Log.LogError($"AddCompoundTech called with duplicate TechType {techType.AsString()}");
				return;
			}

			pendingCompoundTech.Add(techType, dependencies);
			//CoroutineHost.StartCoroutine(ProcessPendingCompounds(bForce));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(KnownTech), nameof(KnownTech.Initialize))]
		public static void PostKnownTechInit()
		{
			Log.LogDebug("Reflection.PostKnownTechInit() executing");
			CoroutineHost.StartCoroutine(ProcessPendingCompounds(false));
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

			Log.LogDebug("ProcessPendingCompounds executing");
			bProcessingCompounds = true;

			int tries = 0;
			while (pendingCompoundTech.Count > 0)
			{
				List<KnownTech.CompoundTech> compounds = (List<KnownTech.CompoundTech>)knownTechCompoundTech.GetValue(null);
				HashSet<TechType> removals = new HashSet<TechType>();
				Log.LogDebug($"Attempting to process pending compound tech: pendingCompoundTech.Count == {pendingCompoundTech.Count}, attempt {++tries}");
				if (compounds != null)
				{
					Log.LogDebug("Successfully retrieved KnownTech.compoundTech: Now processing pendingCompoundTech");
					foreach(KeyValuePair<TechType, List<TechType>> kvp in pendingCompoundTech)
					{
						Log.LogDebug($"Adding compoundTech: techType = {kvp.Key.AsString()}");
						KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
						compound.techType = kvp.Key;
						compound.dependencies = kvp.Value;
						compounds.Add(compound);
						removals.Add(kvp.Key);
					}
				}
				else
				{
					Log.LogDebug($"KnownTech.compoundTech could not be retrieved");
				}

				foreach(TechType tt in removals)
					pendingCompoundTech.Remove(tt);
				removals.Clear();

				yield return new WaitForSecondsRealtime(2f);
			}

			bProcessingCompounds = false;
		}
	}
}
