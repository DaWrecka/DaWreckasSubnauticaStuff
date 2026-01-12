//#define LOGTRANSPILER

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWE;
using UnityEngine;
using Common;
using System.Reflection;
using System.Reflection.Emit;

namespace DWEquipmentBonanza.Patches
{
	[HarmonyPatch(typeof(Drillable))]
	public class DrillablePatches
	{
		public static float ExosuitDrillDamage;

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		public static void PostStart(Drillable __instance)
		{
			LargeWorldEntity.CellLevel target = LargeWorldEntity.CellLevel.VeryFar;

			//if (__instance.resources.Length < 1)
			//	return;

			if (__instance.gameObject.GetComponent<FloatersTarget>() != null)
			{
				target = LargeWorldEntity.CellLevel.Medium;
				return;
			}
			else if (__instance.resources.Length < 1)
				return;

			Log.LogDebug($"Found Drillable {__instance.name}");
			if (__instance.gameObject.TryGetComponent<LargeWorldEntity>(out LargeWorldEntity lwe))
			{
				lwe.cellLevel = target; 
			}
#if LEGACY && SN1
			__instance.kChanceToSpawnResources = Mathf.Max(DWConstants.newKyaniteChance, __instance.kChanceToSpawnResources);
#endif
		}

#if BELOWZERO
		[HarmonyPatch(nameof(Drillable.OnDrill))]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> OnDrillTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo damageField = typeof(DrillablePatches).GetField(nameof(DrillablePatches.ExosuitDrillDamage));

	#if LOGTRANSPILER
			int idx = -1;

			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

			Log.LogDebug("Drillable.OnDrill(), pre-transpiler:");
				for (idx = 0; idx < codes.Count; idx++)
					Log.LogDebug(String.Format("0x{0:X4}", idx) + $" : {codes[idx].opcode.ToString()}	{(codes[idx].operand != null ? codes[idx].operand.ToString() : "")}");

				idx = -1;
	#endif
			CodeMatch codeMatch = new(i => i.opcode == OpCodes.Ldc_R4 && ((float)i.operand == 5f));
			var newInstructions = new CodeMatcher(instructions)
				.MatchForward(false, codeMatch)
				.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldsfld, damageField));

	#if LOGTRANSPILER
			Log.LogDebug("Drillable.OnDrill(), post-transpiler:");
			for (idx = 0; idx < codes.Count; idx++)
				Log.LogDebug(String.Format("0x{0:X4}", idx) + $" : {codes[i].opcode.ToString()}	{(codes[idx].operand != null ? codes[idx].operand.ToString() : "")}");
	#endif
			return newInstructions.InstructionEnumeration();
		}
#endif

	}
}
