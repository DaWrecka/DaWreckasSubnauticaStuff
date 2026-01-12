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
/*
	[HarmonyPatch(typeof(VehicleDockingBay))]
	public class VehicleDockingBayPatches
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(VehicleDockingBay.OnTriggerEnter))]
		public static bool PreTriggerEnter(Collider other)
		{
			Log.LogDebug($"VehicleDockingBay.OnTriggerEnter() fired: Collider name {other.name}; other.isTrigger = {other.isTrigger}");

			if (other is ResourceCollider)
				return false;

			return true;
		}
	}
*/
}
