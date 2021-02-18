using HarmonyLib;
using SMLHelper.V2.Utility;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

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

        private static List<TechTypeSub> Substitutions = new List<TechTypeSub>();

        // We use this as a cache; if PostFix receives a call for which substitutionTargets.Contains(techType) is false, we know nothing has requested to substitute this TechType, so we can ignore it.
        private static List<TechType> substitutionTargets = new List<TechType>();

        public static void AddSubstitution(TechType substituted, TechType substitution)
        {
            Substitutions.Add(new TechTypeSub(substituted, substitution));
            substitutionTargets.Add(substitution);
#if !RELEASE
            Logger.Log(Logger.Level.Debug, $"AddSubstitution: Added sub with substituted {substituted.ToString()} and substitution {substitution.ToString()}, new count {Substitutions.Count}"); 
#endif
        }

        [HarmonyPostfix]
        public static void PostFix(ref Equipment __instance, ref int __result, TechType techType)
        {
            if (!substitutionTargets.Contains(techType))
                return; // No need to do anything more.
            Dictionary<TechType, int> equipCount = __instance.GetInstanceField("equippedCount", BindingFlags.NonPublic | BindingFlags.Instance) as Dictionary<TechType, int>;
            // equipCount.TryGetValue(techType, out result);

            //Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch.PostFix: executing with parameters __result {__result.ToString()}, techType {techType.ToString()}");
            //foreach (TechTypeSub t in Substitutions)
            int count = Substitutions.Count;
            for (int i = 0; i < count; i++)
            {
                TechTypeSub t = Substitutions[i];
                //Logger.Log(Logger.Level.Debug, $"using TechTypeSub at index {i} of {count} with values substituted {t.substituted}, substition {t.substitution}");
                if (t.substitution == techType)
                {
                    int c;
                    if (equipCount.TryGetValue(t.substituted, out c))
                    {
                        //Logger.Log(Logger.Level.Debug, $"Equipment_GetCount_Patch: found {techType.ToString()} equipped");
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
                    float lastDamage = damage;
                    foreach (string s in Main.playerSlots)
                    {
                        //Player.EquipmentType equipmentType = __instance.equipmentModels[i];
                        //TechType techTypeInSlot = equipment.GetTechTypeInSlot(equipmentType.slot);
                        TechType techTypeInSlot = equipment.GetTechTypeInSlot(s);
                        float damageMod = Main.ModifyDamage(techTypeInSlot, baseDamage, type);
                        newDamage += damageMod;
                        //if(newDamage != lastDamage)
                        //    Logger.Log(Logger.Level.Debug, $"Found techTypeInSlot {techTypeInSlot.ToString()}; damage altered from {lastDamage} by {damageMod} to {newDamage}");
                        lastDamage = newDamage;
                    }
                }
            }

            return System.Math.Max(newDamage, 0f);
        }
    }

    [HarmonyPatch(typeof(Player), "EquipmentChanged")]
    internal class Player_EquipmentChanged_Patch
    {
        // Original, unmodified materials.
        private static Material defaultGloveMaterial;
        private static Material defaultSuitMaterial;
        private static Material defaultArmsMaterial;
        private static Material brineGloveMaterial;
        private static Material brineSuitMaterial;
        private static Material brineArmsMaterial;
        private static TechType lastBodyTechType = TechType.None;
        private static TechType lastGlovesTechType = TechType.None;

        // The gloves texture is used for the suit as well, on the arms, so we need to do something about that.
        // The block that generates the glove texture is sizable, so it's made into a function here.
        private static Material GetGloveMaterial(Shader shader, Material OriginalMaterial)
        {
            // if the gloves shader isn't null, add the shader
            //Logger.Log(Logger.Level.Debug, "Creating new brineGloveMaterial");
            if (OriginalMaterial != null)
            {
                Material newMat = new Material(OriginalMaterial);
                // if the suit's shader isn't null, add the shader
                if (shader != null)
                    newMat.shader = shader;
                // add the gloves main Texture when equipped
                //Logger.Log(Logger.Level.Debug, $"add the gloves main Texture when equipped");
                newMat.mainTexture = Main.glovesTexture;
                // add  the gloves illum texture when equipped
                //Logger.Log(Logger.Level.Debug, $"add  the gloves illum texture when equipped"); 
                newMat.SetTexture(ShaderPropertyID._Illum, Main.glovesIllumTexture);
                // add  the gloves spec texture when equipped
                //Logger.Log(Logger.Level.Debug, $"add  the gloves spec texture when equipped"); 
                newMat.SetTexture(ShaderPropertyID._SpecTex, Main.glovesTexture);

                return newMat;
            }
            else
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, "Default material not found while trying to create new brineGloveMaterial"); 
#endif
            }

            return null;
        }


        [HarmonyPostfix]
        public static void Postfix(ref Player __instance, string slot, InventoryItem item)
        {
            List<string> mySlots = new List<string>() { "Body", "Gloves" };

            bool bLog = Main.playerSlots.Contains(slot);
            //Logger.Log(Logger.Level.Debug, "1");
            bool bUseCustomTex = (Main.suitTexture != null && Main.glovesTexture != null);
            if (bLog) Logger.Log(Logger.Level.Debug, $"Player_EquipmentChanged_Patch.Postfix: slot = {slot}. Custom textures enabled: {bUseCustomTex}");
            //if (bLog) Logger.Log(Logger.Level.Debug, "2");
            Equipment equipment = Inventory.main.equipment;
            if (equipment == null)
            {
                if (bLog) Logger.Log(Logger.Level.Error, $"Failed to get Equipment instance");
                return;
            }
            if (__instance == null)
            {
                if (bLog) Logger.Log(Logger.Level.Error, $"Failed to get Player instance");
                return;
            }

            //if (bLog) Logger.Log(Logger.Level.Debug, "3");
            if (__instance.equipmentModels == null)
            {
                if (bLog) Logger.Log(Logger.Level.Error, $"Failed to get equipmentModels member of Player instance");
                return;
            }

            GameObject playerModel = Player.main.gameObject;
            Shader shader = Shader.Find("MarmosetUBER");
            Renderer reinforcedGloves = playerModel.transform.Find("body/player_view/male_geo/reinforcedSuit/reinforced_suit_01_glove_geo").gameObject.GetComponent<Renderer>();
            if (defaultGloveMaterial == null)
            {
                if (reinforcedGloves != null)
                {
                    // Save a copy of the original material, for use later
                    if (bLog) Logger.Log(Logger.Level.Debug, "Found Reinforced Gloves shader and copying default material");
                    defaultGloveMaterial = new Material(reinforcedGloves.material);
                }
                else
                    if (bLog) Logger.Log(Logger.Level.Error, "ReinforcedGloves renderer not found while attempting to copy default material");
            }
            Renderer reinforcedSuit = playerModel.transform.Find("body/player_view/male_geo/reinforcedSuit/reinforced_suit_01_body_geo").gameObject.GetComponent<Renderer>();
            if (reinforcedSuit != null)
            {
                if (defaultSuitMaterial == null)
                {
                    // Save a copy of the original material, for use later
                    if (bLog) Logger.Log(Logger.Level.Debug, "Found Reinforced Suit shader and copying default material");
                    defaultSuitMaterial = new Material(reinforcedSuit.material);
                }
                if (defaultArmsMaterial == null)
                {
                    // Save a copy of the original material, for use later
                    if (bLog) Logger.Log(Logger.Level.Debug, "Found Reinforced Suit shader and copying default arm material");
                    defaultArmsMaterial = new Material(reinforcedSuit.materials[1]);
                }
            }
            else
                if (bLog) Logger.Log(Logger.Level.Error, "ReinforcedSuit renderer not found while attempting to copy default materials");

            foreach (Player.EquipmentType equipmentType in __instance.equipmentModels)
            {
                bool bChangeTex = false;
                bUseCustomTex = (Main.suitTexture != null && Main.glovesTexture != null);
                string activeSlot = equipmentType.slot;
                TechType techTypeInSlot = equipment.GetTechTypeInSlot(activeSlot);
                if (activeSlot == "Body")
                {
                    bChangeTex = (techTypeInSlot != lastBodyTechType);
                    lastBodyTechType = techTypeInSlot;
                    if (techTypeInSlot == Main.prefabSuitMk1.TechType
                        || (Main.prefabSuitMk2 != null && techTypeInSlot == Main.prefabSuitMk2.TechType)
                        || (Main.prefabSuitMk3 != null && techTypeInSlot == Main.prefabSuitMk3.TechType))
                    {
                        techTypeInSlot = TechType.ReinforcedDiveSuit;
                    }
                    else
                    {
                        bUseCustomTex = false;
                    }
                }
                else if (activeSlot == "Gloves")
                {
                    bChangeTex = (techTypeInSlot != lastGlovesTechType);
                    lastGlovesTechType = techTypeInSlot;
                    if (techTypeInSlot == Main.prefabGloves.TechType)
                        techTypeInSlot = TechType.ReinforcedGloves;
                    else
                        bUseCustomTex = false;
                }
                //else
                //    continue;

                bool flag = false;
                if (bLog) Logger.Log(Logger.Level.Debug, $"checking equipmentModels for TechType {techTypeInSlot.AsString(false)}");
                foreach (Player.EquipmentModel equipmentModel in equipmentType.equipment)
                {
                    //Player.EquipmentModel equipmentModel = equipmentType.equipment[j];
                    bool equipmentVisibility = (equipmentModel.techType == techTypeInSlot);
                    if (bChangeTex)
                    {
                        if (bLog) Logger.Log(Logger.Level.Debug, "Equipment changed, changing textures");
                        if (bUseCustomTex)
                        {
                            if (shader == null)
                            {
                                if (bLog) Logger.Log(Logger.Level.Debug, $"Shader is null, custom texture disabled");
                                bUseCustomTex = false;
                            }
                            else if (activeSlot == "Gloves" && reinforcedGloves == null)
                            {
                                if (bLog) Logger.Log(Logger.Level.Debug, $"reinforcedGloves is null, custom texture disabled");
                                bUseCustomTex = false;
                            }
                            else if (activeSlot == "Body" && reinforcedSuit == null)
                            {
                                if (bLog) Logger.Log(Logger.Level.Debug, $"reinforcedSuit is null, custom texture disabled");
                                bUseCustomTex = false;
                            }
                        }
                    }

                    flag = (flag || equipmentVisibility);
                    if (equipmentModel.model != null)
                    {
                        if (bChangeTex)
                        {
                            if (bUseCustomTex)
                            {
                                // Apply the Brine Suit texture
                                if (activeSlot == "Gloves")
                                {
                                    // if the gloves shader isn't null, add the shader
                                    if (reinforcedGloves != null // This shouldn't be necessary but I'm taking no chances
                                        && reinforcedGloves.material != null)
                                    {
                                        if (brineGloveMaterial == null)
                                            brineGloveMaterial = GetGloveMaterial(shader, defaultGloveMaterial);

                                        if (brineGloveMaterial != null)
                                            reinforcedGloves.material = brineGloveMaterial;
                                        else
                                        {
                                            if (bLog) Logger.Log(Logger.Level.Error, "Creation of new Brine glove material failed");
                                        }
                                    }
                                }
                                else if (activeSlot == "Body")
                                {
                                    if (reinforcedSuit != null
                                        && reinforcedSuit.material != null
                                        && reinforcedSuit.material.shader != null)
                                    {
                                        if (brineArmsMaterial == null)
                                        {
                                            if (bLog) Logger.Log(Logger.Level.Debug, "Creating new brineArmsMaterial");
                                            if (defaultArmsMaterial != null)
                                            {
                                                brineArmsMaterial = GetGloveMaterial(null, defaultArmsMaterial);

                                                /*brineArmsMaterial = new Material(defaultArmsMaterial);
                                                // add the suit's arms main Texture when equipped
                                                //if (bLog) Logger.Log(Logger.Level.Debug, $"add the suit's arms main Texture when equipped");
                                                brineArmsMaterial.mainTexture = Main.glovesTexture;
                                                // add the suit's arms spec Texture when equipped
                                                //if (bLog) Logger.Log(Logger.Level.Debug, $"add the suit's arms spec Texture when equipped");
                                                brineArmsMaterial.SetTexture(ShaderPropertyID._SpecTex, Main.glovesTexture);
                                                // add the suit's arms illum texture when equipped
                                                //if (bLog) Logger.Log(Logger.Level.Debug, $"add the suit's arms illum texture when equipped");
                                                brineArmsMaterial.SetTexture(ShaderPropertyID._Illum, Main.glovesIllumTexture);*/

                                            }
                                            else
                                                if (bLog) Logger.Log(Logger.Level.Error, "defaultArmsMaterial not set while trying to create new brineArmsMaterial");
                                        }

                                        if (brineArmsMaterial != null)
                                        {
                                            if (bLog) Logger.Log(Logger.Level.Debug, "Applying brineArmsMaterial");
                                            reinforcedSuit.materials[1] = brineArmsMaterial;
                                        }
                                        else
                                            if (bLog) Logger.Log(Logger.Level.Error, "Error generating brineArmsMaterial");


                                        if (brineSuitMaterial == null)
                                        {
                                            if (bLog) Logger.Log(Logger.Level.Debug, "Creating new brineSuitMaterial");
                                            if (defaultSuitMaterial != null)
                                            {
                                                brineSuitMaterial = new Material(defaultSuitMaterial);

                                                brineSuitMaterial.shader = shader;
                                                brineSuitMaterial.mainTexture = Main.suitTexture;
                                                // add the suit spec texture when equipped
                                                //if (bLog) Logger.Log(Logger.Level.Debug, $"add the suit spec texture when equipped");
                                                brineSuitMaterial.SetTexture(ShaderPropertyID._SpecTex, Main.suitTexture);
                                                // add  the suit illum Texture when equipped
                                                //if (bLog) Logger.Log(Logger.Level.Debug, $"add  the suit illum Texture when equipped");
                                                brineSuitMaterial.SetTexture(ShaderPropertyID._Illum, Main.suitIllumTexture);

                                                /*
                                                // add the suit's arms main Texture when equipped
                                                //if (bLog) Logger.Log(Logger.Level.Debug, $"add the suit's arms main Texture when equipped");
                                                reinforcedSuit.materials[1].mainTexture = Main.glovesTexture;
                                                // add the suit's arms spec Texture when equipped
                                                //if (bLog) Logger.Log(Logger.Level.Debug, $"add the suit's arms spec Texture when equipped");
                                                reinforcedSuit.materials[1].SetTexture(ShaderPropertyID._SpecTex, Main.glovesTexture);
                                                // add the suit's arms illum texture when equipped
                                                //if (bLog) Logger.Log(Logger.Level.Debug, $"add the suit's arms illum texture when equipped");
                                                reinforcedSuit.materials[1].SetTexture(ShaderPropertyID._Illum, Main.glovesIllumTexture);
                                                */
                                            }
                                            else
                                                if (bLog) Logger.Log(Logger.Level.Error, "defaultSuitMaterial not set while trying to create new brineSuitMaterial");
                                        }

                                        if (brineSuitMaterial != null)
                                        {
                                            if (bLog) Logger.Log(Logger.Level.Debug, "Applying brineSuitMaterial");
                                            reinforcedSuit.material = brineSuitMaterial;
                                            reinforcedSuit.materials[0] = brineSuitMaterial;
                                        }
                                        else
                                            if (bLog) Logger.Log(Logger.Level.Error, "Creation of new Brine Suit material failed");
                                    }
                                }
                            }
                            else
                            {
                                if (activeSlot == "Body")
                                {
                                    if (reinforcedSuit != null)
                                    {
                                        if (defaultSuitMaterial != null)
                                        {
                                            reinforcedSuit.material = defaultSuitMaterial;
                                            reinforcedSuit.materials[0] = defaultSuitMaterial;
                                        }
                                        else
                                            if (bLog) Logger.Log(Logger.Level.Error, "Could not restore default suit material; Default suit material not found");

                                        if (defaultArmsMaterial != null)
                                            reinforcedSuit.materials[1] = defaultArmsMaterial;
                                        else
                                            if (bLog) Logger.Log(Logger.Level.Error, "Could not restore default arms material; Default arms material not found");
                                    }
                                }
                                else if (activeSlot == "Gloves")
                                {
                                    if (reinforcedGloves != null)
                                    {
                                        if (defaultGloveMaterial != null)
                                            reinforcedGloves.material = defaultGloveMaterial;
                                        else
                                            if (bLog) Logger.Log(Logger.Level.Error, "Could not restore default glove material; Default glove material not found");
                                    }
                                }
                            }
                        }

                        equipmentModel.model.SetActive(equipmentVisibility);
                    }
                }
                if (equipmentType.defaultModel != null)
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
        /*
        [HarmonyPrefix]
        public static void Prefix(ref Player __instance)
        {
            //Logger.Log(Logger.Level.Debug, $"UpdateReinforcedSuitPatcher.Prefix begin:");
            foreach (string s in Main.playerSlots)
            {
                TechType tt = Inventory.main.equipment.GetTechTypeInSlot(s);
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Found TechType {tt.ToString()} in slot {s}"); 
#endif
                tt = Inventory.main.equipment.GetTechTypeInSlot(s);
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Found patched TechType {tt.ToString()} in slot {s}"); 
#endif
            }
        }
        */

        [HarmonyPostfix]
        public static void Postfix(ref Player __instance)
        {
            if (__instance != null)
            {
                int flags = 0;
                if (Main.EquipmentGetCount(Inventory.main.equipment, new TechType[] { Main.prefabSuitMk1.TechType, Main.prefabSuitMk2.TechType, Main.prefabSuitMk3.TechType }) > 0)
                {
                    flags += 1;
                    __instance.temperatureDamage.minDamageTemperature += 9f;
                }

                if (Inventory.main.equipment.GetCount(Main.prefabGloves.TechType) > 0)
                {
                    flags += 2;
                    __instance.temperatureDamage.minDamageTemperature += 1f;
                }

                if (Inventory.main.equipment.GetCount(Main.prefabHelmet.TechType) > 0)
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

            if (__instance != null)
            {
                // For debugging
                if (Main.EquipmentGetCount(Inventory.main.equipment, new TechType[] { Main.prefabSuitMk1.TechType, Main.prefabSuitMk2.TechType, Main.prefabSuitMk3.TechType }) > 0
                    && Inventory.main.equipment.GetCount(Main.prefabGloves.TechType) > 0
                    && Inventory.main.equipment.GetCount(Main.prefabHelmet.TechType) > 0)
                    return false;
            }

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
            __result = (__result || Main.EquipmentGetCount(Inventory.main.equipment, new TechType[] { Main.prefabSuitMk1.TechType, Main.prefabSuitMk2.TechType, Main.prefabSuitMk3.TechType }) > 0);
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
