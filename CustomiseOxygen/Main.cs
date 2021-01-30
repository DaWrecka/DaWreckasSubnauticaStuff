using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        public struct TankType
        {
            internal Sprite sprite;
            internal TechType tankTechType;
            internal TechType refillTechType;
            internal float BaseO2Capacity;

            public TankType(TechType tank, float baseO2capacity)
            {
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
                    Logger.Log(Logger.Level.Debug, $"Setting crafting time of {craftTime} for TechType.{this.refillTechType.AsString()}");
                    CraftDataHandler.SetCraftingTime(this.refillTechType, craftTime);
                }
                else
                    Logger.Log(Logger.Level.Debug, $"Couldn't find crafting time for TechType.{this.tankTechType}");

                this.BaseO2Capacity = baseO2capacity;
            }

            public void UpdateCapacity(float newCapacity)
            {
                Logger.Log(Logger.Level.Debug, $"UpdateCapacity() called for TechType '{this.tankTechType.AsString()}' with value of {newCapacity}");
                this.BaseO2Capacity = newCapacity;
            }
        }

        public struct TankTypesStruct
        {
            private List<TankType> TankTypes;

            public bool AddTank(TechType tank, float baseCapacity, bool bUpdate = false)
            {
                if (TankTypes == null)
                    TankTypes = new List<TankType>();

                for(int i = 0; i < TankTypes.Count; i++)
                {
                    if (TankTypes[i].tankTechType == tank)
                    {
                        if (bUpdate)
                        {
                            Logger.Log(Logger.Level.Debug, $"Updating tank type for TechType '{tank.AsString()}' with value {baseCapacity}");
                            TankTypes[i].UpdateCapacity(baseCapacity);
                        }
                        return false;
                    }
                }

                Logger.Log(Logger.Level.Debug, $"Adding Tank '{tank.AsString()}' with capacity of {baseCapacity}");
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

            TankTypes.AddTank(TechType.Tank, -30f);
            TankTypes.AddTank(TechType.DoubleTank, -90f);
            TankTypes.AddTank(TechType.SuitBoosterTank, -90f);
            TankTypes.AddTank(TechType.PlasteelTank, -90f);
            TankTypes.AddTank(TechType.HighCapacityTank, -180f);
        }
    }
}
