using Main = DWEquipmentBonanza.DWEBPlugin;
using DWEquipmentBonanza;
using DWEquipmentBonanza.VehicleModules;
using DWEquipmentBonanza.MonoBehaviours;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
#if QMM
	using QModManager.Utility;
#endif
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
#if !LEGACY
	using TMPro;
#endif

namespace DWEquipmentBonanza.Patches
{
#if BELOWZERO
	[HarmonyPatch(typeof(uGUI_SeaTruckHUD))]
	internal class SeaTruckHUDPatches
	{
		//private static readonly FieldInfo seaTruckMotorField = typeof(uGUI_SeaTruckHUD).GetField("seaTruckMotor", BindingFlags.NonPublic | BindingFlags.Instance);
		[HarmonyPostfix]
		[HarmonyPatch("Update")]
		public static void PostUpdate(uGUI_SeaTruckHUD __instance)
		{
			if (__instance == null)
				return;

			if (!Main.config.bHUDAbsoluteValues)
				return;

			if (Player.main == null)
				return;

			if (!Player.main.inSeatruckPilotingChair)
				return;

			SeaTruckMotor motor = Player.main.GetComponentInParent<SeaTruckMotor>();
			if (motor != null)
			{
				PowerRelay relay = motor.relay;
				if (relay != null)
				{
					float power = Mathf.Floor(relay.GetPower());
					float truckhealth = Mathf.Floor(motor.liveMixin.health);
					__instance.textHealth.text = truckhealth.ToString();
					//__instance.textHealth.fontSize = (truckhealth > 9999 ? 20 : 36);
					__instance.textPower.text = power.ToString();
					__instance.textPower.fontSize = (power > 9999 ? 28 : 36);
				}
			}
		}
	}

	[HarmonyPatch(typeof(SeaTruckMotor))]
	internal class SeaTruckMotorPatches
	{
		[HarmonyPatch(nameof(SeaTruckMotor.Start))]
		[HarmonyPostfix]
		public static void PostStart(ref SeaTruckMotor __instance)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
			if (__instance?.gameObject != null)
			{
				if(__instance.gameObject.TryGetComponent<LiveMixin>(out LiveMixin mixin) && Main.defaultHealth.TryGetValue(TechType.SeaTruck, out float defaultHealth))
				{
					float instanceHealthPct = Mathf.Min(mixin.GetHealthFraction(), 1f);
					float maxHealth = defaultHealth * Main.config.SeatruckVehicleHealthMult;

					mixin.data.maxHealth = maxHealth;
					mixin.health = maxHealth * instanceHealthPct;
				}
				__instance.gameObject.EnsureComponent<SeaTruckRepairComponent>().Initialise(__instance.gameObject);
			}

			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
		}
	}

	[HarmonyPatch(typeof(SeaTruckUpgrades))]
	internal class SeaTruckUpgradesPatches
	{
		[HarmonyPostfix]
		[HarmonyPatch("Start")]
		public static void PostStart(SeaTruckUpgrades __instance)
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
			if (__instance?.gameObject != null)
			{
				MonoBehaviour m = __instance as MonoBehaviour;
				SeaTruckUpdater component = __instance.gameObject.EnsureComponent<SeaTruckUpdater>();
				//component.Initialise(ref m);  // This is now handled in VehicleUpdater.Start()
			}

			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
		}

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SeaTruckUpgrades.LazyInitialize))]
        public static void PostInitialize(SeaTruckUpgrades __instance)
        {
            System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
            if (__instance?.gameObject != null)
            {
                MonoBehaviour m = __instance as MonoBehaviour;
                SeaTruckUpdater component = __instance.gameObject.EnsureComponent<SeaTruckUpdater>();
                //component.Initialise(ref m);  // This is now handled in VehicleUpdater.Start()
            }

            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
        }
        //[HarmonyPrefix]
        //[HarmonyPatch("OnUpgradeModuleChange", new Type[] { typeof(int), typeof(TechType), typeof(bool) })]
        //public static void PreUpgradeModuleChange(SeaTruckUpgrades __instance, int slotID, TechType techType, bool added)
        //{
        //	System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
        //	Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}, {techType.AsString()}) executing");

        //	Log.LogDebug($"SeaTruckUpgrades.slotIDs: length = {SeaTruckUpgrades.slotIDs.Length}");
        //	for (int i = 0; i < SeaTruckUpgrades.slotIDs.Length; i++)
        //	{
        //		Log.LogDebug($"SeaTruckUpgrades.slotIDs[{i}] = {SeaTruckUpgrades.slotIDs[i]}");
        //		try
        //		{
        //			TechType techTypeInSlot = __instance.modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[i]);
        //			Log.LogDebug($"    techTypeInSlot = {techTypeInSlot.AsString()}");
        //		}
        //		catch(Exception ex)
        //		{
        //			Log.LogDebug($"    Exception calling GetTechTypeInSlot:");
        //			Log.LogDebug(ex.ToString());
        //		}
        //	}
        //	//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}, {techType.AsString()}) done");
        //}

        [HarmonyPostfix]
		[HarmonyPatch("OnUpgradeModuleChange", new Type[] { typeof(int), typeof(TechType), typeof(bool) })]
		public static void PostUpgradeModuleChange(SeaTruckUpgrades __instance, int slotID, TechType techType, bool added)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}, {techType.AsString()}) executing");
			__instance.gameObject.EnsureComponent<SeaTruckUpdater>()?.PostUpgradeModuleChange(slotID, techType, added, __instance);
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}, {techType.AsString()}) done");
		}

		[HarmonyPostfix]
		[HarmonyPatch("OnUpgradeModuleUse")]
		public static void PostOnUpgradeModuleUse(SeaTruckUpgrades __instance, TechType techType, int slotID)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}, {techType.AsString()}) executing");

			__instance.gameObject.EnsureComponent<SeaTruckUpdater>()?.PostUpgradeModuleUse(__instance, techType, slotID);
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}, {techType.AsString()}) end");
		}

		[HarmonyPostfix]
		[HarmonyPatch("NotifySelectSlot")]
		public static void PostNotifySelectSlot(SeaTruckUpgrades __instance, int slotID)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}) executing");
			__instance.gameObject.EnsureComponent<SeaTruckUpdater>()?.PostNotifySelectSlot(__instance, slotID);
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slotID}) end");
		}


		// Unlike with SN1, we explicitly need to override this - otherwise, only one of a given charger can be installed at a time.
		// We *must* override it to get the behaviour we want.
		[HarmonyPatch("IsAllowedToAdd")]
		[HarmonyPrefix]
		public static bool PreIsAllowedToAdd(SeaTruckUpgrades __instance, Pickupable pickupable, bool verbose, ref bool __result)
		{
			TechType techType = pickupable.GetTechType();
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({techType}) executing");

			SeaTruckUpdater stu = __instance.gameObject.GetComponent<SeaTruckUpdater>();
			if(stu != null)
			{
				bool bAllowed = stu.AllowedToAdd(techType, out bool bOverride, out string message);
				if (bOverride)
				{
					if (!string.IsNullOrEmpty(message))
						ErrorMessage.AddMessage(message);
					__result = bAllowed;
					return false;
				}
			}

			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({techType}) end");
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch("OnPilotEnd")]
		public static void PostOnPilotEnd(SeaTruckUpgrades __instance)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
			if (__instance?.gameObject != null)
			{
				__instance.gameObject.EnsureComponent<SeaTruckUpdater>().PostOnPilotEnd();
			}

			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() end");
		}

		[HarmonyPrefix]
		[HarmonyPatch("IQuickSlots.IsToggled")]
		public static bool PreSeatruckIsToggled(SeaTruckUpgrades __instance, ref bool __result, int slotID)
		{
			//System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			//Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
			//__result = slotID >= 0 && slotID < SeaTruckUpgrades.slotIDs.Length && (TechData.GetSlotType(__instance.GetSlotBinding(slotID)) == QuickSlotType.Passive || this.quickSlotToggled[slotID]);
			if (__instance.gameObject.EnsureComponent<SeaTruckUpdater>().PreQuickSlotIsToggled(__instance, ref __result, slotID))
			{
				return false;
			}

			return true;
		}
	}
#endif
}