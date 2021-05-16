using Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace CustomiseOxygen.Patches
{
    public class CustomOxy : MonoBehaviour, ICraftTarget
    {
        // This class exists pretty much entirely as an ICraftTarget, so we can receive an OnCraftEnd event.
        // There's probably other ways to get OnCraftEnd, but this seems the easiest.

        private Oxygen oxygen;
        private TechType techType;

        public void OnCraftEnd(TechType techType)
        {
            this.techType = techType;
            this.oxygen = base.GetComponent<Oxygen>();
            if (Main.config.bManualRefill)
                this.oxygen.oxygenAvailable = this.oxygen.oxygenCapacity;
        }

        private void Awake()
        {
            if (this.techType == TechType.None)
                this.techType = CraftData.GetTechType(base.gameObject);

            this.oxygen = base.GetComponent<Oxygen>();
            if (this.oxygen == null)
            {
#if !RELEASE
                Log.LogDebug($"CustomOxy: Failed to find Oxygen component in parent"); 
#endif
                return;
            }

            if (this.oxygen.isPlayer)
            {
                return;
            }

            if (Main.config.GetCapacityOverride(this.techType, out float capacityOverride, out float capacityMultiplier))
            {
                bool bIsManual = Main.config.bManualRefill;
                Log.LogDebug($"CustomiseOxygen.Main.GetCapacityOverride returned true with values of capacityOverride={capacityOverride}, capacityMultiplier={capacityMultiplier}");
                if (capacityOverride > 0)
                    this.oxygen.oxygenCapacity = capacityOverride;
                else
                {
                    Main.config.SetCapacityOverride(this.techType, this.oxygen.oxygenCapacity, false, true);
                    this.oxygen.oxygenCapacity *= capacityMultiplier;
                }
                Log.LogDebug($"CustomOxy.Awake(): Oxygen capacity set to {this.oxygen.oxygenCapacity}");
            }
        }
    }

    [HarmonyPatch]
    public class OxygenPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Oxygen), "Awake")]
        public static bool Prefix(ref Oxygen __instance)
        {
            if (!__instance.isPlayer)
            {
                if (__instance.gameObject.GetComponent<CustomOxy>() == null)
                {
                    Logger.Log(Logger.Level.Debug, $"Adding CustomOxy component to instance {__instance.ToString()}");
                    __instance.gameObject.EnsureComponent<CustomOxy>();
                }
                else
                    Logger.Log(Logger.Level.Debug, $"CustomOxy already present on instance {__instance.ToString()}");
                return false;
            }

            return true;
        }
    }
}
