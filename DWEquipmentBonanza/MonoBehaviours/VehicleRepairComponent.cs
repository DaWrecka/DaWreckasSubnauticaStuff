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
        private GameObject ParentGameObject;
        private MonoBehaviour ParentVehicle; // this will either be a Seamoth component, Exosuit component, or SeaTruckMotor component
        private bool bEnabled; // Once added, the component won't be removed; if the upgrade module is removed, regeneration will be disabled using this
        private bool bActive; // In active mode, health regeneration is greatly increased, but so is energy consumption
        private static float fPassiveEnergyConsumptionPerSecond = 1f;
        private static float fPassiveHealthRegenerationPerSecond = 0.5f;
        private static float fActiveModeEnergyMultiplier = 10f; // Energy consumption is multiplied by this amount in active mode
        private static float fActiveModeHealthRegenMultiplier = 8f; // Health regeneration is multiplied by this amount in active mode
        private LiveMixin _livemix;
        public LiveMixin VehicleLife
        {
            get
            {
                if (_livemix == null)
                {
                    if (ParentVehicle == null && ParentGameObject != null)
                    {
#if SUBNAUTICA_STABLE
                        SeaMoth s = vehicle.GetComponent<SeaMoth>();
#elif BELOWZERO
                        SeaTruckMotor s = ParentGameObject.GetComponent<SeaTruckMotor>();
#endif
                        Exosuit exosuit = ParentGameObject.GetComponent<Exosuit>();

                        if (s != null || exosuit != null)
                        {
                            ParentVehicle = (s != null ? s : exosuit);
                        }
                    }

                    if (ParentVehicle is Vehicle v)
                        _livemix = v.liveMixin;
                    else if (ParentVehicle is SeaTruckMotor stm)
                        _livemix = stm.liveMixin;
                }

                return _livemix;
            }
        }

        public float ConsumeVehicleEnergy(float amount)
        {
            if (ParentVehicle is Vehicle v)
            {
                return v.energyInterface.ConsumeEnergy(amount);
            }
            else if (ParentVehicle is SeaTruckMotor s)
            {
                s.relay.ConsumeEnergy(amount, out float consumed);
                return consumed;
            }

            return -1f;
        }

        public bool TryGetEnergyProperties(out float available, out float capacity)
        {
            if (ParentVehicle is Vehicle v)
            {
                v.energyInterface.GetValues(out available, out capacity);
                return true;
            }
            else if (ParentVehicle is SeaTruckMotor stm)
            {
                available = stm.relay.GetPower();
                capacity = stm.relay.GetMaxPower();
                return true;
            }

            available = 0f;
            capacity = 0f;
            return false;

        }

        public void SetEnabled(bool bEnableState, MonoBehaviour vehicle = null)
        {
            if (ParentVehicle == null && vehicle != null)
            {
                Initialise(vehicle.gameObject);
            }

            bEnabled = bEnableState;
        }

        public bool ToggleActiveState(MonoBehaviour vehicle = null)
        {
            if (ParentVehicle == null && vehicle != null)
            {
                Initialise(vehicle.gameObject);
            }

            bActive = !bActive;

            return bActive;
        }

        public void SetActiveState(bool bNewState, MonoBehaviour vehicle = null)
        {
            bActive = bNewState;
        }

        public bool Initialise(GameObject vehicle)
        {
#if SUBNAUTICA_STABLE
            SeaMoth s = vehicle.GetComponent<SeaMoth>();
#elif BELOWZERO
            SeaTruckMotor s = vehicle.GetComponent<SeaTruckMotor>();
#endif
            Exosuit exosuit = vehicle.GetComponent<Exosuit>();

            if (s == null && exosuit == null)
            {
                return false;
            }

            ParentVehicle = (s != null ? s : exosuit);
            ParentGameObject = vehicle;
            return true;
        }

        internal void Update()
        {
            var v = VehicleLife;
            if (v == null)
                return;

            if (!bEnabled)
                return;

            float deltaTime = Time.deltaTime;

            float damage = v.maxHealth - v.health;
            if (damage <= 0f)
            {
                if (bActive)
                    bActive = false;
                return;
            }

            if (TryGetEnergyProperties(out float available, out float capacity))
            {
                float regen = Mathf.Min(damage, fPassiveHealthRegenerationPerSecond * deltaTime * (bActive ? fActiveModeHealthRegenMultiplier : 1f));
                float energyConsumption = Mathf.Min(capacity - available, fPassiveEnergyConsumptionPerSecond * deltaTime * (bActive ? fActiveModeEnergyMultiplier : 1f));
                v.AddHealth(regen);
                ConsumeVehicleEnergy(energyConsumption);
            }
        }
    }
}
