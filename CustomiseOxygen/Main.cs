using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UWE;
using Logger = QModManager.Utility.Logger;
using static HandReticle;
using Common.Utility;
#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace CustomiseOxygen
{
    [QModCore]
    public class Main
    {
        internal const string AssemblyTitle = "CustomiseOxygen";
        internal const string AssemblyProduct = "CustomiseOxygen";
        internal const string AssemblyVersion = "1.1.0.0";
        //private static FieldInfo CraftTreeCraftableTech => typeof(CraftTree).GetField("craftableTech", BindingFlags.NonPublic | BindingFlags.Static);

        // Exclusions which can be added by external mods, such as CombinedItems.
        internal enum ExclusionType
        {
            None = 0,
            Multipliers = 1,    // Don't multiply capacity, but allow CapacityOverrides to override the capacity.
            Override = 2,       // Don't allow CapacityOverrides, but apply multipliers
            Both = 3           // Don't apply CapacityOverrides or multipliers
        }

        private struct PendingTankEntry
        {
            public float capacity { get; private set; }
            public Sprite icon { get; private set; }
            public bool bUnlockAtStart { get; private set; }
            public bool Update { get; private set; }


            public PendingTankEntry(float cap, Sprite icon, bool bUnlockAtStart, bool bUpdate = false)
            {
                this.capacity = cap;
                this.icon = icon;
                this.bUnlockAtStart = bUnlockAtStart;
                this.Update = bUpdate;
            }
        }

        internal static Dictionary<TechType, ExclusionType> Exclusions = new Dictionary<TechType, ExclusionType>();
        internal static HashSet<TechType> bannedTech = new HashSet<TechType>();
        //private static List<PendingTankEntry> pendingTanks = new List<PendingTankEntry>();
        private static Dictionary<TechType, PendingTankEntry> pendingTanks = new Dictionary<TechType, PendingTankEntry>();
        private static HashSet<TechType> processedTanks = new HashSet<TechType>();
        public static bool bMainMenuHasLoaded { get; private set; }
        public static bool bWaitingForSpriteHandler { get; private set; }

        public static void AddExclusion(TechType excludedTank, bool bExcludeMultipliers, bool bExcludeOverride)
        {

            ExclusionType newExclusion = (ExclusionType)((bExcludeMultipliers ? 1 : 0) + (bExcludeOverride ? 2 : 0));
            if (Exclusions.ContainsKey(excludedTank))
            {
                Logger.Log(Logger.Level.Debug, $"Modifying exclusion for TechType.{excludedTank} to {newExclusion.ToString()}");
                Exclusions[excludedTank] = (ExclusionType)newExclusion;
                return;
            }

            Logger.Log(Logger.Level.Debug, $"Assigning new exclusion for TechType.{excludedTank} as {newExclusion.ToString()}");
            Exclusions.Add(excludedTank, (ExclusionType)newExclusion);
        }

        public static void AddTank(TechType tank, float capacity, bool bUnlockAtStart, Sprite sprite = null, bool Update = false)
        {
            if (pendingTanks.ContainsKey(tank))
            {
                Log.LogDebug($"Main.AddTank(TechType: {tank.AsString()}): tank already pending");
                return;
            }
            if (processedTanks.Contains(tank) && !Update)
            {
                Log.LogDebug($"Main.AddTank(TechType: {tank.AsString()}): Tank already processed");
                return;
            }
            Log.LogDebug($"Main.AddTank(TechType: {tank.AsString()}): Registering new tank with capacity {capacity} and bUnlockAtStart: {bUnlockAtStart}");

            if (!config.defaultTankCapacities.ContainsKey(tank.AsString()))
            {
#if !RELEASE
                Log.LogDebug($"Adding TechType {tank.AsString()} to informational dictionary with base capacity {capacity}");
#endif
                config.defaultTankCapacities.Add(tank.AsString(), capacity);
                config.Save();
            }
            pendingTanks[tank] = new PendingTankEntry(capacity, sprite, bUnlockAtStart);
            if(bMainMenuHasLoaded)
                CoroutineHost.StartCoroutine(WaitForSpriteHandler());
        }

        public static void OnMainMenuStarted()
        {
            bMainMenuHasLoaded = true;
            CoroutineHost.StartCoroutine(WaitForSpriteHandler());
        }

        private static IEnumerator WaitForSpriteHandler()
        {
            // We want a singleton method here
            Log.LogDebug($"WaitForSpriteHandler() invoked. pendingTanks.Count == {pendingTanks.Count}, bWaitingForSpriteHandler = {bWaitingForSpriteHandler}");
            if (bWaitingForSpriteHandler)
                yield break;

            bWaitingForSpriteHandler = true;

#if SUBNAUTICA_STABLE
            while (SpriteUtils.Get(TechType.Cutefish, null) == null || Language.main == null || uGUI.isLoading || !bMainMenuHasLoaded)
#elif BELOWZERO
            //while (!SpriteManager.hasInitialized || Language.main == null || uGUI.isLoading || !bMainMenuHasLoaded)
            while(!bMainMenuHasLoaded)
#endif
            {
                Log.LogDebug($"WaitForSpriteHandler() waiting for 0.5 seconds");
                yield return new WaitForSecondsRealtime(0.5f);
            }

            Log.LogDebug($"WaitForSpriteHandler(): Sprite manager initialisation complete. pendingTanks.Count == {pendingTanks.Count}");
            HashSet<TechType> removals = new HashSet<TechType>();
            while (pendingTanks.Count > 0)
            {
                removals.Clear();
                var pendingCopy = new Dictionary<TechType, PendingTankEntry>(pendingTanks); // Cloning a dictionary is probably expensive, but this doesn't need to be done repeatedly,
                                                                                            // and shouldn't have any noticeable impact on performance
                foreach (KeyValuePair<TechType, PendingTankEntry> kvp in pendingCopy)
                {
                    //Log.LogDebug($"WaitForSpriteHandler(): key {kvp.Key.AsString()}");
                    TechType key = kvp.Key;
                    string keyString = key.AsString();
                    Log.LogDebug($"WaitForSpriteHandler(): key {keyString}");

                    float capacity = kvp.Value.capacity;
                    if (capacity <= 0f)
                    {
                        Log.LogDebug($"WaitForSpriteHandler(): No capacity supplied for TechType {keyString}, attempting to retrieve value from prefab");
                        CoroutineTask <GameObject> task = CraftData.GetPrefabForTechTypeAsync(key);
                        yield return task;

                        GameObject prefab = task.GetResult();
                        if (prefab != null)
                        {
                            Oxygen component = prefab.GetComponent<Oxygen>();
                            if (component != null)
                            {
                                capacity = component.oxygenCapacity;
                                Log.LogDebug($"WaitForSpriteHandler(): Got oxygen capacity of {capacity} for TechType {keyString}");
                            }
                            else
                                Log.LogWarning($"WaitForSpriteHandler(): Could not find Oxygen component on prefab for TechType {keyString}");
                        }
                        else
                            Log.LogWarning($"WaitForSpriteHandler(): Could not retrieve prefab for TechType {keyString}");

                    }

                    Sprite icon = kvp.Value.icon;
                    if (icon == null)
                    {
                        Log.LogDebug($"WaitForSpriteHandler(): Searching for icon for TechType {keyString}");
                        icon = SpriteUtils.Get(kvp.Key, null);

                    }
                    if (icon != null)
                    {
                        Log.LogDebug($"WaitForSpriteHandler(): Found icon for TechType {keyString}: {icon.texture.name}");
                        TankTypes.AddTank(key, capacity, kvp.Value.bUnlockAtStart, icon, kvp.Value.Update);
                        removals.Add(key);
                    }
                    else
                    {
                        Log.LogError($"Error retrieving sprite for TechType {keyString}");
                    }
                }

                foreach (TechType tt in removals)
                {
                    if (config.bManualRefill)
                    {
                        Log.LogDebug($"WaitForSpriteHandler(): Verifying craftable status for tank {tt.AsString()}");
                        if (TankTypes.CheckRefillCraftable(tt))
                        {
                            pendingTanks.Remove(tt);
                            processedTanks.Add(tt);
                        }
                        else
                        {
                            Log.LogDebug($"WaitForSpriteHandler(): craftable status failed for tank {tt.AsString()}; will try again");
                        }
                    }
                }

                yield return new WaitForEndOfFrame();
            }

            bWaitingForSpriteHandler = false;
            yield break;
        }

        public class TankType
        {
            public Sprite sprite { get; private set; }
            //internal TechType tankTechType;
            public TechType refillTechType { get; private set; }
            public float BaseO2Capacity { get; private set; }
            public float speedModifier { get; private set; }
            public bool bRefillStatus { get; private set; }

            public TankType(TechType tank, float baseO2capacity, bool bUnlockAtStart, Sprite sprite = null, float speedModifier = 1f)
            {
                Log.LogDebug($"TankType.AddTank(TechType: {tank.AsString()}): Registering tank TechType, bUnlockAtStart: {bUnlockAtStart}");
                if (sprite != null)
                    this.sprite = sprite;
                else
                    this.sprite = SpriteManager.Get(tank);
                string tankName = Language.main.Get(tank);
                this.BaseO2Capacity = baseO2capacity;
                this.speedModifier = speedModifier;
                if (Main.config.bManualRefill)
                {
                    //new TankCraftHelper(tank).Patch();
                    this.refillTechType = TechTypeHandler.AddTechType((tank.AsString(false) + "Refill"), tankName + " Refill", "Refilled " + tankName, false);
                    SpriteHandler.RegisterSprite(this.refillTechType, this.sprite);
                    var techData = new RecipeData()
                    {
                        craftAmount = 0,
                        Ingredients = new List<Ingredient>()
                        {
                            new Ingredient(tank, 1)
                        }
                    };
                    techData.LinkedItems.Add(tank);

                    CraftDataHandler.SetTechData(this.refillTechType, techData);
                    CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, this.refillTechType, new string[] {
                        "Personal",
                        "TankRefill"
                    });
#if SUBNAUTICA_STABLE
                    if(CraftData.GetCraftTime(tank, out float craftTime))
#elif BELOWZERO
                    if (TechData.GetCraftTime(tank, out float craftTime))
#endif
                    {
                        Log.LogDebug($"TankType.AddTank(TechType: {tank.AsString()}): Setting crafting time of {craftTime} for TechType.{this.refillTechType.AsString()}");
                        CraftDataHandler.SetCraftingTime(this.refillTechType, craftTime);
                    }
                    else
                    {
                        Log.LogDebug($"TankType.AddTank(TechType: {tank.AsString()}): Couldn't find crafting time for TechType.{tank}");
                        CraftDataHandler.SetCraftingTime(this.refillTechType, 5f);
                    }

                    if (bUnlockAtStart)
                    {
                        Log.LogDebug($"TankType.AddTank(TechType: {tank.AsString()}): Setting to unlock at start");
                        KnownTechHandler.UnlockOnStart(this.refillTechType);
                    }
                    else
                    {
                        Log.LogDebug($"TankType.AddTank(TechType: {tank.AsString()}): Setting refill {this.refillTechType.AsString()} to unlock with TechType {tank.AsString()}");
                        KnownTechHandler.SetAnalysisTechEntry(tank, new TechType[] { this.refillTechType });
                    }


                    Main.bannedTech.Add(this.refillTechType);
                    this.bRefillStatus = false;
                }
                else
                {
                    this.refillTechType = TechType.None;
                }
            }

            public void UpdateCapacity(float newCapacity)
            {
                this.BaseO2Capacity = newCapacity;
            }
        }

        public struct TankTypesStruct
        {
            private Dictionary<TechType, TankType> TankTypes;

            public bool AddTank(TechType tank, float baseCapacity, bool bUnlockAtStart, Sprite sprite = null, bool bUpdate = false, float speedModifier = 1f)
            {
                if (TankTypes == null)
                    TankTypes = new Dictionary<TechType, TankType>();

                    //if (TankTypes[i].tankTechType == tank)
                    if(TankTypes.TryGetValue(tank, out TankType tt))
                    {
                        if (bUpdate)
                        {
#if !RELEASE
                            Log.LogDebug($"TankTypes.AddTank(TechType: {tank.AsString()}): Updating tank type for TechType with value {baseCapacity}"); 
#endif
                            tt.UpdateCapacity(baseCapacity);
                        }
                        return false;
                    }
#if !RELEASE

                Log.LogDebug($"TankTypes.AddTank(TechType: {tank.AsString()}): Adding Tank with capacity of {baseCapacity}"); 
#endif
                TankTypes.Add(tank, new TankType(tank, baseCapacity, bUnlockAtStart, sprite, speedModifier));

                return true;
            }

            public float GetCapacity(TechType tank)
            {
                //foreach (TankType tt in TankTypes)
                if(TankTypes.TryGetValue(tank, out TankType tt))
                {
                    return tt.BaseO2Capacity;
                }

                return -1f;
            }

            // Check if the refill recipe for this TechType is craftable
            public bool CheckRefillCraftable(TechType tank)
            {
                if (config.bManualRefill)
                {
                    if (TankTypes.TryGetValue(tank, out TankType tankType))
                    {
                        Log.LogDebug($"TankTypes.CheckRefillCraftable(TechType: {tank.AsString()}): {tankType.refillTechType.AsString()} has been registered with TankTypes, checking craftability");
                        //if (CraftTree.IsCraftable(tankType.refillTechType))
                        //HashSet<TechType> craftableTech = (HashSet<TechType>)CraftTreeCraftableTech.GetValue(null);
                        if (CraftTree.craftableTech == null)
                        {
                            Log.LogDebug($"TankTypes.CheckRefillCraftable(TechType: {tank.AsString()}): Cannot verify craftable status, craftableTech is null!");
                        }
                        else if (CraftTree.craftableTech.Contains(tankType.refillTechType))
                        {
                            Log.LogDebug($"TankTypes.CheckRefillCraftable(TechType: {tank.AsString()}): Successfully registered craftable refill");
                        }
                        else
                        {
                            Log.LogDebug($"TankTypes.CheckRefillCraftable(TechType: {tank.AsString()}): Failed to register TechType as craftable");
                            return false;
                        }
                    }
                    else
                    {
                        Log.LogDebug($"TankTypes.CheckRefillCraftable(TechType: {tank.AsString()}): No TankType record found");
                        return false;
                    }
                }
                else
                {
                    Log.LogDebug($"TankTypes.CheckRefillCraftable(TechType: {tank.AsString()}): manual refill mode is disabled");
                }

                return true;
            }
        };

        internal static TankTypesStruct TankTypes = new TankTypesStruct();

        internal static DWOxyConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWOxyConfig>();

        [QModPatch]
        public void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var harmony = new Harmony($"DaWrecka_{assembly.GetName().Name}");
            harmony.PatchAll(assembly);
            config.Init();
#if SUBNAUTICA_STABLE
            bool deathRunPatch = AssemblyUtils.PatchIfExists(harmony, "DeathRun", "DeathRun.NMBehaviours.SpecialtyTanks", "Update", null, null, new HarmonyMethod(typeof(OxygenManagerPatches), nameof(OxygenManagerPatches.SpecialtyTankUpdateTranspiler)));
            bool nitrogenPatch = AssemblyUtils.PatchIfExists(harmony, "NitrogenMod", "NitrogenMod.NMBehaviours.SpecialtyTanks", "Update", null, null, new HarmonyMethod(typeof(OxygenManagerPatches), nameof(OxygenManagerPatches.SpecialtyTankUpdateTranspiler)));
            if (deathRunPatch || nitrogenPatch)
            {
                Log.LogDebug($"Patched SpecialtyTanks: Deathrun patch status {deathRunPatch}, NitrogenMod patch status {nitrogenPatch}");
            }
#endif
        }

        [QModPostPatch]
        public static void PostPatch()
        {
            // Calling these in the main Patch() routine is too early, as the sprite atlases have not yet finished loading.
            //CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "TankRefill", "Tank Refills", SpriteManager.Get(TechType.DoubleTank), new string[] { "Personal" });

            /*AddTank(TechType.Tank, -30f);
            AddTank(TechType.DoubleTank, -90f);
            AddTank(TechType.SuitBoosterTank, -90f);
            AddTank(TechType.PlasteelTank, -90f);
            AddTank(TechType.HighCapacityTank, -180f);*/
        }
    }
}
