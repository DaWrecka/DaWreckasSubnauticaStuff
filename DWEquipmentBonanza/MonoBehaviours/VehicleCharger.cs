using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
    /*public class VehicleCharger : MonoBehaviour
    {
        private Battery cell;
        private float ThermalChargeRate = 0f;
        private float SolarChargeRate = 0f;

        private IEnumerator Awake()
        {
            while (gameObject == null)
                yield return new WaitForEndOfFrame();

            cell = gameObject.GetComponent<Battery>();
        }

        public void Update()
        {
			float generatedEnergy = 0f;
			DayNightCycle daynight = DayNightCycle.main;
			if (daynight == null)
			{
				return;
			}
			float num = SolarChargeRate * Mathf.Clamp01((Constants.kMaxSolarChargeDepth + gameObject.transform.position.y) / Constants.kMaxSolarChargeDepth);
			float localLightScalar = daynight.GetLocalLightScalar();
			generatedEnergy += Constants.kSeamothSolarChargePerSecond * localLightScalar * num;

			WaterTemperatureSimulation waterSim = WaterTemperatureSimulation.main;
			if (waterSim != null)
			{
				float temperature = waterSim.GetTemperature(parentVehicle.transform.position);
				generatedEnergy += SeaTruckUpdater.thermalReactorCharge.Evaluate(temperature) * ThermalChargeRate;
			}
			parentVehicle.relay.ModifyPower(amount, out float modified);
		}
	}*/
}
