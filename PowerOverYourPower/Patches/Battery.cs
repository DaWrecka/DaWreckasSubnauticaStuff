using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Common;
using UnityEngine;
using UWE;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Crafting;
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
#endif

namespace PowerOverYourPower.Patches
{
	[HarmonyPatch(typeof(Battery))]
	internal class Batteries
	{
		private static bool bProcessingBatteries = false;
		internal static Dictionary<TechType, float> typedBatteryValues = new Dictionary<TechType, float>();
		private static HashSet<Battery> pendingBatteryList = new HashSet<Battery>();

		private static Dictionary<TechType, float> DefaultBatteryCharges = new Dictionary<TechType, float>();
		internal static void AddDefaultBatteryCharge(TechType battery, float capacity)
		{
			DefaultBatteryCharges[battery] = capacity;
		}

		internal void AddBattery(Battery b)
		{
			if (!pendingBatteryList.Contains(b))
			{
				pendingBatteryList.Add(b);
				CoroutineHost.StartCoroutine(ProcessPendingBatteries());
			}
		}

		internal static void PostPatch()
		{
			foreach (KeyValuePair<string, float> kvp in POYPPlugin.config.BatteryValues)
			{
				TechType tt = TechTypeUtils.GetTechType(kvp.Key);
				if (tt == TechType.None)
					Log.LogError($"No valid TechType found for string '{kvp.Key}'");
				else
				{
					Log.LogDebug($"Loading TechType {tt} with customised battery capacity of {kvp.Value}");
					//if (typedBatteryValues == null)
					//	typedBatteryValues = new Dictionary<TechType, float>();

					typedBatteryValues[tt] = kvp.Value;
				}
			}
		}

		internal static IEnumerator ProcessPendingBatteries()
		{
			if (bProcessingBatteries)
				yield break;

			if (pendingBatteryList.Count < 1)
				yield break;

			bProcessingBatteries = true;

			HashSet<Battery> removals = new HashSet<Battery>();

			while (pendingBatteryList.Count > 0)
			{
				var pendingCopy = new HashSet<Battery>(pendingBatteryList);
				foreach (Battery b in pendingCopy)
				{
					GameObject go = b.gameObject;

					if (go == null)
						continue;

					if (go.TryGetComponent<TechTag>(out TechTag tt))
					{
						TechType batteryTech = tt.type;
						if (tt.type != TechType.None)
						{
							Log.LogDebug($"ProcessPendingBatteries(): Deserialised battery instance {b.GetInstanceID()} with TechType {batteryTech.AsString()}");
							if (typedBatteryValues.TryGetValue(batteryTech, out float value))
							{
								Log.LogDebug($"ProcessPendingBatteries(): Updating battery with new capacity {value}");
								b._capacity = value;
								b.OnAfterDeserialize();
							}
						}
						removals.Add(b);
					}
					//}
					yield return new WaitForEndOfFrame();
				}

				foreach (Battery b in removals)
				{
					pendingBatteryList.Remove(b);
				}
				removals.Clear();
			}

			bProcessingBatteries = false;
			yield break;
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(Battery.OnProtoDeserialize))]
		public static void AddPendingBattery(ref Battery __instance)
		{
			if (!pendingBatteryList.Contains<Battery>(__instance))
			{
				Log.LogDebug($"Adding Battery instance {__instance.GetInstanceID()}");
				pendingBatteryList.Add(__instance);
			}

		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Battery.OnAfterDeserialize))]
		public static void PostDeserialize(ref Battery __instance)
		{
			float batteryChargePct = (__instance.charge / __instance._capacity);
			Log.LogDebug($"BatteryPatches.PostDeserialise: Processing battery instance {__instance.GetInstanceID()}");
/*#if SN1
			TechTag tt = __instance.gameObject?.GetComponent<TechTag>();
			if(tt != null)
#elif BELOWZERO*/

			// ^^ Turns out all of this crap is unnecessary, as TryGetComponent was implemented in Unity 2019.2; Subnautica 1, as of this writing, is on engine version 2019.2.17f1
			if (__instance.gameObject != null && __instance.gameObject.TryGetComponent<TechTag>(out TechTag tt))
			{
				Log.LogDebug($"BatteryPatches.PostDeserialise: Got TechTag");
				TechType batteryTech = tt.type; //CraftData.GetTechType(__instance.gameObject);

				Log.LogDebug($"Batteries.PostDeserialise(): Found battery TechType {batteryTech.AsString()}");
				if (batteryTech == TechType.None)
				{
					Log.LogDebug($"Failed to get TechType from TechTag, deferring processing");
					pendingBatteryList.Add(__instance);
					CoroutineHost.StartCoroutine(ProcessPendingBatteries());
				}
				else if (typedBatteryValues.TryGetValue(batteryTech, out float newCapacity))
				{
					if (__instance._capacity != newCapacity)
					{
						Log.LogDebug($"Batteries.PostDeserialise(): Updating battery with new capacity {newCapacity}");
						__instance._capacity = newCapacity;
						__instance._charge = (batteryChargePct * newCapacity);
					}
				}
			}
			else
			{
				Log.LogDebug($"Failed to get TechTag, deferring processing");
				pendingBatteryList.Add(__instance);
				CoroutineHost.StartCoroutine(ProcessPendingBatteries());
			}
		}
	}
}
