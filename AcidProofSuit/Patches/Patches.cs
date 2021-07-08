using HarmonyLib;
using SMLHelper.V2.Utility;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace AcidProofSuit.Patches
{
	[HarmonyPatch(typeof(Equipment))]
	internal class EquipmentPatches
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
		// If we were able to use a dictionary then ContainsKey() would do the trick, but since we can't...
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
		[HarmonyPatch(nameof(Equipment.GetCount))]
		public static void PostGetCount(ref Equipment __instance, ref int __result, TechType techType)
		{
			if (!substitutionTargets.Contains(techType))
				return; // No need to do anything more.
			Dictionary<TechType, int> equipCount = __instance.GetInstanceField("equippedCount", BindingFlags.NonPublic | BindingFlags.Instance) as Dictionary<TechType, int>;
			// equipCount.TryGetValue(techType, out result);

			//Logger.Log(Logger.Level.Debug, $"EquipmentPatches.PostFix: executing with parameters __result {__result.ToString()}, techType {techType.ToString()}");
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
						//Logger.Log(Logger.Level.Debug, $"EquipmentPatches: found {techType.ToString()} equipped");
						__result++;
						break;
					}
				}
				//}
			}
		}
	}

	[HarmonyPatch(typeof(DamageSystem))]
	internal class DamageSystem_CalculateDamage_Patch
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(DamageSystem.CalculateDamage))]
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
