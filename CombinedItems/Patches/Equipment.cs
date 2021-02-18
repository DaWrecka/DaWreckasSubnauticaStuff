using HarmonyLib;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace CombinedItems.Patches
{
    [HarmonyPatch]
    internal class EquipmentPatch
    {
        private const bool bVerbose = false;

        // This patch allows for "substitutions", as it were; Specifically, it allows the modder to set up certain TechTypes to return results for both itself and another type.
        // For example, the initial purpose was to allow requests for the Rebreather to return a positive result if the Acid Helmet were worn.
        private struct TechTypeSub
        {
            public TechType substituted { get; } // If this is equipped...
            public TechType substitution { get; } // ...return a positive for this search.

            public TechTypeSub(TechType substituted, TechType substitution)
            {
                this.substituted = substituted;
                this.substitution = substitution;
            }
        }; // We can't use a Dictionary as-is; one way or another we need either a struct or an array, because one TechType - or key - might have multiple substitutions.
        // For example, the Brine Suit needs to be recognised as both a Radiation Suit and a Reinforced Dive Suit.

        private static List<TechTypeSub> Substitutions = new List<TechTypeSub>();

        // We use this as a cache; if PostFix receives a call for which substitutionTargets.Contains(techType) is false, we know nothing has requested to substitute this TechType, so we can ignore it.
        private static List<TechType> substitutionTargets = new List<TechType>();

        public static void AddSubstitution(TechType substituted, TechType substitution)
        {
            Substitutions.Add(new TechTypeSub(substituted, substitution));
            substitutionTargets.Add(substitution);
#if !RELEASE
                //Logger.Log(Logger.Level.Debug, $"EquipmentPatch.AddSubstitution: Added sub with substituted {substituted.AsString()} and substitution {substitution.AsString()}, new count {Substitutions.Count}");
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Equipment), nameof(Equipment.GetCount))]
        public static void GetCountPostFix(ref Equipment __instance, ref int __result, TechType techType)
        {
            if (!substitutionTargets.Contains(techType))
                return; // No need to do anything more.
            Dictionary<TechType, int> equipCount = __instance.GetInstanceField("equippedCount", BindingFlags.NonPublic | BindingFlags.Instance) as Dictionary<TechType, int>;
            // equipCount.TryGetValue(techType, out result);

#if !RELEASE
            if (Main.bVerboseLogging)
            {
                //Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch.PostFix: executing with parameters __result {__result.ToString()}, techType {techType.AsString()}");
            }
#endif
            //foreach (TechTypeSub t in Substitutions)
            int count = Substitutions.Count;
            for (int i = 0; i < count; i++)
            {
                TechTypeSub t = Substitutions[i];
#if !RELEASE
                if (Main.bVerboseLogging)
                {
                    //Logger.Log(Logger.Level.Debug, $"using TechTypeSub at index {i} of {count} with values substituted {t.substituted}, substition {t.substitution}");
                }
#endif
                if (t.substitution == techType)
                {
                    int c;
                    if (equipCount.TryGetValue(t.substituted, out c))
                    {
#if !RELEASE
                        if (Main.bVerboseLogging)
                        {
                            //Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch: found {techType.AsString()} equipped");
                        }
#endif
                        __result++;
                        break;
                    }
                }
                //}
            }
        }
    }
}
