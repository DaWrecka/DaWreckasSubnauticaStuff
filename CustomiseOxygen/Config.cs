using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Handlers;
using System.Collections.Generic;
using Logger = QModManager.Utility.Logger;
using Common;
//using static CustomiseOxygen.Main;

namespace CustomiseOxygen
{
    [Menu("Customise your Oxygen")]
    public class DWOxyConfig : ConfigFile
    {
        const float MIN_MULT = 0.5f;
        const float MAX_MULT = 10f;

        [Toggle("Manual O2 refill", Tooltip = "If enabled, oxygen tanks do not refill automatically, and can only be refilled at fabricators, but have their capacity multiplied by the Manual Mode multiplier below.\n\nChanging this setting requires the game to be restarted to take full effect.")]
        public bool bManualRefill = false; // if true, prevents tanks refilling themselves and applies refillableMultiplier on top of the baseOxyMultiplier.
        [Slider("Base oxygen multiplier", MIN_MULT, MAX_MULT, DefaultValue = 1f, Id = "baseMult", Step = 0.05f, Format = "{0:F2}", Tooltip = "Base multiplier applied to all oxygen tank capacities not explicitly-defined in the config.json")]
        public float baseOxyMultiplier = 1f;
        [Slider("Manual mode multiplier", 2.0f, MAX_MULT, DefaultValue = 4f, Id = "refillMult", Step = 0.05f, Format = "{0:F2}", Tooltip = "Multiplier applied to tank capacities if, and only if, automatic refill mode is not enabled.")]
        public float refillableMultiplier = 4f;

        /*TankTypes.AddTank(TechType.Tank, 30f);
            TankTypes.AddTank(TechType.DoubleTank, 90f);
            TankTypes.AddTank(TechType.SuitBoosterTank, 90f);
            TankTypes.AddTank(TechType.PlasteelTank, 90f);
            TankTypes.AddTank(TechType.HighCapacityTank, 180f);*/

        // We use strings instead of TechTypes to make it easier to support modded TechTypes.
        // Using TechTypes might result in failure to load the config if this mod is loaded before the modded TechType is loaded.
        // So we save strings, then convert them to TechTypes at the latest possible moment.
        public Dictionary<string, float> defaultTankCapacities = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Tank", 30f },
            { "DoubleTank", 90f },
            { "SuitBoosterTank", 90f },
            { "PlasteelTank", 90f },
            { "HighCapacityTank", 180f }
        };

        // This is the dictionary which will be saved to the config.json and may be edited by the end user.
        // The keys within are converted to TechTypes at post-patch time, held in typedCapacityOverrides, and it is that dictionary which
        // is actually used by the code.
        public Dictionary<string, float> CapacityOverrides = new Dictionary<string, float>();

        // This is a separate set of capacities which will be used if manual O2 refill is active. The same rules otherwise apply.
        public Dictionary<string, float> manualCapacityOverrides = new Dictionary<string, float>();

        [System.NonSerialized]
        private Dictionary<TechType, float> typedCapacityOverrides = new Dictionary<TechType, float>();
        [System.NonSerialized]
        private Dictionary<TechType, float> manualTypedCapacityOverrides = new Dictionary<TechType, float>();

        public bool GetCapacityOverride(TechType tank, out float capacityOverride, out float capacityMultiplier)
        {
            Dictionary<TechType, float> activeTypedCapacityOverrides;
            if (Main.config.bManualRefill)
                activeTypedCapacityOverrides = manualTypedCapacityOverrides; 
            else
                activeTypedCapacityOverrides = typedCapacityOverrides;
            // if return value is false, no override should be performed.
            // If return value is true, and capacityOverride == -1, then the base capacity should not be altered.

            capacityOverride = -1f;
            capacityMultiplier = 1f;
            Main.ExclusionType exclusion = Main.Exclusions.GetOrDefault(tank, Main.ExclusionType.None);

            if (tank == TechType.None)
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"DWOxyConfig.GetCapacityOverride called with invalid TechType None");
                return false;
#endif
            }

            if (exclusion == Main.ExclusionType.Both)
            {
                Logger.Log(Logger.Level.Debug, $"DWOxyConfig.GetCapacityOverride called with excluded TechType {tank.AsString()}");
                return false;
            }

            if (exclusion != Main.ExclusionType.Override)
            {
                if (activeTypedCapacityOverrides.TryGetValue(tank, out float value))
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"DWOxyConfig.GetCapacityOverride: found override value of {value} for tank TechType '{tank.AsString()}'");
#endif
                    capacityOverride = value;
                    return true; // Don't apply multipliers
                }
            }

            if (exclusion != Main.ExclusionType.Multipliers)
                capacityMultiplier = baseOxyMultiplier * (bManualRefill ? refillableMultiplier : 1);

            return true;
        }

        public bool SetCapacityOverride(TechType tank, float capacity, bool bUpdateIfPresent = false, bool bIsDefault = false, bool bIsManualMode = false)
        {
            // if bIsDefault, the value should be added to the defaults list, not the active list.
            if (bIsDefault)
            {
                string tankID = tank.AsString(true);
                if (defaultTankCapacities.ContainsKey(tankID))
                {
                    if (bUpdateIfPresent)
                    {
                        defaultTankCapacities[tankID] = capacity;
                        Save();
                        return true;
                    }

                    return false;
                }

                Main.TankTypes.AddTank(tank, capacity, null, bUpdateIfPresent);
                defaultTankCapacities[tank.AsString(true)] = capacity;
                return false;
            }

            if (bIsManualMode)
            {
                if (manualTypedCapacityOverrides.ContainsKey(tank))
                {
                    if (!bUpdateIfPresent)
                        return false;
                }
                manualTypedCapacityOverrides[tank] = capacity;
            }
            else
            {
                if (typedCapacityOverrides.ContainsKey(tank))
                {
                    if (!bUpdateIfPresent)
                        return false;
                }
                typedCapacityOverrides[tank] = capacity;
            }
            Save();
            return true;
        }

        public void Init()
        {
            bool bUpdated = false;
            if (bManualRefill)
            {
                Log.LogDebug($"Config.Init(): Adding tab node for oxygen tank refills");
                CraftTreeHandler.AddTabNode(CraftTree.Type.Fabricator, "TankRefill", "Tank Refills", SpriteManager.Get(TechType.DoubleTank), new string[] { "Personal" });
            }
            if (CapacityOverrides == null || CapacityOverrides.Count < 1)
            {
                CapacityOverrides = new Dictionary<string, float>();
                bUpdated = true;
            }

            foreach (KeyValuePair<string, float> kvp in CapacityOverrides)
            {
                TechType tt = TechTypeUtils.GetTechType(kvp.Key);
                if (tt == TechType.None)
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"Failed to load TechType for string '{kvp.Key}'"); 
#endif
                    continue;
                }

                SetCapacityOverride(tt, kvp.Value, false, false, false);
            }

            if (manualCapacityOverrides == null || manualCapacityOverrides.Count < 1)
            {
                manualCapacityOverrides = new Dictionary<string, float>();
                bUpdated = true;
            }

            foreach (KeyValuePair<string, float> kvp in manualCapacityOverrides)
            {
                TechType tt = TechTypeUtils.GetTechType(kvp.Key);
                if(tt == TechType.None)
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"Failed to load TechType for string '{kvp.Key}'");
#endif
                    continue;
                }

                SetCapacityOverride(tt, kvp.Value, false, false, true);
            }

            if (bUpdated)
            {
                Save();
#if !RELEASE
                Logger.Log(Logger.Level.Debug, "Some values reset to defaults"); 
#endif
            }
            else
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, "All values present and correct"); 
#endif
            }


        }
    }
}
