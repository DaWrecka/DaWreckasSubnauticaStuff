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

namespace AcidProofSuit.Patches
{
    /*[HarmonyPatch(typeof(Player), nameof(Player.GetBreathPeriod))]
    internal class Player_GetBreathPeriod_Patch
    {
        internal static bool Prefix(ref float __result)
        {
            if(Inventory.main.equipment.GetCount(Main.helmetPrefab.TechType) > 0)
            {
                __result = 3f;
                return false;
            }

            return true;
        }
    }*/

    /*[HarmonyPatch(typeof(Player), nameof(Player.GetOxygenPerBreath))]
    internal class Player_GetOxygenPerBreath_Patch
    {
        internal static bool Prefix(Player __instance, float breathingInterval, int depthClass)
        {
            float num = 1f;
            if (Inventory.main.equipment.GetCount(Main.helmetPrefab.TechType) == 0 && __instance.mode != Player.Mode.Piloting && __instance.mode != Player.Mode.LockedPiloting)
            {
                if (depthClass == 2)
                {
                    num = 1.5f;
                }
                else if (depthClass == 3)
                {
                    num = 2f;
                }
            }
            float result = breathingInterval * num;
            if (!GameModeUtils.RequiresOxygen())
            {
                result = 0f;
            }
            return result;
        }
    }*/

    /*[HarmonyPatch(typeof(Player), nameof(Player.OnTakeDamage))]
    internal class Player_OnTakeDamage_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(Player __instance, DamageInfo damageInfo)
        {
            if (Time.timeScale == 0f)
            {
                damageInfo.damage = 0f;
                return false;
            }
            //Logger.Log(Logger.Level.Debug, $"OnTakeDamage prefixed with DamageInfo ({damageInfo.damage}, {damageInfo.type})");

            float baseDamage = damageInfo.damage;

            if (damageInfo.type == DamageType.Acid)
            {
                if (Inventory.main.equipment.GetCount(Main.suitPrefab.TechType) > 0)
                {
                    damageInfo.damage -= baseDamage * 0.7f;
                    //Logger.Log(Logger.Level.Debug, $"Acid damage reduced to {damageInfo.damage}");
                }

                if (Inventory.main.equipment.GetCount(Main.glovesPrefab.TechType) > 0)
                {
                    damageInfo.damage -= baseDamage * 0.30f;
                    //Logger.Log(Logger.Level.Debug, $"Acid damage reduced to {damageInfo.damage}");
                }

                //if (Inventory.main.equipment.GetCount(Main.helmetPrefab.TechType) > 0)
                //{
                //    damageInfo.damage -= baseDamage * 0.25f;
                //    //Logger.Log(Logger.Level.Debug, $"Acid damage reduced to {damageInfo.damage}");
                //}
            }

            damageInfo.damage = System.Math.Max(damageInfo.damage, 0f);
            return true;
        }
    }*/

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
            }
            return damage;
        }
    }

    [HarmonyPatch(typeof(Player), "EquipmentChanged")]
    internal class Player_EquipmentChanged_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref Player __instance, string slot, InventoryItem item)
        {
            Equipment equipment = Inventory.main.equipment;
            int i = 0;
            int num = __instance.equipmentModels.Length;
            while (i < num)
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
            MethodInfo dynMethod = __instance.GetType().GetMethod("UpdateReinforcedSuit", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(__instance, null);
            //__instance.UpdateReinforcedSuit();
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
