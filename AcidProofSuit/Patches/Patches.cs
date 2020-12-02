using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Logger = QModManager.Utility.Logger;
using UWE;
using System;
using System.Diagnostics;
using System.Reflection;
using Steamworks;
using SMLHelper.V2.Utility;
using static Player;
using System.IO;

namespace AcidProofSuit.Patches
{
    [HarmonyPatch(typeof(Equipment), nameof(Equipment.GetCount))]
    internal class Equipment_GetCount_Patch
    {
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

        /*internal static Dictionary<TechType, TechType> Substitutions = new Dictionary<TechType, TechType>
        {
            // The key is the TT to search for, and the value is the TT to return a positive value for.
            // so for key "TechType.Rebreather" and value "TechType.AcidHelmet", if GetCount(Rebreather) is called, the function will add one if the AcidHelmet is equipped.
            { TechType.Rebreather, Main.prefabHelmet.TechType },
            { TechType.RadiationHelmet, Main.prefabHelmet.TechType },
            { TechType.RadiationSuit, Main.prefabSuitMk1.TechType },
            { TechType.RadiationGloves, Main.prefabGloves.TechType }
        };*/

        private static List<TechTypeSub> Substitutions = new List<TechTypeSub>();

        // We use this as a cache; if PostFix receives a call for which substitutionTargets.Contains(techType) is false, we know nothing has requested to substitute this TechType, so we can ignore it.
        private static List<TechType> substitutionTargets = new List<TechType>();

        public static void AddSubstitution(TechType substituted, TechType substitution)
        {
            Substitutions.Add(new TechTypeSub(substituted, substitution));
            substitutionTargets.Add(substitution);
            Logger.Log(Logger.Level.Debug, $"AddSubstitution: Added sub with substituted {substituted.ToString()} and substitution {substitution.ToString()}, new count {Substitutions.Count}");
        }

        [HarmonyPostfix]
        public static void PostFix(ref Equipment __instance, ref int __result, TechType techType)
        {
            if (!substitutionTargets.Contains(techType))
                return; // No need to do anything more.
            Dictionary<TechType, int> equipCount = __instance.GetInstanceField("equippedCount", BindingFlags.NonPublic | BindingFlags.Instance) as Dictionary<TechType, int>;
            // equipCount.TryGetValue(techType, out result);

            Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch.PostFix: executing with parameters __result {__result.ToString()}, techType {techType.ToString()}");
            //foreach (TechTypeSub t in Substitutions)
            int count = Substitutions.Count;
            for (int i = 0; i < count; i++)
            {
                TechTypeSub t = Substitutions[i];
                Logger.Log(Logger.Level.Debug, $"using TechTypeSub at index {i} of {count} with values substituted {t.substituted}, substition {t.substitution}");
                if (t.substitution == techType)
                {
                    int c;
                    if (equipCount.TryGetValue(t.substituted, out c))
                    {
                        Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch: found {techType.ToString()} equipped");
                        __result++;
                        break;
                    }
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
                    Player __instance = Player.main;
                    foreach (string s in Main.playerSlots)
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
            if (!Main.playerSlots.Contains(slot))
                return;
            //Texture2D glovesTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidGlovesskin.png"));
            //Texture2D suitTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidSuitskin.png"));
            bool bUseCustomTex = (Main.suitTexture != null && Main.glovesTexture != null);
            Equipment equipment = Inventory.main.equipment;
            int num = __instance.equipmentModels.Length;
            //for(int i = 0; i < num; i++)
            foreach(Player.EquipmentType equipmentType in __instance.equipmentModels)
            {
                TechType techTypeInSlot = equipment.GetTechTypeInSlot(equipmentType.slot);
                if (techTypeInSlot == Main.prefabSuitMk1.TechType || techTypeInSlot == Main.prefabSuitMk2.TechType || techTypeInSlot == Main.prefabSuitMk3.TechType)
                    techTypeInSlot = TechType.ReinforcedDiveSuit;
                else if (techTypeInSlot == Main.prefabGloves.TechType)
                    techTypeInSlot = TechType.ReinforcedGloves;
                else
                  continue;

                bool flag = false;

                foreach(Player.EquipmentModel equipmentModel in equipmentType.equipment)
                {
                    //Player.EquipmentModel equipmentModel = equipmentType.equipment[j];
                    bool equipmentVisibility = (equipmentModel.techType == techTypeInSlot);
                    Renderer reinforcedGloves = null;
                    Renderer reinforcedSuit = null;
                    Shader shader = null;
                    if (bUseCustomTex)
                    {
                        shader = Shader.Find("MarmosetUBER");
                        GameObject playerModel = Player.main.gameObject;
                        // find the gloves material and get it's renderer
                        reinforcedGloves = playerModel.transform.Find("body/player_view/male_geo/reinforcedSuit/reinforced_suit_01_glove_geo").gameObject.GetComponent<Renderer>();
                        // find the suit material and get it's renderer
                        reinforcedSuit = playerModel.transform.Find("body/player_view/male_geo/reinforcedSuit/reinforced_suit_01_body_geo").gameObject.GetComponent<Renderer>();
                    }

                    flag = (flag || equipmentVisibility)
                    if (equipmentModel.model)
                    {
                        if (bUseCustomTex)
                        {
                            // if the gloves shader isn't null, add the shader
                            if (reinforcedGloves.material.shader != null)
                                reinforcedGloves.material.shader = shader;
                            // if the suit's shader isn't null, add the shader
                            if (reinforcedSuit.material.shader != null)
                                reinforcedSuit.material.shader = shader;
                            // add the gloves main Texture when equipped
                            reinforcedGloves.material.mainTexture = Main.glovesTexture;
                            // add  the gloves illum texture when equipped
                            reinforcedGloves.material.SetTexture(ShaderPropertyID._Illum, Main.glovesIllumTexture);
                            // add  the gloves spec texture when equipped
                            reinforcedGloves.material.SetTexture(ShaderPropertyID._SpecTex, Main.glovesTexture);
                            // add the suit main Texture when equipped
                            reinforcedSuit.material.mainTexture = Main.suitTexture;
                            // add the suit spec texture when equipped
                            reinforcedSuit.material.SetTexture(ShaderPropertyID._SpecTex, Main.suitTexture);
                            // add  the suit illum Texture when equipped
                            reinforcedSuit.material.SetTexture(ShaderPropertyID._Illum, Main.suitIllumTexture);
                            // add the suit's arms main Texture when equipped
                            reinforcedSuit.materials[1].mainTexture = Main.glovesTexture;
                            // add the suit's arms spec Texture when equipped
                            reinforcedSuit.materials[1].SetTexture(ShaderPropertyID._SpecTex, Main.glovesTexture);
                            // add the suit's arms illum texture when equipped
                            reinforcedSuit.materials[1].SetTexture(ShaderPropertyID._Illum, Main.glovesIllumTexture);
                        }

                        equipmentModel.model.SetActive(equipmentVisibility);
                    }
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
        [HarmonyPrefix]
        public static void Prefix(ref Player __instance)
        {
            //Logger.Log(Logger.Level.Debug, $"UpdateReinforcedSuitPatcher.Prefix begin:");
            foreach (string s in Main.playerSlots)
            {
                TechType tt = Inventory.main.equipment.GetTechTypeInSlot(s);
                //Logger.Log(Logger.Level.Debug, $"Found TechType {tt.ToString()} in slot {s}");
                tt = Inventory.main.equipment.GetTechTypeInSlot(s);
                //Logger.Log(Logger.Level.Debug, $"Found patched TechType {tt.ToString()} in slot {s}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(ref Player __instance)
        {
            if (__instance != null)
            {
                int flags = 0;
                if(Inventory.main.equipment.GetCount(Main.prefabSuitMk1.TechType) > 0)
                {
                    flags += 1;
                    __instance.temperatureDamage.minDamageTemperature += 9f;
                }

                if (Inventory.main.equipment.GetCount(Main.prefabGloves.TechType) > 0)
                {
                    flags += 2;
                    __instance.temperatureDamage.minDamageTemperature += 1f;
                }

                if(Inventory.main.equipment.GetCount(Main.prefabHelmet.TechType) > 0)
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
                && Inventory.main.equipment.GetCount(Main.prefabSuitMk1.TechType) > 0
                && Inventory.main.equipment.GetCount(Main.prefabGloves.TechType) > 0
                && Inventory.main.equipment.GetCount(Main.prefabHelmet.TechType) > 0)
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
            __result = (__result || Inventory.main.equipment.GetCount(Main.prefabSuitMk1.TechType) > 0);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HasReinforcedGloves))]
    public static class Player_HasReinforcedGloves_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            __result = (__result || Inventory.main.equipment.GetCount(Main.prefabGloves.TechType) > 0);
        }
    }

}
