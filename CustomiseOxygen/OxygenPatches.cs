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
            this.oxygen.oxygenAvailable = this.oxygen.oxygenCapacity;
        }

        private void Awake()
        {
            if (this.techType == TechType.None)
                this.techType = CraftData.GetTechType(base.gameObject);

            this.oxygen = base.GetComponent<Oxygen>();
            if (this.oxygen == null)
            {
                Logger.Log(Logger.Level.Debug, "CustomOxy: Failed to find Oxygen component in parent");
                return;
            }

            if (!this.oxygen.isPlayer)
            {
                float capacityOverride = Main.config.GetCapacityOverride(this.techType);
                if (capacityOverride > 0)
                    this.oxygen.oxygenCapacity = capacityOverride;
                else
                {
                    Main.config.SetCapacityOverride(this.techType, this.oxygen.oxygenCapacity, false, true);
                    this.oxygen.oxygenCapacity *= Main.config.baseOxyMultiplier;
                    if (!Main.config.bAllowAutoRefill)
                        this.oxygen.oxygenCapacity *= Main.config.refillableMultiplier;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Oxygen), "Awake")]
    class OxygenPatches
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Oxygen __instance)
        {
            //return (__instance.isPlayer || Main.config.bAllowAutoRefill);
            if (!__instance.isPlayer)
            {
                __instance.gameObject.AddComponent<CustomOxy>();
                return false;
            }

            return true;
        }

        /*[HarmonyPostfix]
        public static void Postfix(ref Oxygen __instance)
        {
            if (__instance.isPlayer)
                return;

            __instance.oxygenCapacity *= Main.config.baseOxyMultiplier;
            if (Main.config.bAllowAutoRefill)
                return;
            __instance.oxygenCapacity *= Main.config.refillableMultiplier;
        }*/
    }
}
