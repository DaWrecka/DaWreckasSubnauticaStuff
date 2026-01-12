//#define LOGTRANSPILERS
using Common;
using DWEquipmentBonanza.MonoBehaviours;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static VoxelandChunk;

namespace DWEquipmentBonanza.Patches
{
	[HarmonyPatch(typeof(Survival))]
	internal class SurvivalPatches
	{
		private static float defaultMultiplier = 100f;

		private static Dictionary<TechType, float> NeedsOverrides = new Dictionary<TechType, float>(); // Dictionary using suit TechTypes as keys; if the worn suit is present in the dictionary, then we override the water cap with the associated value.
		private static TechType cachedSuitType = TechType.None; // if the equipped body suit is this, we don't need to check the dictionary.
		private static float cachedOverride = defaultMultiplier;

		public static float GetPrimaryNeedsMult()
		{
			TechType equippedSuit = Inventory.main.equipment.GetTechTypeInSlot("Body");

			//Log.LogDebug($"SurvivalPatches.GetPrimaryNeedsCap: found equipped TechType of {equippedSuit.ToString()}");
			if (equippedSuit != cachedSuitType)
			{
				cachedSuitType = equippedSuit;

				/*if (NeedsOverrides.TryGetValue(equippedSuit, out float value))
				{
					cachedOverride = value;
					Log.LogDebug($"SurvivalPatches.GetPrimaryNeedsCap: Using override value of {cachedOverride} for TechType of {equippedSuit.ToString()}");
				}
				else
				{
					cachedOverride = defaultMultiplier;
					Log.LogDebug($"SurvivalPatches.GetPrimaryNeedsCap: Couldn't find override value for TechType of {equippedSuit.ToString()}, using {defaultMultiplier}");
				}*/
				cachedOverride = NeedsOverrides.GetOrDefault(equippedSuit, defaultMultiplier);
			}

			return cachedOverride;
		}

		internal static void AddNeedsTimeOverride(TechType suit, float value)
		{
			Log.LogDebug($"SurvivalPatches.AddNeedsCapOverride: Adding override of value {value} for TechType {suit.AsString()}");
			NeedsOverrides[suit] = value;
		}

		// Updating of stats is done in a different way in SN1 and BZ; SN1 uses a single method, BZ uses two
#if SN1
		[HarmonyTranspiler]
		[HarmonyPatch(nameof(Survival.UpdateStats))]
		public static IEnumerable<CodeInstruction> UpdateStatsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo getPrimaryNeedsOverride = typeof(SurvivalPatches).GetMethod(nameof(SurvivalPatches.GetPrimaryNeedsMult));

			int i;
			int maxIndex = codes.Count - 8;

	#if LOGTRANSPILERS
			Log.LogDebug("Survival.UpdateStats(), pre-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
	#endif

			i = -1;
			while (++i < maxIndex)
			{
				//Our first target pattern is as follows:
				//IL_001f: ldarg.1      // timePassed
				//IL_0020: ldc.r4       2520
				//IL_0025: div
				//IL_0026: ldc.r4       100
				//IL_002b: mul
				//IL_002c: stloc.3      // num2


				//The second pattern is:
				//IL_0064: ldarg.1      // timePassed
				//IL_0065: ldc.r4       1800
				//IL_006a: div
				//IL_006b: ldc.r4       100
				//IL_0070: mul
				//IL_0071: stloc.s      num3

				//That 'ldc.r4 100' is what we want to change; it loads 100 on the stack as a constant, which is the 'maximum' value passed to Clamp. We want, instead, to put our value on the stack.

				// The lines of C# where this value is used are:
				//float num2 = (float)((double)timePassed / 2520.0 * 100.0);
				//[...]
				//float num3 = (float)((double)timePassed / 1800.0 * 100.0);
				// num2 and num3 are the amounts that should be subtracted from the food and water values; by replacing them with a lower value, we reduce the rate at which primary needs deplete.

				#if LOGTRANSPILERS
				/*
				Log.LogDebug(String.Format("0x{0:X4}", i));
				if (codes[i].opcode == OpCodes.Ldarg_1
					&& codes[i + 1].opcode == OpCodes.Ldc_R4)
				{
					Log.LogDebug("	codes[i + 1].operand == " + Convert.ToDouble(codes[i + 1].operand).ToString());
				}*/
				#endif
				if (codes[i].opcode == OpCodes.Ldarg_1
					&& codes[i + 1].opcode == OpCodes.Ldc_R4 && ((double)(codes[i + 1].operand) == 2520f || (double)(codes[i + 1].operand) == 1800f)
					&& codes[i + 2].opcode == OpCodes.Div
					&& codes[i + 3].opcode == OpCodes.Ldc_R4 && (float)(codes[i + 3].operand) == 100f
					&& codes[i + 4].opcode == OpCodes.Mul)
				{
					#if LOGTRANSPILERS
						Log.LogDebug("Code found at index " + String.Format("0x{0:X4}", i) + ", replacing");
					#endif
					codes[i + 3] = new CodeInstruction(OpCodes.Callvirt, getPrimaryNeedsOverride);
				}
			}

	#if LOGTRANSPILERS
			Log.LogDebug("Survival.UpdateStats(), post-transpiler:");
			for (i = 0; i < codes.Count; i++)
				Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
	#endif
				
			return codes.AsEnumerable();
		}
#elif BELOWZERO

		[HarmonyTranspiler]
		[HarmonyPatch(nameof(Survival.UpdateFoodStats))]
		public static IEnumerable<CodeInstruction> UpdateFoodStatsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo getPrimaryNeedsOverride = typeof(SurvivalPatches).GetMethod(nameof(SurvivalPatches.GetPrimaryNeedsMult));

			int i;
			int maxIndex = codes.Count - 4;
#if LOGTRANSPILERS
			Log.LogDebug("Survival.UpdateFoodStatsTranspiler(): getPrimaryNeedsOverride " + (getPrimaryNeedsOverride == null ? "is" : "is not") + " null; maxIndex = 0x" + String.Format("0x{0:X4}", maxIndex), null, true);
			Log.LogDebug("Survival.UpdateFoodStatsTranspiler(), pre-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
#endif
			i = 0;
			while (++i < maxIndex)
			{
#if LOGTRANSPILERS
				Log.LogDebug(String.Format("0x{0:X4}", i));
#endif
				//Our food target pattern is as follows:
				//IL_001c: stloc.1      // food
				//IL_001d: ldarg.1      // timePassed
				//IL_001e: ldc.r4       2520
				//IL_0023: div
				//IL_0024: ldc.r4       100
				//IL_0029: mul
				//IL_002a: stloc.2      // num2				//That 'ldc.r4 100' is what we want to change; it loads 100 on the stack as a constant, which is the 'maximum' value passed to Clamp. We want, instead, to put our value on the stack.

				// The lines of C# where this value is used are:
				//float num2 = (float)((double)timePassed / 2520.0 * 100.0);
				// num2 is the amount that should be subtracted from the food value; by replacing it with a lower value, we reduce the rate at which primary needs deplete.


				if (codes[i-1].opcode == OpCodes.Stloc_1
					&& codes[i].opcode == OpCodes.Ldarg_1
					&& codes[i + 1].opcode == OpCodes.Ldc_R4 && ((double)codes[i + 1].operand == 2520f || (double)codes[i + 1].operand == 1800f)
					&& codes[i + 2].opcode == OpCodes.Div
					&& codes[i + 3].opcode == OpCodes.Ldc_R4 && (float)codes[i + 3].operand == 100f
					&& codes[i + 4].opcode == OpCodes.Mul)
				{
					codes[i + 3] = new CodeInstruction(OpCodes.Callvirt, getPrimaryNeedsOverride);
					break;
				}
			}

#if LOGTRANSPILERS
			Log.LogDebug("Survival.UpdateFoodStatsTranspiler(), post-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
#endif
			return codes.AsEnumerable();
		}

		[HarmonyTranspiler]
		[HarmonyPatch(nameof(Survival.UpdateWaterStats))]
		public static IEnumerable<CodeInstruction> UpdateWaterStatsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo getPrimaryNeedsOverride = typeof(SurvivalPatches).GetMethod(nameof(SurvivalPatches.GetPrimaryNeedsMult));

			int i;
			int maxIndex = codes.Count - 4;

#if LOGTRANSPILERS
			Log.LogDebug($"Survival.UpdateWaterStatsTranspiler(): getPrimaryNeedsOverride " + (getPrimaryNeedsOverride == null ? "is" : "is not") + " null");
			Log.LogDebug("Survival.UpdateWaterStatsTranspiler(), pre-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
#endif

			i = 0;
			while (++i < maxIndex)
			{
				//Our water target pattern is as follows:
				//IL_001c: stloc.1      // water
				//IL_001d: ldarg.1      // timePassed
				//IL_001e: ldc.r4       1800
				//IL_0023: div
				//IL_0024: ldc.r4       100
				//IL_0029: mul
				//IL_002a: stloc.2      // num2
				//That 'ldc.r4 100' is what we want to change; it loads 100 on the stack as a constant, which is the 'maximum' value passed to Clamp. We want, instead, to put our value on the stack.

				// The lines of C# where this value is used are:
				//float num2 = (float)((double)timePassed / 2520.0 * 100.0);
				//[...]
				//float num3 = (float)((double)timePassed / 1800.0 * 100.0);
				// num2 and num3 are the amounts that should be subtracted from the food and water values; by replacing them with a lower value, we reduce the rate at which primary needs deplete.


				if (codes[i - 1].opcode == OpCodes.Stloc_1
					&& codes[i].opcode == OpCodes.Ldarg_1
					&& codes[i + 1].opcode == OpCodes.Ldc_R4// && (double)codes[i + 1].operand == 1800f
					&& codes[i + 2].opcode == OpCodes.Div
					&& codes[i + 3].opcode == OpCodes.Ldc_R4 && (float)codes[i + 3].operand == 100f
					&& codes[i + 4].opcode == OpCodes.Mul)
				{
					codes[i + 3] = new CodeInstruction(OpCodes.Callvirt, getPrimaryNeedsOverride);
					break;
				}
			}

#if LOGTRANSPILERS
			Log.LogDebug("Survival.UpdateWaterStatsTranspiler(), post-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
#endif
			return codes.AsEnumerable();
		}
#endif

		/*[HarmonyTranspiler]
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
				//	IL_00C7: ldfld	 float32 Survival::water
				//	IL_00CC: ldloc.2
				//	IL_00CD: callvirt  instance float32 Eatable::GetWaterValue()
				//	IL_00D2: add
				//	IL_00D3: ldc.r4	0.0
				//	IL_00D8: ldc.r4	100
				//	IL_00DD: call	  float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Clamp(float32, float32, float32)
				//	IL_00E2: stfld	 float32 Survival::water

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

		/*private static float preHunger;
		private static float preWater;

		[HarmonyPrefix]
		[HarmonyPatch("UpdateStats")]
		internal static void PreUpdateStats(Survival __instance, float timePassed)
		{
			bool bHasSurvivalSuit = PlayerPatch.bHasSurvivalSuit;
#if SUBNAUTICA
			if (bHasSurvivalSuit && GameModeUtils.RequiresSurvival() && !Player.main.IsFrozenStats())
#elif BELOWZERO
			if (bHasSurvivalSuit && GameModeManager.GetOption<bool>(GameOption.Hunger) && GameModeManager.GetOption<bool>(GameOption.Thirst) && !Player.main.IsFrozenStats())
#endif
			{
				preHunger = __instance.food;
				preWater = __instance.water;
				float regenRate = SurvivalsuitBehaviour.SurvivalRegenRate;
				float kMaxStat = SurvivalConstants.kMaxStat;
				//float kFoodTime = SurvivalConstants.kFoodTime;
				//float kWaterTime = SurvivalConstants.kWaterTime;
				//Log.LogDebug($"SurvivalPatches.PreUpdateStats: food = {__instance.food}, water = {__instance.water}, bHasSurvivalSuit = {bHasSurvivalSuit}\nkFoodTime = {kFoodTime}, kWaterTime = {kWaterTime}, kMaxStat = {kMaxStat}, regenRate = {regenRate}");

				// now we can calculate the current calorie/water consumption rates and calibrate based on those.
				// Assuming the buggers at UWE don't change the algorithm.

				float foodRestore = (timePassed / SurvivalConstants.kFoodTime * kMaxStat) * regenRate;
				float waterRestore = (timePassed / SurvivalConstants.kWaterTime * kMaxStat) * regenRate;
				__instance.food += foodRestore;
				__instance.water += waterRestore;
				//Log.LogDebug($"SurvivalPatches.PreUpdateStats: done running Survival Suit routine; food = {__instance.food}, water = {__instance.water}, foodRestore = {foodRestore}, waterRestore = {waterRestore}");
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch("UpdateStats")]
		internal static void PostUpdateStats(Survival __instance)
		{
			float foodDelta = preHunger - __instance.food;
			float waterDelta = preWater - __instance.water;
			//Log.LogDebug($"SurvivalPatches.PostUpdateStats: food = {__instance.food}, water = {__instance.water}, foodDelta = {foodDelta}, waterDelta = {waterDelta}");
		}*/
	}
}
