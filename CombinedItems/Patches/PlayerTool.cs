using HarmonyLib;

namespace CombinedItems.Patches
{
    [HarmonyPatch(typeof(PlayerTool))]
    public static class PlayerToolPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("animToolName", MethodType.Getter)]
        public static void animToolName_PostGet(ref string __result)
        {
            if (__result == Main.prefabPowerglide.TechType.AsString(true))
                __result = TechType.Seaglide.AsString(true);
        }
    }
}