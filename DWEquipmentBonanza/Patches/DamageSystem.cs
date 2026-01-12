using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using DWEquipmentBonanza;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DWEquipmentBonanza.MonoBehaviours;

namespace DWEquipmentBonanza.Patches
{
	[HarmonyPatch(typeof(DamageSystem))]
	public class DamageSystemPatches
	{
		//public static GameObject lastCyclopsDamager { get; private set; }

		[HarmonyPatch(nameof(DamageSystem.CalculateDamage))]
#if SN1
		[HarmonyPatch(new[]
		{
			typeof(float), typeof(DamageType), typeof(GameObject), typeof(GameObject)
		})]
		[HarmonyPriority(Priority.Last)]
		[HarmonyPostfix]
		public static float PostCalculateDamage(
			float preResult,
			float damage,
			DamageType type,
			GameObject target,
			GameObject dealer = null)
#elif BELOWZERO
		[HarmonyPatch(new[]
		{
			typeof(TechType), typeof(float), typeof(float), typeof(DamageType), typeof(GameObject), typeof(GameObject)
		})]
		[HarmonyPostfix]
		public static float PostCalculateDamage(
			float preResult,
			TechType techType,
			float damageModifier,
			float damage,
			DamageType type,
			GameObject target,
			GameObject dealer = null)
#endif
		{
			TechType targetTT = target != null ? CraftData.GetTechType(target) : TechType.None;
			TechType dealerTT = dealer != null ? CraftData.GetTechType(dealer) : TechType.None;
			//Log.LogDebug($"DamageSystemPatches.PostCalculateDamage executing: parameters (damage = {damage}, DamageType = {type}, target = {targetTT.AsString()}, dealer = {dealerTT.AsString()}", null, true);

			float baseDamage = preResult;
			float newDamage = preResult;
			if (targetTT == TechType.Player)
			{

				if (type == DamageType.Acid)
				{
					// In vanilla, there is a bug where if a player is in acid, taking damage, and gets into a vehicle without leaving acid - the vehicle is within the acid too - they will continue to take damage.
					if (Player.main.GetVehicle() != null)
					{
						//Log.LogDebug("Player in vehicle, negating damage");
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

				Equipment equipment = Inventory.Get().equipment;
				foreach (string s in Main.playerSlots)
				{
					TechType techTypeInSlot = equipment.GetTechTypeInSlot(s);
					float damageMod = Main.ModifyDamage(techTypeInSlot, baseDamage, type);
					newDamage -= damageMod;
				}



				// This is to cover the instances where the player is in acid, gets in a vehicle - sound is stopped by above - and then gets out again.
				// It needs to run after damage is modified in order to determine whether or not the player is taking acid damage; if they are, the acid sound needs to be played again
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

				var shields = target.GetComponentsInChildren<HazardShieldComponent>(true);
				//Log.LogDebug($"DamageSystemPatches.PostCalculateDamage found {shields.Length}x Hazard Shield components");

				for (int i = 0; i < shields.Length; i++)
				//foreach(var shield in target.GetComponentsInChildren<HazardShieldComponent>(true))
				{
					var shield = shields[i];
					float absorbed = shield.AbsorbDamage(newDamage, type);
					newDamage -= absorbed;
					//Log.LogDebug($"DamageSystemPatches.PostCalculateDamage; shield chip {i} absorbed {absorbed} damage");
				}
			}
			else if (dealerTT == TechType.Player && type == DamageType.Pressure)
			{
				damage *= VibrobladeBehaviour.FaunaDamageMultiplier;

			}

			newDamage = System.Math.Max(newDamage, 0f);
			/*if (target == Player.mainObject && shieldedDamageType.Contains(type))
			{
				ErrorMessage.AddMessage($"Damage taken: Type {type.ToString()}, unmodified amount {damage}, modified amount {newDamage}");
				return 0f;
			}*/

			return newDamage;
		}
	}
}
