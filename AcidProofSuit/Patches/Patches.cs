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
        // This patch is specifically for the Rebreather; specifically the Acid Helmet.
        // If the function requests the Rebreather, we'll also check for the Acid Helmet
        [HarmonyPostfix]
        public static void PostFix(ref Equipment __instance, ref int __result, TechType techType)
        {
            //int result;
            // FieldInfo f = __instance.GetType().GetField("equippedCount");
            //Dictionary<TechType, int> equipCount = (Dictionary<TechType, int>)(__instance.GetPrivateField("equippedCount", BindingFlags.NonPublic | BindingFlags.Instance));

            /*MethodInfo dynMethod = __instance.GetType().GetMethod("UpdateReinforcedSuit", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(__instance, null);*/

            //this.equippedCount.TryGetValue(techType, out result);

            Dictionary<TechType, int> equipCount = __instance.GetInstanceField("equippedCount", BindingFlags.NonPublic | BindingFlags.Instance) as Dictionary<TechType, int>;
            // equipCount.TryGetValue(techType, out result);

            Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch: techType = {techType.ToString()}, __result = {__result}");
            if (techType == TechType.Rebreather)
            {
                int i;
                if (equipCount.TryGetValue(Main.helmetPrefab.TechType, out i))
                {
                    Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch: found AcidHelmet equipped");
                    __result++;
                }
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

            if (target == Player.main.gameObject)
            {
                float baseDamage = damage;

                if (type == DamageType.Acid)
                {
                    if (Player.main.GetVehicle() != null)
                    {
                        //Logger.Log(Logger.Level.Debug, "Player in vehicle, negating damage");
                        if (Player.main.acidLoopingSound.playing)
                            Player.main.acidLoopingSound.Stop();
                        return 0f;
                    }


                    if (Inventory.main.equipment.GetCount(Main.suitPrefab.TechType) > 0)
                    {
                        damage -= baseDamage * 0.7f;
                        //Logger.Log(Logger.Level.Debug, $"Acid damage reduced to {damage}");
                    }

                    if (Inventory.main.equipment.GetCount(Main.glovesPrefab.TechType) > 0)
                    {
                        damage -= baseDamage * 0.30f;
                        //Logger.Log(Logger.Level.Debug, $"Acid damage reduced to {damage}");
                    }

                    //if (Inventory.main.equipment.GetCount(Main.helmetPrefab.TechType) > 0)
                    //{
                    //    damageInfo.damage -= baseDamage * 0.25f;
                    //    Logger.Log(Logger.Level.Debug, $"Acid damage reduced to {damageInfo.damage}");
                    //}
                    if (damage > 0f && !(Player.main.acidLoopingSound.playing))
                        Player.main.acidLoopingSound.Play();
                }
                else if(type == DamageType.Radiation)
                {
                    if (Inventory.main.equipment.GetCount(Main.suitPrefab.TechType) > 0)
                    {
                        damage -= baseDamage * 0.65f;
                        //Logger.Log(Logger.Level.Debug, $"Acid damage reduced to {damage}");
                    }

                    if (Inventory.main.equipment.GetCount(Main.glovesPrefab.TechType) > 0)
                    {
                        damage -= baseDamage * 0.30f;
                        //Logger.Log(Logger.Level.Debug, $"Acid damage reduced to {damage}");
                    }
                }
            }
            return System.Math.Max(damage, 0f);
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
            for(int i = 0; i < num; i++)
            {
                Player.EquipmentType equipmentType = __instance.equipmentModels[i];
                TechType techTypeInSlot = equipment.GetTechTypeInSlot(equipmentType.slot);
                if (techTypeInSlot == Main.suitPrefab.TechType)
                    techTypeInSlot = TechType.ReinforcedDiveSuit;
                else if (techTypeInSlot == Main.glovesPrefab.TechType)
                    techTypeInSlot = TechType.ReinforcedGloves;
                else
                  continue;

                bool flag = false;
                int j = 0;
                int num2 = equipmentType.equipment.Length;
                while (j < num2)
                {
                    Player.EquipmentModel equipmentModel = equipmentType.equipment[j];
                    bool flag2 = equipmentModel.techType == techTypeInSlot;
                    flag = (flag || flag2);
                    if (equipmentModel.model)
                    {
                        equipmentModel.model.SetActive(flag2);
                    }
                    j++;
                }
                if (equipmentType.defaultModel)
                {
                    equipmentType.defaultModel.SetActive(!flag);
                }
                i++;
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
        [HarmonyPrefix]
        public static bool Prefix(ref Player __instance)
        {
            if (__instance != null)
            {
                if (Main.bInAcid)
                {
                    // Player is currently immersed in acid; if this change changes their acid immunity, start/stop playing the effects
                    if (Inventory.main.equipment.GetCount(Main.suitPrefab.TechType) > 0 && Inventory.main.equipment.GetCount(Main.glovesPrefab.TechType) > 0)
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

            return true;
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

            if (__instance != null && Inventory.main.equipment.GetCount(Main.suitPrefab.TechType) > 0 && Inventory.main.equipment.GetCount(Main.glovesPrefab.TechType) > 0)
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
