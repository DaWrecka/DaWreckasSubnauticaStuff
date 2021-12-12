using Common;
using HarmonyLib;
using QModManager.API.ModLoading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UWE;

namespace GravTrapBeacons
{
    [QModCore]
    public class Main
    {
        internal const LargeWorldEntity.CellLevel GravCellLevel = LargeWorldEntity.CellLevel.Global;
        internal const string version = "1.0.0.0";

        [QModPatch]
        public void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
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
