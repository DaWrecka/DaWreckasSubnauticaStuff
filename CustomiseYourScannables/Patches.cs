using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace CustomiseYourScannables
{
    [HarmonyPatch(typeof(CraftData), nameof(CraftData.GetPrefabForTechType))]
    class Patches
    {
        //public static GameObject GetPrefabForTechType(TechType techType, bool verbose = true);
        [HarmonyPostfix]
        public static GameObject GetPrefabForTechTypePatch(GameObject __result, TechType techType, bool verbose = true)
        {
            Logger.Log(Logger.Level.Debug, $"GetPrefabForTechTypePatch running: TechType {techType.ToString()}"); 
            if (Main.config.NewScannables.Contains(techType))
            {
                Logger.Log(Logger.Level.Debug, $"Found TechType in NewScannables list"); 
                if (__result.GetComponent<ResourceTracker>() == null)
                {
                    Logger.Log(Logger.Level.Debug, $"No existing ResourceTracker found"); 
                    ResourceTracker rt = __result.EnsureComponent<ResourceTracker>();
                    if (rt != null)
                    {
                        Logger.Log(Logger.Level.Debug, $"Added new ResourceTracker"); 
                        rt = __result.EnsureComponent<ResourceTracker>();
                        rt.prefabIdentifier = __result.GetComponent<PrefabIdentifier>();
                        rt.techType = TechType.SeaDragon;
                        rt.overrideTechType = TechType.SeaDragon;
                        rt.rb = __result.GetComponent<Rigidbody>();
                        rt.pickupable = __result.GetComponent<Pickupable>();
                    }
                }
            }
            else if (Main.config.NonScannables.Contains(techType))
            {
                Logger.Log(Logger.Level.Debug, $"Found TechType in NonScannables list"); 
                ResourceTracker rt = __result.GetComponent<ResourceTracker>();
                if (rt != null)
                {
                    Logger.Log(Logger.Level.Debug, $"Attempting to destroy existing ResourceTracker"); 
                    UnityEngine.Object.Destroy(rt);
                }
                else
                {
                    Logger.Log(Logger.Level.Debug, $"No ResourceTracker found in prefab"); 
                }
            }

            return __result;
        }
    }
}
