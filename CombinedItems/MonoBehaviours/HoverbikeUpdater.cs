using CombinedItems;
using CombinedItems.VehicleModules;
using CombinedItems.MonoBehaviours;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using QModManager.Utility;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;


namespace CombinedItems.MonoBehaviours
{
    internal class HoverbikeUpdater : MonoBehaviour
    {
        private Hoverbike parentHoverbike;
        private float defaultWaterDampening;
        private float defaultWaterOffset;
        private bool bHasTravelModule;
        private const float moduleWaterDampening = 1f; // Movement is divided by this value when travelling over water.
            // Don't set it below 1f, as that makes the Snowfox *more* manoeuvrable over water than over land.
        private const float moduleWaterOffset = 1f; // The default value for ground travel is 2m.

        public virtual void Initialise(ref Hoverbike vehicle)
        {
            parentHoverbike = vehicle;
            defaultWaterDampening = vehicle.waterDampening;
            defaultWaterOffset = vehicle.waterLevelOffset;
        }

        protected virtual int GetModuleCount(TechType techType)
        {
            if (parentHoverbike == null || parentHoverbike.modules == null)
                return 0;

            return parentHoverbike.modules.GetCount(techType);
        }

        internal virtual void PreUpdate(Hoverbike instance = null)
        {
        }

        internal virtual void Update(Hoverbike instance = null)
        {
        }

        internal virtual void PostUpdate(Hoverbike instance = null)
        {
        }

        internal virtual void PostUpgradeModuleChange(int slotID, TechType techType, bool added, Hoverbike instance = null)
        {
            if (parentHoverbike == null)
            {
                if (instance != null)
                    Initialise(ref instance);
                else
                    return;
            }

            if (techType == Main.prefabHbWaterTravelModule.TechType)
            {
                bHasTravelModule = added;
            }
        }

        internal virtual void PrePhysicsMove(Hoverbike instance = null)
        {
            if (parentHoverbike == null)
                Initialise(ref instance);

            if (bHasTravelModule)
            {
                if (parentHoverbike.GetPilotingCraft())
                {
                    parentHoverbike.waterLevelOffset = moduleWaterOffset; // Makes the bike hover above the surface when piloted with the Water Travel Module
                    parentHoverbike.waterDampening = moduleWaterDampening;
                    return;
                }
            }

            parentHoverbike.waterLevelOffset = defaultWaterOffset; // Let the hoverbike sink to water level when not piloted.
                                                                   // Otherwise it can be kind of hard to get on if the water offset is too high.
                                                                   // Of course this also runs if the module isn't installed.
            parentHoverbike.waterDampening = defaultWaterDampening; // Without this, the hoverbike will bob up and down on the water erratically when not piloted.
                                                                    // And we have to do it in PhysicsMove because piloted/not piloted changes more often than PostUpgradeModuleChange.
        }
    }
}
