using Common;
using DWEquipmentBonanza.MonoBehaviours;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.Patches
{
#if SUBNAUTICA_STABLE
	[HarmonyPatch(typeof(SeaMoth))]
	internal class SeaMothPatches
    {
		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		public static void PostStart(SeaMoth __instance)
		{
			Log.LogDebug("SeaMothPatches.PostStart() begin");
			if (__instance.gameObject != null && __instance.gameObject.TryGetComponent<LiveMixin>(out LiveMixin mixin) && Main.defaultHealth.TryGetValue(TechType.Seamoth, out float defaultHealth))
			{
				float instanceHealthPct = Mathf.Min(mixin.GetHealthFraction(), 1f);
				float maxHealth = defaultHealth * Main.config.SeaMothHealthMult;

				mixin.data.maxHealth = maxHealth;
				mixin.health = maxHealth * instanceHealthPct;
				mixin.initialHealth = defaultHealth;
			}

			__instance.gameObject.EnsureComponent<VehicleRepairComponent>();
			MonoBehaviour m = __instance as MonoBehaviour;
			SeamothUpdater component = __instance.gameObject.EnsureComponent<SeamothUpdater>();
			component.Initialise(ref m);
			Log.LogDebug("SeaMothPatches.PostStart() end");
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(SeaMoth.OnUpgradeModuleChange), new Type[] { typeof(int), typeof(TechType), typeof(bool) })]
		public static void PostUpgradeChange(ref Exosuit __instance, int slotID, TechType techType, bool added)
		{
			Log.LogDebug($"SeamothPatches.OnUpgradeModuleChange(): slotID = {slotID}, techType = {techType.AsString()}, added = {added}");
			__instance.gameObject.EnsureComponent<SeamothUpdater>()?.PostUpgradeModuleChange(slotID, techType, added, __instance);
		}

		[HarmonyPatch(nameof(SeaMoth.OnUpgradeModuleUse))]
		[HarmonyPostfix]
		public static void PostOnUpgradeModuleUse(SeaMoth __instance, TechType techType, int slotID)
		{
			__instance.gameObject.EnsureComponent<SeamothUpdater>()?.PostUpgradeModuleUse(__instance, techType, slotID);
		}

		// SeaMoth doesn't have a NotifySelectSlot, but that's okay, we can manage
		/*
		[HarmonyPatch(nameof(SeaMoth.NotifySelectSlot))]
		[HarmonyPostfix]
		public static void PostNotifySelectSlot(SeaMoth __instance, int slotID)
		{
			__instance.gameObject.EnsureComponent<SeamothUpdater>()?.PostNotifySelectSlot(__instance, slotID);
		}
		*/

		/*[HarmonyPatch(nameof(SeaMoth.IsAllowedToAdd))]
		[HarmonyPrefix]
		public static bool PreIsAllowedToAdd(SeaMoth __instance, Pickupable pickupable, bool verbose, ref bool __result)
		{
			TechType techType = pickupable.GetTechType();

			SeamothUpdater stu = __instance.gameObject.GetComponent<SeamothUpdater>();
			if (stu != null)
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

			return true;
		}*/

		[HarmonyPatch(nameof(SeaMoth.OnPilotModeEnd))]
		[HarmonyPostfix]
		public static void PostOnPilotEnd(SeaMoth __instance)
		{
			__instance.gameObject.EnsureComponent<SeamothUpdater>()?.PostOnPilotEnd();
		}

		/*[HarmonyPatch("IQuickSlots.IsToggled")]
		[HarmonyPrefix]
		public static bool PreSeaMothIsToggled(SeaMoth __instance, ref bool __result, int slotID)
		{
			//__result = slotID >= 0 && slotID < SeaTruckUpgrades.slotIDs.Length && (TechData.GetSlotType(__instance.GetSlotBinding(slotID)) == QuickSlotType.Passive || this.quickSlotToggled[slotID]);
			__result = __result || __instance.gameObject.EnsureComponent<SeamothUpdater>().PreQuickSlotIsToggled(__instance, slotID);
			//Log.LogDebug($"PreSeatruckIsToggled: slotID = {slotID}, __result = {__result}");
			if (__result)
				return false;

			return true;
		}

		[HarmonyPatch("IQuickSlots.IsToggled")]
		[HarmonyPostfix]
		public static void PostSeaMothIsToggled(SeaMoth __instance, ref bool __result, int slotID)
		{
			//__result = slotID >= 0 && slotID < SeaTruckUpgrades.slotIDs.Length && (TechData.GetSlotType(__instance.GetSlotBinding(slotID)) == QuickSlotType.Passive || this.quickSlotToggled[slotID]);
			//Log.LogDebug($"PostSeatruckIsToggled: slotID = {slotID}, __result = {__result}");
		}*/
	}
#endif
}
