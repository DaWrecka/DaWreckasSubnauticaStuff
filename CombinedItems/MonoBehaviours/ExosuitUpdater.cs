using CombinedItems.ExosuitModules;
using CombinedItems.Patches;
using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CombinedItems.MonoBehaviours
{
    internal class ExosuitUpdater : MonoBehaviour
    {
        private bool bLastSprint = false;
        protected float defaultForwardForce;
        protected Exosuit parentVehicle;
        protected Equipment exosuitModules => parentVehicle != null ? parentVehicle.modules : null;

        internal virtual void Initialise(ref Vehicle vehicle)
        {
            if (vehicle is Exosuit exosuit)
            {
                parentVehicle = exosuit;
                defaultForwardForce = exosuit.forwardForce;
                Log.LogDebug($"ExosuitUpdater.Initialise(): Found Exosuit with forwardForce of {defaultForwardForce}");
            }
        }

        internal virtual void PostUpgradeModuleChange(TechType changedTechType = TechType.None)
        {
            if (parentVehicle != null)
            {
                defaultForwardForce = parentVehicle != null ? parentVehicle.forwardForce : 0f;
            }
        }

        internal virtual void Tick()
        {
            if (parentVehicle != null)
            {
                bool bExosuitSprint = parentVehicle.modules.GetCount(Main.prefabExosuitSprintModule.TechType) > 0 && GameInput.GetButtonHeld(GameInput.Button.Sprint);
                bool bJumpJetsUpgraded = ExosuitPatches.JumpJetsUpgraded(parentVehicle);
                float forceMultiplier = 1f * (bExosuitSprint ? (bJumpJetsUpgraded ? 2.5f : 2f) : 1f); // These constants will likely be tweaked, but they're here for testing
                parentVehicle.forwardForce = defaultForwardForce * forceMultiplier;
                if(bExosuitSprint != bLastSprint)
                    Log.LogDebug($"VehiclePatches.Tick(): Applying forwardForce of {parentVehicle.forwardForce} to Exosuit with defaultForwardForce of {defaultForwardForce}");
                bLastSprint = bExosuitSprint;
            }
        }

        public void PostOverrideAcceleration(ref Vector3 acceleration, bool JumpJetsUpgraded = false)
        {
            if (exosuitModules?.GetCount(Main.prefabExosuitSprintModule.TechType) > 0 && GameInput.GetButtonDown(GameInput.Button.Sprint))
            {
                float thrust = JumpJetsUpgraded ? 2.5f : 2f;
                acceleration.x *= thrust;
                acceleration.z *= thrust;
            }
        }

        public virtual void ApplyPhysicsMove()
        {
        }

        internal virtual void Update()
        {
        }
    }
}
