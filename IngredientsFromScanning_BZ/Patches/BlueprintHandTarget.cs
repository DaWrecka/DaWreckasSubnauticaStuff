using HarmonyLib;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Logger = QModManager.Utility.Logger;
#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Json = Oculus.Newtonsoft.Json;
#elif BELOWZERO
using Json = Newtonsoft.Json;
#endif


namespace PartsFromScanning.Patches
{
/*
#if !BELOWZERO
    [HarmonyPatch(typeof(PDAScanner), nameof(PDAScanner.CanScan), new Type[] { typeof(GameObject) })]
    internal class PDAScanner_CanScan_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result, GameObject go)
        {
            if (!Main.config.bOverrideMapRoom)
            {
                return true;
            }

            __result = false;
            //Logger.Log(Logger.Level.Debug, $"PDAScanner.CanScan Override: checking GameObject: {JsonConvert.SerializeObject(go.GetInstanceID(), Newtonsoft.Json.Formatting.Indented)}");

            UniqueIdentifier component = go.GetComponent<UniqueIdentifier>();
            if (component != null)
            {
                TechType techType = CraftData.GetTechType(go);
                string id = component.Id;
                //if (!PDAScanner.fragments.ContainsKey(id) && !PDAScanner.complete.Contains(techType))
                //{
                //return true;
                __result = true;
                //}
            }
            return false;
        }
    }
#endif
*/

    [HarmonyPatch(typeof(BlueprintHandTarget))]
    public class BlueprintHandTargetPatches
    {
        public static bool bIsDatabox { get; private set; }
        public static TechType databoxUnlock { get; private set; }

        /*[HarmonyPatch(nameof(BlueprintHandTarget.UnlockBlueprint))]
        [HarmonyPrefix]
        internal static bool PreUnlockBlueprint(BlueprintHandTarget __instance)
        {
            databoxUnlock = __instance.unlockTechType;
            bIsDatabox = true;
            return true;
        }

        [HarmonyPatch(nameof(BlueprintHandTarget.UnlockBlueprint))]
        [HarmonyPostfix]
        internal static void PostUnlockBlueprint()
        {
            databoxUnlock = TechType.None;
            bIsDatabox = false;
        }*/
    }
}