//#define LOGTRANSPILER
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace DWEquipmentBonanza.Patches
{
	// Enabled for BelowZero only as mods for this purpose already exist for SN1.
#if BELOWZERO
	[HarmonyPatch(typeof(ExosuitGrapplingArm))]
	public class ExosuitGrapplingArmPatches
	{
		private static readonly Dictionary<string, bool> bGrappleUpgradeInstalled = [];
		private const float upgradedGrappleHookSpeed = 60f;
		private const float upgradedGrappleAcceleration = 45f;
		private const float upgradedGrappleForce = 800f;
		private const float upgradedGrappleHookRange = 70f;
		private const float basicGrappleHookSpeed = 25f;
		private const float basicGrappleAcceleration = 15f;
		private const float basicGrappleForce = 400f;
		private const float basicGrappleHookRange = 35f;


		[HarmonyPatch(nameof(ExosuitGrapplingArm.OnHit))]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> OnHitTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			//MethodInfo hookSpeedMethod = typeof(ExosuitGrapplingArmPatches).GetMethod(nameof(ExosuitGrapplingArmPatches.GetHookSpeed), BindingFlags.Static | BindingFlags.Public);

#if LOGTRANSPILER
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

			Log.LogDebug("ExosuitGrapplingArm.OnHit(), pre-transpiler:");
			for (int index = 0; index < codes.Count; index++)
				Log.LogDebug(String.Format("0x{0:X4}", index) + $" : {codes[index].opcode.ToString()}	{(codes[index].operand != null ? codes[index].operand.ToString() : "")}");

#endif
			CodeMatch codeMatch = new(i => i.opcode == OpCodes.Ldc_R4 && ((float)i.operand == basicGrappleHookSpeed));
			var newInstructions = new CodeMatcher(instructions)
				.MatchForward(false, codeMatch)
				.Repeat(m =>
				{
					m.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0));
					//m.SetInstruction(new CodeInstruction(OpCodes.Call, hookSpeedMethod));
					m.SetInstruction(Transpilers.EmitDelegate(ExosuitGrapplingArmPatches.GetHookSpeed));
				});

#if LOGTRANSPILER
			var enumeration = new List<CodeInstruction>(newInstructions.InstructionEnumeration());
			Log.LogDebug($"ExosuitGrapplingArm.OnHit(), post-transpiler: {String.Format("0x{0:X4}", enumeration.Count)} CodeInstructions in collection");
			for (int index = 0; index < enumeration.Count; index++)
				Log.LogDebug(String.Format("0x{0:X4}", index) + $" : {enumeration[index].opcode.ToString()}	{(enumeration[index].operand != null ? enumeration[index].operand.ToString() : "")}");
#endif
			return newInstructions.InstructionEnumeration();
			//return codes.AsEnumerable();
		}
		
		[HarmonyPatch(nameof(ExosuitGrapplingArm.FixedUpdate))]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> FixedUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
		{
#if LOGTRANSPILER
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

			Log.LogDebug("ExosuitGrapplingArm.FixedUpdate(), pre-transpiler:");
			for (int index = 0; index < codes.Count; index++)
				Log.LogDebug(String.Format("0x{0:X4}", index) + $" : {codes[index].opcode.ToString()}	{(codes[index].operand != null ? codes[index].operand.ToString() : "")}");

#endif
			//MethodInfo pullAccelerationMethod = typeof(ExosuitGrapplingArmPatches).GetMethod(nameof(ExosuitGrapplingArmPatches.GetPullAcceleration));
			//MethodInfo pullForceMethod = typeof(ExosuitGrapplingArmPatches).GetMethod(nameof(ExosuitGrapplingArmPatches.GetPullForce));
			//MethodInfo hookRangeMethod = typeof(ExosuitGrapplingArmPatches).GetMethod(nameof(ExosuitGrapplingArmPatches.GetHookRange));


			CodeMatch accelerationMatch = new(i => i.opcode == OpCodes.Ldc_R4 && ((float)i.operand == basicGrappleAcceleration));
			CodeMatch forceMatch = new(i => i.opcode == OpCodes.Ldc_R4 && ((float)i.operand == basicGrappleForce));
			CodeMatch hookRangeMatch = new(i => i.opcode == OpCodes.Ldc_R4 && ((float)i.operand == basicGrappleHookRange));
			var newInstructions = new CodeMatcher(instructions)
				.MatchForward(false, accelerationMatch)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
				.SetInstruction(Transpilers.EmitDelegate(ExosuitGrapplingArmPatches.GetPullAcceleration))
				.MatchForward(false, forceMatch)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
				.SetInstruction(Transpilers.EmitDelegate(ExosuitGrapplingArmPatches.GetPullForce))
				.MatchForward(false, hookRangeMatch)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
				.SetInstruction(Transpilers.EmitDelegate(ExosuitGrapplingArmPatches.GetHookRange));


#if LOGTRANSPILER
			var enumeration = new List<CodeInstruction>(newInstructions.InstructionEnumeration());
			Log.LogDebug($"ExosuitGrapplingArm.OnHit(), post-transpiler: {String.Format("0x{0:X4}", enumeration.Count)} CodeInstructions in collection");
			for (int index = 0; index < enumeration.Count; index++)
				Log.LogDebug(String.Format("0x{0:X4}", index) + $" : {enumeration[index].opcode.ToString()}	{(enumeration[index].operand != null ? enumeration[index].operand.ToString() : "")}");
#endif
			return newInstructions.InstructionEnumeration();
			//return codes.AsEnumerable();
		}

		public static void UpdateGrappleUpgradeState(Exosuit e, bool state)
		{
			var identifier = e.GetComponent<PrefabIdentifier>().id;
			bGrappleUpgradeInstalled[identifier] = state;
			Log.LogDebug($"Grapple upgrade state for Exosuit {identifier} set to {bGrappleUpgradeInstalled[identifier]}");
		}

		public static float GetHookSpeed(ExosuitGrapplingArm e)
		{
			string id = e.GetComponentInParent<PrefabIdentifier>().id;
			return (!(String.IsNullOrEmpty(id)) && bGrappleUpgradeInstalled.GetOrDefault(id, false)) ? upgradedGrappleHookSpeed : basicGrappleHookSpeed;
		}

		public static float GetPullAcceleration(ExosuitGrapplingArm e)
		{
			string id = e.GetComponentInParent<PrefabIdentifier>().id;
			return (!(String.IsNullOrEmpty(id)) && bGrappleUpgradeInstalled.GetOrDefault(id, false)) ? upgradedGrappleAcceleration : basicGrappleAcceleration;
		}

		public static float GetPullForce(ExosuitGrapplingArm e)
		{
			string id = e.GetComponentInParent<PrefabIdentifier>().id;
			return (!(String.IsNullOrEmpty(id)) && bGrappleUpgradeInstalled.GetOrDefault(id, false)) ? upgradedGrappleForce : basicGrappleForce;
		}

		public static float GetHookRange(ExosuitGrapplingArm e)
		{
			string id = e.GetComponentInParent<PrefabIdentifier>().id;
			return (!(String.IsNullOrEmpty(id)) && bGrappleUpgradeInstalled.GetOrDefault(id, false)) ? upgradedGrappleHookRange : basicGrappleHookRange;

		}
#endif
	}
}