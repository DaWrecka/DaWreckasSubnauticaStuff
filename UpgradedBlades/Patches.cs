using HarmonyLib;

namespace UpgradedBlades.Patches
{
    [HarmonyPatch(typeof(PlayerTool), "animToolName", MethodType.Getter)]
    public static class PlayerTool_animToolName_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref string __result)
        {
            if (__result == Main.prefabBlade1.TechType.AsString(true))
                __result = TechType.Knife.AsString(true);
        }
    }
}