using CombinedItems;
using CombinedItems.ExosuitModules;
using CombinedItems.MonoBehaviours;
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

namespace CombinedItems.Patches
{
	[HarmonyPatch(typeof(Exosuit))]
	public class ExosuitPatches
	{
		private static readonly FieldInfo jumpJetUpgrade = typeof(Exosuit).GetField("jumpJetsUpgraded", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly PropertyInfo jetsActive = typeof(Exosuit).GetProperty("jetsActive", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo timeJetsActiveChanged = typeof(Exosuit).GetField("timeJetsActiveChanged", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo thrustPower = typeof(Exosuit).GetField("thrustPower", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo thrustConsumption = typeof(Exosuit).GetField("thrustConsumption", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo onGround = typeof(Exosuit).GetField("onGround", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo timeOnGround = typeof(Exosuit).GetField("timeOnGround", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo jetDownLastFrame = typeof(Exosuit).GetField("jetDownLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo powersliding = typeof(Exosuit).GetField("powersliding", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo areFXPlaying = typeof(Exosuit).GetField("areFXPlaying", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly MethodInfo ApplyJumpForceMethod = typeof(Exosuit).GetMethod("ApplyJumpForce", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static bool JumpJetsUpgraded(Exosuit instance) { return (bool)jumpJetUpgrade.GetValue(instance); }
		/*internal static bool JetsActive(Exosuit instance) => (bool)jetsActive.GetValue(instance);
		internal static void JetsActive(Exosuit instance, bool value) { jetsActive.SetValue(instance, value); }
		internal static float TimeJetsActiveChanged(Exosuit instance) => (float)timeJetsActiveChanged.GetValue(instance);
		internal static float ThrustPower(Exosuit instance) => (float)thrustPower.GetValue(instance);
		internal static void ThrustPower(Exosuit instance, float value) { thrustPower.SetValue(instance, value); }
		internal static float ThrustConsumption(Exosuit instance) => (float)thrustConsumption.GetValue(instance);
		internal static bool OnGround(Exosuit instance) => (bool)onGround.GetValue(instance);
		internal static float TimeOnGround(Exosuit instance) => (float)timeOnGround.GetValue(instance);
		internal static bool JetDownLastFrame(Exosuit instance) => (bool)jetDownLastFrame.GetValue(instance);
		internal static void JetDownLastFrame(Exosuit instance, bool value) { jetDownLastFrame.SetValue(instance, value); }
		internal static bool Powersliding(Exosuit instance) => (bool)powersliding.GetValue(instance);
		internal static bool AreFXPlaying(Exosuit instance) => (bool)areFXPlaying.GetValue(instance);
		internal static void AreFXPlaying(Exosuit instance, bool value) { areFXPlaying.SetValue(instance, value); }*/
		internal static void ApplyJumpForce(Exosuit instance) { ApplyJumpForceMethod.Invoke(instance, new object[] { }); }

		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		public static void PreStart(Exosuit __instance)
		{
			// Exosuit actually has a GetArmPrefab method - but the motherfucker is private.
			// WWWWWWWWWWWWWHHHHHHHHHHHHHHHHHHHHHHHHHYYYYYYYYYYYYYYYYYYYYYYYYYYYYY

			int count = __instance.armPrefabs.Length;
			int clawIndex = -1;

			for (int i = 0; i < count; i++)
			{
				if (__instance.armPrefabs[i].techType == Main.prefabLightningClaw.TechType)
				{
					//Log.LogDebug($"Lightning claw prefab already found at index {i}");
					return;
				}
				else if (__instance.armPrefabs[i].techType == TechType.ExosuitClawArmModule)
					clawIndex = i; // Save this index for later use
			}

			GameObject armPrefab = null;
			if (clawIndex > -1)
				armPrefab = GameObject.Instantiate(__instance.armPrefabs[clawIndex].prefab);

			if (armPrefab != null)
			{
				// Since we're making a variant of Claw Arm, we don't actually have to do much here.
				// Just remove the base ExosuitClawArm component, and swap in our ExosuitLightningClaw prefab
				GameObject.DestroyImmediate(armPrefab.GetComponent<ExosuitClawArm>());
				armPrefab.AddComponent<ExosuitLightningClaw>();
				armPrefab.SetActive(true);
				Array.Resize(ref __instance.armPrefabs, count + 1);
				__instance.armPrefabs[count] = new Exosuit.ExosuitArmPrefab()
				{
					prefab = armPrefab,
					techType = Main.prefabLightningClaw.TechType
				};
			}
			else
			{
				//Log.LogDebug($"Failed to find arm prefab in Exosuit prefab");
			}
		}

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		public static void PostStart(Exosuit __instance)
		{
			ExosuitUpdater exosuitUpdate = __instance.gameObject.EnsureComponent<ExosuitUpdater>();
			Vehicle v = __instance as Vehicle;
			exosuitUpdate.Initialise(ref v);
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Exosuit.HasClaw))]
		public static bool PostHasClaw(bool __result, Exosuit __instance)
		{
			return __result || __instance.leftArmType == Main.prefabLightningClaw.TechType || __instance.rightArmType == Main.prefabLightningClaw.TechType;
		}

		[HarmonyPostfix]
		[HarmonyPatch("OnUpgradeModuleChange")]
		public static void PostUpgradeChange(ref Exosuit __instance, int slotID, TechType techType, bool added)
		{
			__instance.gameObject.EnsureComponent<ExosuitUpdater>().PostUpgradeModuleChange(techType);
			if (techType == Main.prefabLightningClaw.TechType)
				__instance.MarkArmsDirty();
		}

		[HarmonyPostfix]
		[HarmonyPatch("OverrideAcceleration")]
		public static void PostOverrideAcceleration(ref Exosuit __instance, ref Vector3 acceleration)
		{
			__instance.gameObject.EnsureComponent<ExosuitUpdater>().PostOverrideAcceleration(ref acceleration, JumpJetsUpgraded(__instance));
		}

		[HarmonyTranspiler]
		[HarmonyPatch(nameof(Exosuit.Update))]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo usingJumpjetsMethod = typeof(ExosuitPatches).GetMethod(nameof(ExosuitPatches.ExosuitUsingJumpJets));
			MethodInfo tryApplyJumpForceMethod = typeof(ExosuitPatches).GetMethod(nameof(ExosuitPatches.TryApplyJumpForce));

			int flag2index = -1;
			int jumpForceIndex = -1;
			int maxIndex = codes.Count - 4; // We search for five-opcode patterns. If we go past (count-4) then we'll get an Index Out of Range exception.
			// So we limit our max index here.
			int i = 0;

			for(i = 0; i < codes.Count; i++)
				Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}");

			i = -1;

			while (++i < maxIndex && (flag2index == -1 || jumpForceIndex == -1))
			{
				if (flag2index == -1)
				{
					if (codes[i].opcode == OpCodes.Stloc_1 && codes[i + 1].opcode == OpCodes.Ldloc_1 && codes[i + 2].opcode == OpCodes.Ldfld
						&& codes[i + 3].opcode == OpCodes.Ldc_R4 && codes[i + 4].opcode == OpCodes.Cgt)
					{
						/* These codes correspond to "bool flag2 = vector.y > 0;"
							IL_00dd: stloc.1
							IL_00de: ldloc.1
							IL_00df: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector3::y
							IL_00e4: ldc.r4 0.0
							IL_00e9: cgt
						 * We want to replace them with the equivalent of bool flag2 = ExosuitPatches.ExosuitUsingJumpJets(this, vector);
						 * So we first use ldarg.0 to put the current Exosuit instance on the stack, then ldloc.1 to put local variable 1 - vector - on the stack.
						 * Then we call the 'using jump jets' method.*/
						flag2index = i+1;
						Log.LogDebug("Found first patch region at index " + String.Format("0x{0:X4}", flag2index));
						codes[flag2index + 1] = new CodeInstruction(OpCodes.Ldarg_0);
						codes[flag2index + 2] = new CodeInstruction(OpCodes.Ldarg_0);
						codes[flag2index + 3] = new CodeInstruction(OpCodes.Ldloc_1);
						codes[flag2index + 4] = new CodeInstruction(OpCodes.Callvirt, usingJumpjetsMethod);
						i = flag2index + 4;
					}
				}
				else if (jumpForceIndex == -1) // jumpForceIndex cannot be found before flag2index. Even if this particular sequence of opcodes is found before this,
					// it's not the one we're looking for. Thus, 'else if' is appropriate here.
				{
					/*
						IL_0141: ldarg.0
						IL_0142: ldfld bool Exosuit::jetDownLastFrame
						IL_0147: brtrue.s IL_014f

						IL_0149: ldarg.0
						IL_014a: call instance void Exosuit::ApplyJumpForce()

					We're searching here for the three opcodes ahead of the two we need, since the two opcodes we're looking for are pretty commonly found together.
					A very cursory inspection on my part found an ldarg.0 followed by a call within about half a page of IL code. With no effort
						required, I found another sequence of opcodes identical to the one I was interested in.
					So; Search for a specific group of three opcodes ahead of the ones we're interested in, to make sure they're the ones we're interested in.
					*/
					if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Ldfld
						&& (codes[i + 2].opcode == OpCodes.Brtrue_S || codes[i + 2].opcode == OpCodes.Brtrue) // Strangely, while ILSpy and dnSpy both display the opcode here as brtrue.s,
							// the actual opcode passed to the transpiler is brtrue.
						&& codes[i + 3].opcode == OpCodes.Ldarg_0 && codes[i + 4].opcode == OpCodes.Call)
					{
						jumpForceIndex = i + 3;
						Log.LogDebug("Found second patch region at index " + String.Format("0x{0:X4}", jumpForceIndex));
						/* What we need to do here, in no particular order, is
						 * a) Insert an additional ldarg.0 and a ldloc.1 between the ldarg.0 and the call
						 * b) change the destination of the call. */
						codes.Insert(jumpForceIndex + 1, new CodeInstruction(OpCodes.Ldarg_0));
						codes.Insert(jumpForceIndex + 2, new CodeInstruction(OpCodes.Ldloc_1));
						codes[jumpForceIndex + 3] = new CodeInstruction(OpCodes.Callvirt, tryApplyJumpForceMethod);
						i = jumpForceIndex + 3;
					}
				}
			}

			if (flag2index == -1)
				Log.LogError("Exosuit.Update() transpiler could not find first patch location in method");
			else if (jumpForceIndex == -1)
				Log.LogError("Exosuit.Update() transpiler could not find second patch location in method");

			return codes.AsEnumerable();
		}

		[HarmonyPostfix]
		[HarmonyPatch("Update")]
		public static void PostUpdate(ref Exosuit __instance)
		{
			__instance.gameObject.EnsureComponent<ExosuitUpdater>().Tick();
			Vector3 vector = AvatarInputHandler.main.IsEnabled() ? GameInput.GetMoveDirection() : Vector3.zero;
		}
		/*
			* Methods which may need to be patched to accomodate future plans:

			private bool GetIsGrappling()
			{
				this.arms.Clear();
				base.GetComponentsInChildren<ExosuitGrapplingArm>(this.arms);
				for (int i = 0; i < this.arms.Count; i++)
				{
					if (this.arms[i].GetIsGrappling())
					{
						return true;
					}
				}
				return false;
			}

			// This method appears to be called in all circumstances - as long as the vehicle's controlSheme (sic) is either Submarine or Mech.
			protected override void OverrideAcceleration(ref Vector3 acceleration)
			{
				if (!this.onGround)
				{
					float num = this.jumpJetsUpgraded ? 0.3f : 0.22f;
					acceleration.x *= num;
					acceleration.z *= num;
				}
			}
			*/

		public static bool ExosuitIsJumping(Exosuit exosuit, Vector3 velocity)
		{
			return velocity.y > 0;
		}

		public static bool ExosuitIsSprinting(Exosuit exosuit)
		{
			return exosuit.modules.GetCount(Main.prefabExosuitSprintModule.TechType) > 0 && GameInput.GetButtonHeld(GameInput.Button.Sprint);
		}

		public static bool ExosuitUsingJumpJets(Exosuit exosuit, Vector3 velocity)
		{
			return ExosuitIsJumping(exosuit, velocity) || ExosuitIsSprinting(exosuit);
		}

		public static void TryApplyJumpForce(Exosuit exosuit, Vector3 velocity)
		{
			if (ExosuitIsJumping(exosuit, velocity))
			{
				ApplyJumpForceMethod.Invoke(exosuit, null);
			}
		}
	} 
}
