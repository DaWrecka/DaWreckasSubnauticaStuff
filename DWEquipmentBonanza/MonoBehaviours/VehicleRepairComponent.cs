using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
    public class VehicleRepairComponent : MonoBehaviour
    {
        protected MonoBehaviour ParentVehicle; // this will either be a Seamoth component, Exosuit component, or SeaTruckMotor component

        // On the surface these accessors could be done with properties; they're defined as virtual so that subclasses can override them, however.
        protected bool _enabled;

        public virtual bool GetIsEnabled()
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");

            return _enabled;
        } // Once added, the component won't be removed; if the upgrade module is removed, regeneration will be disabled using this

        protected bool _active;
        public virtual bool GetIsActive()
        {
            return this._active;
        } // In active mode, health regeneration is greatly increased, but so is energy consumption

        protected static float fPassiveEnergyConsumptionPerSecond = 1f;
        protected static float fPassiveHealthRegenerationPerSecond = 0.5f;
        protected static float fActiveModeEnergyMultiplier = 10f; // Energy consumption is multiplied by this amount in active mode
        protected static float fActiveModeHealthRegenMultiplier = 8f; // Health regeneration is multiplied by this amount in active mode

        protected virtual LiveMixin GetVehicleMixin()
        {
            LiveMixin _livemix = null;
            if (ParentVehicle == null && gameObject != null)
            {
                if (gameObject.TryGetComponent<Vehicle>(out Vehicle v))
                    ParentVehicle = v;
            }

            if (ParentVehicle is Vehicle vehicle)
                _livemix = vehicle.liveMixin;
            return _livemix;
        }

        public virtual float ConsumeVehicleEnergy(float amount)
        {
            if (ParentVehicle is Vehicle v)
            {
                return v.energyInterface.ConsumeEnergy(amount);
            }

            return -1f;
        }

        public virtual bool TryGetEnergyProperties(out float available, out float capacity)
        {
            if (ParentVehicle is Vehicle v)
            {
                v.energyInterface.GetValues(out available, out capacity);
                return true;
            }

            available = 0f;
            capacity = 0f;
            return false;
        }

        public virtual void SetEnabled(bool bEnableState, MonoBehaviour vehicle = null)
        {
            if (ParentVehicle == null && vehicle != null)
            {
                Initialise(vehicle.gameObject);
            }

            _enabled = bEnableState;
        }

        public virtual bool ToggleActiveState(MonoBehaviour vehicle = null)
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");

            if (ParentVehicle == null && vehicle != null)
            {
                Initialise(vehicle.gameObject);
            }

            this._active = !this._active;

            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() returning {this._active}");
            return this._active;
        }

        public virtual bool SetActiveState(bool bNewState, MonoBehaviour vehicle = null)
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
            if (ParentVehicle == null && vehicle != null)
                Initialise(vehicle.gameObject);

            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
            this._active = bNewState;
            this._enabled = true;

            return true;
        }

        public virtual bool Initialise(GameObject obj)
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");

            Vehicle v;
            if(!obj.TryGetComponent<Vehicle>(out v))
            {
                return false;
            }

            ParentVehicle = v;

            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
            return true;
        }

        public virtual void Update()
        {
            LiveMixin mixin = gameObject.GetComponent<LiveMixin>();
            if (mixin == null)
                return;

            if (!GetIsEnabled())
                return;

            float deltaTime = Time.deltaTime;
            float damage = mixin.maxHealth - mixin.health;
            bool isActiveMode = this.GetIsActive();
            if (damage <= 0f)
            {
                if (isActiveMode)
                    this.SetActiveState(false);
                return;
            }

            if (TryGetEnergyProperties(out float available, out float capacity))
            {
                float regen = Mathf.Min(damage, fPassiveHealthRegenerationPerSecond * deltaTime * (isActiveMode ? fActiveModeHealthRegenMultiplier : 1f));
                float energyConsumption = Mathf.Min(capacity - available, fPassiveEnergyConsumptionPerSecond * deltaTime * (isActiveMode ? fActiveModeEnergyMultiplier : 1f));
                if(ConsumeVehicleEnergy(energyConsumption) >= 0f)
                    mixin.AddHealth(regen);
            }
        }
    }

#if BELOWZERO
    internal class SeaTruckRepairComponent : VehicleRepairComponent
    {
        private SeaTruckSegment _Segment;
        private SeaTruckSegment thisSegment
        {
            get
            {
                _Segment ??= this.gameObject.GetComponent<SeaTruckSegment>();
                return _Segment;
            }
        }
        private bool bIsCabSegment => (thisSegment != null && thisSegment.isMainCab);
        private bool bHasFrontConnection => (thisSegment != null && thisSegment.isFrontConnected);

        protected override LiveMixin GetVehicleMixin()
        {
            if (thisSegment != null)
            {
                return this.gameObject.GetComponent<LiveMixin>();
            }

            return null;
        }

        private bool IsMainCab()
        {
            return (thisSegment != null && thisSegment.isMainCab);
        }

        private SeaTruckSegment GetSeaTruckCab()
        {
            if (IsMainCab())
                return thisSegment;

            SeaTruckSegment currentSegment = gameObject.GetComponent<SeaTruckSegment>()?.GetFirstSegment();

            if (currentSegment != null && currentSegment.isMainCab)
                return currentSegment;

            return null;
        }

        public override bool GetIsEnabled()
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");

            if (IsMainCab())
                return this._enabled;

            SeaTruckSegment cab = GetSeaTruckCab();

            if (cab != null)
            {
                SeaTruckRepairComponent component = cab.gameObject.GetComponent<SeaTruckRepairComponent>();

                bool result = component.GetIsEnabled();
                //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
                return component.GetIsEnabled();
            }

            return false;
        }

        public override bool GetIsActive()
        {
            if (IsMainCab())
                return this._active;

            SeaTruckSegment cab = GetSeaTruckCab();

            if (cab != null)
            {
                var component = cab.gameObject.GetComponent<SeaTruckRepairComponent>();
                return component.GetIsActive();
            }

            return false;
        } // In active mode, health regeneration is greatly increased, but so is energy consumption

        public override void SetEnabled(bool bEnableState, MonoBehaviour vehicle = null)
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}(): begin");

            if (!IsMainCab())
            {
                //Log.LogDebug("not main cab; exiting");
                return;
            }

            var cab = GetSeaTruckCab();
            if (cab == null)
            {
                //Log.LogDebug("Could not get SeaTruck cab; exiting");
                return;
            }

            this._enabled = bEnableState;
            //Log.LogDebug($"Enable state set to {this._enabled}");
        }

        public override float ConsumeVehicleEnergy(float amount)
        {
            if (thisSegment != null)
            {
                thisSegment.relay.ConsumeEnergy(amount, out float consumed);
                return consumed;
            }

            return -1f;
        }

        public override bool ToggleActiveState(MonoBehaviour vehicle = null)
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");

            if (!IsMainCab())
                return false;

            /*SeaTruckSegment cab = GetSeaTruckCab();
            if (cab == null)
            {
                //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() failed to retrieve Seatruck cab, returning false");
                return false;
            }*/

            this._active = !this._active;
            this._enabled = true;

            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() returning {this._active}");
            return this._active;
        }

        public override bool SetActiveState(bool bNewState, MonoBehaviour vehicle = null)
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}({this.GetInstanceID()}).{thisMethod.Name}({bNewState}) executing");
            if (ParentVehicle == null && vehicle != null)
            {
                //Log.LogDebug($"{thisMethod.ReflectedType.Name}({this.GetInstanceID()}).{thisMethod.Name}({bNewState}) initialising");
                Initialise(vehicle.gameObject);
            }

            if (!IsMainCab())
            {
                //Log.LogDebug($"{thisMethod.ReflectedType.Name}({this.GetInstanceID()}).{thisMethod.Name}({bNewState}) Not main cab; terminating");
                return false;
            }

            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
            this._active = bNewState;
            return true;
        }

        public override bool Initialise(GameObject vehicle)
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");

            SeaTruckSegment s = vehicle.GetComponent<SeaTruckSegment>();
            if (s == null)
                return false;

            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
            return true;
        }

        public override bool TryGetEnergyProperties(out float available, out float capacity)
        {
            if (thisSegment?.relay != null)
            {
                capacity = thisSegment.relay.GetMaxPower();
                available = thisSegment.relay.GetPower();
                return true;
            }

            available = 0f;
            capacity = 0f;
            return false;
        }

        public override void Update()
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
            LiveMixin v = this.GetVehicleMixin();
            if (v == null)
            {
                //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() no LiveMixin, terminating");
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            if (!GetIsEnabled())
            {
                //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() component disabled, terminating");
                return;
            }

            float damage = v.maxHealth - v.health;
            bool isActiveMode = this.GetIsActive();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() Active mode: {isActiveMode}");
            if (damage <= 0f)
            {
                // Active mode may still be useful even if the cab is at full health, as modules may *not* be at full health

                //if (isActiveMode)
                //    this.SetActiveState(false);
                return;
            }

            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() getting energy properties");
            if (TryGetEnergyProperties(out float available, out float capacity))
            {
                float regen = Mathf.Min(damage, fPassiveHealthRegenerationPerSecond * deltaTime * (isActiveMode ? fActiveModeHealthRegenMultiplier : 1f));
                float energyConsumption = Mathf.Min(available, fPassiveEnergyConsumptionPerSecond * deltaTime * (isActiveMode ? fActiveModeEnergyMultiplier : 1f));

                //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() energy status {available}/{capacity}, attempting to add {regen} health while consuming {energyConsumption} energy");
                if (energyConsumption <= 0f)
                {
                    //Log.LogDebug($"No energy available");
                    return;
                }
                if (ConsumeVehicleEnergy(energyConsumption) > 0f)
                    v.AddHealth(regen);
            }
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() complete");
        }
    }
#endif
}
