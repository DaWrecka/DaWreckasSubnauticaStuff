using HarmonyLib;
using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CombinedItems.ReinforcedColdSuitPrefabs;
using QModManager.API;
using QModManager.API.ModLoading;
using CombinedItems.Equipables;
using CombinedItems.ExosuitModules;
using CombinedItems.Patches;
using Common;
using UWE;
using UnityEngine;

namespace CombinedItems
{
    [QModCore]
    public class Main
    {
        internal static bool bVerboseLogging = false;
        internal const string version = "0.8.0.1";

        private static readonly Type CustomiseOxygen = Type.GetType("CustomiseOxygen.Main, CustomiseOxygen", false, false);
        private static readonly MethodInfo CustomOxyAddExclusionMethod = CustomiseOxygen?.GetMethod("AddExclusion", BindingFlags.Public | BindingFlags.Static);
        private static readonly MethodInfo CustomOxyAddTankMethod = CustomiseOxygen?.GetMethod("AddTank", BindingFlags.Public | BindingFlags.Static);

        internal static InsulatedRebreather prefabInsulatedRebreather = new InsulatedRebreather();
        internal static ReinforcedColdSuit prefabReinforcedColdSuit = new ReinforcedColdSuit();
        internal static ReinforcedColdGloves prefabReinforcedColdGloves = new ReinforcedColdGloves();
        internal static HighCapacityBooster prefabHighCapacityBooster = new HighCapacityBooster();
        internal static ExosuitLightningClawPrefab prefabLightningClaw = new ExosuitLightningClawPrefab();
        internal static ExosuitSprintModule prefabExosuitSprintModule = new ExosuitSprintModule();

        private static readonly Assembly myAssembly = Assembly.GetExecutingAssembly();
        private static void AddSubstitution(TechType custom, TechType vanilla)
        {
            Patches.EquipmentPatch.AddSubstitution(custom, vanilla);
            Patches.PlayerPatch.AddSubstitution(custom, vanilla);
        }

        internal static void AddCustomOxyExclusion(TechType excludedTank, bool bExcludeMultipliers, bool bExcludeOverride)
        {
            if (CustomOxyAddExclusionMethod != null)
                CustomOxyAddExclusionMethod.Invoke(null, new object[] { excludedTank, bExcludeMultipliers, bExcludeOverride });
        }

        internal static void AddCustomOxyTank(TechType tank, float capacity)
        {
            if (CustomOxyAddTankMethod != null)
                CustomOxyAddTankMethod.Invoke(null, new object[] { tank, capacity });
        }

        [QModPatch]
        public static void Load()
        {
            prefabInsulatedRebreather.Patch();
            prefabReinforcedColdGloves.Patch();
            prefabReinforcedColdSuit.Patch();
            prefabHighCapacityBooster.Patch();
            prefabLightningClaw.Patch();
            prefabExosuitSprintModule.Patch();

            new Harmony($"DaWrecka_{myAssembly.GetName().Name}").PatchAll(myAssembly);
        }

        [QModPostPatch]
        public static void PostPatch()
        {
            AddSubstitution(prefabReinforcedColdSuit.TechType, TechType.ColdSuit);
            AddSubstitution(prefabInsulatedRebreather.TechType, TechType.ColdSuitHelmet);
            AddSubstitution(prefabReinforcedColdGloves.TechType, TechType.ColdSuitGloves);
            AddSubstitution(prefabReinforcedColdSuit.TechType, TechType.ReinforcedDiveSuit);
            AddSubstitution(prefabInsulatedRebreather.TechType, TechType.Rebreather);
            AddSubstitution(prefabReinforcedColdGloves.TechType, TechType.ReinforcedGloves);
            AddSubstitution(prefabHighCapacityBooster.TechType, TechType.SuitBoosterTank);
            AddSubstitution(prefabHighCapacityBooster.TechType, TechType.HighCapacityTank);
            AddCustomOxyExclusion(prefabHighCapacityBooster.TechType, true, true);
            AddCustomOxyTank(prefabHighCapacityBooster.TechType, -1f);
            CraftDataHandler.SetBackgroundType(prefabLightningClaw.TechType, CraftData.BackgroundType.ExosuitArm);
            CraftDataHandler.SetItemSize(prefabLightningClaw.TechType, new Vector2int(1, 2));
            CraftDataHandler.SetCraftingTime(prefabLightningClaw.TechType, 10f);
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.SeamothUpgrades, prefabLightningClaw.TechType, new string[] { "ExosuitModules" });
            CraftDataHandler.SetBackgroundType(prefabExosuitSprintModule.TechType, CraftData.BackgroundType.Normal);
            CraftDataHandler.SetItemSize(prefabExosuitSprintModule.TechType, new Vector2int(1, 1));
            CraftDataHandler.SetCraftingTime(prefabExosuitSprintModule.TechType, 10f);
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.SeamothUpgrades, prefabExosuitSprintModule.TechType, new string[] { "ExosuitModules" });

            // This is test code
            //string PrefabFilename;
            //if it works, the following changes are made;

            // Preston's Plant becomes 2x2 instead of 1x1
            //WorldEntities/Flora/Expansion/Shared/vegetable_plant_01_fruit.prefab
            CraftDataHandler.SetItemSize(TechType.SnowStalkerPlant, new Vector2int(2, 2)); // this affects the inventory only, or more specifically how much space the plant requires in a planter.
            // It does nothing about the actual growing size of the plant; other methods are required for that.
            // We also need to know the prefab ahead of time; there is no known method to get the following prefab filename from the plant's TechType. The methods we can use for batteries
            // won't work for Preston's Plant, and likely other plantables too.
            AddressablesUtility.LoadAsync<GameObject>("WorldEntities/Flora/Expansion/Shared/vegetable_plant_01_fruit.prefab").Completed += (x) =>
            {
                GameObject gameObject1 = x.Result;
                Plantable component = gameObject1?.GetComponent<Plantable>();
                if (component != null)
                    component.size = Plantable.PlantSize.Large;
            };


            // Ion Batteries have a capacity of 750
            // Ion Power Cells have a capacity of 1500
            foreach (TechType tt in new List<TechType>()
                {
                    TechType.Battery,
                    TechType.LithiumIonBattery,
					TechType.PowerCell,
                    TechType.PrecursorIonBattery,
                    TechType.PrecursorIonPowerCell
                })
            {
                string classId = CraftData.GetClassIdForTechType(TechType.PrecursorIonBattery);
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
            Batteries.PostPatch();
        }
    }

    internal class Reflection
    {
        private static readonly MethodInfo addJsonPropertyInfo = typeof(CraftDataHandler).GetMethod("AddJsonProperty", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo playerUpdateReinforcedSuitInfo = typeof(Player).GetMethod("UpdateReinforcedSuit", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo playerCheckColdsuitGoalInfo = typeof(Player).GetMethod("CheckColdsuitGoal", BindingFlags.NonPublic | BindingFlags.Instance);

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
    }
}
