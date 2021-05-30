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
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;

namespace CombinedItems.Patches
{
	[HarmonyPatch(typeof(Battery))]
	internal class Batteries
	{
		private static bool bProcessingBatteries = false;
		// TODO: Make configurable
		internal static Dictionary<string, float> BatteryValues = new Dictionary<string, float>()
		{
			{ "Battery", 150f },
			{ "PowerCell", 300f },
			{ "LithiumIonBattery", 300f },
			{ "PrecursorIonBattery", 800f },
			{ "PrecursorIonPowerCell", 1600f }
		};
		internal static Dictionary<TechType, float> typedBatteryValues = new Dictionary<TechType, float>();
		internal static HashSet<Battery> pendingBatteryList = new HashSet<Battery>();

		private static Dictionary<TechType, float> DefaultBatteryCharges = new Dictionary<TechType, float>();
		internal static void AddDefaultBatteryCharge(TechType battery, float capacity)
		{
			DefaultBatteryCharges[battery] = capacity;
		}


		internal static void PostPatch()
		{
			foreach (KeyValuePair<string, float> kvp in BatteryValues)
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

			bProcessingBatteries = true;

			while (pendingBatteryList.Count > 0)
			{
				int i = 0;
				//for (int i = pendingBatteryList.Count - 1; i >= 0; i--)
				foreach(Battery b in pendingBatteryList)
				{
					//Battery b = pendingBatteryList[i];
					TechTag tt = b.GetComponent<TechTag>();
					if (tt != null)
					{
						TechType batteryTech = tt.type;
						Log.LogDebug($"Deserialised battery at index {i} with TechType {batteryTech.AsString()}");
						if (typedBatteryValues.TryGetValue(batteryTech, out float value))
						{
							Log.LogDebug($"Updating battery with new capacity {value}");
							b._capacity = value;
							//b.OnAfterDeserialize();
						}
						//pendingBatteryList.RemoveAt(i);
					}
					yield return new WaitForEndOfFrame();
					i++;
				}
				yield return new WaitForSecondsRealtime(1f);
			}

			bProcessingBatteries = true;
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

			CoroutineHost.StartCoroutine(ProcessPendingBatteries());
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Battery.OnAfterDeserialize))]
		public static void PostDeserialize(Battery __instance)
		{
			/*bool bIsFull = (__instance.charge == __instance._capacity);
			TechType batteryTech = CraftData.GetTechType(__instance.gameObject);

			Log.LogDebug($"Batteries.PostAwake(): Found battery TechType {batteryTech.AsString()}");
			if (typedBatteryValues.ContainsKey(batteryTech))
			{
				float newCapacity = typedBatteryValues[batteryTech];
				if (__instance._capacity != newCapacity)
				{
					Log.LogDebug($"Updating battery with new capacity {newCapacity}");
					__instance._capacity = newCapacity;
					if (bIsFull)
						__instance._charge = newCapacity;
				}
			}*/
		}
	}
}
