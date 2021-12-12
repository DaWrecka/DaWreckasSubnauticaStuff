using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace DWEquipmentBonanza.Patches
{
    [HarmonyPatch(typeof(Creature))]
    public class TestPatches
    {
        [HarmonyPatch(nameof(Creature.Start))]
        [HarmonyPostfix]
        private static void PostCreatureStart(Creature __instance)
        {
            if (__instance is not ReaperLeviathan && __instance is not SeaDragon && __instance is not GhostLeviathan)
                return;

            bool bIsReaper = (__instance is ReaperLeviathan);
            bool bIsSeaDragon = (__instance is SeaDragon);
            bool bIsGhost = (__instance is GhostLeviathan);
            string creatureType = __instance.GetType().ToString();
            string UID = "<invalid>";
            PrefabIdentifier IdComponent = __instance.gameObject.GetComponent<PrefabIdentifier>();
            if (IdComponent != null)
                UID = IdComponent.Id;

            //Log.LogDebug($"PostCreatureStart: Type is {creatureType}, unique ID = {UID}, leashPosition = {__instance.leashPosition.ToString()}");
            LeviathanLogging.Main.saveData.AddLeviathan(UID, creatureType, __instance.leashPosition);

        }
    }
}
