using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GravTrapBeacons
{
    [HarmonyPatch(typeof(Gravsphere))]
    class GravspherePatches
    {
        [HarmonyPatch(nameof(Gravsphere.Start))]
        [HarmonyPostfix]
        internal static void PostStart(Gravsphere __instance)
        {
            var obj = __instance.gameObject;

            var ping = __instance.gameObject.EnsureComponent<PingInstance>();
            ping.pingType = PingType.Signal; // u can make one with SML
            ping.origin = obj.transform;
            ping.SetLabel("GravTrap");

            // Gravtraps default to the Near celllevel, so if we want the ping to be visible from a distance greater than 50m, we need to change that.
            var lwe = __instance.gameObject.EnsureComponent<LargeWorldEntity>();
            lwe.cellLevel = Main.GravCellLevel;
            lwe.initialCellLevel = Main.GravCellLevel;
        }
    }
}
