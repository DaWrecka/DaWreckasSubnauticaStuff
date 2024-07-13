using Common;
using HarmonyLib;
#if NAUTILUS
using Nautilus.Utility;
using Common.NautilusHelper;
#else
using SMLHelper.V2.Utility;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DWEquipmentBonanza.Patches
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(Equipment))]
    internal class EquipmentPatch
    {
        private const bool bVerbose = false;

        // This patch allows for "substitutions", as it were; Specifically, it allows the modder to set up certain TechTypes to return results for both itself and another type.
        // For example, the initial purpose was to allow requests for the Rebreather to return a positive result if the Acid Helmet were worn.
        private static Dictionary<TechType, HashSet<TechType> > Substitutions = new Dictionary<TechType, HashSet<TechType> >();

        public static void AddSubstitution(TechType custom, TechType vanilla)
        {
            HashSet<TechType> set;

            if (Substitutions.TryGetValue(vanilla, out set))
            {
                set.Add(custom);
                Log.LogDebug($"EquipmentPatch.AddSubstitution: Added sub {custom.AsString()} to existing HashSet for TechType {vanilla.AsString()}, new count for this vanilla TechType {set.Count}");
                return;
            }

            set = new HashSet<TechType>()
            {
                custom
            };
            Substitutions.Add(vanilla, set);
            Log.LogDebug($"EquipmentPatch.AddSubstitution: Added new substitution set for TechType {vanilla.AsString()} with sub {custom.AsString()}");
            //substitutionTargets.Add(substitution);

        }

        public static void AddSubstitutions(TechType custom, HashSet<TechType> substitutions)
        {
            foreach (TechType vanilla in substitutions)
            {
                AddSubstitution(custom, vanilla);
            }

        }

        //[HarmonyPatch(nameof(Equipment.GetItemInSlot))]
        //[HarmonyPrefix]
        //public static void PreGetItemInSlot(Equipment __instance, string slot)
        //{
        //    System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
        //    Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({slot}) executing");
        //}

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Equipment.GetCount))]
        public static void PostGetCount(ref Equipment __instance, ref int __result, TechType techType)
        {
            if (!Substitutions.ContainsKey(techType))
                return; // No need to do anything more.
            Log.LogDebug($"EquipmentPatch.PostGetCount: executing with parameters __result {__result.ToString()}, techType {techType.AsString()}");

            Dictionary<TechType, int> equipCount = __instance.GetInstanceField("equippedCount", BindingFlags.NonPublic | BindingFlags.Instance) as Dictionary<TechType, int>;

            if (Substitutions.TryGetValue(techType, out HashSet<TechType> subs))
            {
                Log.LogDebug($"EquipmentPatch.PostGetCount: got substitution set with {subs.Count} members");
                foreach (TechType sub in subs)
                {
                    if (equipCount.TryGetValue(sub, out int c))
                    {
                        //Log.LogDebug($"EquipmentPatch.PostGetCount: found TechType {sub.AsString()} equipped {c} times");
                        __result += c;
                    }
                    else
                    {
                        //Log.LogDebug($"EquipmentPatch.PostGetCount: TechType {sub.AsString()} not found in equipment.");
                    }
                    //__result += equipCount.GetOrDefault(sub, 0);
                }
            }
            Log.LogDebug($"EquipmentPatch.PostGetCount: finished with result {__result.ToString()}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Equipment.IsCompatible))]
        public static void PostIsCompatible(EquipmentType itemType, EquipmentType slotType, ref bool __result)
        {
            //Log.LogDebug($"itemType = {itemType.ToString()}, slotType = {slotType.ToString()}, __result = {__result}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.AllowedToAdd))]
        public static bool PreAllowedToAdd(Equipment __instance, ref bool __result, string slot, Pickupable pickupable, bool verbose)
        {
            //Log.LogDebug($"EquipmentPatches.PreAllowedToAdd(): __result = {__result}, slot = '{slot}'");

            TechType objTechType = pickupable.GetTechType();
            //Log.LogDebug($"EquipmentPatches.PreAllowedToAdd(): objTechType = {objTechType.AsString()}");
            EquipmentType slotType = Equipment.GetSlotType(slot);
            if (slotType == EquipmentType.BatteryCharger && InventoryPatches.IsChip(objTechType))
            {
#if BELOWZERO
                EquipmentType eType = TechData.GetEquipmentType(objTechType);
#else
                EquipmentType eType = CraftData.GetEquipmentType(objTechType);
#endif
                if (eType == EquipmentType.Chip || eType == EquipmentType.BatteryCharger)
                {
#if false
                    Logger.Log("DEBUG: AllowedToAdd battery charger for " + objTechType.AsString(false));
#endif
                    bool result = ((IItemsContainer)__instance).AllowedToAdd(pickupable, verbose);
                    __result = result;
                    return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IItemsContainer.AllowedToAdd")]
        public static bool PreIItemsContainerAllowedToAdd(Pickupable pickupable, Equipment __instance, ref bool __result)
        {
            //Log.LogDebug($"PreIItemsContainerAllowedToAdd(): __instance.label = {__instance._label}, pickupable = {pickupable.ToString()}, __result = {__result}");
            TechType tt = pickupable.GetTechType();
            // IsRechargeableChip() is probably faster than a string.Contains() so we're doing that first, so that the slower check doesn't even happen if it's not needed.
            if (InventoryPatches.IsRechargeableChip(tt) && __instance._label.Contains("BatteryCharger"))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
