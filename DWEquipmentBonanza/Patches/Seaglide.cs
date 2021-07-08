using DWEquipmentBonanza;
using DWEquipmentBonanza.VehicleModules;
using DWEquipmentBonanza.MonoBehaviours;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using QModManager.Utility;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace DWEquipmentBonanza
{
	[HarmonyPatch(typeof(Seaglide))]
	class SeaglidePatches
	{
		internal static string cachedUseText;
		internal static string customUseText;

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		public static void PostUpdate(Seaglide __instance)
		{
			//PowerglideBehaviour behaviour = __instance.gameObject.GetComponent<PowerglideBehaviour>();
			//if(behaviour != null)

			if(__instance.gameObject.TryGetComponent<PowerglideBehaviour>(out PowerglideBehaviour behaviour))
				behaviour.PostUpdate(__instance);
		}

#if BELOWZERO
		[HarmonyPatch(nameof(Seaglide.GetCustomUseText))]
		[HarmonyPostfix]
		public static void GetCustomUseText(ref string __result, Seaglide __instance)
		{
			if (__instance == null || __instance.gameObject == null || __instance.gameObject.GetComponent<PowerglideBehaviour>() == null)
				return;

			string sprintText = LanguageCache.GetButtonFormat(Language.main.Get("HoverbikeBoostDisplay") + "({0})", GameInput.Button.Sprint); ;
			if (string.IsNullOrEmpty(customUseText) || cachedUseText != sprintText)
			{
				if (Language.main != null)
				{
					customUseText = __result + ", " + sprintText;
					cachedUseText = sprintText;
					typeof(Seaglide).GetField("customUseCachedString", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, customUseText);
				}
				else
				{
					//Log.LogDebug("Language.main is not available");
				}
			}
			//__result = customUseText;
			//__instance.customUseCachedString = customUseText;
		}
#endif

		// I initially implemented my Powerglide code as a function of the default Seaglide.
		// When I decided it worked better as a separate unit due to being a bit too OP to be available right from the start, much of the below code was moved to a separate component.


		/*private static float powerGlideForce = 3000f;
		private static float powerLerpRate = 1200f;

		public static bool PowerGlideActive(Seaglide instance = null)
		{
			Log.LogDebug($"SeaglidePatches.PowerGlideActive() called");
			bool result = GameInput.GetButtonHeld(GameInput.Button.Sprint);
			if (instance != null)
			{
				instance.powerGlideActive = result;
			}

			return result;
		}

		[HarmonyPatch("OnRightHandUp")]
		[HarmonyPatch("OnRightHandHeld")]
		[HarmonyPostfix]
		internal static void OnRightHandPostfix(Seaglide __instance)
		{
			Log.LogDebug($"SeaglidePatches.OnRightHandPostfix() called");
			PowerGlideActive(__instance);
		}

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		internal static void Postfix(Seaglide __instance)
		{
			PowerGlideActive(__instance);
		}*/

		/* There are two regions we want to check:
		 *  in FixedUpdate(), we want to change "if (this.powerGlideActive)" to "if( DWEquipmentBonanza.Patches.PowerGlideActive() )"
		 *  in UpdateEnergy(), we want to change:
		 *			if(this.powerGlideActive)
		 *	to:
		 *			if(SeaglidePatches.PowerGlideActive)
		 *  
		 *  		this.powerGlideForce = Mathf.Lerp(this.powerGlideForce, this.powerGlideActive ? 50000f : 0f, Time.deltaTime * 20000f);
		 *  to:
		 *  		this.powerGlideForce = Mathf.Lerp(this.powerGlideForce, this.powerGlideActive ? powerGlideForce : 0f, Time.deltaTime * powerLerpRate);
		 *  The precise numbers are subject to change.
		 * The patterns in UpdateEnergy; First:
		 * 
			IL_004A: ldarg.0
			IL_004B: ldfld     bool Seaglide::powerGlideActive
			IL_0050: brfalse.s IL_0079
			IL_0052: ldc.r4    1

		 * and second:
			IL_00A9: ldarg.0
			IL_00AA: ldarg.0
			IL_00AB: ldfld     float32 Seaglide::powerGlideForce
			IL_00B0: ldarg.0
			IL_00B1: ldfld     bool Seaglide::powerGlideActive
			IL_00B6: brtrue.s  IL_00BF
			IL_00B8: ldc.r4    0.0
			IL_00BD: br.s      IL_00C4
			IL_00BF: ldc.r4    50000
			IL_00C4: call      float32 [UnityEngine.CoreModule]UnityEngine.Time::get_deltaTime()
			IL_00C9: ldc.r4    20000
			IL_00CE: mul
			IL_00CF: call      float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Lerp(float32, float32, float32)
			IL_00D4: stfld     float32 Seaglide::powerGlideForce
			IL_00D9: ret

		 * The FixedUpdate pattern:
			IL_0000: ldarg.0
			IL_0001: ldfld     bool Seaglide::powerGlideActive
			IL_0006: brfalse.s IL_0038

			IL_0008: ldsfld    class Player Player::main
			IL_000D: callvirt  instance class [UnityEngine.CoreModule]UnityEngine.GameObject [UnityEngine.CoreModule]UnityEngine.Component::get_gameObject()
			IL_0012: callvirt  instance !!0 [UnityEngine.CoreModule]UnityEngine.GameObject::GetComponent<class [UnityEngine.PhysicsModule]UnityEngine.Rigidbody>()

		 */

		/*[HarmonyTranspiler]
		[HarmonyPatch("UpdateEnergy")]
		public static IEnumerable<CodeInstruction> UpdateEnergyTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo activeMethod = typeof(SeaglidePatches).GetMethod(nameof(SeaglidePatches.PowerGlideActive));
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Seaglide.UpdateEnergy(), pre-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}

			int maxIndex = codes.Count - 15;
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldarg_0                                                     // IL_00A9: ldarg.0
					&& codes[i + 1].opcode == OpCodes.Ldarg_0                                               // IL_00AA: ldarg.0
					&& codes[i + 2].opcode == OpCodes.Ldfld                                                 // IL_00AB: ldfld     float32 Seaglide::powerGlideForce
					&& codes[i + 3].opcode == OpCodes.Ldarg_0                                               // IL_00B0: ldarg.0
					&& codes[i + 4].opcode == OpCodes.Ldfld                                                 // IL_00B1: ldfld     bool Seaglide::powerGlideActive
					&& (codes[i + 5].opcode == OpCodes.Brtrue || codes[i + 5].opcode == OpCodes.Brtrue_S)   // IL_00B6: brtrue.s  IL_00BF
					&& codes[i + 6].opcode == OpCodes.Ldc_R4                                                // IL_00B8: ldc.r4    0.0
					&& (codes[i + 7].opcode == OpCodes.Br || codes[i + 7].opcode == OpCodes.Br_S)           // IL_00BD: br.s      IL_00C4
					&& codes[i + 8].opcode == OpCodes.Ldc_R4                                                // IL_00BF: ldc.r4    50000
					&& codes[i + 9].opcode == OpCodes.Call                                                  // IL_00C4: call      float32 [UnityEngine.CoreModule]UnityEngine.Time::get_deltaTime()
					&& codes[i + 10].opcode == OpCodes.Ldc_R4                                               // IL_00C9: ldc.r4    20000
					&& codes[i + 11].opcode == OpCodes.Mul                                                  // IL_00CE: mul
					&& codes[i + 12].opcode == OpCodes.Call                                                 // IL_00CF: call      float32 [UnityEngine.CoreModule]UnityEngine.Mathf::Lerp(float32, float32, float32)
					&& codes[i + 13].opcode == OpCodes.Stfld                                                // IL_00D4: stfld     float32 Seaglide::powerGlideForce
					&& codes[i + 14].opcode == OpCodes.Ret)                                                 // IL_00D9: ret

				{
					int index = i + 8;
					codes[index].operand = powerGlideForce;
					codes[index + 2].operand = powerLerpRate;
					break; // No need to go any further.
				}
			}

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Seaglide.UpdateEnergy(), post-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}
			return codes.AsEnumerable();
		}*/

		/*[HarmonyPatch("UpdateEnergy")]
		[HarmonyPostfix]
		public static void PostUpdateEnergy(Seaglide __instance)
		{
			__instance.powerGlideForce = Mathf.Lerp(__instance.powerGlideForce, __instance.powerGlideActive ? powerGlideForce : 0f, Time.deltaTime * powerLerpRate);
		}*/

		/*[HarmonyTranspiler]
		[HarmonyPatch("FixedUpdate")]
		public static IEnumerable<CodeInstruction> FixedUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo activeMethod = typeof(SeaglidePatches).GetMethod(nameof(SeaglidePatches.PowerGlideActive)); 
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Seaglide.FixedUpdate(), pre-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}
			int maxIndex = codes.Count - 6;
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldarg_0                                                      // IL_0000: ldarg.0
					&& codes[i + 1].opcode == OpCodes.Ldfld                                                 // IL_0001: ldfld     bool Seaglide::powerGlideActive
					&& (codes[i + 2].opcode == OpCodes.Brfalse || codes[i + 2].opcode == OpCodes.Brfalse_S) // IL_0006: brfalse.s IL_0038
					&& codes[i + 3].opcode == OpCodes.Ldsfld                                                // IL_0008: ldsfld    class Player Player::main
					&& codes[i + 4].opcode == OpCodes.Callvirt                                              // IL_000D: callvirt  instance class [UnityEngine.CoreModule]UnityEngine.GameObject [UnityEngine.CoreModule]UnityEngine.Component::get_gameObject()
					&& codes[i + 5].opcode == OpCodes.Callvirt)                                             // IL_0012: callvirt  instance !!0 [UnityEngine.CoreModule]UnityEngine.GameObject::GetComponent<class [UnityEngine.PhysicsModule]UnityEngine.Rigidbody>()
				{
					// We want to change that Ldfld into a Call pointing to our method
					codes[i + 1] = new CodeInstruction(OpCodes.Callvirt, activeMethod);
					break; // Since we only want to change one part of the method, we are now done.
				}
			}

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Seaglide.FixedUpdate(), post-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}

			return codes.AsEnumerable();
		}*/
	}
}
