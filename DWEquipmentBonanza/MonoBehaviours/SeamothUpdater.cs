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
using System.Reflection;
using System.Diagnostics;
using Main = DWEquipmentBonanza.DWEBPlugin;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if SN1
	public class SeamothUpdater : VehicleUpdater
	{
		protected static TechType repairModuleTechType => Main.GetModTechType("VehicleRepairModule");

		protected override void Start()
		{
			base.Start();
			if (activeChargers.Count > 0)
			{
				foreach (var charger in activeChargers)
					charger.Init(parentVehicle);
			}

		}

		public override void Initialise(ref MonoBehaviour vehicle)
		{
			base.Initialise(ref vehicle);
			if (vehicle is SeaMoth V)
			{
				parentVehicle = vehicle;
				V.onToggle -= OnToggle;
				V.onSelect -= OnSelect;
				V.modules.onEquip -= OnEquipModule;
				V.modules.onUnequip -= OnUnequipModule;
				V.modules.isAllowedToAdd -= IsAllowedToAdd;

				V.onToggle += OnToggle;
				V.onSelect += OnSelect;
				V.modules.onEquip += OnEquipModule;
				V.modules.onUnequip += OnUnequipModule;
				V.modules.isAllowedToAdd += IsAllowedToAdd;
			}
		}

		protected override void OnToggle(int slotID, bool state)
		{
			if (parentVehicle == null)
				return;

			if (slotID == repairSlotID)
			{
				gameObject.EnsureComponent<VehicleRepairComponent>().SetActiveState(state, parentVehicle);
				ErrorMessage.AddMessage("Repair module state: " + (state ? "active" : "passive"));
			}
		}

		/*protected override void OnSelect(int slotID)
		{
			MethodBase thisMethod = MethodBase.GetCurrentMethod();
			//MethodBase callingMethod = new StackFrame(1).GetMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}({this.GetInstanceID()}).{thisMethod.Name}({slotID}) begin");
			Vehicle V = parentVehicle as Vehicle;
			if (V == null)
				return;

			string[] slots = V.slotIDs;

			if (slotID >= slots.Length)
			{
				Log.LogDebug("SeamothUpdater.OnSelect() invoked with slotID outside the bounds of the slotIDs array");
				return;
			}

			Equipment modules = V.modules;
			TechType equippedTech = modules.GetTechTypeInSlot(slots[slotID]);
			if (equippedTech == repairModuleTechType)
			{
				ErrorMessage.AddMessage("Repair module state: " + (gameObject.EnsureComponent<VehicleRepairComponent>().ToggleActiveState(parentVehicle) ? "active" : "passive"));
			}
			Log.LogDebug($"{thisMethod.ReflectedType.Name}({this.GetInstanceID()}).{thisMethod.Name}({slotID}) end");
		}*/

		internal override void PostNotifySelectSlot(MonoBehaviour instance, int slotID) { }

		internal override void PreUpdate(MonoBehaviour instance = null) { }

		// Not to be confused with Unity's Update
		internal override void OnUpdate(MonoBehaviour instance = null) { }

		internal override void PostUpdate(MonoBehaviour instance = null) { }

		internal override void PreUpdateEnergy(MonoBehaviour instance = null) { }

		internal override void PostUpdateEnergy(MonoBehaviour instance = null) { }

		internal override void PostUpgradeModuleChange(int slotID, TechType techType, bool added, MonoBehaviour instance)
		{
			if (parentVehicle == null && instance != null)
			{
				parentVehicle = instance;
			}

			if (parentVehicle == null)
				return;

			base.PostUpgradeModuleChange(slotID, techType, added, instance);
			if (techType == repairModuleTechType)
			{
				repairSlotID = (added ? slotID : -1);

				VehicleRepairComponent repairComponent = gameObject.EnsureComponent<VehicleRepairComponent>();
				repairComponent.SetEnabled(added, parentVehicle);
				repairComponent.SetActiveState(false);
			}
		}

		internal override void PostUpgradeModuleUse(MonoBehaviour instance, TechType tt, int slotID)
		{
			if (tt == repairModuleTechType)
			{
				gameObject.EnsureComponent<VehicleRepairComponent>().SetActiveState(parentVehicle);
			}
		}

		internal override bool PreQuickSlotIsToggled(MonoBehaviour instance, ref bool __result, int slotID)
		{
			if (slotID == repairSlotID)
				return gameObject.EnsureComponent<VehicleRepairComponent>().GetIsActive();

			return false;
		}

		public override int GetModuleCount(TechType techType)
		{
			if (parentVehicle is Vehicle V)
				return V.modules.GetCount(techType);

			return 0;
		}
	}
#endif
}
