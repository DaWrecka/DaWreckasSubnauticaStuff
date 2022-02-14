using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Common
{
	internal static class AssemblyUtils
	{
		// Copied from weskey007's ShowAvailableItems code, this can be used to patch methods from a different assembly without making a hard dependency on that assembly.
		// This version has been modified to return true if the target was successfully found and patched, and false in any other situation

		public static bool PatchIfExists(Harmony harmony, string assemblyName, string typeName, string methodName, HarmonyMethod prefix, HarmonyMethod postfix, HarmonyMethod transpiler)
		{
			var assembly = FindAssembly(assemblyName);
			if (assembly == null)
			{
				Log.LogDebug("Could not find assembly " + assemblyName + ", don't worry this probably just means you don't have the mod installed");
				return false;
			}

			Type targetType = assembly.GetType(typeName);
			if (targetType == null)
			{
				Log.LogWarning("Could not find class/type " + typeName + ", the mod/assembly " + assemblyName + " might have changed");
				return false;
			}

			Log.LogDebug("Found targetClass " + typeName);
			var targetMethod = AccessTools.Method(targetType, methodName);
			if (targetMethod != null)
			{
				Log.LogDebug("Found targetMethod " + typeName + "." + methodName + ", Patching...");
				harmony.Patch(targetMethod, prefix, postfix, transpiler);
				Log.LogDebug("Patched " + typeName + "." + methodName);
			}
			else
			{
				Log.LogWarning("Could not find method " + typeName + "." + methodName + ", the mod/assembly " + assemblyName + " might have been changed");
				return false;
			}

			return true;
		}

		private static Assembly FindAssembly(string assemblyName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				if (assembly.FullName.StartsWith(assemblyName + ","))
					return assembly;

			return null;
		}
	}
}
