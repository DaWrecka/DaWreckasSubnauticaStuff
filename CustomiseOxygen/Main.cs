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
#if SUBNAUTICA
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
            public TechType techType;
            public float capacity;
            public Sprite icon;

            public PendingTankEntry(TechType tt, float cap, Sprite icon)
            {
                this.techType = tt;
                this.capacity = cap;
                this.icon = icon;
            }
        }

        internal static Dictionary<TechType, ExclusionType> Exclusions = new Dictionary<TechType, ExclusionType>();
        private static List<PendingTankEntry> pendingTanks = new List<PendingTankEntry>();

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
            for (int i = 0; i < pendingTanks.Count; i++)
            {
                if(pendingTanks[i].techType == tank)
                    return;
            }
            pendingTanks.Add(new PendingTankEntry(tank, capacity, sprite));
            CoroutineHost.StartCoroutine(WaitForSpriteHandler());
        }

        private static IEnumerator WaitForSpriteHandler()
        {
            if (!SpriteManager.hasInitialized)
            {
                while (!SpriteManager.hasInitialized)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                Log.LogDebug($"WaitForSpriteHandler(): Sprite manager initialisation complete");
            }
            while (pendingTanks.Count > 0)
            {
                Log.LogDebug($"WaitForSpriteHandler(): Beginning iteration, pendingTanks.Count == {pendingTanks.Count}");
                for (int i = pendingTanks.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        Log.LogDebug($"WaitForSpriteHandler(): index {i}, pendingTanks.Count == {pendingTanks.Count}");
                        TechType key = pendingTanks[i].techType;
                        Log.LogDebug($"WaitForSpriteHandler(): Processing TechType {key.AsString(false)}");
                        float capacity = pendingTanks[i].capacity;
                        Sprite icon = pendingTanks[i].icon;
                        if (icon == null || icon == SpriteManager.defaultSprite)
                        {
                            Log.LogDebug($"WaitForSpriteHandler(): Searching for icon for TechType {key.AsString(false)}");
                            if (icon == null || icon == SpriteManager.defaultSprite)
                            {
                                icon = SpriteManager.Get(key);
                            }

                        }
                        if (icon != null || icon != SpriteManager.defaultSprite)
                        {
                            Log.LogDebug($"WaitForSpriteHandler(): Found icon for TechType {key.AsString(false)}: {icon.texture.name}");
                            TankTypes.AddTank(key, capacity, icon);
                            pendingTanks.RemoveAt(i);
                        }
                    }
                    catch(Exception e)
                    {
                        Log.LogDebug($"WaitForSpriteHandler(): Caught exception {e.ToString()} at index {i}");
                    }
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        public struct TankType
        {
            internal Sprite sprite;
            internal TechType tankTechType;
            internal TechType refillTechType;
            internal float BaseO2Capacity;

            public TankType(TechType tank, float baseO2capacity, Sprite sprite = null)
            {
                Logger.Log(Logger.Level.Debug, $"Registering tank TechType.{tank.AsString()}");
                if (sprite != null)
                    this.sprite = sprite;
                else
                    this.sprite = SpriteManager.Get(tank);
                this.tankTechType = tank;
                string tankName = Language.main.Get(tank);
                this.refillTechType = TechTypeHandler.AddTechType((tank.AsString(false) + "Refill"), tankName+" Refill", "Refilled "+tankName, false);
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
                    "Refill",
                    this.refillTechType.AsString(false)
                });
                if (TechData.GetCraftTime(this.tankTechType, out float craftTime))
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"Setting crafting time of {craftTime} for TechType.{this.refillTechType.AsString()}"); 
#endif
                    CraftDataHandler.SetCraftingTime(this.refillTechType, craftTime);
                }
                else
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"Couldn't find crafting time for TechType.{this.tankTechType}"); 
#endif
                }

                this.BaseO2Capacity = baseO2capacity;
            }

            public void UpdateCapacity(float newCapacity)
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"UpdateCapacity() called for TechType '{this.tankTechType.AsString()}' with value of {newCapacity}"); 
#endif
                this.BaseO2Capacity = newCapacity;
            }
        }

        public struct TankTypesStruct
        {
            private List<TankType> TankTypes;

            public bool AddTank(TechType tank, float baseCapacity, Sprite sprite = null, bool bUpdate = false)
            {
                if (TankTypes == null)
                    TankTypes = new List<TankType>();

                for(int i = 0; i < TankTypes.Count; i++)
                {
                    if (TankTypes[i].tankTechType == tank)
                    {
                        if (bUpdate)
                        {
#if !RELEASE
                            Logger.Log(Logger.Level.Debug, $"Updating tank type for TechType '{tank.AsString()}' with value {baseCapacity}"); 
#endif
                            TankTypes[i].UpdateCapacity(baseCapacity);
                        }
                        return false;
                    }
                }
#if !RELEASE

                Logger.Log(Logger.Level.Debug, $"Adding Tank '{tank.AsString()}' with capacity of {baseCapacity}"); 
#endif
                TankTypes.Add(new TankType(tank, baseCapacity));
                return true;
            }

            public float GetCapacity(TechType tank)
            {
                foreach (TankType tt in TankTypes)
                {
                    if (tt.tankTechType == tank || tt.refillTechType == tank)
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
            CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "Refill", "Tank Refills", SpriteManager.Get(TechType.DoubleTank), new string[] { "Personal" });

            /*AddTank(TechType.Tank, -30f);
            AddTank(TechType.DoubleTank, -90f);
            AddTank(TechType.SuitBoosterTank, -90f);
            AddTank(TechType.PlasteelTank, -90f);
            AddTank(TechType.HighCapacityTank, -180f);*/
            foreach (TechType tt in new TechType[] { TechType.Tank, TechType.DoubleTank, TechType.SuitBoosterTank, TechType.PlasteelTank, TechType.HighCapacityTank })
            {
                Logger.Log(Logger.Level.Debug, $"Initial processing, TechType.{tt.AsString()}");
                AddTank(tt, -1f);
            }
        }
    }
}
