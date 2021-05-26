using HarmonyLib;
using IngredientsFromScanning_BZ.Configuration;
using QModManager.API.ModLoading;
//using SMLHelper.V2.Json;
//using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
using System.Reflection;

namespace IngredientsFromScanning_BZ
{
	[QModCore]
	public static class Main
	{
		internal const string version = "1.0.0.1";

		internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();

		[QModPatch]
		public static void Load()
		{
			var assembly = Assembly.GetExecutingAssembly();
			new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
			config.Init();
		}
	}
}