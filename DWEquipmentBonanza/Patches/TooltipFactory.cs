using Main = DWEquipmentBonanza.DWEBPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using Common;
using System.Diagnostics;
#if SN1
using Common.Interfaces;
#endif

namespace DWEquipmentBonanza.Patches
{
	[HarmonyPatch(typeof(TooltipFactory))]
    public class TooltipFactoryPatches
    {
#if SN1
        public static bool GetInventoryDescription(StringBuilder sb, GameObject obj)
        {
            var component = obj.GetComponent<IInventoryDescriptionSN1>();
            if (component != null)
            {
                TooltipFactory.WriteDescription(sb, component.GetInventoryDescription());
                return false;
            }
            return true;
        }
#elif BELOWZERO
		private static HashSet<TechType> noEnergyBarTypes = new HashSet<TechType>()
		{
			TechType.FlashlightHelmet
		};

		public static bool AddNoBarTechType(TechType type)
		{
			if (type == TechType.None)
			{
				MethodBase callingMethod = new StackFrame(1).GetMethod();
				//Log.LogError($"{thisMethod.ReflectedType.Name}({vehicle.name}, {vehicle.GetInstanceID()}).{thisMethod.Name}() executing, invoked by: '{callingMethod.ReflectedType.Name}.{callingMethod.Name}'");
				Log.LogError($"AddNoBarTechType called with null TechType! Invoking method was '{callingMethod.ReflectedType.Name}.{callingMethod.Name}'");
				return false;
			}

			if (noEnergyBarTypes.Add(type))
			{
				Log.LogDebug($"Successfully added TechType {type.AsString()} as 'no energy bar' type");
				return true;
			}
			else
			{
				Log.LogDebug($"Failed to add TechType {type.AsString()} to 'no energy bar' set; Type is probably already on list");
			}

			return false;
		}

		public static bool ShouldShowBar(TechType target)
		{
			return !noEnergyBarTypes.Contains(target);
		}
#endif

		[HarmonyPatch("ItemCommons")]
        [HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> ItemCommonsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
#if SN1
            MethodInfo getDescription = typeof(TooltipFactoryPatches).GetMethod(nameof(TooltipFactoryPatches.GetInventoryDescription));
			if (getDescription == null)
				throw new Exception("Failed to get MethodInfo for TooltipFactoryPatches.GetInventoryDescription");

			MethodInfo writeTargetMethod = typeof(TooltipFactory).GetMethod("WriteTitle", BindingFlags.NonPublic | BindingFlags.Static);
			if (writeTargetMethod == null)
				throw new Exception("Failed to get MethodInfo for TooltipFactory.WriteTitle");
#elif BELOWZERO
			MethodInfo shouldShowBarMethod = typeof(TooltipFactoryPatches).GetMethod(nameof(TooltipFactoryPatches.ShouldShowBar));
			if (shouldShowBarMethod == null)
				throw new Exception("Failed to get MethodInfo for TooltipFactoryPatches.ShouldShowBar");

#endif
			if (Main.bLogTranspilers)
			{
				Log.LogInfo("TooltipFactoryPatches.ItemCommons(), pre-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogInfo(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}

			for (int i = 0; i < codes.Count; i++)
			{
#if SN1
                /*
				 Our target pattern is very simple: We want to find the call to TooltipFactory.WriteTitle. That's all we need to find.
				*/
                if (codes[i].Calls(writeTargetMethod))
				{
					// This is where things get a little more complicated. In effect, we need to replace:
					//		bool flag = true;
					// with
					//		bool flag = TooltipFactoryPatches.GetInventoryDescription(sb, obj);
					// To do that, we need to have put the StringBuilder on the stack, then the GameObject.
					// THEN we need to replace the ldc.i4.1 that follows with a call to our GetInventoryDescription method.
					// After that, we leave the original method's stloc.3 in order to load the 'flag' bool with the result from our method.

					codes[i + 1] = new CodeInstruction(OpCodes.Call, getDescription); // Change `ldc.i4.1` to `call GetInventoryDescription`
					codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0)); // Insert ldarg.0 in front of what is now `call GetInventoryDescription`
					codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_2)); // Insert ldarg.2 after that
					break;
				}
#elif BELOWZERO
				/*
				Our goal with the BZ version of this transpiler is to change this line:
							if ((UnityEngine.Object) component5 != (UnityEngine.Object) null && techType != TechType.FlashlightHelmet)
				into
							if ((UnityEngine.Object) component5 != (UnityEngine.Object) null && ShouldShowBar(techType))

				The IL we need to change is:
					IL_00e8: ldarg.1      // techType
					IL_00e9: ldc.i4       533 // 0x00000215
					IL_00ee: beq.s        IL_0117

				and we want to change it to:
					IL_00e8: ldarg.1      // techType
					IL_00e9: call		  ShouldShowBar
					IL_00ee: beq.s        IL_0117

				sadly, there's no conveniently-placed method call ahead of what we're trying to find, but the opcodes themselves are unique-enough to act as identifiers.
				 */

				if (codes[i].opcode == OpCodes.Ldarg_1 && codes[i + 1].opcode == OpCodes.Ldc_I4 && (int)(codes[i + 1].operand) == 533)
				{
					// We still need the TechType loaded on to the stack, so we're not changing the code at index i
					codes[i + 1] = new CodeInstruction(OpCodes.Call, shouldShowBarMethod);
					codes[i + 2] = new CodeInstruction(OpCodes.Brfalse, codes[i + 2].operand);
				}
#endif
			}

			if (Main.bLogTranspilers)
			{
				Log.LogInfo("TooltipFactoryPatches.ItemCommons(), post-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogInfo(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}
			return codes.AsEnumerable();
		}

#if BELOWZERO
		[HarmonyPatch("GetBarValue", new[] { typeof(Pickupable) } )]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> GetBarValueTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			if (Main.bLogTranspilers)
			{
				Log.LogInfo("TooltipFactoryPatches.GetBarValue(), pre-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogInfo(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}
			MethodInfo shouldShowBarMethod = typeof(TooltipFactoryPatches).GetMethod(nameof(TooltipFactoryPatches.ShouldShowBar));
			if (shouldShowBarMethod == null)
				throw new Exception("Failed to get MethodInfo for TooltipFactoryPatches.ShouldShowBar");


			for (int i = 0; i < codes.Count; i++)
			{
				/*
				Our goal with this transpiler is much as it is with ItemCommons; We need to change this code:
							&& techType != TechType.FlashlightHelmet)
				into
							&& ShouldShowBar(techType))

				The IL we need to change is:
					IL_003d: ldloc.1      // techType
					IL_003e: ldc.i4       533 // 0x00000215
					IL_0043: beq.s        IL_006c
	
				and we want to change it to:
					IL_003d: ldloc.1      // techType
					IL_003e: ldc.i4       533 // 0x00000215
					IL_0043: beq.s        IL_006c
	
				All of which, barring the precise position within the method, is almost exactly as it is with ItemCommons
				 */

				if (codes[i].opcode == OpCodes.Ldloc_1 && codes[i + 1].opcode == OpCodes.Ldc_I4 && (int)(codes[i + 1].operand) == 533)
				{
					// We still need the TechType loaded on to the stack, so we're not changing the code at index i
					codes[i + 1] = new CodeInstruction(OpCodes.Call, shouldShowBarMethod);
					codes[i + 2] = new CodeInstruction(OpCodes.Brfalse, codes[i + 2].operand);
				}
			}

			if (Main.bLogTranspilers)
			{
				Log.LogInfo("TooltipFactoryPatches.GetBarValue(), post-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogInfo(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}
			return codes.AsEnumerable();
		}
#endif
	}
}
