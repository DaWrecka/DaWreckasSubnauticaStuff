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
    [HarmonyPatch(typeof(Knife))]
    public static class KnifePatches
    {
		public static Dictionary<TechType, float> MakeHarvestables { get; private set; } = new Dictionary<TechType, float>();
		internal static void AddHarvestable(TechType ObjectToBeKnifed, float LiveMixinHealth)
		{
			MakeHarvestables[ObjectToBeKnifed] = LiveMixinHealth;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(nameof(Knife.OnToolUseAnim))]
		public static IEnumerable<CodeInstruction> ToolUseTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo interceptMethod = typeof(KnifePatches).GetMethod(nameof(KnifePatches.InterceptTrace));
			MethodInfo TraceMethod = typeof(UWE.Utils).GetMethod(nameof(UWE.Utils.TraceFPSTargetPosition), new Type[] { typeof(GameObject), typeof(float), typeof(GameObject).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(bool) });
			if (interceptMethod == null)
				throw new Exception("Failed to get MethodInfo for InterceptTrace method!");
			if (TraceMethod == null)
				throw new Exception("Failed to get MethodInfo for UWE.Utils.TraceFPSTargetPosition!");

			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			for(int i = 0; i < codes.Count; i++)
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
				The call is all we need to change.
				*/
				//if(codes[i].opcode == OpCodes.Call && object.Equals(TraceMethod, codes[i].operand))


				// And then it turns out we can do it so much more easily!
				if(codes[i].Calls(TraceMethod))
                {
                    Log.LogDebug($"ToolUseTranspiler found Call in IL at index {i}");
                    codes[i] = new CodeInstruction(OpCodes.Call, interceptMethod);
					break;
				}
			}

			return codes.AsEnumerable();
		}
		
		public static bool InterceptTrace(GameObject ignoreObj, float maxDist, ref GameObject closestObj, ref Vector3 position, out Vector3 normal, bool includeUseableTriggers = true)
        {
            bool result = UWE.Utils.TraceFPSTargetPosition(ignoreObj, maxDist, ref closestObj, ref position, out normal, includeUseableTriggers);
            TechType key = (closestObj != null ? CraftData.GetTechType(closestObj) : TechType.None);
			if (key == TechType.None)
				return result;

			if (MakeHarvestables.TryGetValue(key, out float value))
            {
				Log.LogDebug($"InterceptTrace found closestObj with TechType {key}");
				LiveMixin component = closestObj.EnsureComponent<LiveMixin>();
				if (component.data == null)
				{
					Log.LogDebug($"Adding LiveMixin data to object {closestObj.GetInstanceID()} with TechType {key}", null, false);
					component.data = new LiveMixinData();
					component.data.maxHealth = value;
					component.health = value;
				}
            }

            return result;
        }
    }
}
