using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using DWEquipmentBonanza.MonoBehaviours;
using HarmonyLib;
using UnityEngine;

namespace DWEquipmentBonanza.Patches
{
	// SN1 already has a mod for this, so the component is only enabled in BZ.
#if BELOWZERO
	[HarmonyPatch(typeof(PropulsionCannon))]
	internal class PropulsionCannonPatches
	{
		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		public static void PostStart(PropulsionCannon __instance)
		{
			//Log.LogDebug($"PropulsionCannonPatches.PostStart executing");
			// Check if this PropulsionCannon is attached to a vehicle, and add the collector if so
			GameObject parentObj = __instance.gameObject;

			// the handheld Propulsion Cannon has a PlayerTool component; the vehicle versions don't. Easy peasy.
			if (parentObj.GetComponentInParent<PlayerTool>() != null)
				return;

			if (parentObj.GetComponentInParent<Exosuit>() != null)
				__instance.gameObject.EnsureComponent<ExosuitPropulsionArmCollector>();
			else if (parentObj.GetComponentInParent<SeaTruckUpgrades>() != null)
				__instance.gameObject.EnsureComponent<SeaTruckPropulsionArmCollector>();
			else
				Log.LogError($"PropulsionCannonPatches.PostStart running, but instance does not have a Player, Exosuit, or Seatruck as parent");
		}
	}
#endif
}
