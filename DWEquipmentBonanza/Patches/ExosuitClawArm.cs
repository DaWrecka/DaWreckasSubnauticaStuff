using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UWE;

namespace DWEquipmentBonanza.Patches
{
	[HarmonyPatch(typeof(ExosuitClawArm))]
	internal class ExosuitClawArmPatches
	{
		protected const float damageMultiplier = 0.6f; // Whatever standard damage is inflicted, this multiple of that value will be inflicted as Electrical
		private static TechType LightningGeneratorTechType => Main.GetModTechType("ExosuitLightningClawGeneratorModule");
		public static bool InflictDamage(LiveMixin instance, float originalDamage, Vector3 position = default(Vector3), DamageType type = DamageType.Normal, GameObject dealer = null)
		{
			Log.LogDebug($"ExosuitClawArmPatches.InflictDamage running, instance {instance?.name}, originalDamage {originalDamage}, position ({position.ToString()}, type {type.ToString()}, dealer "
				+ (dealer == null ? "null" : dealer.name));
			bool result = instance.TakeDamage(originalDamage, position, type, dealer);
			if (type != DamageType.Electrical)
			{
				Exosuit exosuit = Player.main.GetVehicle() as Exosuit;
				if (exosuit?.modules != null // This shouldn't fail, but this is here for paranoia's sake
					&& exosuit.modules.GetCount(LightningGeneratorTechType) > 0)
				{
					result |= instance.TakeDamage(originalDamage*damageMultiplier, position, DamageType.Electrical, dealer);
				}
			}
			return result;
		}

		[HarmonyPatch("OnHit")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> OnHitTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo inflictDamageMethod = typeof(ExosuitClawArmPatches).GetMethod(nameof(ExosuitClawArmPatches.InflictDamage));
			MethodInfo takeDamageMethod = typeof(LiveMixin).GetMethod(nameof(LiveMixin.TakeDamage));

			if(inflictDamageMethod == null)
			{
				throw new Exception("InflictDamage method could not be retrieved!");
			}

			if (takeDamageMethod == null)
			{
				throw new Exception("LiveMixin.TakeDamage could not be retrieved!");
			}

			int i = -1;
			int maxIndex = codes.Count - 6;

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Dump of ExosuitClawArm.OnHit method, pre-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");

				i = -1;
			}

			while (++i < maxIndex)
			{
				//Our target pattern is as follows:
				// IL_00a3: ldloc.s 5
				// IL_00a5: ldc.r4 50
				// IL_00aa: ldloc.1
				// IL_00ab: ldc.i4.0
				// IL_00ac: ldnull
				// IL_00ad: callvirt instance bool LiveMixin::TakeDamage(float32, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3, valuetype DamageType, class [UnityEngine.CoreModule]UnityEngine.GameObject)

				//The call is all we need to change; the rest of the stuff is just so we know we're targetting the *right* call.

				/*
				if (codes[i].opcode == OpCodes.Ldloc_S			// IL_00a3: ldloc.s 5
					&& codes[i + 1].opcode == OpCodes.Ldc_R4	// IL_00a5: ldc.r4 50
					&& codes[i + 2].opcode == OpCodes.Ldloc_1	// IL_00aa: ldloc.1
					&& codes[i + 3].opcode == OpCodes.Ldc_I4_0	// IL_00ab: ldc.i4.0
					&& codes[i + 4].opcode == OpCodes.Ldnull	// IL_00ac: ldnull
					&& codes[i + 5].opcode == OpCodes.Callvirt	// IL_00ad: callvirt instance bool LiveMixin::TakeDamage(float32, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3, valuetype DamageType, class [UnityEngine.CoreModule]UnityEngine.GameObject)
					)
				{
					codes[i + 5] = new CodeInstruction(OpCodes.Callvirt, inflictDamageMethod);
					break;
				}*/

				// ^^^^ That was the old method. This is the new approach.
				if (codes[i].Calls(takeDamageMethod))
				{
					codes[i] = new CodeInstruction(OpCodes.Call, inflictDamageMethod);
					break;
				}
			}

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Dump of ExosuitClawArm.OnHit method, post-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");

				i = -1;
			}
			return codes.AsEnumerable();
		}
	}
}
