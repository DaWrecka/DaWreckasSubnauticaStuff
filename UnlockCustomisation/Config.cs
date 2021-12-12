﻿using Common;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnlockCustomisation
{
    public class UCConfig : ConfigFile
    {
        public List<string> UnlockAtStart = new List<string>(); // List of blueprints to unlock at the start
        public Dictionary<string, string> SingleUnlocks = new Dictionary<string, string>(); // List of blueprints to unlock when one other blueprint is unlocked; Key is unlocked when value is unlocked
        public Dictionary<string, List<string>> CompoundTechs = new Dictionary<string, List<string>>(); // List of blueprints that should unlock when two or more other blueprints are unlocked
            // Key is unlocked when all of the TechTypes in the collection are unlocked

        private HashSet<TechType> UnlockStartTypes = new HashSet<TechType>();
        private HashSet<TechType> SingleUnlockTypes = new HashSet<TechType>();
        private HashSet<TechType> CompoundTechTypes = new HashSet<TechType>();

        public string UnlockAtStartHelp => "TechTypes whose names are in this list will be unlocked and available for crafting from the start of the game.";
        public string SingleUnlocksHelp => "Allows blueprints to be unlocked when a different blueprint is unlocked. The sample entries unlock the Creature Decoy when the Cyclops Decoy Tube Upgrade, and the Mobile Vehicle Bay (whose TechType is Constructor) unlocks when the Seamoth is unlocked.";
        public string CompoundTechsHelp => "Allows one blueprint to be unlocked when two or more other blueprints are unlocked. The sample entry unlocks the Prawn Suit (Exosuit) when all three of the Propulsion, Torpedo, and Grappling Arm modules are unlocked.";

        public List<string> SampleUnlockAtStart => new List<string>()
        {
            TechType.DoubleTank.AsString(),
            TechType.LEDLight.AsString()
        };
        public Dictionary<string, string> SampleSingleUnlocks => new Dictionary<string, string>()
        {
            { TechType.CyclopsDecoy.AsString(), TechType.CyclopsDecoyModule.AsString() },
            { TechType.Constructor.AsString(), TechType.Seamoth.AsString() }
        };
        public Dictionary<string, List<string>> SampleCompoundTechs => new Dictionary<string, List<string>>()
        {
            { TechType.Exosuit.AsString(), new List<string>()
                {
                    TechType.ExosuitDrillArmModule.AsString(),
                    TechType.ExosuitPropulsionArmModule.AsString(),
                    TechType.ExosuitTorpedoArmModule.AsString(),
                    TechType.ExosuitGrapplingArmModule.AsString()
                }
            }
        };

        public void Patch()
        {
            foreach (string s in UnlockAtStart)
            {
                TechType tt = TechTypeUtils.GetModTechType(s);
                if (tt != TechType.None)
                {
                    if (UnlockStartTypes.Contains(tt))
                    {
                        Log.LogError($"Duplicate entry {s} in UnlockAtStart list");
                        // Unlike with the other collections, this is actually an error. A blueprint can't be unlocked multiple times at the start of the game.
                    }
                    else
                    {
                        UnlockStartTypes.Add(tt);
                        KnownTechHandler.UnlockOnStart(tt);
                    }
                }
                else
                {
                    Log.LogError($"Could not parse string '{s}' in UnlockAtStart list as TechType");
                }
            }

            foreach (KeyValuePair<string, string> kvp in SingleUnlocks)
            {
                TechType key = TechTypeUtils.GetModTechType(kvp.Key);
                if (key == TechType.None)
                {
                    Log.LogError($"Could not parse key '{kvp.Key}' in SingleUnlocks collection as TechType");
                    continue;
                }

                TechType value = TechTypeUtils.GetModTechType(kvp.Value);
                if (value == TechType.None)
                {
                    Log.LogError($"Could not parse value '{kvp.Value}' in SingleUnlocks collection as TechType");
                    continue;
                }

                if (SingleUnlockTypes.Contains(key))
                {
                    // A duplicate key in this collection isn't necessarily an error; It may be that a single blueprint has been configured with multiple ways to unlock it.
                    // It's worth logging as a warning, however.
                    Log.LogWarning($"Duplicate key '{kvp.Key}' found in SingleUnlocks collection; Verify that this is intended behaviour.");
                }
                else
                    SingleUnlockTypes.Add(key);

                KnownTechHandler.SetAnalysisTechEntry(value, new HashSet<TechType>() { key });
            }

            foreach (KeyValuePair<string, List<string>> compound in CompoundTechs)
            {
                TechType tt = TechTypeUtils.GetModTechType(compound.Key);
                if (tt == TechType.None)
                {
                    Log.LogError($"Could not parse string '{compound.Key}' for key as TechType in CompoundTechs");
                    continue;
                }
                if (CompoundTechTypes.Contains(tt))
                {
                    Log.LogWarning($"Multiple entries found in CompoundTechs for TechType {tt.AsString()}; Verify that this is intended behaviour.");
                }
                else
                    CompoundTechTypes.Add(tt);

                HashSet<TechType> compoundTechs = new HashSet<TechType>();
                foreach (string s in compound.Value)
                {
                    TechType t = TechTypeUtils.GetModTechType(s);
                    if (t == TechType.None)
                    {
                        Log.LogError($"Could not parse string '{s}' as TechType in CompoundTechs collection, key {tt.AsString()}");
                    }
                    else if (compoundTechs.Contains(t))
                    {
                        Log.LogError($"Duplicate entry '{s}' in CompoundTechs, key {tt.AsString()}");
                    }
                    else
                    {
                        compoundTechs.Add(t);
                    }
                }

                if (compoundTechs.Count < 2)
                {
                    Log.LogWarning($"Not enough valid TechTypes found in list for CompoundTechs entry {tt.AsString()}; Verify the spelling of all entries within its list.");
                }
                else
                {
                    Reflection.AddCompoundTech(tt, compoundTechs.ToList<TechType>());
                }
            }
        }

        public void MakeSample()
        {
            UnlockAtStart = new List<string>(SampleUnlockAtStart);
            SingleUnlocks = new Dictionary<string, string>(SampleSingleUnlocks);
            CompoundTechs = new Dictionary<string, List<string>>(SampleCompoundTechs);

            Save();
            ErrorMessage.AddMessage("Save game now");
        }
    }
}
