using DWEquipmentBonanza.VehicleModules;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if BELOWZERO
    internal class SeaTruckUpdater : MonoBehaviour
    {
		private const float SonarCooldown = 5f;
		internal const float SonarDisableThreshold = 100f;
		protected static TechType solarModuleTechType => Main.GetModTechType("SeaTruckSolarModule");
		protected static TechType thermalModuleTechType => Main.GetModTechType("SeaTruckThermalModule");
		protected static TechType sonarModuleTechType => Main.GetModTechType("SeaTruckSonarModule");
		public static AnimationCurve thermalReactorCharge;
		protected static int solarCount;
		protected static int thermalCount;
		protected SeaTruckUpgrades parentVehicle;
		protected FMOD_CustomEmitter sonarSound;
		protected bool bSonarActive = false;
		protected int sonarSlotID = -1;

		protected void Start()
		{
			string RCFilename;
			if (thermalReactorCharge is null && PrefabDatabase.TryGetPrefabFilename(CraftData.GetClassIdForTechType(TechType.Exosuit), out RCFilename))
			{
				AddressablesUtility.LoadAsync<GameObject>(RCFilename).Completed += (x) =>
				{
					GameObject gameObject1 = x.Result;
					Exosuit exosuit = gameObject1?.GetComponent<Exosuit>();
					thermalReactorCharge = exosuit?.thermalReactorCharge;
				};
			}

			if (sonarSound is null && PrefabDatabase.TryGetPrefabFilename(CraftData.GetClassIdForTechType(TechType.Seamoth), out RCFilename))
			{
				AddressablesUtility.LoadAsync<GameObject>(RCFilename).Completed += (x) =>
				{
					GameObject gameObject1 = x.Result;
					SeaMoth seamoth = gameObject1?.GetComponent<SeaMoth>();
					if (seamoth?.sonarSound != null)
					{
						sonarSound = this.gameObject.AddComponent<FMOD_CustomEmitter>();
						sonarSound.SetAsset(seamoth.sonarSound.asset);
						sonarSound.UpdateEventAttributes();
					}
				};
			}
		}

		internal void Initialise(ref SeaTruckUpgrades seaTruckUpgrades)
		{
			parentVehicle = seaTruckUpgrades;
			parentVehicle.onToggle += OnToggle;
			parentVehicle.onSelect += OnSelect;
		}

        private void Modules_onEquip(string slot, InventoryItem item)
        {
            
        }

        protected void OnDestroy()
		{
			parentVehicle.onToggle -= OnToggle;
			parentVehicle.onSelect -= OnSelect;
		}

		private void OnToggle(int slotID, bool state)
		{
			if (parentVehicle == null)
				return;

			if (slotID == sonarSlotID)
			{
				bSonarActive = state;

				if(state)
				{
					base.InvokeRepeating("UpdateSonar", 0f, SonarCooldown);
				}
				else
				{
					base.CancelInvoke("UpdateSonar");
				}
			}
		}

		private void OnSelect(int slotID)
		{
			if (parentVehicle == null)
				return;

			if (slotID > SeaTruckUpgrades.slotIDs.Length)
			{
				Log.LogDebug("SeatruckUpdate.OnSelect() invoked with slotID outside the bounds of the slotIDs array");
				return;
			}

			if (parentVehicle.modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[slotID]) == sonarModuleTechType)
			{
				sonarSlotID = slotID;
				bSonarActive = !bSonarActive;
				if (bSonarActive)
				{
					base.InvokeRepeating("UpdateSonar", 0f, SonarCooldown);
				}
				else
				{
					base.CancelInvoke("UpdateSonar");
				}
			}
		}

		internal virtual void PostNotifySelectSlot(SeaTruckUpgrades instance, int slotID)
		{
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

			if (techType == sonarModuleTechType)
			{
				if (added)
					sonarSlotID = slotID;
				else
					sonarSlotID = -1;
			}
			base.CancelInvoke("UpdateRecharge");
			base.CancelInvoke("UpdateSonar");

			solarCount = parentVehicle.modules.GetCount(solarModuleTechType);
			thermalCount = parentVehicle.modules.GetCount(thermalModuleTechType);
			if (solarCount + thermalCount > 0)
				base.InvokeRepeating("UpdateRecharge", 1f, 1f);
		}

		internal virtual void PostUpgradeModuleUse(SeaTruckUpgrades instance, TechType tt, int slotID)
		{
			if (tt == sonarModuleTechType)
			{
				bSonarActive = !bSonarActive;
				sonarSlotID = slotID;
				uGUI_QuickSlots qs = uGUI.main.quickSlots;
				qs.SetBackground(qs.icons[slotID], tt, bSonarActive);
				if (bSonarActive)
				{
					base.InvokeRepeating("UpdateSonar", 0f, SonarCooldown);
				}
				else
				{
					base.CancelInvoke("UpdateSonar");
				}
			}
		}

		internal virtual bool PreQuickSlotIsToggled(SeaTruckUpgrades instance, int slotID)
		{
			//Log.LogDebug($"SeaTruckUpdater.PreQuickSlotIsToggled(): slotID = {slotID}, sonarSlotID = {sonarSlotID}, bSonarActive = {bSonarActive}");
			return (slotID == sonarSlotID && bSonarActive);
		}

		private void UpdateSonar()
		{
			if (!Player.main.inSeatruckPilotingChair)
			{
				CancelSonar();
			}

			if (!bSonarActive || sonarSlotID == -1)
				return;

			if (parentVehicle == null || parentVehicle.relay == null)
			{
				base.CancelInvoke("UpdateSonar");
				return;
			}

			if (parentVehicle.relay.GetPower() >= SonarDisableThreshold)
			{
				parentVehicle.relay.ConsumeEnergy(SeaTruckSonarModule.EnergyCost, out float consumed);
				this.sonarSound.Stop();
				this.sonarSound.Play();
				SNCameraRoot.main.SonarPing();
				//parentVehicle.quickSlotTimeUsed[sonarSlotID] = Time.time;
				//parentVehicle.quickSlotCooldown[sonarSlotID] = SonarCooldown;
			}
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

		private void CancelSonar()
		{
			bSonarActive = false;
			base.CancelInvoke("UpdateSonar");
		}

		public void PostOnPilotEnd()
		{
			CancelSonar();
		}
	}
#endif
}
