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
        internal const string AssemblyVersion = "1.0.0.0";

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
            public float capacity;
            public Sprite icon;

            public PendingTankEntry(float cap, Sprite icon)
            {
                this.capacity = cap;
                this.icon = icon;
            }
        }

        internal static Dictionary<TechType, ExclusionType> Exclusions = new Dictionary<TechType, ExclusionType>();
        internal static HashSet<TechType> bannedTech = new HashSet<TechType>();
        //private static List<PendingTankEntry> pendingTanks = new List<PendingTankEntry>();
        private static Dictionary<TechType, PendingTankEntry> pendingTanks = new Dictionary<TechType, PendingTankEntry>();
        private static bool bWaitingForSpriteHandler;

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

        public static void AddTank(TechType tank, float capacity, Sprite sprite = null)
        {
            Logger.Log(Logger.Level.Debug, $"Registering new tank TechType.{tank.AsString()} with capacity {capacity}");
            /*for (int i = 0; i < pendingTanks.Count; i++)
            {
                if(pendingTanks[i].techType == tank)
                    return;
            }*/
            if (pendingTanks.ContainsKey(tank))
                return;

            pendingTanks[tank] = new PendingTankEntry(capacity, sprite);
            CoroutineHost.StartCoroutine(WaitForSpriteHandler());
        }

        private static IEnumerator WaitForSpriteHandler()
        {
            // We want a singleton method here
            if (bWaitingForSpriteHandler)
                yield break;

            bWaitingForSpriteHandler = true;

#if SUBNAUTICA_STABLE
            if(SpriteManager.GetWithNoDefault(TechType.Cutefish) == null)
            {
                while(SpriteManager.GetWithNoDefault(TechType.Cutefish) == null)
#elif BELOWZERO
            if (!SpriteManager.hasInitialized)
            {
                while (!SpriteManager.hasInitialized)
#endif
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            Log.LogDebug($"WaitForSpriteHandler(): Sprite manager initialisation complete. pendingTanks.Count == {pendingTanks.Count}");
            HashSet<TechType> removals = new HashSet<TechType>();
            while (pendingTanks.Count > 0)
            {
                removals.Clear();
                foreach (KeyValuePair<TechType, PendingTankEntry> kvp in pendingTanks)
                {
                    try
                    {
                        //Log.LogDebug($"WaitForSpriteHandler(): key {kvp.Key.AsString()}");
                        TechType key = kvp.Key;
                        Log.LogDebug($"WaitForSpriteHandler(): key {key.AsString()}");
                        
                        float capacity = kvp.Value.capacity;
                        Sprite icon = kvp.Value.icon;
                        if (icon == null || icon == SpriteManager.defaultSprite)
                        {
                            Log.LogDebug($"WaitForSpriteHandler(): Searching for icon for TechType {key.AsString(false)}");
                            if (icon == null || icon == SpriteManager.defaultSprite)
                            {
                                icon = SpriteManager.Get(kvp.Key);
                            }

                        }
                        if (icon != null || icon != SpriteManager.defaultSprite)
                        {
                            Log.LogDebug($"WaitForSpriteHandler(): Found icon for TechType {key.AsString(false)}: {icon.texture.name}");
                            TankTypes.AddTank(key, capacity, icon);
                            removals.Add(key);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.LogDebug($"WaitForSpriteHandler(): Caught exception: {e.ToString()}\nat key {kvp.Key.AsString()}");
                    }
                }

                foreach (TechType tt in removals)
                    pendingTanks.Remove(tt);

                yield return new WaitForEndOfFrame();
            }

            bWaitingForSpriteHandler = false;
        }

        public struct TankType
        {
            internal Sprite sprite;
            //internal TechType tankTechType;
            internal TechType refillTechType;
            internal float BaseO2Capacity;

            public TankType(TechType tank, float baseO2capacity, Sprite sprite = null)
            {
                Logger.Log(Logger.Level.Debug, $"Registering tank TechType.{tank.AsString()}");
                if (sprite != null)
                    this.sprite = sprite;
                else
                    this.sprite = SpriteManager.Get(tank);
                string tankName = Language.main.Get(tank);
                this.BaseO2Capacity = baseO2capacity;
                if (Main.config.bManualRefill)
                {
                    //new TankCraftHelper(tank).Patch();
                    this.refillTechType = TechTypeHandler.AddTechType((tank.AsString(false) + "Refill"), tankName + " Refill", "Refilled " + tankName, false);
                    KnownTechHandler.SetAnalysisTechEntry(tank, new TechType[] { this.refillTechType });
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
                        "TankRefill",
                        this.refillTechType.AsString(false)
                    });
#if BELOWZERO
                    if (TechData.GetCraftTime(tank, out float craftTime))
                    {
                        Logger.Log(Logger.Level.Debug, $"Setting crafting time of {craftTime} for TechType.{this.refillTechType.AsString()}");
                        CraftDataHandler.SetCraftingTime(this.refillTechType, craftTime);
                    }
                    else
                    {
                        Logger.Log(Logger.Level.Debug, $"Couldn't find crafting time for TechType.{tank}");
                    }
#endif
                    if (!Main.bannedTech.Contains(this.refillTechType))
                    {
                        Main.bannedTech.Add(this.refillTechType);
                    }
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

            public bool AddTank(TechType tank, float baseCapacity, Sprite sprite = null, bool bUpdate = false)
            {
                if (TankTypes == null)
                    TankTypes = new Dictionary<TechType, TankType>();

                    //if (TankTypes[i].tankTechType == tank)
                    if(TankTypes.TryGetValue(tank, out TankType tt))
                    {
                        if (bUpdate)
                        {
#if !RELEASE
                            Logger.Log(Logger.Level.Debug, $"Updating tank type for TechType '{tank.AsString()}' with value {baseCapacity}"); 
#endif
                            TankTypes[tank].UpdateCapacity(baseCapacity);
                        }
                        return false;
                    }
#if !RELEASE

                Logger.Log(Logger.Level.Debug, $"Adding Tank '{tank.AsString()}' with capacity of {baseCapacity}"); 
#endif
                TankTypes[tank] = new TankType(tank, baseCapacity);
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
        };

        internal static TankTypesStruct TankTypes = new TankTypesStruct();

        internal static DWOxyConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWOxyConfig>();

        [QModPatch]
        public void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
            config.Init();
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
            foreach (TechType tt in new TechType[] {
                TechType.Tank,
                TechType.DoubleTank,
#if BELOWZERO
                TechType.SuitBoosterTank,
#endif
                TechType.PlasteelTank,
                TechType.HighCapacityTank
                })
            {
                Logger.Log(Logger.Level.Debug, $"Initial processing, TechType.{tt.AsString()}");
                AddTank(tt, -1f);
            }
        }
    }
}
