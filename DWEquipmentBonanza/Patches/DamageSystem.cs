using Common;
using DWEquipmentBonanza;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CombinedItems.Patches
{
	[HarmonyPatch(typeof(DamageSystem))]
	internal class DamageSystem_CalculateDamage_Patch
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(DamageSystem.CalculateDamage))]
		public static float Postfix(float damage, DamageType type, GameObject target, GameObject dealer = null)
		{
			Log.LogDebug($"DamageSystem_CalculateDamage_Patch.Postfix executing: parameters (damage = {damage}, DamageType = {type})");

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

					// I could probably rewrite this with DamageModifier components, but I'm still not sure whether those actually work in SN1.
					// By default, DamageModifier components only modify a single DamageType, but all we'd have to do is add multiple DamageModifier components.
					// Or even code a custom DamageModifier.

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
}
