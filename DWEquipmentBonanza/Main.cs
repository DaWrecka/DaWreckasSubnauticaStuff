#if BEPINEX
	using BepInEx;
	using BepInEx.Logging;
#elif QMM
	using QModManager.API;
	using QModManager.API.ModLoading;
#endif
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DWEquipmentBonanza.Equipables;
using DWEquipmentBonanza.Patches;
using DWEquipmentBonanza.VehicleModules;
using Common;
using UWE;
using UnityEngine;
using DWEquipmentBonanza.Spawnables;
using System.Collections;
using System.IO;
using CustomDataboxes.API;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Handlers;
using Nautilus.Utility;
using Nautilus.Json.Attributes;
using Nautilus.Json;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#if SN1
	//using Ingredient = CraftData\.Ingredient;
#endif
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using SMLHelper.V2.Json.Attributes;
using SMLHelper.V2.Json;
#endif
using DWEquipmentBonanza.MonoBehaviours;
using Main = DWEquipmentBonanza.DWEBPlugin;
#if SN1
	//using Sprite = Atlas.Sprite;
using System.Diagnostics.Eventing.Reader;
#endif
#if LEGACY
	using Oculus.Newtonsoft.Json;
	using Oculus.Newtonsoft.Json.Serialization;
	using Oculus.Newtonsoft.Json.Converters;
#elif BELOWZERO
#endif

#if !NAUTILUS
using RecipeData = SMLHelper.V2.Crafting.RecipeData;
#endif

namespace DWEquipmentBonanza
{
#if BEPINEX
	[BepInPlugin(GUID, pluginName, version)]
	[BepInDependency(Common.Constants.CustomiseOxygenGUID, BepInDependency.DependencyFlags.SoftDependency)]

	#if BELOWZERO
		[BepInProcess("SubnauticaZero.exe")]
	#elif SN1
		[BepInDependency(Common.Constants.DeathRunGUID, BepInDependency.DependencyFlags.SoftDependency)]
		[BepInProcess("Subnautica.exe")]
	#endif
	public class DWEBPlugin : BaseUnityPlugin
	{
#elif QMM
	[QModCore]
	public static class DWEBPlugin
	{
#endif
#region[Declarations]
	public const string
			MODNAME = "DWEquipmentBonanza",
			AUTHOR = "dawrecka",
			GUID = "com." + AUTHOR + "." + MODNAME;
			private const string pluginName = "DW's Equipment Bonanza";
			internal const string version = "1.20.1.0";
		//private const string CustomOxygenGUID = "com." + AUTHOR + "." + "CustomiseOxygen";
#endregion

		private static readonly Harmony harmony = new Harmony(GUID);
		internal const bool bVerboseLogging = true;
		internal const bool bLogTranspilers = false;
#if SN1
		public static bool bInAcid { get; internal set; } = false; // Whether or not the player is currently immersed in acid
#endif
		public static HashSet<string> playerSlots => Equipment.slotMapping.Keys.ToHashSet<string>();

		internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();
		public static DWDataFile saveCache { get; private set; }

		private static readonly Type CustomiseOxygen = Type.GetType("CustomiseOxygen.CustomiseOxygenPlugin, CustomiseOxygen", false, false);
		private static readonly MethodInfo CustomOxyAddExclusionMethod = CustomiseOxygen?.GetMethod("AddExclusion", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo CustomOxyAddTankMethod = CustomiseOxygen?.GetMethod("AddTank", BindingFlags.Public | BindingFlags.Static);
		internal static HashSet<TechType> compatibleBatteries => BatteryCharger.compatibleTech;

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
		public static readonly string modPath = Path.GetDirectoryName(myAssembly.Location);
		public static readonly string AssetsFolder = Path.Combine(modPath, "Assets");

#if LEGACY && SN1
		private static Type NitrogenMain => Type.GetType("NitrogenMod.Main, NitrogenMod", false, true);
		private static Type DeathRunMain => Type.GetType("DeathRun.DeathRun, DeathRun", false, true);
		private static MethodInfo NitroAddDiveSuit => NitrogenMain?.GetMethod("AddDiveSuit", BindingFlags.Public | BindingFlags.Static);
		private static MethodInfo DeathRunAddDiveSuit => DeathRunMain?.GetMethod("AddDiveSuit", BindingFlags.Public | BindingFlags.Static);
		public static bool bUseNitrogenAPI => NitroAddDiveSuit != null || DeathRunAddDiveSuit != null; // If true, use the Nitrogen API instead of patching GetTechTypeInSlot. Overrides bNoPatchTechTypeInSlot.
#elif SN1
		private static Type DeathrunAPI => AccessTools.TypeByName("DeathrunRemade.DeathrunAPI");
		private static MethodInfo AddSuitCrushDepth => DeathrunAPI != null ? DeathrunAPI.GetMethod("AddSuitCrushDepth", new Type[] { typeof(TechType), typeof(IEnumerable<float>) }) : null;
		private static MethodInfo AddNitrogenModifier => DeathrunAPI != null ? DeathrunAPI.GetMethod("AddNitrogenModifier", new Type[] { typeof(TechType), typeof(IEnumerable<float>) }) : null;
		public static bool bUseNitrogenAPI => DeathrunAPI != null; // If true, use the Nitrogen API instead of patching GetTechTypeInSlot. Overrides bNoPatchTechTypeInSlot.
#endif

#if SN1
		internal static readonly Texture2D glovesTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidGlovesskin.png"));
		internal static readonly Texture2D suitTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidSuitskin.png"));
		internal static readonly Texture2D glovesIllumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidGlovesillum.png"));
		internal static readonly Texture2D suitIllumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidSuitillum.png"));
		private static Dictionary<string, TechType> NitrogenTechtypes = new Dictionary<string, TechType>();
#elif BELOWZERO
		//private static readonly Type VehicleUpgraderType = Type.GetType("UpgradedVehicles.VehicleUpgrader, UpgradedVehicles", false, false);
		//private static readonly MethodInfo VehicleUpgraderAddSpeedModifier = VehicleUpgraderType?.GetMethod("AddSpeedModifier", BindingFlags.Public | BindingFlags.Static);
#endif

#if QMM
		private static bool bCustomOxygenMode => QModServices.Main.ModPresent("CustomiseOxygen");
#elif BEPINEX
		private static bool bCustomOxygenMode => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(Common.Constants.CustomiseOxygenGUID);
#endif

		internal static GameObject HighCapacityTankPrefab = null;
		internal static TechType StillSuitType
		{
			get
			{
#if LEGACY
				return TechType.Stillsuit;
#else
				return TechType.WaterFiltrationSuit;
#endif
			}
		}

		private static Dictionary<TechType, float> equipTempBonus = new Dictionary<TechType, float>();

		internal static void AddSubstitution(TechType custom, TechType vanilla)
		{
			EquipmentPatches.AddSubstitution(custom, vanilla);
			PlayerPatch.AddSubstitution(custom, vanilla);
		}

		internal static void AddCustomOxyExclusion(TechType excludedTank, bool bExcludeMultipliers, bool bExcludeOverride)
		{
			if (CustomOxyAddExclusionMethod != null)
				CustomOxyAddExclusionMethod.Invoke(null, new object[] { excludedTank, bExcludeMultipliers, bExcludeOverride });
			else if(bCustomOxygenMode)
				Log.LogError($"Could not get Custom Oxygen AddExclusion method");
		}

		internal static void AddCustomOxyTank(TechType tank, float capacity, Sprite icon = null, bool bUnlockAtStart = false)
		{
			if (CustomOxyAddTankMethod != null)
				CustomOxyAddTankMethod.Invoke(null, new object[] { tank, capacity, bUnlockAtStart, icon, false });
			else if (bCustomOxygenMode)
				Log.LogError($"Could not get Custom Oxygen AddTank method");
		}

		internal static void AddModTechType(TechType tech, GameObject prefab = null)
		{
			//Console.WriteLine($"AddModTechType running: tech={tech.AsString()}");
			TechTypeUtils.AddModTechType(tech, prefab);
			//Console.WriteLine($"AddModTechType complete");
		}

		public static TechType GetModTechType(string key)
		{
			return TechTypeUtils.GetModTechType(key);
		}

		public static void AddTempBonusOnly(TechType itemType, float minTempBonus)
		{
			equipTempBonus[itemType] = minTempBonus;
		}

		public static void AddDiveSuit(TechType diveSuit, float depth = 0f, float breathMultiplier = 1f, float minTempBonus = 0f, float DeathRunDepth = -1f)
		{
			equipTempBonus[diveSuit] = minTempBonus;
#if SN1 && LEGACY
			if (NitroAddDiveSuit != null)
			{
				NitroAddDiveSuit.Invoke(null, new object[] { diveSuit, depth, breathMultiplier, minTempBonus });
				return;
			}

			if (DeathRunAddDiveSuit != null)
			{
				DeathRunAddDiveSuit.Invoke(null, new object[] { diveSuit, depth, breathMultiplier, minTempBonus, DeathRunDepth });
				return;
			}
#elif SN1 && NAUTILUS

			if (DeathrunAPI != null)
			{
				if (DeathRunDepth != -1f)
					AddSuitCrushDepth.Invoke(null, new object[] { diveSuit, new float[] { depth, DeathRunDepth } });
				else
					AddSuitCrushDepth.Invoke(null, new object[] { diveSuit, new float[] { depth } });
				AddNitrogenModifier.Invoke(null, new object[] { diveSuit, new float[] { breathMultiplier } });
				return;
			}

			if (HasNitrogenMod())
				Log.LogError($"Nitrogen mode is enabled, but neither NitrogenMod.AddDiveSuit nor DeathRun.AddDiveSuit could be found");
#endif
		}

		/*public static bool AddUVSpeedModifier(TechType module, float speedModifier, float efficiencyMultiplier)
		{
			if (VehicleUpgraderAddSpeedModifier == null)
				return false;

			VehicleUpgraderAddSpeedModifier.Invoke(null, new object[] { module, speedModifier, efficiencyMultiplier, false });
			return true;
		}*/

		internal static float GetTempBonusForTechType(TechType suit)
		{
			return equipTempBonus.GetOrDefault(suit, 0f);
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

#if SN1
		public static TechType GetNitrogenTechtype(string name)
		{
			TechType tt;
			if (NitrogenTechtypes.TryGetValue(name, out tt))
				return tt;

			if (Common.TechTypeUtils.TryGetModTechType(name, out tt))
				return tt;
			return TechType.None;
		}

		public static bool HasNitrogenMod() => NitrogenTechtypes.Count > 0;
#endif

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
			//Log.LogDebug($"Main.ModifyDamage called: tt = {tt.ToString()}, damage = {damage}; DamageType = {type}");
			//foreach (DamageResistance r in DamageResistances)
			if (DamageResistances.TryGetValue(tt, out Dictionary<DamageType, float> diList))
			{
				if (diList.TryGetValue(type, out float mult))
				{
					//Log.LogDebug($"Got damage multiplier of {mult}");
					damageMod += baseDamage * mult;
				}
			}
			//Log.LogDebug($"DamageMod = {damageMod}");
			return damageMod;
		}

		protected void SetTechData(TechType tt, RecipeData recipe)
		{
#if NAUTILUS
			CraftDataHandler.SetRecipeData(tt, recipe);
#else
			CraftDataHandler.SetTechData(tt, recipe);
#endif
		}

#if QMM
		[QModPatch]
#endif
		public void Start()
		{
			bool bHasN2 = false;
			//bool bHasDeathrun = false;
			string thisName = Assembly.GetExecutingAssembly().GetName().Name;
			//Console.WriteLine($"{thisName} Awake() executing");
#if LEGACY
			if (QModServices.Main.ModPresent("CombinedItems") || QModServices.Main.ModPresent("AcidProofSuit"))
			{
				throw new Exception("Equipment Bonanza is a replacement for Combined Items and Brine Suit and is not compatible with either. Remove those mods and try again.");
			}
#else
			Log.InitialiseLog(GUID);

	#if SN1
			Log.LogDebug("Checking for Nitrogen mod");
		#if QMM
			bHasN2 = QModServices.Main.ModPresent("NitrogenMod");
			bHasDeathrun = QModServices.Main.ModPresent("DeathRun");
			//string sStatus = "Nitrogen mod " + (bHasN2 ? "" : "not ") + "present";
			Log.LogDebug("Nitrogen mod " + (bHasN2 ? "" : "not ") + "present; DeathRun mod " + (bHasDeathrun ? "" : "not ") + "present");
		#endif
	#endif
			// We're going to try and remove crafting nodes from the root of the workbench menu and move them into tabs.
			//Console.WriteLine($"{thisName} Reordering Workbench");
			// Knives
			//Console.WriteLine($"{thisName} Creating Knife Upgrades tab");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.KnifeMenuPath, "Knife Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "workbench_knifemenu"));
			//Console.WriteLine($"{thisName} Removing existing Heatblade node");
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "HeatBlade" });
			//Console.WriteLine($"{thisName} Creating new Heatblade node");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.HeatBlade, new string[] { DWConstants.KnifeMenuPath });

			// Tanks
			//Console.WriteLine($"{thisName} Removing existing HighCapacityTank node");
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "HighCapacityTank" });
			//Console.WriteLine($"{thisName} Creating Tank Upgrades node");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.TankMenuPath, "Tank Upgrades", SpriteManager.Get(TechType.HighCapacityTank));
			//Console.WriteLine($"{thisName} Adding Plasteel Tank node");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.PlasteelTank, new string[] { DWConstants.TankMenuPath });
			//Console.WriteLine($"{thisName} Setting Plasteel Tank recipe");
			SetTechData(TechType.PlasteelTank, new RecipeData()
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
			//Console.WriteLine($"{thisName} Adding new HighCapacityTank node");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.HighCapacityTank, new string[] { DWConstants.TankMenuPath });
			//Console.WriteLine($"{thisName} Setting AnalysisTechEntry for PlasteelTank");
			KnownTechHandler.SetAnalysisTechEntry(TechType.HighCapacityTank, new TechType[] { TechType.PlasteelTank });

			// Fins menu
			//Console.WriteLine($"{thisName} Setting Ultra Glide Fins recipe");
			SetTechData(TechType.UltraGlideFins, new RecipeData()
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
			//Console.WriteLine($"{thisName} Setting AnalysisTechEntry for UltraGlideFins");
			KnownTechHandler.SetAnalysisTechEntry(TechType.SwimChargeFins, new TechType[] { TechType.UltraGlideFins });
			//Console.WriteLine($"{thisName} Creating Fin Upgrades tab");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.FinsMenuPath, "Fin Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "workbench_finsmenu"));
			//Console.WriteLine($"{thisName} Adding crafting node for UltraGlideFins");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.UltraGlideFins, new string[] { DWConstants.FinsMenuPath });

			//Console.WriteLine($"{thisName} Removing existing SwimChargeFins node");
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "SwimChargeFins" });
			//Console.WriteLine($"{thisName} Creating new SwimChargeFins crafting node");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.SwimChargeFins, new string[] { DWConstants.FinsMenuPath });

			// Exosuit Upgrades
			//Console.WriteLine($"{thisName} Creating Exosuit Upgrades tab");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "ExosuitMenu", "Exosuit Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "workbench_exosuitmenu"));
			//Console.WriteLine($"{thisName} Removing existing ExoHullModule2 node");
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "ExoHullModule2" });
			//Console.WriteLine($"{thisName} Creating new ExoHullModule2 node");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.ExoHullModule2, new string[] { "ExosuitMenu" });

	#if BELOWZERO
			// Now our custom stuff
			//Console.WriteLine($"{thisName} Removing existing HoverbikeSilentModule node");
			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeSilentModule" });
			//Console.WriteLine($"{thisName} Removing existing HoverbikeJumpModule node");
			CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Machines", "HoverbikeJumpModule" });
			//Console.WriteLine($"{thisName} Creating new HoverbikeIceWormReductionModule node");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeIceWormReductionModule, new string[] { "Upgrades", "HoverbikeUpgrades" });
			//Console.WriteLine($"{thisName} Creating new HoverbikeJumpModule node");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.HoverbikeJumpModule, new string[] { "Upgrades", "HoverbikeUpgrades" });

			// Seatruck Upgrades
			//Console.WriteLine($"{thisName} Creating Seatruck Upgrades tab");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.SeatruckMenuPath, "Seatruck Upgrades", SpriteManager.Get(SpriteManager.Group.Category, "fabricator_seatruckupgrades"));
			//Console.WriteLine($"{thisName} Removing existing SeaTruckUpgradeHull2 node");
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "SeaTruckUpgradeHull2" });
			//Console.WriteLine($"{thisName} Removing existing SeaTruckUpgradeHull3 node");
			CraftTreeHandler.RemoveNode(CraftTree.Type.Workbench, new string[] { "SeaTruckUpgradeHull3" });
			//Console.WriteLine($"{thisName} Creating new SeaTruckUpgradeHull2 node");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.SeaTruckUpgradeHull2, new string[] { DWConstants.SeatruckMenuPath });
			//Console.WriteLine($"{thisName} Creating new SeaTruckUpgradeHull3 node");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.SeaTruckUpgradeHull3, new string[] { DWConstants.SeatruckMenuPath });

			//Console.WriteLine($"{thisName} Adding VehicleArmorPlating crafting nodes");
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.SeamothUpgrades, TechType.VehicleArmorPlating, new string[] { "ExosuitModules" });
			CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.VehicleArmorPlating, new string[] { "Upgrades", "ExosuitUpgrades" });
			//Console.WriteLine($"{thisName} Setting VehicleArmorPlating AnalysisTechEntry");
			KnownTechHandler.SetAnalysisTechEntry(TechType.Exosuit, new TechType[] { TechType.VehicleArmorPlating });
			CraftTreeHandler.AddTabNode(CraftTree.Type.SeaTruckFabricator, DWConstants.ChipsMenuPath, "Chips", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal" });
	#endif

			//Console.WriteLine($"{thisName} Adding Suit Upgrades tab node");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, DWConstants.BodyMenuPath, "Suit Upgrades", SpriteManager.Get(Main.StillSuitType));
			//Console.WriteLine($"{thisName} Adding Vehicle Chargers tab node");
			CraftTreeHandler.AddTabNode(CraftTree.Type.SeamothUpgrades, DWConstants.ChargerMenuPath, "Vehicle Chargers", SpriteManager.Get(TechType.ExosuitThermalReactorModule));
			//Console.WriteLine($"{thisName} Adding Chips tab node");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, DWConstants.ChipsMenuPath, "Chips", SpriteManager.Get(TechType.MapRoomHUDChip), new string[] { "Personal" });

			//Console.WriteLine($"{thisName} Adding Headwear tab node");
//			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "TankRefill", "Tank Refills", SpriteManager.Get(TechType.DoubleTank), new string[] { "Personal" });
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, DWConstants.BaseHelmetsMenuName, "Headwear", DWConstants.BaseHelmetsIcon, new string[] { "Personal" });
#endif

			foreach (TechType tt in new List<TechType>()
			{
				TechType.Rebreather,
#if BELOWZERO
				TechType.ColdSuitHelmet
#endif
			})
			{
				//Console.WriteLine($"{thisName} Removing existing {tt.AsString()} node");
				CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Personal", "Equipment", tt.AsString() });
				//Console.WriteLine($"{thisName} Adding new {tt.AsString()} node");
				CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, tt, DWConstants.BaseHelmetPath);
			}
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, DWConstants.BaseSuitsMenuName, "Bodywear", DWConstants.BaseSuitsIcon, new string[] { "Personal" });
			foreach (TechType tt in new List<TechType>()
			{
				Main.StillSuitType,
				TechType.ReinforcedDiveSuit,
#if SN1
				TechType.RadiationSuit
#elif BELOWZERO
				TechType.ColdSuit,
				TechType.ColdSuitGloves
#endif
			})
			{
				//Console.WriteLine($"{thisName} Removing existing {tt.AsString()} node");
				CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, new string[] { "Personal", "Equipment", tt.AsString() });
				//Console.WriteLine($"{thisName} Adding new {tt.AsString()} node");
				CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, tt, DWConstants.BaseSuitsPath);
			}

			//Console.WriteLine($"{thisName} Preparing Spawnables list");
			var prefabs = new List<Spawnable>();
			prefabs.Add(new SurvivalSuit());
			prefabs.Add(new ReinforcedSurvivalSuit());
#if SN1
			prefabs.Add(new AcidGloves());
			prefabs.Add(new AcidHelmet());
			prefabs.Add(new AcidSuit());
				//new Blueprint_Suits(),
			prefabs.Add(new SeamothSolarModuleMk2());
			prefabs.Add(new SeamothThermalModule());
			prefabs.Add(new SeamothThermalModuleMk2());
			prefabs.Add(new SeamothUnifiedChargerModule());
			prefabs.Add(new FlashlightHelmet());
			prefabs.Add(new IlluminatedRebreather());
			prefabs.Add(new LightRadHelmet());
			prefabs.Add(new UltimateHelmet());
			prefabs.Add(new Blueprint_LightRebreatherPlus());
			prefabs.Add(new Blueprint_LightRadHelmetPlusRebreather());
			prefabs.Add(new Blueprint_FlashlightPlusBrineHelmet());
#elif BELOWZERO
			prefabs.Add(new ReinforcedColdGloves());
			prefabs.Add(new ReinforcedColdSuit());
			prefabs.Add(new HighCapacityBooster());
			prefabs.Add(new SurvivalColdSuit());
			prefabs.Add(new SeaTruckSolarModule());
			prefabs.Add(new SeatruckSolarModuleMk2());
			prefabs.Add(new SeatruckThermalModule());
			prefabs.Add(new SeatruckThermalModuleMk2());
			prefabs.Add(new SeatruckUnifiedChargerModule());
			prefabs.Add(new SeaTruckSonarModule());
			prefabs.Add(new ShadowLeviathanSample());
			prefabs.Add(new IonBoosterTank());
			prefabs.Add(new SeatruckRepairModule());
			prefabs.Add(new SeaTruckUpgradeHorsepower2());
			prefabs.Add(new SeaTruckUpgradeHorsepower3());
			prefabs.Add(new HoverbikeBoostUpgradeModule());
			prefabs.Add(new SeaTruckQuantumLocker());
			prefabs.Add(new HoverbikeQuantumLocker());
			prefabs.Add(new IlluminatedRebreather());
			prefabs.Add(new LightColdHelmet());
			prefabs.Add(new InsulatedRebreather());
			prefabs.Add(new UltimateHelmet());
			prefabs.Add(new Blueprint_LightRebreatherPlus());
			prefabs.Add(new Blueprint_LightColdToUltimateHelmet());
			prefabs.Add(new ExosuitGrappleUpgradeModule());
#endif
			prefabs.Add(new DiverPerimeterDefenceChip_Broken());
			prefabs.Add(new DiverPerimeterDefenceChipItem());
			prefabs.Add(new DiverDefenceSystemMk2());
			prefabs.Add(new DiverDefenceMk2_FromBrokenChip());
			prefabs.Add(new DiverDefenceSystemMk3());
			prefabs.Add(new PowerglideFragmentPrefab());
			prefabs.Add(new DWEBPowerglide());
			prefabs.Add(new ExosuitLightningClawGeneratorModule());
			prefabs.Add(new Vibroblade());
			prefabs.Add(new DWUltraGlideSwimChargeFins());
			prefabs.Add(new PlasteelHighCapTank());
			prefabs.Add(new ExosuitSolarModule());
			prefabs.Add(new ExosuitSolarModuleMk2());
			prefabs.Add(new ExosuitThermalModuleMk2());
			prefabs.Add(new ExosuitUnifiedChargerModule());
			prefabs.Add(new VehicleRepairModule());
			prefabs.Add(new HazardShieldItem());
#if BELOWZERO
			prefabs.Add(new SurvivalSuitBlueprint_FromReinforcedCold());
			prefabs.Add(new SurvivalSuitBlueprint_FromSurvivalCold());
			prefabs.Add(new SurvivalSuitBlueprint_FromReinforcedSurvival());
#endif


#if SN1
			foreach (string sTechType in new List<string> { "deathrunremade_reinforcedsuit2", "deathrunremade_reinforcedsuit3", "deathrunremade_spineeelscale", "deathrunremade_lavalizardscale" })
			{
				if (TechTypeUtils.TryGetModTechType(sTechType, out TechType tt))
				{
					NitrogenTechtypes.Add(sTechType, tt);
					bHasN2 = true;
				}
				else
				{
					Log.LogDebug($"Load(): Could not find TechType for Nitrogen class ID {sTechType}");
				}
			}
	#if NAUTILUS
			if (bHasN2)
			{
				if (DeathrunAPI == null)
				{
					Log.LogError($"Found Deathrun TechTypes but could not find DeathrunAPI type");
				}
				else if (AddSuitCrushDepth == null && AddNitrogenModifier == null)
				{
					Log.LogError($"Found DeathrunAPI type but could not find corresponding API methods");
				}
				else
				{
					Log.LogDebug("Main.Load(): Found DeathrunAPI, adding Deathrun prefabs");
					prefabs.Add(new NitrogenBrineSuit2());
					prefabs.Add(new NitrogenBrineSuit3());
					prefabs.Add(new Blueprint_ReinforcedMk2toBrineMk2());
					prefabs.Add(new Blueprint_ReinforcedMk3toBrineMk3());
				}
			}

	#else
			if (bHasN2)
			{
				if (DeathRunMain == null && NitrogenMain == null)
				{
					Log.LogError($"Found Nitrogen TechTypes but could not find DeathRun.Main type nor NitrogenMod.Main type");
				}
				else if (NitroAddDiveSuit == null && DeathRunAddDiveSuit == null)
				{
					Log.LogError($"Found NitrogenMod.Main type or DeathRun.Main type, but could not find corresponding AddDiveSuit method");
				}
				else
				{
					Log.LogDebug($"Main.Load(): Found NitrogenMod or DeathRun, adding Nitrogen prefabs");
					prefabs.Add(new NitrogenBrineSuit2());
					prefabs.Add(new NitrogenBrineSuit3());
					prefabs.Add(new Blueprint_ReinforcedMk2toBrineMk2());
					prefabs.Add(new Blueprint_ReinforcedMk3toBrineMk3());
				}
			}
	#endif
#endif

			// These may depend on Nitrogen, or they may not; but if they do they must be loaded afterwards.
			prefabs.Add(new SuperSurvivalSuit());

			Log.LogDebug($"Patching in {prefabs.Count} Spawnables");
			foreach (Spawnable s in prefabs)
			{
				//Log.LogDebug($"Patching {s.ClassID}");
				s.Patch();
			}

			Log.LogDebug($"{thisName} Patching Powerglide databox");
			Databox powerglideDatabox = new Databox()
			{
				DataboxID = "PowerglideBox",
				PrimaryDescription = DWEBPowerglide.friendlyName + " Databox",
				SecondaryDescription = DWEBPowerglide.description,
				TechTypeToUnlock = GetModTechType("DWEBPowerglide"),
#if LEGACY
	#if SN1
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
#else
	#if SN1
				CoordinatedSpawns = new List<SpawnLocation>()
				{
					new SpawnLocation(new Vector3(-1407.51f, -332.47f, 740.66f), new Vector3(6.93f, 275.67f, 0.00f)),
					//new Spawnable.SpawnLocation(new Vector3(-1384.79f, -330.18f, 718.84f), new Vector3(1.22f, 194.60f, 357.64f))
				}
	#elif BELOWZERO
				CoordinatedSpawns = new List<SpawnLocation>()
				{
					new SpawnLocation(new Vector3(285f, -242.07f, -1299f), new Vector3(344f, 3.77f, 14f))
				}
	#endif
#endif
			};
			powerglideDatabox.Patch();

			Log.LogDebug($"{thisName} Registering SaveDataCache");
#if LEGACY
			saveCache = SaveDataHandler.Main.RegisterSaveDataCache<DWDataFile>();
#else
			saveCache = SaveDataHandler.RegisterSaveDataCache<DWDataFile>();
#endif
			saveCache.Init();

#if SN1
			Databox headLampDatabox = new Databox()
			{
				DataboxID = "HeadlampBox",
				PrimaryDescription = "Headlamp Databox",
				SecondaryDescription = "Head-mounted lamp for hands-free illumination",
				TechTypeToUnlock = GetModTechType("FlashlightHelmet"),
				CoordinatedSpawns = new List<SpawnLocation>()
				{
					new SpawnLocation(new Vector3(-403f, -229.4f, -98.48f), Vector3.zero),
				},
				ModifyGameObject = (go =>
				{
					Log.LogDebug($"HeadlampBox: adding OverrideTransform");
					go.EnsureComponent<OverrideTransform>();
				})
			};
			headLampDatabox.Patch();
#endif

			Log.LogDebug($"DWEBPlugin.Awake: Patching all");
			harmony.PatchAll(myAssembly);
			/*if (QModServices.Main.ModPresent("UpgradedVehicles"))
			{
				Log.LogDebug("UpgradedVehicles found, attempting to patch GetSpeedMultiplierBonus method");
				bool success = AssemblyUtils.PatchIfExists(harmony, "UpgradedVehicles", "UpgradedVehicles.VehicleUpgrader", "GetSpeedMultiplierBonus", null, new HarmonyMethod(typeof(HorsepowerPatches), nameof(HorsepowerPatches.PostGetBonusGetSpeedMultiplierBonus)), null);
				Log.LogDebug("Patching " + (success ? "success" : "fail"));
			}*/

#if BELOWZERO

			Sprite hoverbike = SpriteManager.Get(SpriteManager.Group.Pings, "Hoverbike");
			//Console.WriteLine($"{thisName} Adding Snowfox Upgrades tab");
			CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "HoverbikeUpgrades", "Snowfox Upgrades", hoverbike, new string[] { "Upgrades" });
			foreach (Spawnable s in new List<Spawnable>() {
				new HoverbikeWaterTravelModule(),
				new HoverbikeSolarChargerModule(),
				new HoverbikeStructuralIntegrityModule(),
				new HoverbikeEngineEfficiencyModule(),
				new HoverbikeSelfRepairModule(),
				new HoverbikeDurabilitySystem(),
				new HoverbikeSpeedModule(),
				new HoverbikeMobilityUpgrade(),
			})
			{
				//Console.WriteLine($"{thisName} Patching {s.ClassID}");
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
			CoroutineHost.StartCoroutine(ProcessPendingCompounds(bForce));
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

	[FileName("DWEquipmentBonanza")]
	[Serializable]
	public class DWDataFile : SaveDataCache
	{
		private HashSet<IProtoEventListener> activeReceivers = new();
		public Dictionary<string, float> ModuleCharges = new Dictionary<string, float>();
#if SN1
		public Vector3 HeadlampDataboxPosition { get; set; }
		public Vector3 HeadlampDataboxRotation { get; set; }
#endif
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
				try
				{
					if (c == null)
						continue;

					c.OnProtoSerialize(null);
				}
				catch (Exception e)
				{
					Log.LogError($"Exception calling OnBeforeSerialize() in {c.GetType().ToString()}! Saving may not account for this object properly.", e, true);
					//Log.LogError(e.ToString());
				}
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
		public bool RegisterReceiver(IProtoEventListener v)
		{
			if (v == null || activeReceivers.Contains(v))
				return false;

			activeReceivers.Add(v);
			return true;
		}

		public bool UnregisterReceiver(IProtoEventListener v) => activeReceivers.Remove(v);
	}
}
