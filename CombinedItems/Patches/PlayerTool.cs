using HarmonyLib;

namespace CombinedItems.Patches
{
    [HarmonyPatch(typeof(PlayerTool))]
    public static class PlayerToolPatches
    {
        private static TechType powerGlideTechType => Main.GetModTechType("PowerglideEquipable");

        [HarmonyPostfix]
        [HarmonyPatch("animToolName", MethodType.Getter)]
        public static void animToolName_PostGet(ref string __result)
        {
            if (__result == powerGlideTechType.AsString(true))
                __result = TechType.Seaglide.AsString(true);
        }
    }
}