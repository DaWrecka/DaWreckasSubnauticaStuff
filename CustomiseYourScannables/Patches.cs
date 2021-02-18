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
#if !RELEASE
            Logger.Log(Logger.Level.Debug, $"GetPrefabForTechTypePatch running: TechType {techType.ToString()}"); 
#endif
            if (Main.config.NewScannables.Contains(techType))
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Found TechType in NewScannables list"); 
#endif
                if (__result.GetComponent<ResourceTracker>() == null)
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"No existing ResourceTracker found"); 
#endif
                    ResourceTracker rt = __result.EnsureComponent<ResourceTracker>();
                    if (rt != null)
                    {
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Added new ResourceTracker"); 
#endif
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
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Found TechType in NonScannables list"); 
#endif
                ResourceTracker rt = __result.GetComponent<ResourceTracker>();
                if (rt != null)
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"Attempting to destroy existing ResourceTracker"); 
#endif
                    UnityEngine.Object.Destroy(rt);
                }
                else
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"No ResourceTracker found in prefab"); 
#endif
                }
            }

            return __result;
        }
    }
}
