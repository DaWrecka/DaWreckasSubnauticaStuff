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

namespace CombinedItems.Patches
{
    [HarmonyPatch(typeof(Battery))]
    class Batteries
    {
        // TODO: Make configurable
        internal static Dictionary<string, float> BatteryValues = new Dictionary<string, float>()
        {
            { "Battery", 150f },
            { "PowerCell", 300f },
            { "LithiumIonBattery", 300f },
            { "PrecursorIonBattery", 800f },
            { "PrecursorIonPowerCell", 1600f }
        };

        internal static Dictionary<TechType, float> typedBatteryValues;

        internal static List<Battery> pendingBatteryList = new List<Battery>();

        internal static void PostPatch()
        {
            foreach (KeyValuePair<string, float> kvp in BatteryValues)
            {
                TechType tt = TechTypeUtils.GetTechType(kvp.Key);
                if (tt == TechType.None)
                    Log.LogError($"No valid TechType found for string '{kvp.Key}'");
                else
                {
                    if (typedBatteryValues == null)
                        typedBatteryValues = new Dictionary<TechType, float>();

                    typedBatteryValues.Add(tt, kvp.Value);
                }    
            }
        }

        internal static IEnumerator ProcessPendingBatteries()
        {
            while (pendingBatteryList.Count > 0)
            {
                for(int i = pendingBatteryList.Count - 1; i >= 0; i--)
                {
                    Battery b = pendingBatteryList[i];
                    TechTag tt = b.GetComponent<TechTag>();
                    if (tt != null)
                    {
                        TechType batteryTech = tt.type;
                        Log.LogDebug($"Deserialised battery at index {i} with TechType {batteryTech.AsString()}");
                        if (typedBatteryValues.TryGetValue(batteryTech, out float value))
                        {
                            Log.LogDebug($"Updating battery with new capacity {value}");
                            b._capacity = value;
                            b.OnAfterDeserialize();
                        }
                        pendingBatteryList.RemoveAt(i);
                    }
                    yield return new WaitForEndOfFrame();
                }
                yield return new WaitForSecondsRealtime(1f);
            }
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
    }
}
