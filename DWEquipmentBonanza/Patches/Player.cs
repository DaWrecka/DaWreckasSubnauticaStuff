using Common;
using HarmonyLib;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace DWEquipmentBonanza.Patches
{
    [HarmonyPatch(typeof(Player))]
	internal class PlayerPatch
    {
		private static HashSet<TechType> SurvivalSuits = new HashSet<TechType>();
		private static Dictionary<TechType, TechType> DisplaySubstitutions = new Dictionary<TechType, TechType>();
		public static bool bHasSurvivalSuit { get; private set; }

        // Original, unmodified materials.
#if SUBNAUTICA_STABLE
        private static Material defaultGloveMaterial;
        private static Material defaultSuitMaterial;
		private static Material defaultArmsMaterial;
		private static Material brineGloveMaterial;
		private static Material brineSuitMaterial;
		private static Material brineArmsMaterial;
		private static TechType lastBodyTechType = TechType.None;
		private static TechType lastGlovesTechType = TechType.None;
		private static TechType ttGloves => TechTypeUtils.GetModTechType("AcidGloves");
		private static TechType ttHelmet => TechTypeUtils.GetModTechType("AcidHelmet");
        private static TechType ttSuit => TechTypeUtils.GetModTechType("AcidSuit");
        private static TechType ttSuitMk2 => TechTypeUtils.GetModTechType("NitrogenBrineSuit2");
        private static TechType ttSuitMk3 => TechTypeUtils.GetModTechType("NitrogenBrineSuit3");
        private static TechType ttSuperSuit => TechTypeUtils.GetModTechType("SuperSurvivalSuit");
#endif
        internal static float oldMaxTemp;

        [HarmonyPrefix]
        [HarmonyPatch("UpdateReinforcedSuit")]
        public static bool Prefix(ref Player __instance)
        {
#if SUBNAUTICA_STABLE
            if (Main.bUseNitrogenAPI)
                return true;
#endif
            oldMaxTemp = __instance.temperatureDamage.minDamageTemperature;

            __instance.temperatureDamage.minDamageTemperature = 49f;
            float minTempBonus = 0f;
            foreach(string slot in new List<string> { "Body", "Gloves", "Head" })
            {
                TechType bodySlot = Inventory.main.equipment.GetTechTypeInSlot(slot);
                minTempBonus += Main.GetTempBonusForTechType(bodySlot);
            }

            __instance.temperatureDamage.minDamageTemperature += minTempBonus;

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateReinforcedSuit")]
        public static void Postfix(ref Player __instance)
        {
            float maxTemp = __instance.temperatureDamage.minDamageTemperature;
            if (maxTemp != oldMaxTemp)
                ErrorMessage.AddMessage($"Maximum safe water temperature now {maxTemp.ToString()}");
        }

#if SUBNAUTICA_STABLE
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
#endif

        // Used by our Player.EquipmentChanged patch.
        // If TechType key is equipped, search instead for TechType value
        // so for example if key is prefabReinforcedColdSuit.TechType, value will be TechType.ColdSuit
        // This should mean that when the ReinforcedColdSuit is equipped, then the Cold Suit graphics will be displayed.
        public static void AddSubstitution(TechType custom, TechType vanilla, bool bUpdate = false)
		{
			//Logger.Log(Logger.Level.Debug, $"Adding substitution: custom TechType {custom.AsString()}, for vanilla {vanilla.AsString()}");
			if (DisplaySubstitutions.ContainsKey(custom))
			{
				if(bUpdate)
					DisplaySubstitutions[custom] = vanilla;
			}
			else
				DisplaySubstitutions.Add(custom, vanilla);
		}

		public static void AddSurvivalSuit(TechType suit)
		{
			//Log.LogDebug($"AddSurvivalSuit: called with TechType {suit.AsString()}");
			if (!SurvivalSuits.Contains(suit))
				SurvivalSuits.Add(suit);
		}

		// Because of our patch to Equipment.GetCount, {Inventory.main.equipment.GetCount(TechType.ReinforcedDiveSuit)} will return a value greater than zero if the Reinforced Cold Suit is equipped.
		[HarmonyPrefix]
        [HarmonyPatch(nameof(Player.HasReinforcedSuit))]
        public static bool PreHasReinforcedSuit(ref bool __result)
        {
            __result = (Inventory.main.equipment.GetCount(TechType.ReinforcedDiveSuit) > 0);
			return false;
        }

        // This, too, exploits our patch to Equipment.GetCount.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.HasReinforcedGloves))]
        public static bool PreHasReinforcedGloves(ref bool __result)
        {
            __result = (Inventory.main.equipment.GetCount(TechType.ReinforcedGloves) > 0);
			return false;
        }

		public static TechType CheckSubstitute(TechType vanilla)
		{
            /*if (Main.bVerboseLogging)
				Logger.Log(Logger.Level.Debug, $"CheckSubstitute: Checking for substitute for TechType {vanilla.AsString()}");*/
            /*if (DisplaySubstitutions.TryGetValue(vanilla, out TechType value))
			{
				//if (Main.bVerboseLogging)
				//	Logger.Log(Logger.Level.Debug, $"Found substitute TechType.{kvp.Key.AsString()}");
				return value;
			}

			//if (Main.bVerboseLogging)
			//	Logger.Log(Logger.Level.Debug, $"No substitute found for TechType ${vanilla.AsString()}");
			return vanilla;*/
            return DisplaySubstitutions.GetOrDefault(vanilla, vanilla); // Return the value in the dictionary for the key matching vanilla, or return vanilla
		}

#if SUBNAUTICA_STABLE
        [HarmonyPostfix]
        [HarmonyPatch("EquipmentChanged")]
        public static void PostEquipmentChanged(ref Player __instance, string slot, InventoryItem item)
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
                TechType techTypeInSlot = equipment.GetTechTypeInSlot(equipmentType.slot);

                if (activeSlot == "Body")
                {
                    bHasSurvivalSuit = SurvivalSuits.Contains(techTypeInSlot);
                    Log.LogDebug($"PlayerPatches.EquipmentChanged(): TechType in slot Body = '{techTypeInSlot}', bHasSurvivalSuit = {bHasSurvivalSuit}");
                    bChangeTex = (techTypeInSlot != lastBodyTechType);
                    techTypeInSlot = CheckSubstitute(techTypeInSlot);
                    lastBodyTechType = techTypeInSlot;
                    if (techTypeInSlot == ttSuit
                        || (TechTypeUtils.TryGetModTechType("NitrogenBrineSuit2", out TechType tt2) && techTypeInSlot == tt2)
                        || (TechTypeUtils.TryGetModTechType("NitrogenBrineSuit3", out TechType tt3) && techTypeInSlot == tt3))
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
                    if (techTypeInSlot == ttGloves)
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
                                // Yeah this could be a switch, but there's only two options and they're a little easier to read this way
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

        [HarmonyPrefix]
        [HarmonyPatch("OnAcidEnter")]
        public static bool PreOnAcidEnter(ref Player __instance)
        {
            //Logger.Log(Logger.Level.Debug, "Player entered acid");

            Main.bInAcid = true;

            if (__instance != null)
            {
                // For debugging
                if (Main.EquipmentGetCount(Inventory.main.equipment, new TechType[] { ttSuit, ttSuitMk2, ttSuitMk3, ttSuperSuit }) > 0
                    && Inventory.main.equipment.GetCount(ttGloves) > 0
                    && Inventory.main.equipment.GetCount(ttHelmet) > 0)
                    return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnAcidExit")]
        public static void PostOnAcidExit(ref Player __instance)
        {
            //Logger.Log(Logger.Level.Debug, "Player exited acid");

            Main.bInAcid = false;
        }

#elif BELOWZERO
        [HarmonyPrefix]
        [HarmonyPatch("EquipmentChanged")]
		public static bool PlayerEquipmentChanged(ref Player __instance, string slot, InventoryItem item)
		{
			/*if (Main.bVerboseLogging)
			{
				Logger.Log(Logger.Level.Debug, $"PlayerEquipmentChanged() start");
			}*/
			Equipment equipment = Inventory.main.equipment;
            foreach(Player.EquipmentType equipmentType in __instance.equipmentModels)
			{
				bool bIsUnderwaterOrNotFlipper = equipmentType.slot != "Foots" || __instance.isUnderwater.value;
				TechType techTypeInSlot = equipment.GetTechTypeInSlot(equipmentType.slot);

				if (equipmentType.slot == "Body")
				{
					bHasSurvivalSuit = SurvivalSuits.Contains(techTypeInSlot);
					//Log.LogDebug($"PlayerPatches.EquipmentChanged(): TechType in slot Body = '{techTypeInSlot}', bHasSurvivalSuit = {bHasSurvivalSuit}");
				}
				techTypeInSlot = CheckSubstitute(techTypeInSlot);
				bool flag2 = false;
				GameObject y = null;
				int count = equipmentType.equipment.Length;
				for(int j = 0;  j < count; j++)
				{
					Player.EquipmentModel equipmentModel = equipmentType.equipment[j];
					/*if (Main.bVerboseLogging)
					{
						Logger.Log(Logger.Level.Debug, $"equipmentModel at index {j} has techType {equipmentModel.techType}");
					}*/
					bool bShowEquipped = equipmentModel.techType == techTypeInSlot && bIsUnderwaterOrNotFlipper;
					if (equipmentModel.model)
					{
						flag2 = (flag2 || bShowEquipped);
						if (bShowEquipped)
						{
							equipmentModel.model.SetActive(true);
							y = equipmentModel.model;
							if (equipmentType.defaultModel)
							{
								equipmentType.defaultModel.SetActive(equipmentModel.enableDefaultModelWhenEquipped);
							}
						}
						else if (equipmentModel.model != y)
						{
							equipmentModel.model.SetActive(false);
						}
					}
				}
				if (equipmentType.defaultModel && !flag2)
				{
					equipmentType.defaultModel.SetActive(true);
				}
			}
			Reflection.PlayerUpdateReinforcedSuit(__instance);
			Reflection.PlayerCheckColdsuitGoal(__instance);
			return false;
		}
#endif
    }
}
