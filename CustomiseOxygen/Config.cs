using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Handlers;
using System.Collections.Generic;
using Logger = QModManager.Utility.Logger;

namespace CustomiseOxygen
{
    [Menu("Customise your Oxygen")]
    public class DWOxyConfig : ConfigFile
    {
        const float MIN_MULT = 0.5f;
        const float MAX_MULT = 10f;

        [Toggle("Automatic O2 refill", Tooltip = "If enabled, oxygen tanks refill automatically; if disabled, tanks can only be refilled at fabricators, but have their capacity multiplied by the Refillable Mode multiplier below.")]
        public bool bAllowAutoRefill = true; // if true, prevents tanks refilling themselves and applies refillableMultiplier on top of the baseOxyMultiplier.
        [Slider("Base oxygen multiplier", MIN_MULT, MAX_MULT, DefaultValue = 1f, Id = "baseMult", Step = 0.05f, Format = "{0:F2}", Tooltip = "Base multiplier applied to all oxygen tank capacities")]
        public float baseOxyMultiplier = 1f;
        [Slider("Refillable mode multiplier", 2.0f, MAX_MULT, DefaultValue = 4f, Id = "refillMult", Step = 0.05f, Format = "{0:F2}", Tooltip = "Multiplier applied to tank capacities if, and only if, non-refillable tanks are enabled.")]
        public float refillableMultiplier = 4f;

        /*TankTypes.AddTank(TechType.Tank, 30f);
            TankTypes.AddTank(TechType.DoubleTank, 90f);
            TankTypes.AddTank(TechType.SuitBoosterTank, 90f);
            TankTypes.AddTank(TechType.PlasteelTank, 90f);
            TankTypes.AddTank(TechType.HighCapacityTank, 180f);*/

        // We use strings instead of TechTypes to make it easier to support modded TechTypes.
        // Using TechTypes might result in failure to load the config if this mod is loaded before the modded TechType is loaded.
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
        
        [System.NonSerialized]
        private Dictionary<TechType, float> typedCapacityOverrides = new Dictionary<TechType, float>();

        public float GetCapacityOverride(TechType tank)
        {
            if (tank == TechType.None)
            {
                Logger.Log(Logger.Level.Debug, $"DWOxyConfig.GetCapacityOverride called with invalid TechType None");
                return -1f;
            }

            if (typedCapacityOverrides.TryGetValue(tank, out float value))
            {
                Logger.Log(Logger.Level.Debug, $"DWOxyConfig.GetCapacityOverride: found override value of {value} for tank TechType '{tank.AsString()}' using TryGetValue");
                return value;
            }

            Logger.Log(Logger.Level.Debug, $"DWOxyConfig.GetCapacityOverride: found no override value for TechType '{tank}' using TryGetValue or manual search");
            return -1f;
        }

        public bool SetCapacityOverride(TechType tank, float capacity, bool bUpdateIfPresent = false, bool bIsDefault = false)
        {
            // if bIsDefault, the value should be added to the defaults list, not the active list.

            if (bIsDefault)
            {
                string tankID = tank.AsString(true);
                foreach (KeyValuePair<string, float> kvp in defaultTankCapacities)
                {
                    if (kvp.Key.ToLower() == tankID)
                    {
                        if (bUpdateIfPresent)
                        {
                            defaultTankCapacities[kvp.Key] = capacity;
                            Save();
                            return true;
                        }
                        else
                            return false;
                    }
                }

                defaultTankCapacities.Add(tank.AsString(false), capacity);
                return false;
            }

            if (typedCapacityOverrides.TryGetValue(tank, out float value))
            {
                if (bUpdateIfPresent)
                {
                    typedCapacityOverrides[tank] = capacity;
                    Save();
                    return true;
                }
                else
                    return false;
            }
            typedCapacityOverrides.Add(tank, capacity);
            Save();
            return true;
        }

        // Useful function provided by PrimeSonic. Ta!
        public static TechType GetTechType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return TechType.None;

            // Look for a known TechType
            if (TechTypeExtensions.FromString(value, out TechType tType, true))
                return tType;

            //  Not one of the known TechTypes - is it registered with SMLHelper?
            if (TechTypeHandler.TryGetModdedTechType(value, out TechType custom))
                return custom;

            return TechType.None;
        }

        public void Init()
        {
            bool bUpdated = false;
            if (CapacityOverrides == null || CapacityOverrides.Count < 1)
            {
                CapacityOverrides = new Dictionary<string, float>();
                bUpdated = true;
            }

            foreach (KeyValuePair<string, float> kvp in CapacityOverrides)
            {
                TechType tt = GetTechType(kvp.Key);
                if (tt == TechType.None)
                {
                    Logger.Log(Logger.Level.Debug, $"Failed to load TechType for string '{kvp.Key}'");
                    continue;
                }

                SetCapacityOverride(tt, kvp.Value);
            }

            if(bUpdated)
            {
                Save();
                Logger.Log(Logger.Level.Debug, "Some values reset to defaults");
            }
            else
                Logger.Log(Logger.Level.Debug, "All values present and correct");
        }
    }
}
