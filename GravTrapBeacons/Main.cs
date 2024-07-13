using Common;
using HarmonyLib;
#if BEPINEX
using BepInEx;
using BepInEx.Logging;
#elif QMM
	using QModManager.API.ModLoading;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UWE;

namespace GravTrapBeacons
{
#if BEPINEX
    [BepInPlugin(GUID, pluginName, version)]
#if BELOWZERO
	[BepInProcess("SubnauticaZero.exe")]
#elif SN1
    [BepInProcess("Subnautica.exe")]
#endif
    public class GravTrapBeaconPlugin: BaseUnityPlugin
    {
#elif QMM
    [QModCore]
	public static class GravTrapBeaconPlugin
    {
#endif
        #region[Declarations]
        public const string
            MODNAME = "GravTrapBeacons",
            AUTHOR = "dawrecka",
            GUID = "com." + AUTHOR + "." + MODNAME;
        private const string pluginName = "Grav Trap Beacons";
        internal const string version = "1.0.0.0";
        #endregion

        private static readonly Harmony harmony = new Harmony(GUID);
        internal const LargeWorldEntity.CellLevel GravCellLevel = LargeWorldEntity.CellLevel.Global;

#if QMM
        [QModPatch]
#endif
        public void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            foreach (string s in new HashSet<string>() { "Gravsphere", "GravTrapMk2" })
            {
                TechType tt = TechTypeUtils.GetTechType(s);
                if (tt == TechType.None)
                {
                    Log.LogWarning($"Could not retrieve TechType for string {s}");
                }
                else
                {
                    var classid = CraftData.GetClassIdForTechType(TechType.Gravsphere);
                    if (WorldEntityDatabase.TryGetInfo(classid, out var worldEntityInfo))
                    {
                        worldEntityInfo.cellLevel = LargeWorldEntity.CellLevel.Global;

                        WorldEntityDatabase.main.infos[classid] = worldEntityInfo;
                    }
                }
            }
        }
    }
}
