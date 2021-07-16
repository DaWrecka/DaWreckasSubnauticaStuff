using DWEquipmentBonanza.MonoBehaviours;
using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.Patches
{
	[HarmonyPatch(typeof(Survival))]
	internal class SurvivalPatches
	{
		/*private const float defaultSurvivalCap = 100f;

		private static Dictionary<TechType, float> NeedsCapOverrides = new Dictionary<TechType, float>()
		{
			{ TechType.None, defaultSurvivalCap },
			{ TechType.ColdSuit, defaultSurvivalCap },
			{ TechType.ReinforcedDiveSuit, defaultSurvivalCap },
			{ TechType.Stillsuit, defaultSurvivalCap }
		}; // Dictionary using suit TechTypes as keys; if the worn suit is present in the dictionary, then we override the water cap with the associated value.
		private static TechType cachedSuitType; // if the equipped body suit is this, we don't need to check the dictionary.
		private static float cachedOverride = defaultSurvivalCap;

		public static float GetPrimaryNeedsCap()
		{
			TechType equippedSuit = Inventory.main.equipment.GetTechTypeInSlot("Body");

			//Log.LogDebug($"SurvivalPatches.GetPrimaryNeedsCap: found equipped TechType of {equippedSuit.ToString()}");
			if (equippedSuit != cachedSuitType)
			{
				cachedSuitType = equippedSuit;

				if (NeedsCapOverrides.TryGetValue(equippedSuit, out float value))
				{
					cachedOverride = value;
					Log.LogDebug($"SurvivalPatches.GetPrimaryNeedsCap: Using override value of {cachedOverride} for TechType of {equippedSuit.ToString()}");
				}
				else
				{
					cachedOverride = defaultSurvivalCap;
					Log.LogDebug($"SurvivalPatches.GetPrimaryNeedsCap: Couldn't find override value for TechType of {equippedSuit.ToString()}, using {defaultSurvivalCap}");
				}
			}

			return cachedOverride;
		}

		internal static void AddNeedsCapOverride(TechType suit, float value)
		{
			//Log.LogDebug($"SurvivalPatches.AddNeedsCapOverride: Adding override of value {value} for TechType {suit.AsString()}");
			NeedsCapOverrides[suit] = value;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(nameof(Survival.UpdateStats))]
		public static IEnumerable<CodeInstruction> UpdateStatsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo getPrimaryNeedsOverride = typeof(SurvivalPatches).GetMethod(nameof(SurvivalPatches.GetPrimaryNeedsCap));

			int i;
			int maxIndex = codes.Count - 8;

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Survival.UpdateStats(), pre-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}

			i = -1;
			while (++i < maxIndex)
			{
				//Our target pattern is as follows:
				//	IL_008F: ldarg.0
				//	IL_0090: ldarg.0
				//	IL_0091: ldfld     float32 Survival::water
				//	IL_0096: ldloc.s   V_4
				//	IL_0098: sub
				//	IL_0099: ldc.r4    0.0
				//	IL_009E: ldc.r4    100
				//	IL_00A3: call      float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Clamp(float32, float32, float32)
				//	IL_00A8: stfld     float32 Survival::water

				//That 'ldc.r4 100' is what we want to change; it loads 100 on the stack as a constant, which is the 'maximum' value passed to Clamp. We want, instead, to put our value on the stack.
				
				if (codes[i].opcode == OpCodes.Ldarg_0
					&& codes[i + 1].opcode == OpCodes.Ldarg_0
					&& codes[i + 2].opcode == OpCodes.Ldfld
					&& codes[i + 3].opcode == OpCodes.Ldloc_S
					&& codes[i + 4].opcode == OpCodes.Sub
					&& codes[i + 5].opcode == OpCodes.Ldc_R4
					&& codes[i + 6].opcode == OpCodes.Ldc_R4 && (float)codes[i + 6].operand == 100f
					&& codes[i + 7].opcode == OpCodes.Call
					&& codes[i + 8].opcode == OpCodes.Stfld)
				{
					codes[i + 6] = new CodeInstruction(OpCodes.Callvirt, getPrimaryNeedsOverride);
					break;
				}
			}

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Survival.UpdateStats(), post-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}
			return codes.AsEnumerable();
		}

		[HarmonyTranspiler]
		[HarmonyPatch(nameof(Survival.Eat))]
		public static IEnumerable<CodeInstruction> SurvivalEatTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo getPrimaryNeedsOverride = typeof(SurvivalPatches).GetMethod(nameof(SurvivalPatches.GetPrimaryNeedsCap));

			int i;
			int maxIndex = codes.Count - 8;

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Survival.Eat(), pre-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}

			i = -1;
			while (++i < maxIndex)
			{
				//Our target pattern is as follows:
				//	IL_00C5: ldarg.0
				//	IL_00C6: ldarg.0
				//	IL_00C7: ldfld     float32 Survival::water
				//	IL_00CC: ldloc.2
				//	IL_00CD: callvirt  instance float32 Eatable::GetWaterValue()
				//	IL_00D2: add
				//	IL_00D3: ldc.r4    0.0
				//	IL_00D8: ldc.r4    100
				//	IL_00DD: call      float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Clamp(float32, float32, float32)
				//	IL_00E2: stfld     float32 Survival::water

				//As with UpdateStats, the 'ldc.r4 100' is loading a value of 100 on the stack as the 'max' parameter of Clamp. We want to swap this for our method.
				if (codes[i].opcode == OpCodes.Ldarg_0
					&& codes[i + 1].opcode == OpCodes.Ldarg_0
					&& codes[i + 2].opcode == OpCodes.Ldfld
					&& codes[i + 3].opcode == OpCodes.Ldloc_2
					&& codes[i + 4].opcode == OpCodes.Callvirt
					&& codes[i + 5].opcode == OpCodes.Add
					&& codes[i + 6].opcode == OpCodes.Ldc_R4
					&& codes[i + 7].opcode == OpCodes.Ldc_R4 && (float)codes[i + 7].operand == 100f
					&& (codes[i + 8].opcode == OpCodes.Call || codes[i+8].opcode == OpCodes.Callvirt))
				{
					codes[i + 7] = new CodeInstruction(OpCodes.Callvirt, getPrimaryNeedsOverride);
					break;
				}
			}

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Survival.Eat(), post-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}
			return codes.AsEnumerable();
		}*/

		private static float preHunger;
		private static float preWater;

		[HarmonyPrefix]
		[HarmonyPatch("UpdateStats")]
		internal static void PreUpdateStats(Survival __instance, float timePassed)
		{
			bool bHasSurvivalSuit = PlayerPatch.bHasSurvivalSuit;
			if (bHasSurvivalSuit && GameModeUtils.RequiresSurvival() && !Player.main.IsFrozenStats())
			{
				preHunger = __instance.food;
				preWater = __instance.water;
				float regenRate = SurvivalsuitBehaviour.SurvivalRegenRate;
				float kMaxStat = SurvivalConstants.kMaxStat;
				/*float kFoodTime = SurvivalConstants.kFoodTime;
				float kWaterTime = SurvivalConstants.kWaterTime;
				Log.LogDebug($"SurvivalPatches.PreUpdateStats: food = {__instance.food}, water = {__instance.water}, bHasSurvivalSuit = {bHasSurvivalSuit}\nkFoodTime = {kFoodTime}, kWaterTime = {kWaterTime}, kMaxStat = {kMaxStat}, regenRate = {regenRate}");*/

				// now we can calculate the current calorie/water consumption rates and calibrate based on those.
				// Assuming the buggers at UWE don't change the algorithm.

				float foodRestore = (timePassed / SurvivalConstants.kFoodTime * kMaxStat) * regenRate;
				float waterRestore = (timePassed / SurvivalConstants.kWaterTime * kMaxStat) * regenRate;
				__instance.food = __instance.food + foodRestore;
				__instance.water = __instance.water + waterRestore;
				//Log.LogDebug($"SurvivalPatches.PreUpdateStats: done running Survival Suit routine; food = {__instance.food}, water = {__instance.water}, foodRestore = {foodRestore}, waterRestore = {waterRestore}");
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch("UpdateStats")]
		internal static void PostUpdateStats(Survival __instance)
		{
			float foodDelta = preHunger - __instance.food;
			float waterDelta = preWater - __instance.water;
			Log.LogDebug($"SurvivalPatches.PostUpdateStats: food = {__instance.food}, water = {__instance.water}, foodDelta = {foodDelta}, waterDelta = {waterDelta}");
		}
	}
}
