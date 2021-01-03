using SMLHelper.V2.Crafting;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Logger = QModManager.Utility.Logger;

namespace CustomiseYourScannables
{
    class CYScanConfig : ConfigFile
    {
        public List<TechType> NewScannables;
        private List<TechType> DefaultScannables = new List<TechType>() // List of TechTypes to add ResourceTrackers to, if they don't have one already.
        {
            TechType.BasaltChunk,
            TechType.BaseFiltrationMachine,
            TechType.BatteryChargerFragment,
            TechType.Bleeder,
            TechType.Crabsnake,
            TechType.CrabSquid,
            TechType.SeaCrown,
            TechType.Warper
        };
        public List<TechType> NonScannables;
        private List<TechType> DefaultNonScannables = new List<TechType>() // For any object whose TechType is on this list, its ResourceTracker, if it has one, should be removed.
        {
            TechType.Wreck
        };
    }
}
