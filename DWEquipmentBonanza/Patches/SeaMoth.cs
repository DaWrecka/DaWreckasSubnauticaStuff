using Common;
using DWEquipmentBonanza.MonoBehaviours;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Main = DWEquipmentBonanza.DWEBPlugin;

namespace DWEquipmentBonanza.Patches
{
#if SN1
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
#if LEGACY
				mixin.initialHealth = defaultHealth;
#else
				mixin.defaultHealth = defaultHealth;
#endif
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

		[HarmonyPatch(nameof(SeaMoth.OnPilotModeEnd))]
		[HarmonyPostfix]
		public static void PostOnPilotEnd(SeaMoth __instance)
		{
			__instance.gameObject.EnsureComponent<SeamothUpdater>()?.PostOnPilotEnd();
		}
	}
#endif
}
