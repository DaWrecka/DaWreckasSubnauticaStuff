using Common;
using HarmonyLib;
using PartsFromScanning.Configuration;
#if NAUTILUS
using Nautilus;
using Nautilus.Options;
using Nautilus.Handlers;
#else
using SMLHelper.V2.Handlers;
#endif

#if BEPINEX
	using BepInEx;
	using BepInEx.Logging;
	using BepInEx.Configuration;
#elif QMM
	using QModManager.API.ModLoading;
#endif

//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
using System.Reflection;

namespace PartsFromScanning
{
#if BEPINEX
	[BepInPlugin(GUID, pluginName, version)]
	#if BELOWZERO
		[BepInProcess("SubnauticaZero.exe")]
	#elif SN1
		[BepInProcess("Subnautica.exe")]
	#endif
	[BepInDependency("com.snmodding.nautilus")]
	public class PartsFromScanningPlugin: BaseUnityPlugin
	{
#elif QMM
	[QModCore]
	public static class PartsFromScanningPlugin
	{
#endif
#region[Declarations]
		public const string
			MODNAME = "PartsFromScanning",
			AUTHOR = "dawrecka",
			GUID = "com." + AUTHOR + "." + MODNAME;
		private const string pluginName = "Parts from Scanning";
		public const string version = "1.20.1.1";
#endregion

		private static readonly Harmony harmony = new Harmony(GUID);
		internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();
#if QMM
		[QModPatch]
#endif
		public void Start()
		{
			var assembly = Assembly.GetExecutingAssembly();
			harmony.PatchAll(assembly);
			Log.InitialiseLog(GUID);
			Log.LogInfo("Parts from Scanning version " + version + " initialised");
			config.Init();
		}
	}
}