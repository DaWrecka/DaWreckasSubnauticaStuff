using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace CombinedItems.MonoBehaviours
{
    class SeaTruckUpdater : MonoBehaviour
    {
		private const int MaxThermalModules = 1;
		protected static TechType solarModuleTechType => Main.GetModTechType("SeaTruckSolarModule");
		protected static TechType thermalModuleTechType => Main.GetModTechType("SeaTruckThermalModule");
		protected static AnimationCurve thermalReactorCharge;
		protected static int solarCount;
		protected static int thermalCount;
		protected SeaTruckUpgrades parentVehicle;

		protected void Start()
		{
			if (thermalReactorCharge is null && PrefabDatabase.TryGetPrefabFilename(CraftData.GetClassIdForTechType(TechType.Exosuit), out string RCFilename))
			{
				AddressablesUtility.LoadAsync<GameObject>(RCFilename).Completed += (x) =>
				{
					GameObject gameObject1 = x.Result;
					Exosuit exosuit = gameObject1?.GetComponent<Exosuit>();
					thermalReactorCharge = exosuit?.thermalReactorCharge;
				};
			}
		}

		internal void Initialise(ref SeaTruckUpgrades seaTruckUpgrades)
		{
			parentVehicle = seaTruckUpgrades;
		}


		internal virtual void PreUpdate(SeaTruckUpgrades instance = null)
		{
		}

		// Not to be confused with Unity's Update
		internal virtual void OnUpdate(SeaTruckUpgrades instance = null)
		{
		}

		internal virtual void PostUpdate(SeaTruckUpgrades instance = null)
		{
		}

		internal virtual void PreUpdateEnergy(SeaTruckUpgrades instance = null)
		{
		}

		internal virtual void PostUpdateEnergy(SeaTruckUpgrades instance = null)
		{
		}

		internal virtual void PostUpgradeModuleChange(int slotID, TechType techType, bool added, SeaTruckUpgrades instance)
		{
			if (parentVehicle == null)
				parentVehicle = instance;

			if (parentVehicle == null)
				return;

			base.CancelInvoke("UpdateRecharge");
			solarCount = parentVehicle.modules.GetCount(solarModuleTechType);
			thermalCount = parentVehicle.modules.GetCount(thermalModuleTechType);
			if (solarCount + thermalCount > 0)
				base.InvokeRepeating("UpdateRecharge", 1f, 1f);
		}

		private void UpdateRecharge()
		{
			if (solarCount > 0)
			{
				DayNightCycle daynight = DayNightCycle.main;
				if (daynight == null)
				{
					return;
				}
				float num = Mathf.Clamp01((Constants.kMaxSolarChargeDepth + parentVehicle.transform.position.y) / Constants.kMaxSolarChargeDepth);
				float localLightScalar = daynight.GetLocalLightScalar();
				float amount = Constants.kSeamothSolarChargePerSecond * localLightScalar * num * (float)solarCount;
				parentVehicle.relay.ModifyPower(amount, out float modified);
			}

			if (thermalCount > 0)
			{
				WaterTemperatureSimulation waterSim = WaterTemperatureSimulation.main;
				if (waterSim != null)
				{
					float temperature = waterSim.GetTemperature(parentVehicle.transform.position);
					float num = thermalReactorCharge.Evaluate(temperature) * thermalCount;
					parentVehicle.relay.ModifyPower(num * Time.deltaTime, out float modified);
				}
			}
		}
	}
}
