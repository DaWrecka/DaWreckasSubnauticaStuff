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
using QModManager.Utility;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
#if BELOWZERO
	using TMPro;
#endif

namespace DWEquipmentBonanza.Patches
{
#if BELOWZERO
	[HarmonyPatch(typeof(SeaTruckMotor))]
	internal class SeaTruckMotorPatches
	{
		[HarmonyPatch(nameof(SeaTruckMotor.Start))]
		[HarmonyPostfix]
		public static void PostStart(ref SeaTruckMotor __instance)
		{
			if(__instance.gameObject != null && __instance.gameObject.TryGetComponent<LiveMixin>(out LiveMixin mixin))
			{
				float defaultHealth = mixin.defaultHealth;
				float instanceHealthPct = Mathf.Min(mixin.GetHealthFraction(), 1f);
				float maxHealth = defaultHealth;
				if (__instance.gameObject.GetComponent<SeaTruckUpgrades>() != null)
				{
					maxHealth *= Main.config.SeatruckVehicleHealthMult;
				}
				else
				{
					maxHealth *= Main.config.SeatruckModulesHealthMult;
				}

				mixin.data.maxHealth = maxHealth;
				mixin.health = maxHealth * instanceHealthPct;
			}
		}
	}

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

	[HarmonyPatch(typeof(SeaTruckUpgrades))]
	internal class SeaTruckUpgradesPatches
	{
		private static Dictionary<TechType, int> maxModulesOverrides = new Dictionary<TechType, int>();

		internal static void AddMaxModuleOverride(TechType tt, int newOverride)
		{
			if (maxModulesOverrides.ContainsKey(tt))
			{
				maxModulesOverrides[tt] = newOverride;
				return;
			}

			maxModulesOverrides.Add(tt, newOverride);
		}

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		public static void PostStart(SeaTruckUpgrades __instance)
		{
			SeaTruckUpdater component = __instance.gameObject.EnsureComponent<SeaTruckUpdater>();
			component.Initialise(ref __instance);
		}

		[HarmonyPatch("OnUpgradeModuleChange", new Type[] { typeof(int), typeof(TechType), typeof(bool) })]
		[HarmonyPostfix]
		public static void PostUpgradeModuleChange(SeaTruckUpgrades __instance, int slotID, TechType techType, bool added)
		{
			__instance.gameObject.EnsureComponent<SeaTruckUpdater>()?.PostUpgradeModuleChange(slotID, techType, added, __instance);
		}

		[HarmonyPatch("OnUpgradeModuleUse")]
		[HarmonyPostfix]
		public static void PostOnUpgradeModuleUse(SeaTruckUpgrades __instance, TechType techType, int slotID)
		{
			__instance.gameObject.EnsureComponent<SeaTruckUpdater>()?.PostUpgradeModuleUse(__instance, techType, slotID);
		}

		[HarmonyPatch("NotifySelectSlot")]
		[HarmonyPostfix]
		public static void PostNotifySelectSlot(SeaTruckUpgrades __instance, int slotID)
		{
			__instance.gameObject.EnsureComponent<SeaTruckUpdater>()?.PostNotifySelectSlot(__instance, slotID);
		}

		[HarmonyPatch("IsAllowedToAdd")]
		[HarmonyPrefix]
		public static bool PreIsAllowedToAdd(SeaTruckUpgrades __instance, Pickupable pickupable, bool verbose, ref bool __result)
		{
			TechType techType = pickupable.GetTechType();

			if (maxModulesOverrides.TryGetValue(techType, out int value))
			{
				if (__instance.modules.GetCount(techType) <= value)
				{
					__result = true;
					return false;
				}
			}

			return true;
		}

		[HarmonyPatch("OnPilotEnd")]
		[HarmonyPostfix]
		public static void PostOnPilotEnd(SeaTruckUpgrades __instance)
		{
			__instance.gameObject.EnsureComponent<SeaTruckUpdater>()?.PostOnPilotEnd();
		}

		[HarmonyPatch("IQuickSlots.IsToggled")]
		[HarmonyPrefix]
		public static bool PreSeatruckIsToggled(SeaTruckUpgrades __instance, ref bool __result, int slotID)
		{
			//__result = slotID >= 0 && slotID < SeaTruckUpgrades.slotIDs.Length && (TechData.GetSlotType(__instance.GetSlotBinding(slotID)) == QuickSlotType.Passive || this.quickSlotToggled[slotID]);
			__result = __result || __instance.gameObject.EnsureComponent<SeaTruckUpdater>().PreQuickSlotIsToggled(__instance, slotID);
			//Log.LogDebug($"PreSeatruckIsToggled: slotID = {slotID}, __result = {__result}");
			if(__result)
				return false;

			return true;
		}

		[HarmonyPatch("IQuickSlots.IsToggled")]
		[HarmonyPostfix]
		public static void PostSeatruckIsToggled(SeaTruckUpgrades __instance, ref bool __result, int slotID)
		{
			//__result = slotID >= 0 && slotID < SeaTruckUpgrades.slotIDs.Length && (TechData.GetSlotType(__instance.GetSlotBinding(slotID)) == QuickSlotType.Passive || this.quickSlotToggled[slotID]);
			//Log.LogDebug($"PostSeatruckIsToggled: slotID = {slotID}, __result = {__result}");
		}
	}
#endif
}