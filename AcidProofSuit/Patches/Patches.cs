using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Oculus.Newtonsoft.Json;
using Logger = QModManager.Utility.Logger;
using UWE;
using System;
using System.Diagnostics;
using System.Reflection;
using Steamworks;
using SMLHelper.V2.Utility;

namespace AcidProofSuit.Patches
{
    [HarmonyPatch(typeof(Equipment), nameof(Equipment.GetCount))]
    internal class Equipment_GetCount_Patch
    {
        // This patch allows for "substitutions", as it were; Specifically, it allows the modder to set up certain TechTypes to return results for both itself and another type.
        // For example, the initial purpose was to allow requests for the Rebreather to return a positive result if the Acid Helmet were worn.

        internal static Dictionary<TechType, TechType> Substitutions = new Dictionary<TechType, TechType>
        {
            // The key is the TT to search for, and the value is the TT to return a positive value for.
            // so for key "TechType.Rebreather" and value "TechType.AcidHelmet", if GetCount(Rebreather) is called, the function will add one if the AcidHelmet is equipped.
            { TechType.Rebreather, Main.helmetPrefab.TechType },
            { TechType.RadiationHelmet, Main.helmetPrefab.TechType },
            { TechType.RadiationSuit, Main.suitPrefab.TechType },
            { TechType.RadiationGloves, Main.glovesPrefab.TechType }
        };

        [HarmonyPostfix]
        public static void PostFix(ref Equipment __instance, ref int __result, TechType techType)
        {
            Dictionary<TechType, int> equipCount = __instance.GetInstanceField("equippedCount", BindingFlags.NonPublic | BindingFlags.Instance) as Dictionary<TechType, int>;
            // equipCount.TryGetValue(techType, out result);

            //Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch: techType = {techType.ToString()}, __result = {__result}");
            //if (techType == TechType.Rebreather)
            if (Substitutions.TryGetValue(techType, out TechType sub))
            //foreach (KeyValuePair<TechType, TechType> sub in Substitutions)
            {
                //if (techType == sub.Key)
                //{
                int i;
                if (equipCount.TryGetValue(sub, out i))
                {
                    //Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch: found {techType.ToString()} equipped");
                    __result++;
                }
                //}
            }
        }
    }


    [HarmonyPatch(typeof(DamageSystem), nameof(DamageSystem.CalculateDamage))]
    internal class DamageSystem_CalculateDamage_Patch
    {
        [HarmonyPostfix]
        public static float Postfix(float damage, DamageType type, GameObject target, GameObject dealer = null)
        {
            //Logger.Log(Logger.Level.Debug, $"DamageSystem_CalculateDamage_Patch.Postfix executing: parameters (damage = {damage}, DamageType = {type})");

            float baseDamage = damage;
            float newDamage = damage;
            if (target == Player.main.gameObject)
            {

                if (type == DamageType.Acid)
                {
                    if (Player.main.GetVehicle() != null)
                    {
                        //Logger.Log(Logger.Level.Debug, "Player in vehicle, negating damage");
                        if (Player.main.acidLoopingSound.playing)
                            Player.main.acidLoopingSound.Stop();
                        return 0f;
                    }

                    Equipment equipment = Inventory.main.equipment;
                    string[] slots = new string[]
                    {
                    "Head",
                    "Body",
                    "Gloves",
                    "Foots", // Seriously? 'Foots'?
                    "Chip1",
                    "Chip2",
                    "Tank"
                    };
                    Player __instance = Player.main;
                    //int num = __instance.equipmentModels.Length;
                    //for (int i = 0; i < num; i++)
                    foreach (string s in slots)
                    {
                        //Player.EquipmentType equipmentType = __instance.equipmentModels[i];
                        //TechType techTypeInSlot = equipment.GetTechTypeInSlot(equipmentType.slot);
                        TechType techTypeInSlot = equipment.GetTechTypeInSlot(s);

                        newDamage += Main.ModifyDamage(techTypeInSlot, baseDamage, type);
                        //Logger.Log(Logger.Level.Debug, $"Found techTypeInSlot {techTypeInSlot.ToString()}; damage altered to {damage}");
                    }
                }
            }
            
            return System.Math.Max(newDamage, 0f);
        }
    }

    [HarmonyPatch(typeof(Player), "EquipmentChanged")]
    internal class Player_EquipmentChanged_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref Player __instance, string slot, InventoryItem item)
        {
            Equipment equipment = Inventory.main.equipment;
            int num = __instance.equipmentModels.Length;
            //for(int i = 0; i < num; i++)
            foreach(Player.EquipmentType equipmentType in __instance.equipmentModels)
            {
                //Player.EquipmentType equipmentType = __instance.equipmentModels[i];
                TechType techTypeInSlot = equipment.GetTechTypeInSlot(equipmentType.slot);
                if (techTypeInSlot == Main.suitPrefab.TechType)
                    techTypeInSlot = TechType.ReinforcedDiveSuit;
                else if (techTypeInSlot == Main.glovesPrefab.TechType)
                    techTypeInSlot = TechType.ReinforcedGloves;
                else
                  continue;

                bool flag = false;
                /*int j = 0;
                int num2 = equipmentType.equipment.Length;*/
                //while (j < num2)
                foreach(Player.EquipmentModel equipmentModel in equipmentType.equipment)
                {
                    //Player.EquipmentModel equipmentModel = equipmentType.equipment[j];
                    bool flag2 = equipmentModel.techType == techTypeInSlot;
                    flag = (flag || flag2);
                    if (equipmentModel.model)
                    {
                        equipmentModel.model.SetActive(flag2);
                    }
                    //j++;
                }
                if (equipmentType.defaultModel)
                {
                    equipmentType.defaultModel.SetActive(!flag);
                }
            }

            // Actually we don't need this, since we're not doing anything that would change the outcome of UpdateReinforcedSuit.
            // Might be useful in the future though, so it's getting commented-out instead of deleted.
            
            /*MethodInfo dynMethod = __instance.GetType().GetMethod("UpdateReinforcedSuit", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(__instance, null);*/
        }
    }

    [HarmonyPatch(typeof(Player), "UpdateReinforcedSuit")]
    internal class UpdateReinforcedSuitPatcher
    {
        [HarmonyPostfix]
        public static void Postfix(ref Player __instance)
        {
            if (__instance != null)
            {
                int flags = 0;
                if(Inventory.main.equipment.GetCount(Main.suitPrefab.TechType) > 0)
                {
                    flags += 1;
                    __instance.temperatureDamage.minDamageTemperature += 9f;
                }

                if (Inventory.main.equipment.GetCount(Main.glovesPrefab.TechType) > 0)
                {
                    flags += 2;
                    __instance.temperatureDamage.minDamageTemperature += 1f;
                }

                if(Inventory.main.equipment.GetCount(Main.suitPrefab.TechType) > 0)
                {
                    flags += 4;
                    __instance.temperatureDamage.minDamageTemperature += 5f;
                }
                //Logger.Log(Logger.Level.Debug, $"UpdatedReinforcedSuitPatcher.Postfix: minDamageTemperature patched to {__instance.temperatureDamage.minDamageTemperature}", null, true);

                if (Main.bInAcid)
                {
                    // Player is currently immersed in acid; if this change changes their acid immunity, start/stop playing the effects
                    if (flags == 7)
                    {
                        if (__instance.acidLoopingSound.playing)
                            __instance.acidLoopingSound.Stop();
                    }
                    else
                    {
                        if (!(__instance.acidLoopingSound.playing))
                            __instance.acidLoopingSound.Play();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), "OnAcidEnter")]
    internal class Player_OnAcidEnter_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Player __instance)
        {
            //Logger.Log(Logger.Level.Debug, "Player entered acid");

            Main.bInAcid = true;

            if (__instance != null
                && Inventory.main.equipment.GetCount(Main.suitPrefab.TechType) > 0
                && Inventory.main.equipment.GetCount(Main.glovesPrefab.TechType) > 0
                && Inventory.main.equipment.GetCount(Main.helmetPrefab.TechType) > 0)
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "OnAcidExit")]
    internal class Player_OnAcidExit_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Player __instance)
        {
            //Logger.Log(Logger.Level.Debug, "Player exited acid");

            Main.bInAcid = false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HasReinforcedSuit))]
    public static class Player_HasReinforcedSuit_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            __result = (__result || Inventory.main.equipment.GetCount(Main.suitPrefab.TechType) > 0);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HasReinforcedGloves))]
    public static class Player_HasReinforcedGloves_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            __result = (__result || Inventory.main.equipment.GetCount(Main.glovesPrefab.TechType) > 0);
        }
    }

}
