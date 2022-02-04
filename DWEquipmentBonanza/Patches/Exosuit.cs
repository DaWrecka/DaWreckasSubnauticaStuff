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

namespace DWEquipmentBonanza.Patches
{
	[HarmonyPatch(typeof(uGUI_ExosuitHUD))]
	public class uGUI_ExosuitHUDPatches
	{
		private static Exosuit exosuit;

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		public static void PostUpdate(uGUI_ExosuitHUD __instance)
		{
			if (__instance == null || !Main.config.bHUDAbsoluteValues || Player.main == null)
				return;

			exosuit = Player.main.GetVehicle() as Exosuit;
			if (exosuit == null)
			{
				return;
			}

			if (exosuit.liveMixin == null)
			{
				return;
			}

			//Log.LogDebug($"ExosuitHUDPatches.PostUpdate() begin");

			int charge;
			//float capacity;

			if (__instance.textHealth != null)
			{
				int health = Mathf.RoundToInt(exosuit.liveMixin.health);
				__instance.textHealth.text = IntStringCache.GetStringForInt(health);
			}

			if (__instance.textPower != null)
			{
				EnergyInterface energy = (EnergyInterface)(ExosuitPatches.energyInterfaceField.GetValue(exosuit));
				if (energy == null)
				{
					return;
				}

				//energy.GetValues(out charge, out capacity);
				charge = Mathf.RoundToInt(energy.TotalCanProvide(out int i));
				//ErrorMessage.AddMessage($"Current Charge {charge}");
				__instance.textPower.text = IntStringCache.GetStringForInt(charge);
				__instance.textPower.fontSize = (charge > 9999 ? 28 : 36);
			}
			//Log.LogDebug($"ExosuitHUDPatches.PostUpdate() finish");
		}
	}

	[HarmonyPatch(typeof(Exosuit))]
	public class ExosuitPatches
	{
		private static readonly MethodInfo ApplyJumpForceMethod = typeof(Exosuit).GetMethod("ApplyJumpForce", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo thrustPowerField = typeof(Exosuit).GetField("thrustPower", BindingFlags.Instance | BindingFlags.NonPublic);
		internal static readonly FieldInfo energyInterfaceField = typeof(Vehicle).GetField("energyInterface", BindingFlags.Instance | BindingFlags.NonPublic);

		//private static TechType lightningClawTechType => Main.GetModTechType("ExosuitLightningClawArm");
		//private static TechType ExosuitSprintModuleTechType => Main.GetModTechType("ExosuitSprintModule");
		internal static bool JumpJetsUpgraded(Exosuit instance)
		{
			if (instance == null)
				return false;

			return instance.modules.GetCount(TechType.ExosuitJetUpgradeModule) > 0;
		}
		internal static void ApplyJumpForce(Exosuit instance) { ApplyJumpForceMethod.Invoke(instance, null); }
		internal static float GetThrustPower(Exosuit instance) { return (float)thrustPowerField.GetValue(instance); }

		/*[HarmonyPatch(nameof(Exosuit.GetHUDValues))]
		[HarmonyPrefix]
		public static bool PostGetHUDValues(Exosuit __instance, out float health, out float power, out float thrust)
		{
			EnergyInterface energy = (EnergyInterface)(energyInterfaceField.GetValue(__instance));
			float charge = 0f;
			float capacity = 0f;
			if(energy != null)
				energy.GetValues(out charge, out capacity);
			thrust = (float)thrustPowerField.GetValue(__instance);
			if (Main.config.bHUDAbsoluteValues)
			{
				health = Mathf.Floor(__instance.liveMixin.health) * 0.01f;
				power = Mathf.Floor(charge) * 0.01f;
			}
			else
			{
				health = __instance.liveMixin.GetHealthFraction();
				power = ((charge > 0f && capacity > 0f) ? charge / capacity : 0f);
			}

			return false;
		}*/

		/*
		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		public static void PreStart(Exosuit __instance)
		{
			// Exosuit actually has a GetArmPrefab method - but the motherfucker is private.
			// WWWWWWWWWWWWWHHHHHHHHHHHHHHHHHHHHHHHHHYYYYYYYYYYYYYYYYYYYYYYYYYYYYY

			int count = __instance.armPrefabs.Length;
			int index = -1;
			TechType armTechType = Main.GetModTechType("ExosuitGrapplingArmMk2Prefab");

			for (int i = 0; i < count; i++)
			{
				if (__instance.armPrefabs[i].techType == armTechType)
				{
					Log.LogDebug($"Grapple Arm Mk2 prefab already found at index {i}");
					return;
				}
				else if (__instance.armPrefabs[i].techType == TechType.ExosuitGrapplingArmModule)
					index = i; // Save this index for later use
			}

			GameObject armPrefab = null;
			if (index > -1)
				armPrefab = GameObject.Instantiate(__instance.armPrefabs[index].prefab);

			if (armPrefab != null)
			{
				GameObject.DestroyImmediate(armPrefab.GetComponent<ExosuitGrapplingArm>());
				armPrefab.AddComponent<ExosuitGrapplingArmMk2>();
				armPrefab.SetActive(true);

				var mk2Arm = armPrefab.EnsureComponent<ExosuitGrapplingArmMk2>();
				var oldGrapple = armPrefab.GetComponent<ExosuitGrapplingArm>();
				if (oldGrapple != null)
				{
					mk2Arm.hookPrefab = oldGrapple.hookPrefab;
					mk2Arm.rope = oldGrapple.rope;
					GameObject.Destroy(oldGrapple);
				}

				Array.Resize(ref __instance.armPrefabs, count + 1);
				__instance.armPrefabs[count] = new Exosuit.ExosuitArmPrefab()
				{
					prefab = armPrefab,
					techType = armTechType
				};
			}
			else
			{
				//Log.LogDebug($"Failed to find arm prefab in Exosuit prefab");
			}
		}
		*/

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		public static void PostStart(Exosuit __instance)
		{
			if(__instance.gameObject != null && __instance.gameObject.TryGetComponent<LiveMixin>(out LiveMixin mixin) && Main.defaultHealth.TryGetValue(TechType.Exosuit, out float defaultHealth))
			{
				float instanceHealthPct = Mathf.Min(mixin.GetHealthFraction(), 1f);
				float maxHealth = defaultHealth;
					maxHealth *= Main.config.ExosuitHealthMult;

				mixin.data.maxHealth = maxHealth;
				mixin.health = maxHealth * instanceHealthPct;
#if SUBNAUTICA_STABLE
				mixin.initialHealth = defaultHealth;
#endif
			}

			ExosuitUpdater exosuitUpdate = __instance.gameObject.EnsureComponent<ExosuitUpdater>();
			MonoBehaviour vehicle = __instance;
			//exosuitUpdate.Initialise(ref vehicle);
		}

		/*
		[HarmonyPostfix]
		[HarmonyPatch(nameof(Exosuit.HasClaw))]
		public static bool PostHasClaw(bool __result, Exosuit __instance)
		{
			return __result || __instance.leftArmType == lightningClawTechType || __instance.rightArmType == lightningClawTechType;
		}
		*/

		[HarmonyPostfix]
		[HarmonyPatch("OnUpgradeModuleChange")]
		public static void PostUpgradeChange(ref Exosuit __instance, int slotID, TechType techType, bool added)
		{
			Log.LogDebug($"ExosuitPatches.OnUpgradeModuleChange(): slotID = {slotID}, techType = {techType.AsString()}, added = {added}");
			__instance.gameObject.EnsureComponent<ExosuitUpdater>().PostUpgradeModuleChange(slotID, techType, added, __instance);
			//if (techType == lightningClawTechType)
			//	__instance.MarkArmsDirty();
		}

		[HarmonyPostfix]
		[HarmonyPatch("OverrideAcceleration")]
		public static void PostOverrideAcceleration(ref Exosuit __instance, ref Vector3 acceleration)
		{
			//Log.LogDebug($"ExosuitPatches.PostOverrideAcceleration() begin");

			__instance.gameObject.EnsureComponent<ExosuitUpdater>().PostOverrideAcceleration(ref acceleration);
			//Log.LogDebug($"ExosuitPatches.PostOverrideAcceleration() end");
		}


		/* These transpilers were disabled after the Seaworthy update made them obsolete - they served to allow my Sprint Module to function, but the Seaworthy update
		 improved the Exosuit speed considerably, at least when it wasn't on the ground. 
		 However, they still serve as useful reference material in the making of transpilers, and so they haven't been deleted for that reason.
		*/
		//[HarmonyTranspiler]
		//[HarmonyPatch(nameof(Exosuit.Update))]
		public static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo usingJumpjetsMethod = typeof(ExosuitPatches).GetMethod(nameof(ExosuitPatches.ExosuitUsingJumpJets));
			MethodInfo tryApplyJumpForceMethod = typeof(ExosuitPatches).GetMethod(nameof(ExosuitPatches.TryApplyJumpForce));

			int flag2index = -1;
			int jumpForceIndex = -1;
			int maxIndex = codes.Count - 4; // We search for five-opcode patterns. If we go past (count-4) then we'll get an Index Out of Range exception.
			// So we limit our max index here.
			int i = -1;

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Exosuit.Update(), pre-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");

				i = -1;
			}


			while (++i < maxIndex && (flag2index == -1 || jumpForceIndex == -1))
			{
				if (flag2index == -1)
				{
					if (codes[i].opcode == OpCodes.Stloc_1 && codes[i + 1].opcode == OpCodes.Ldloc_1 && codes[i + 2].opcode == OpCodes.Ldfld
						&& codes[i + 3].opcode == OpCodes.Ldc_R4 && codes[i + 4].opcode == OpCodes.Cgt && codes[i+5].opcode == OpCodes.Stloc_2)
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
						//Log.LogDebug("Found first patch region at index " + String.Format("0x{0:X4}", flag2index));
						codes[flag2index + 0] = new CodeInstruction(OpCodes.Ldarg_0);
						codes[flag2index + 1] = new CodeInstruction(OpCodes.Ldloc_1);
						codes[flag2index + 2] = new CodeInstruction(OpCodes.Callvirt, usingJumpjetsMethod);
						codes[flag2index + 3] = new CodeInstruction(OpCodes.Nop);
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
						//Log.LogDebug("Found second patch region at index " + String.Format("0x{0:X4}", jumpForceIndex));
						/* What we need to do here, in no particular order, is
						 * a) Insert an additional ldloc.1 between the ldarg.0 and the call
						 * b) change the destination of the call. */
						codes.Insert(jumpForceIndex + 1, new CodeInstruction(OpCodes.Ldloc_1));
						codes[jumpForceIndex + 2] = new CodeInstruction(OpCodes.Callvirt, tryApplyJumpForceMethod);
						i = jumpForceIndex + 3;
					}
				}
			}

			if (flag2index == -1)
				Log.LogError("Exosuit.Update() transpiler could not find first patch location in method");
			else if (jumpForceIndex == -1)
				Log.LogError("Exosuit.Update() transpiler could not find second patch location in method");

			if (Main.bLogTranspilers)
			{
				Log.LogDebug("Exosuit.Update(), post-transpiler:");
				for (i = 0; i < codes.Count; i++)
					Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}

			return codes.AsEnumerable();
		}

		//[HarmonyTranspiler]
		//[HarmonyPatch(nameof(Exosuit.FixedUpdate))]
		public static IEnumerable<CodeInstruction> FixedUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo underwaterJumpingMethod = typeof(ExosuitPatches).GetMethod(nameof(ExosuitPatches.UnderwaterJumping));

			int i = -1;
			int flagIndex = -1;
			int maxIndex = codes.Count - 4;

			while (++i < maxIndex && flagIndex == -1)
			{
				/*
				 Our target pattern is as follows:
					IL_001A: ceq
					IL_001C: stfld     bool WorldForces::handleGravity
					IL_0021: ldarg.0
					IL_0022: call instance bool Exosuit::IsUnderwater()
					IL_0027: stloc.1

				The call is all we need to change; the rest of the stuff is just so we know we're targetting the *right* call.
				*/
				if (codes[i].opcode == OpCodes.Ceq 
					&& codes[i + 1].opcode == OpCodes.Stfld 
					&& codes[i + 2].opcode == OpCodes.Ldarg_0 
					&& codes[i + 3].opcode == OpCodes.Call 
					&& codes[i + 4].opcode == OpCodes.Stloc_1)
				{
					codes[i + 3] = new CodeInstruction(OpCodes.Callvirt, underwaterJumpingMethod);
					break;
				}
			}

			return codes.AsEnumerable();
		}

		[HarmonyPrefix]
		[HarmonyPatch("Update")]
		public static void PreUpdate(ref Exosuit __instance)
		{
			//Log.LogDebug($"ExosuitPatches.PreUpdate() begin");
			if(__instance?.gameObject != null)
				__instance.gameObject.EnsureComponent<ExosuitUpdater>().PreUpdate(__instance);
			//Vector3 vector = AvatarInputHandler.main.IsEnabled() ? GameInput.GetMoveDirection() : Vector3.zero;
			//Log.LogDebug("ExosuitPatches.PreUpdate() end");
		}

		[HarmonyPostfix]
		[HarmonyPatch("Update")]
		public static void PostUpdate(ref Exosuit __instance)
		{
			//Log.LogDebug($"ExosuitPatches.PostUpdate() begin");
			if (__instance?.gameObject != null)
				__instance.gameObject.EnsureComponent<ExosuitUpdater>().PostUpdate(__instance);
			//Vector3 vector = AvatarInputHandler.main.IsEnabled() ? GameInput.GetMoveDirection() : Vector3.zero;
			//Log.LogDebug("ExosuitPatches.PostUpdate() end");
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
			if (Player.main.GetVehicle() != exosuit)
				return false;

			bool bJumpHeld = GameInput.GetButtonHeld(GameInput.Button.Jump);
			bool bMovingUp = velocity.y > 0;
			//Log.LogDebug($"ExosuitPatches.ExosuitIsJumping: bJumpHeld = " + (bJumpHeld ? " true" : "false") + ", bMovingUp = " + (bMovingUp ? " true" : "false"));

			return bJumpHeld || bMovingUp;
		}

		public static bool UnderwaterJumping(Exosuit exosuit)
		{
			if (Player.main.GetVehicle() != exosuit)
				return false;

			bool bJumpHeld = GameInput.GetButtonHeld(GameInput.Button.Jump);
			bool bIsUnderwater = exosuit.IsUnderwater();
			//Log.LogDebug($"ExosuitPatches.UnderwaterJumping: bJumpHeld = " + (bJumpHeld ? " true" : "false") + ", bIsUnderwater = " + (bIsUnderwater ? " true" : "false"));
			return bJumpHeld && bIsUnderwater;
		}

		public static bool ExosuitIsSprinting(Exosuit exosuit, Vector3 velocity)
		{
			//if (Player.main.GetVehicle() != exosuit)
				return false;

			/*
			Vector3 groundVector = new Vector3(velocity.x, 0, velocity.z);
			bool bIsMoving = (velocity != Vector3.zero && groundVector.magnitude > 0f);
			bool bPdaInUse = (Player.main.GetPDA() != null ? Player.main.GetPDA().isInUse : false);
			bool bSprintControlActive = exosuit.modules.GetCount(ExosuitSprintModuleTechType) > 0 && GameInput.GetButtonHeld(GameInput.Button.Sprint);
			//Log.LogDebug($"ExosuitPatches.ExosuitIsSprinting: bIsMoving = " + (bIsMoving ? " true" : "false") + ", bSprintControlActive = " + (bSprintControlActive ? " true" : "false") + ", bPdaInUse = " + (bPdaInUse ? " true" : "false"));
			return bIsMoving && bSprintControlActive && !bPdaInUse;
			*/
		}

		public static bool ExosuitUsingJumpJets(Exosuit exosuit, Vector3 velocity)
		{
			if (Player.main.GetVehicle() != exosuit)
				return false;

			bool bJumping = ExosuitIsJumping(exosuit, velocity);
			bool bSprinting = ExosuitIsSprinting(exosuit, velocity);
			//Log.LogDebug($"ExosuitPatches.ExosuitUsingJumpJets: bJumping = " + (bJumping ? " true" : "false") + ", bSprinting = " + (bSprinting ? " true" : "false"));
			return bJumping || bSprinting;
		}

		public static void TryApplyJumpForce(Exosuit exosuit, Vector3 velocity)
		{
			if (Player.main.GetVehicle() != exosuit)
				return;

			bool bIsJumping = ExosuitIsJumping(exosuit, velocity);
			//Log.LogDebug($"ExosuitPatches.TryApplyJumpForce(): ExosuitIsJumping() returned {bIsJumping.ToString()}");
			if (bIsJumping)
			{
				ApplyJumpForce(exosuit);
			}
		}
	} 
}
