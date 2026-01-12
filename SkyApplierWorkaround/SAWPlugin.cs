using BepInEx;
using HarmonyLib;
#if QMM
using QModManager.API.ModLoading;
#endif

using System.Reflection;
using Common;

namespace TrueSolarPowerCells
{
#if BEPINEX
	[BepInPlugin(GUID, pluginName, version)]
	[BepInProcess("Subnautica.exe")]
	public class SAWPlugin : BaseUnityPlugin
	{
#elif QMM
	[QModCore]
	public class SAWPlugin
	{
#endif
		public const string
			MODNAME = "SkyApplierWorkaround",
			AUTHOR = "dawrecka",
			GUID = "com." + AUTHOR + "." + MODNAME;
		private const string pluginName = "SkyApplierWorkaround";
		public const string version = "1.20.0.0";

#if QMM
		[QModPatch]
#endif

		public void Start()
		{
			var assembly = Assembly.GetExecutingAssembly();
			new Harmony(GUID).PatchAll(assembly);
			Log.InitialiseLog(GUID);
			Log.LogInfo("SkyApplier Workaround initialised");
		}
	}
}
