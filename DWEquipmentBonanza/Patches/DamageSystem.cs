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
	internal class DamageSystemPatches
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(DamageSystem.CalculateDamage))]
		public static float PostCalculateDamage(float damage, DamageType type, GameObject target, GameObject dealer = null)
		{
			TechType targetTT = target != null ? CraftData.GetTechType(target) : TechType.None;
			TechType dealerTT = dealer != null ? CraftData.GetTechType(dealer) : TechType.None;
			//Log.LogDebug($"DamageSystemPatches.PostCalculateDamage executing: parameters (damage = {damage}, DamageType = {type}, target = {targetTT.AsString()}, dealer = {dealerTT.AsString()}", null, true);

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

				}
				// I could probably rewrite this with DamageModifier components, but I'm still not sure whether those actually work in SN1.
				// By default, DamageModifier components only modify a single DamageType, but all we'd have to do is add multiple DamageModifier components.
				// Or even code a custom DamageModifier.
				// Plus, using DamageModifiers carries the disadvantage that no combination of them could reduce damage all the way to zero, unless one of them actually set the damage to zero.
				// This makes it insufficient for the Brine Suit, which has multiple components that all reduce the final damage by a fixed proportion of the original damage.
				// There's still a workaround for this, in that a fourth DamageModifier could be added/removed when the set is completed/broken, one that does set acid damage to zero.

				Equipment equipment = Inventory.main.equipment;
				Player __instance = Player.main;
				foreach (string s in Main.playerSlots)
				{
					TechType techTypeInSlot = equipment.GetTechTypeInSlot(s);
					float damageMod = Main.ModifyDamage(techTypeInSlot, baseDamage, type);
					newDamage -= damageMod;
				}

				// This is to cover the instances where the player is in acid, gets in a vehicle - sound is stopped by above - and then gets out again.
				if (type == DamageType.Acid)
				{
					if (newDamage > 0f)
					{
						if (!Player.main.acidLoopingSound.playing)
							Player.main.acidLoopingSound.Start();
					}
					else if (Player.main.acidLoopingSound.playing)
						Player.main.acidLoopingSound.Stop();
				}
			}

			return System.Math.Max(newDamage, 0f);
		}
	}
}
