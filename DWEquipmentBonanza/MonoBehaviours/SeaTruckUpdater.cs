using DWEquipmentBonanza.VehicleModules;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if BELOWZERO
	public class SeaTruckUpdater : VehicleUpdater, ISerializationCallbackReceiver
	{
		protected SeaTruckMotor parentMotor => (parentVehicle is SeaTruckUpgrades stg ? stg.motor : null);
		protected FMOD_CustomEmitter sonarSound;
		protected bool bSonarActive = false;
		public int sonarSlotID { get; protected set; } = -1;

		private const float SonarCooldown = 5f;
		internal const float SonarDisableThreshold = 100f;
		protected static TechType sonarModuleTechType => Main.GetModTechType("SeaTruckSonarModule");
		protected static TechType quantumModuleType => Main.GetModTechType("SeaTruckQuantumLocker");
		public int QuantumModuleSlot { get; protected set; } = -1;
		//protected static int solarCount;
		//protected static int thermalCount;

		//private static float fPassiveEnergyConsumptionPerSecond = 2f;
		//private static float fPassiveHealthRegenerationPerSecond = 2f;
		//private static float fActiveModeEnergyMultiplier = 6f; // Energy consumption is multiplied by this amount in active mode
		//private static float fActiveModeHealthRegenMultiplier = 5f; // Health regeneration is multiplied by this amount in active mode

		protected override void Start()
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
			string RCFilename;

			base.Start();

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
					foreach (var r in gameObject1.GetComponentsInChildren<Renderer>())
					{
					// This is where I'd add textures to the Seamoth IF I HAD ANY
					}
				};
			}

			foreach (var charger in activeChargers)
				charger.Init(parentVehicle);
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
		}

		public override void Initialise(ref MonoBehaviour instance)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//MethodBase callingMethod = new StackFrame(1).GetMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}({instance.name}, {instance.GetInstanceID()}).{thisMethod.Name}() executing, invoked by: '{callingMethod.ReflectedType.Name}.{callingMethod.Name}'");

			SeaTruckUpgrades stg = null;
			if (instance is SeaTruckMotor stm)
				stg = stm.upgrades;
			else if (instance is SeaTruckUpgrades)
				stg = instance as SeaTruckUpgrades;

			if (stg != null)
                InitInternal(ref stg);
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}({instance.name}, {instance.GetInstanceID()}).{thisMethod.Name}() end");
		}

		private void InitInternal(ref SeaTruckUpgrades instance)
		{
			if (bInitialised)
			{
				//Log.LogDebug($"Initialise called multiple times for SeaTruck! Vehicle name {instance.name}, vehicle ID {instance.GetInstanceID()})");
			}
			else
			{
				//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
				//MethodBase callingMethod = new StackFrame(1).GetMethod();
				//Log.LogDebug($"{thisMethod.ReflectedType.Name}({instance.name}, {instance.GetInstanceID()}).{thisMethod.Name}() executing, invoked by: '{callingMethod.ReflectedType.Name}.{callingMethod.Name}'");

				parentVehicle = instance;
				instance.onToggle += OnToggle;
				instance.onSelect += OnSelect;
				instance.modules.onEquip += OnEquipModule;
				instance.modules.onUnequip += OnUnequipModule;
				instance.modules.isAllowedToAdd += IsAllowedToAdd;

				//Log.LogDebug($"{thisMethod.ReflectedType.Name}({instance.name}, {instance.GetInstanceID()}).{thisMethod.Name}() end");
				bInitialised = true;
			}
		}

		protected override void OnEquipModule(string slot, InventoryItem item)
		{
			//MethodBase thisMethod = MethodBase.GetCurrentMethod();
			//MethodBase callingMethod = new StackFrame(1).GetMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}({this.GetInstanceID()}).{thisMethod.Name}({slot}, item TechType: {item.item.GetTechType().AsString()}, item ID {item.item.GetInstanceID()}) executing, invoked by: '{callingMethod.ReflectedType.Name}.{callingMethod.Name}'");

			Pickupable pickup = item.item;
			TechType moduleType = pickup.GetTechType();
			//ErrorMessage.AddMessage($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}(): item TechType {moduleType.AsString()}");
			if (moduleType == quantumModuleType)
			{
				VehicleQuantumLockerComponent componentQL = pickup.gameObject.GetComponent<VehicleQuantumLockerComponent>();
				if (componentQL != null)
					componentQL.ToggleQuantumStorage(false);
			}
			else if (ChargerWeights.TryGetValue(moduleType, out float weight))
			{
				//ErrorMessage.AddMessage($"Attempting to find VehicleCharger component");
				VehicleCharger component = pickup.gameObject.GetComponent<VehicleCharger>();
				if (component != null && activeChargers.Add(component))
				{
					// HashSet.Add() will only return true if the component isn't already in the set, so we know this code will only run if this VehicleCharger is being newly-added.
					if (activeChargers.Count == 1) // This will only invoke if this is the first charger added; this prevents the timer from running multiple times consecutively
						base.InvokeRepeating("UpdateRecharge", InvokeInterval, InvokeInterval);
					chargerWeightCumulative += weight;
					//ErrorMessage.AddMessage($"Adding {moduleType.AsString()} to active chargers list");
					component.Init(parentVehicle);
				}
			}
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
		}

		protected override void OnUnequipModule(string slot, InventoryItem item)
		{
			string memberName = new StackFrame(1)?.GetMethod().Name;
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}({this.GetInstanceID()}).{thisMethod.Name}({slot}, item TechType: {item.item.GetTechType().AsString()}, item ID {item.item.GetInstanceID()}) base executing, invoked by: '{memberName}'");
			Pickupable pickup = item.item;

			TechType moduleType = pickup.GetTechType();
			//ErrorMessage.AddMessage($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slot}, {item.item.name}): item TechType {moduleType.AsString()}");
			if (moduleType == quantumModuleType)
			{
				VehicleQuantumLockerComponent componentQL = pickup.gameObject.GetComponent<VehicleQuantumLockerComponent>();
				if (componentQL != null)
					componentQL.ToggleQuantumStorage(false);
			}
			else if (ChargerWeights.TryGetValue(moduleType, out float weight))
			{
				chargerWeightCumulative -= weight;
				//ErrorMessage.AddMessage($"Attempting to find VehicleCharger component");
				VehicleCharger component = pickup.gameObject.GetComponent<VehicleCharger>();
				if (component != null && activeChargers.Contains(component))
				{
					//ErrorMessage.AddMessage($"Removing {moduleType.AsString()} from active chargers list");
					activeChargers.Remove(component);
				}
				if (activeChargers.Count < 1)
					base.CancelInvoke("UpdateRecharge");
			}
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slot}, {item.item.name}) end");
		}
		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			CoroutineHost.StartCoroutine(OnAfterDeserializeCoroutine());
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{

		}
		protected override IEnumerator OnAfterDeserializeCoroutine()
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): waiting for Vehicle");

			SeaTruckUpgrades v = parentVehicle as SeaTruckUpgrades;
			while (v == null)
			{
				yield return new WaitForSecondsRealtime(0.1f);
				v = parentVehicle as SeaTruckUpgrades;
			}

			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): waiting for Equipment");
			ICollection<string> slots = v.modules?.equipment?.Keys;
			while (slots == null || slots.Count < 1)
			{
				yield return new WaitForEndOfFrame();
				slots = v.modules?.equipment?.Keys;
			}

			foreach (string s in slots)
			{
				InventoryItem i = v.modules.GetItemInSlot(s);
				if (i?.item != null && ChargerWeights.ContainsKey(i.item.GetTechType()))
				{
					//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): re-equipping item in slot '{s}', TechType {i.item.GetTechType()}");
					OnEquipModule(s, i);
				}

				yield return new WaitForEndOfFrame();
			}

			yield break;
		}

		protected override void OnToggle(int slotID, bool state)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}, {state}) executing");

			if (parentVehicle == null)
				return;

			if (slotID == sonarSlotID)
			{
				bSonarActive = state;

				if (state)
				{
					base.InvokeRepeating("UpdateSonar", 0f, SonarCooldown);
				}
				else
				{
					base.CancelInvoke("UpdateSonar");
				}
			}
			else if (slotID == repairSlotID)
			{
				gameObject.EnsureComponent<VehicleRepairComponent>().SetActiveState(state, parentMotor);
			}

			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}, {state}) end");
		}

		protected override void OnSelect(int slotID) { }
		internal override void PostNotifySelectSlot(MonoBehaviour instance, int slotID)
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin; slotID = {slotID}, SeaTruckUpgrades.slotIDs.Length = {SeaTruckUpgrades.slotIDs.Length}");

			if (parentVehicle == null)
				return;

			if (slotID < 0 || slotID >= SeaTruckUpgrades.slotIDs.Length)
			{
				Log.LogDebug("SeatruckUpdater.PostNotifySelectSlot() invoked with slotID outside the bounds of the slotIDs array");
				return;
			}

			Equipment modules = (parentVehicle as SeaTruckUpgrades).modules;
			TechType equippedTech = modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[slotID]);
			if (equippedTech == sonarModuleTechType)
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
			else if (repairModuleTechTypes.Contains(equippedTech))
			{
				ErrorMessage.AddMessage("Repair module state: " + (gameObject.EnsureComponent<SeaTruckRepairComponent>().ToggleActiveState(parentMotor) ? "active" : "passive"));
			}
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
		}

		internal override void PostUpgradeModuleChange(int slotID, TechType techType, bool added, MonoBehaviour instance)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}(TechType: {techType.AsString()}) begin; repairModuleTechTypes.Contains() returns: {repairModuleTechTypes.Contains(techType)}");
			if (parentVehicle == null && instance != null)
			{
				parentVehicle = instance;
			}

			if (parentVehicle == null)
				return;

			if (techType == quantumModuleType)
			{
				QuantumModuleSlot = (added ? slotID : -1);
				string slot = SeaTruckUpgrades.slotIDs[slotID];
				VehicleQuantumLockerComponent vQL = null;
				try
				{
					InventoryItem item = (parentVehicle as SeaTruckUpgrades)?.modules.GetItemInSlot(slot);
					vQL = item.item.gameObject.GetComponent<VehicleQuantumLockerComponent>();
				}
				catch (NullReferenceException nre)
				{
					Log.LogError(nre.ToString());
				}
				if (vQL != null)
				{
					vQL.ToggleQuantumStorage(added);
				}
			}
			else if (techType == sonarModuleTechType)
			{
				sonarSlotID = (added ? slotID : -1);
			}
			else if (repairModuleTechTypes.Contains(techType))
			{
				//Log.LogDebug($"Found repair module: adding Repair Component and " + (added ? "enabling" : "disabling") + " it");
				repairSlotID = (added ? slotID : -1);

				SeaTruckRepairComponent repairComponent = gameObject.EnsureComponent<SeaTruckRepairComponent>();
				repairComponent.SetEnabled(added, parentMotor);
				repairComponent.SetActiveState(false);
			}
			else if (added && ChargerWeights.ContainsKey(techType))
			{
				// We're doing this here because unlike with the regular On(Un)EquipModule methods, this is invoked after a game load.
				SeaTruckUpgrades stg = gameObject.GetComponent<SeaTruckUpgrades>();
				string slot = SeaTruckUpgrades.slotIDs[slotID];
				InventoryItem item = stg.modules.GetItemInSlot(slot);

				OnEquipModule(slot, item);
			}
			base.CancelInvoke("UpdateSonar");
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
		}

		internal override void PostUpgradeModuleUse(MonoBehaviour instance, TechType tt, int slotID)
		{
			base.PostUpgradeModuleUse(instance, tt, slotID);
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
			else if (tt == quantumModuleType)
			{
				var playerTransform = Player.main.gameObject?.transform;
				var QuantumStorage = QuantumLockerStorage.GetStorageContainer(false);
				if (playerTransform != null && QuantumStorage != null)
					QuantumStorage.Open(playerTransform);
				else
					Log.LogError("Couldn't open Seatruck Quantum Storage" + (playerTransform == null ? "; playerTransform is null" : "") + (QuantumStorage == null ? "; QuantumStorage is null" : ""), null, true);
			}
		}

		internal override bool PreQuickSlotIsToggled(MonoBehaviour instance, int slotID)
		{
			return (slotID == sonarSlotID && bSonarActive) || (slotID == repairSlotID && gameObject.EnsureComponent<SeaTruckRepairComponent>().GetIsActive());
		}

		private void UpdateSonar()
		{
			if (!Player.main.inSeatruckPilotingChair)
			{
				CancelSonar();
			}

			if (!bSonarActive || sonarSlotID == -1)
				return;
			PowerRelay relay = (parentVehicle as SeaTruckUpgrades)?.relay;
			if (relay == null)
			{
				base.CancelInvoke("UpdateSonar");
				return;
			}

			if (relay.GetPower() >= SonarDisableThreshold)
			{
				relay.ConsumeEnergy(SeaTruckSonarModule.EnergyCost, out float consumed);
				this.sonarSound.Stop();
				this.sonarSound.Play();
				SNCameraRoot.main.SonarPing();
				//parentVehicle.quickSlotTimeUsed[sonarSlotID] = Time.time;
				//parentVehicle.quickSlotCooldown[sonarSlotID] = SonarCooldown;
			}
		}

		private void CancelSonar()
		{
			bSonarActive = false;
			base.CancelInvoke("UpdateSonar");
		}

		public override void PostOnPilotEnd()
		{
			CancelSonar();
		}

		protected override void OnDestroy()
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
			if (parentVehicle is SeaTruckUpgrades V)
			{
				V.onToggle -= OnToggle;
				V.onSelect -= OnSelect;
				V.modules.onEquip -= OnEquipModule;
				V.modules.onUnequip -= OnUnequipModule;
				V.modules.isAllowedToAdd -= IsAllowedToAdd;
			}
			parentVehicle = null;

			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
		}

		public override int GetModuleCount(TechType techType)
		{
			if (parentVehicle is SeaTruckUpgrades stg)
				return stg.modules.GetCount(techType);

			return 0;
		}
	}
#endif
}