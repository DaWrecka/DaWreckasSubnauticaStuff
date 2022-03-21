using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CustomiseOxygen
{
    [HarmonyPatch(typeof(OxygenManager))]
    public static class OxygenManagerPatches
    {
        private static readonly FieldInfo sourcesInfo = typeof(OxygenManager).GetField("sources", BindingFlags.Instance | BindingFlags.NonPublic);
        public static bool bAllowAddOxygen { get; private set; }

        public static float AddSpecialtyOxygen(OxygenManager manager, float oxygen)
        {
            bAllowAddOxygen = true;
            float added = manager.AddOxygen(oxygen);
            bAllowAddOxygen = false;
            return added;
        }

        public static bool AllowRegenerateOxygen(Player main)
        {
            return main.IsSwimming() || Main.config.bManualRefill;
        }
#if SUBNAUTICA_STABLE
        // Transpiler for the SpecialtyTanks.Update() method, from the DeathRun and NitrogenMod mods. Invoked from Main.Load() using PatchIfExists from weskey007
        public static IEnumerable<CodeInstruction> SpecialtyTankUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo specialtyMethod = typeof(OxygenManagerPatches).GetMethod(nameof(OxygenManagerPatches.AddSpecialtyOxygen));
            MethodInfo AddOxygenMethod = typeof(OxygenManager).GetMethod(nameof(OxygenManager.AddOxygen), new Type[] { typeof(float) });
            MethodInfo PlayerIsSwimmingMethod = typeof(Player).GetMethod(nameof(Player.IsSwimming), new Type[] { });
            MethodInfo RegenMethod = typeof(OxygenManagerPatches).GetMethod(nameof(OxygenManagerPatches.AllowRegenerateOxygen), new Type[] { typeof(Player) });
            if (specialtyMethod == null)
                throw new Exception("Failed to get MethodInfo for AddSpecialtyOxygen method!");
            if (RegenMethod == null)
                throw new Exception("Failed to get MethodInfo for AllowRegenerateOxygen method!");
            if (AddOxygenMethod == null)
                throw new Exception("Failed to get MethodInfo for OxygenManager.AddOxygen!");
            if (PlayerIsSwimmingMethod == null)
                throw new Exception("Failed to get MethodInfo for Player.IsSwimming!");


            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
#if LOGTRANSPILERS
            Log.LogDebug("OxygenManager.AddOxygen(), pre-transpiler:");
            for (int i = 0; i < codes.Count; i++)
                Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
#endif


            for (int i = 0; i < codes.Count; i++)
            {
                // Nice and simple; there are two points during the Update() method that the SpecialtyTanks class calls AddOxygen
                // We want these to go through even in manual refill mode, because that's the point of the specialty tanks.
                if (codes[i].Calls(PlayerIsSwimmingMethod))
                {
                    Log.LogDebug($"Call to IsSwimming found in IL at index {i}");
                    codes[i] = new CodeInstruction(OpCodes.Call, RegenMethod);
                }
                else if (codes[i].Calls(AddOxygenMethod))
                {
                    Log.LogDebug($"Call to AddOxygen found in IL at index {i}");
                    codes[i] = new CodeInstruction(OpCodes.Call, specialtyMethod);
                }
            }

#if LOGTRANSPILERS
            Log.LogDebug("OxygenManager.AddOxygen(), post-transpiler:");
            for (int i = 0; i < codes.Count; i++)
                Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");
#endif
            return codes.AsEnumerable();
        }
#endif

        [HarmonyPrefix]
        [HarmonyPatch(nameof(OxygenManager.AddOxygen))]
        public static bool Prefix(ref OxygenManager __instance, float __result, float secondsToAdd)
        {
            if (bAllowAddOxygen)
                return true;

            if (Main.config.bManualRefill)
            {
                if (sourcesInfo == null)
                    return true; // Fail safe

#if SUBNAUTICA_STABLE
                List<Oxygen> oxySources = (List<Oxygen>)sourcesInfo.GetValue(__instance);
#elif BELOWZERO
                List<IOxygenSource> oxySources = (List<IOxygenSource>)sourcesInfo.GetValue(__instance);
#endif
                if (oxySources == null)
                    return true;

                float O2added = 0f;
                for (int i = 0; i < oxySources.Count; i++)
                {
#if SUBNAUTICA_STABLE
                    if (!oxySources[i].isPlayer)
#elif BELOWZERO
                    if (!oxySources[i].IsPlayer())
#endif
                        continue;

                    float num = oxySources[i].AddOxygen(secondsToAdd);
                    secondsToAdd -= num;
                    O2added += num;
                    if (Utils.NearlyEqual(secondsToAdd, 0f, 1.401298E-45f))
                    {
                        break;
                    }
                }
                __result = O2added;

                return false;
            }

            return true;
        }
    }
}
