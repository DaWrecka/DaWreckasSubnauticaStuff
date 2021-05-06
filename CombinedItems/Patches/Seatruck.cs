using CombinedItems;
using CombinedItems.VehicleModules;
using CombinedItems.MonoBehaviours;
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
using TMPro;

namespace CombinedItems.Patches
{
	[HarmonyPatch(typeof(uGUI_SeaTruckHUD))]
	class SeaTruckHUDPatches
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
}
