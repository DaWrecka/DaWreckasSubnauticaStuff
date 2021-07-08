using HarmonyLib;
using System.Collections.Generic;

namespace DWEquipmentBonanza.Patches
{
    [HarmonyPatch(typeof(PlayerTool))]
    public static class PlayerToolPatches
    {
        // The key is a modded TechType, while the value is a vanilla TechType.
        // The idea is that the modded TechType will use the animation of the TechType in the key
        // For example, if the Key is "powerglideequipable", then value will be for Seaglide.
        private static Dictionary<string, TechType> animToolSubstitutions = new Dictionary<string, TechType>();

        //private static TechType powerGlideTechType => Main.GetModTechType("PowerglideEquipable");

        [HarmonyPatch(nameof(PlayerTool.Awake))]
        [HarmonyPostfix]
        public static void PostAwake(ref PlayerTool __instance)
        {
            if (__instance is Knife)
            {
                TechType itemTechType = CraftData.GetTechType(__instance.gameObject);
                if (itemTechType == TechType.None)
                    return; // We can't do much without this.

                Knife knife = __instance as Knife;
                float damage;
#if BELOWZERO
                float spikeyTrapDamage;
#endif
                if (itemTechType == TechType.HeatBlade)
                {
                    damage = Main.config.HeatbladeDamage;
#if BELOWZERO
                    spikeyTrapDamage = Main.config.HeatbladeTentacleDamage;
#endif
                }
                else
                {
                    damage = Main.config.KnifeDamage;
#if BELOWZERO
                    spikeyTrapDamage = Main.config.KnifeTentacleDamage;
#endif
                }

                knife.damage = damage;
                knife.bleederDamage = damage;
#if BELOWZERO
                knife.spikeyTrapDamage = spikeyTrapDamage;
#endif
            }
        }

        public static void AddToolSubstitution(TechType key, TechType value)
        {
            string techKey = key.AsString(true);
            if (!animToolSubstitutions.ContainsKey(techKey))
                animToolSubstitutions.Add(techKey, value);
        }

        [HarmonyPostfix]
        [HarmonyPatch("animToolName", MethodType.Getter)]
        public static void animToolName_PostGet(ref string __result)
        {
            /*if (__result == powerGlideTechType.AsString(true))
                __result = TechType.Seaglide.AsString(true);*/
            if (animToolSubstitutions.TryGetValue(__result, out TechType sub))
                __result = sub.AsString(true);
        }
    }
}