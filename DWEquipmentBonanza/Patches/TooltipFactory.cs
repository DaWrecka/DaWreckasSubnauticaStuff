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
#if SUBNAUTICA_STABLE
using Common.Interfaces;
#endif

namespace DWEquipmentBonanza.Patches
{
#if SUBNAUTICA_STABLE
	[HarmonyPatch(typeof(TooltipFactory))]
    public class TooltipFactoryPatches
    {
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

        [HarmonyPatch("ItemCommons")]
        [HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> ItemCommonsTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			MethodInfo getDescription = typeof(TooltipFactoryPatches).GetMethod(nameof(TooltipFactoryPatches.GetInventoryDescription));
			if (getDescription == null)
				throw new Exception("Failed to get MethodInfo for TooltipFactoryPatches.GetInventoryDescription");

			MethodInfo writeTargetMethod = typeof(TooltipFactory).GetMethod("WriteTitle", BindingFlags.NonPublic | BindingFlags.Static);
			if (writeTargetMethod == null)
				throw new Exception("Failed to get MethodInfo for TooltipFactory.WriteTitle");

			if (Main.bLogTranspilers)
			{
				Log.LogInfo("TooltipFactoryPatches.ItemCommons(), pre-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogInfo(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}

			for (int i = 0; i < codes.Count; i++)
			{
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
			}

			if (Main.bLogTranspilers)
			{
				Log.LogInfo("TooltipFactoryPatches.ItemCommons(), post-transpiler:");
				for (int i = 0; i < codes.Count; i++)
					Log.LogInfo(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
			}
			return codes.AsEnumerable();
		}
	}
#endif
}
