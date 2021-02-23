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
    internal class VehicleUpdater : MonoBehaviour
    {
        protected Vehicle parentVehicle;

        internal virtual void Initialise(ref Vehicle vehicle)
        {
            if (vehicle != null)
            {
                parentVehicle = vehicle;
            }
        }

        protected virtual int GetModuleCount(TechType techType)
        {
            if (parentVehicle == null || parentVehicle.modules == null)
                return 0;

            return parentVehicle.modules.GetCount(techType);
        }

        internal virtual void PostUpgradeModuleChange(TechType changedTechType = TechType.None)
        { }

        internal virtual void PreUpdate()
        { }

        internal virtual void Update()
        {
        }

        internal virtual void PostUpdate()
        { }

        internal virtual void PostOverrideAcceleration(ref Vector3 acceleration)
        { }

        internal virtual void ApplyPhysicsMove()
        { }

    }
}
