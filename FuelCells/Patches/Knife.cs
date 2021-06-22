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

namespace FuelCells.Patches
{
    [HarmonyPatch(nameof(Knife))]
    public class KnifePatches
    {
        private static Dictionary<TechType, float> MakeHarvestables = new Dictionary<TechType, float>();

		internal static void AddHarvestable(TechType ObjectToBeKnifed, float LiveMixinHealth)
		{
			MakeHarvestables[ObjectToBeKnifed] = LiveMixinHealth;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(nameof(Knife.OnToolUseAnim))]
		public static IEnumerable<CodeInstruction> ToolUseTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo interceptMethod = typeof(KnifePatches).GetMethod(nameof(KnifePatches.InterceptTrace));
			if (interceptMethod == null)
				throw new Exception("Failed to get MethodInfo for InterceptTrace method!");

			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			int i = -1;
			int maxIndex = codes.Count - 12;

			while (++i < maxIndex)
			{
				/*
				 Our target pattern is as follows:

					IL_0000: ldloca.s  V_1
					IL_0002: initobj   [UnityEngine.CoreModule]UnityEngine.Vector3
					IL_0008: ldnull
					IL_0009: stloc.2
					IL_000A: ldsfld    class Player Player::main
					IL_000F: callvirt  instance class [UnityEngine.CoreModule]UnityEngine.GameObject [UnityEngine.CoreModule]UnityEngine.Component::get_gameObject()
					IL_0014: ldarg.0
					IL_0015: ldfld     float32 Knife::attackDist
					IL_001A: ldloca.s  V_2
					IL_001C: ldloca.s  V_1
					IL_001E: ldloca.s  V_3
					IL_0020: ldc.i4.1
					IL_0021: call      bool ['Assembly-CSharp-firstpass']UWE.Utils::TraceFPSTargetPosition(class [UnityEngine.CoreModule]UnityEngine.GameObject, float32, class [UnityEngine.CoreModule]UnityEngine.GameObject&, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3&, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3&, bool)
				The call is all we need to change; the rest of the stuff is just so we know we're targetting the *right* call.
				*/
				if ((codes[i].opcode == OpCodes.Ldloca || codes[i].opcode == OpCodes.Ldloca_S)
					&& codes[i + 1].opcode == OpCodes.Initobj
					&& codes[i + 2].opcode == OpCodes.Ldnull
					&& codes[i + 3].opcode == OpCodes.Stloc_2
					&& codes[i + 4].opcode == OpCodes.Ldsfld
					&& codes[i + 5].opcode == OpCodes.Callvirt
					&& codes[i + 6].opcode == OpCodes.Ldarg_0
					&& codes[i + 7].opcode == OpCodes.Ldfld
					&& (codes[i + 8].opcode  == OpCodes.Ldloca || codes[i + 8].opcode  == OpCodes.Ldloca_S)
					&& (codes[i + 9].opcode  == OpCodes.Ldloca || codes[i + 9].opcode  == OpCodes.Ldloca_S)
					&& (codes[i + 10].opcode == OpCodes.Ldloca || codes[i + 10].opcode == OpCodes.Ldloca_S)
					&& codes[i + 11].opcode == OpCodes.Ldc_I4_1
					&& codes[i + 12].opcode == OpCodes.Call)
                {
                    Log.LogDebug($"ToolUseTranspiler found Call in IL at index {i + 12}");
                    codes[i + 12] = new CodeInstruction(OpCodes.Callvirt, interceptMethod);
					break;
				}
			}

			return codes.AsEnumerable();
		}
		
		public static bool InterceptTrace(GameObject ignoreObj, float maxDist, ref GameObject closestObj, ref Vector3 position, out Vector3 normal, bool includeUseableTriggers = true)
        {
            bool result = UWE.Utils.TraceFPSTargetPosition(ignoreObj, maxDist, ref closestObj, ref position, out normal, includeUseableTriggers);
            TechType key = CraftData.GetTechType(closestObj);
            if (MakeHarvestables.ContainsKey(key))
            {
                LiveMixin component = closestObj.EnsureComponent<LiveMixin>();
				if (component.data == null)
				{
					component.data = new LiveMixinData();
					component.data.maxHealth = MakeHarvestables[key];
				}
            }

            return result;
        }
    }
}
