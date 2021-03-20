using HarmonyLib;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace CombinedItems.Patches
{
	[HarmonyPatch(typeof(Player))]

	internal class PlayerPatch
    {
		private static Dictionary<TechType, TechType> DisplaySubstitutions = new Dictionary<TechType, TechType>();
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
			foreach (KeyValuePair<TechType, TechType> kvp in DisplaySubstitutions)
			{
				if (vanilla == kvp.Key)
				{
					/*if (Main.bVerboseLogging)
						Logger.Log(Logger.Level.Debug, $"Found substitute TechType.{kvp.Key.AsString()}");*/
					return kvp.Value;
				}
			}

			/*if (Main.bVerboseLogging)
				Logger.Log(Logger.Level.Debug, $"No substitute found for TechType ${vanilla.AsString()}");*/
			return vanilla;
		}

		[HarmonyPrefix]
        [HarmonyPatch("EquipmentChanged")]
		public static bool PlayerEquipmentChanged(ref Player __instance, string slot, InventoryItem item)
		{
			/*if (Main.bVerboseLogging)
			{
				Logger.Log(Logger.Level.Debug, $"PlayerEquipmentChanged() start");
			}*/
			Equipment equipment = Inventory.main.equipment;
			int num = __instance.equipmentModels.Length;
			for(int i = 0; i < num; i++)
			{
				Player.EquipmentType equipmentType = __instance.equipmentModels[i];
				bool bIsUnderwaterOrNotFlipper = equipmentType.slot != "Foots" || __instance.isUnderwater.value;
				TechType techTypeInSlot = CheckSubstitute(equipment.GetTechTypeInSlot(equipmentType.slot));

				bool flag2 = false;
				GameObject y = null;
				int num2 = equipmentType.equipment.Length;
				for(int j = 0;  j < num2; j++)
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
	}
}
