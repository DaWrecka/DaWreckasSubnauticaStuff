﻿using DWEquipmentBonanza;
using DWEquipmentBonanza.VehicleModules;
using DWEquipmentBonanza.MonoBehaviours;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using QModManager.Utility;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace DWEquipmentBonanza.Patches
{
#if BELOWZERO
    [HarmonyPatch(typeof(Hoverbike))]
    public class HoverbikePatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PostStart(Hoverbike __instance)
        {
            HoverbikeUpdater component = __instance.gameObject.EnsureComponent<HoverbikeUpdater>();
            component.Initialise(ref __instance);

            if (__instance.gameObject != null && __instance.gameObject.TryGetComponent<LiveMixin>(out LiveMixin mixin))
            {
                float defaultHealth = mixin.defaultHealth;
                float instanceHealthPct = Mathf.Min(mixin.GetHealthFraction(), 1f);
                float maxHealth = defaultHealth;
                maxHealth *= Main.config.SnowfoxHealthMult;

                mixin.data.maxHealth = maxHealth;
                mixin.health = maxHealth * instanceHealthPct;
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void PreUpdate(Hoverbike __instance)
        {
            __instance.gameObject.EnsureComponent<HoverbikeUpdater>()?.PreUpdate(__instance);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void OnUpdate(Hoverbike __instance)
        {
            __instance.gameObject.EnsureComponent<HoverbikeUpdater>()?.OnUpdate(__instance);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void PostUpdate(Hoverbike __instance)
        {
            __instance.gameObject.EnsureComponent<HoverbikeUpdater>()?.PostUpdate(__instance);
        }

        [HarmonyPatch("UpdateEnergy")]
        [HarmonyPrefix]
        public static void PreUpdateEnergy(Hoverbike __instance)
        {
            __instance.gameObject?.EnsureComponent<HoverbikeUpdater>().PreUpdateEnergy(__instance);
        }

        [HarmonyPatch("UpdateEnergy")]
        [HarmonyPostfix]
        public static void PostUpdateEnergy(Hoverbike __instance)
        {
            __instance.gameObject?.EnsureComponent<HoverbikeUpdater>().PostUpdateEnergy(__instance);
        }

        [HarmonyPatch("OnUpgradeModuleChange", new Type[] { typeof(int), typeof(TechType), typeof(bool) })]
        [HarmonyPostfix]
        public static void PostUpgradeModuleChange(Hoverbike __instance, int slotID, TechType techType, bool added)
        {
            __instance.gameObject.EnsureComponent<HoverbikeUpdater>()?.PostUpgradeModuleChange(slotID, techType, added, __instance);
        }

        [HarmonyPatch("PhysicsMove")]
        [HarmonyPrefix]
        public static void PrePhysicsMove(Hoverbike __instance)
        {
            __instance.gameObject.EnsureComponent<HoverbikeUpdater>()?.PrePhysicsMove(__instance);
        }

        [HarmonyPatch("PhysicsMove")]
        [HarmonyPostfix]
        public static void PostPhysicsMove(Hoverbike __instance)
        {
            __instance.gameObject.EnsureComponent<HoverbikeUpdater>()?.PostPhysicsMove(__instance);
        }

        [HarmonyPatch(nameof(Hoverbike.GetHUDValues))]
        [HarmonyPrefix]
        public static bool PreGetHUDValues(Hoverbike __instance, out float health, out float power)
        {
            if (!Main.config.bHUDAbsoluteValues)
            {
                health = 0f;
                power = 0f;
                return true;
            }

            // The HUD code is expecting values between 0.0 and 1.0 and thus multiplies both of these values by 100 before displaying them.
            // Easiest way for us to sort this out is to divide the values by 100 first. Less-efficient, but if the player is running on a CPU
            // where a division by 100 takes a significant chunk of CPU time, they probably can't play Subnautica anyway.
            health = Mathf.Floor(__instance.liveMixin.health) * 0.01f;
            float charge = __instance.energyMixin.charge;
            float capacity = __instance.energyMixin.capacity;
            //power = ((charge > 0f && capacity > 0f) ? (charge / capacity) : 0f);
            power = Mathf.Floor(charge) * 0.01f;
            return false;
        }

        [HarmonyPatch("HoverEngines")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HoverEnginesTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo waterHoverMethod = typeof(HoverbikePatches).GetMethod(nameof(HoverbikePatches.WaterHoverMode));

            if (waterHoverMethod == null)
                throw new Exception("null method for WaterHoverMode");

            int maxIndex = codes.Count - 14; // We search for a six-opcode pattern and a 15-opcode pattern. If we go past (count-14) then we'll get an Index Out of Range exception.
                                            // So we limit our max index here.
            int i = -1;
            int overWaterIndex = -1;
            int wasOnGroundIndex = -1;

            if (Main.bLogTranspilers)
            {
                Log.LogDebug("Dump of Hoverbike.HoverEngines method, pre-transpiler:");
                for (i = 0; i < codes.Count; i++)
                    Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");

                i = -1;
            }

            while (++i < maxIndex && (overWaterIndex == -1 || wasOnGroundIndex == -1))
            {
                /* 
                 * IL_007E: ldc.r4    3
                 * IL_0083: add
                 * IL_0084: bge.un.s IL_008D
                 * IL_0086: ldarg.0
                 * IL_0087: ldc.i4.1
                 * IL_0088: stfld     bool Hoverbike::overWater
                 */
                if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i + 1].opcode == OpCodes.Add && (codes[i + 2].opcode == OpCodes.Bge_Un || codes[i + 2].opcode == OpCodes.Bge_Un_S)
                    && codes[i + 3].opcode == OpCodes.Ldarg_0 && codes[i + 4].opcode == OpCodes.Ldc_I4_1 && codes[i + 5].opcode == OpCodes.Stfld)
                {
                    overWaterIndex = i + 4;
                    Log.LogDebug($"Located overWater segment at {String.Format("0x{0:X4}", overWaterIndex)}");
                    //codes[flag2index + 2] = new CodeInstruction(OpCodes.Callvirt, usingJumpjetsMethod);
                    codes[overWaterIndex] = new CodeInstruction(OpCodes.Callvirt, waterHoverMethod);
                    // We now need to insert another ldarg.0 before this call.
                    codes.Insert(overWaterIndex, new CodeInstruction(OpCodes.Ldarg_0));
                }
                /* We're using a 15-opcode search pattern here just to be on the safe side. Maxim 37: There is no overkill. There is only "open fire" and "I need to reload"
                 * IL_00AD: stloc.0
                 * IL_00AE: ldloc.0
                 * IL_00AF: brfalse IL_015D
                 * IL_00B4: ldarg.0
                 * IL_00B5: ldfld     bool Hoverbike::jumpReset
                 * IL_00BA: brfalse IL_015D
                 * IL_00BF: ldarg.0
                 * IL_00C0: ldfld     bool Hoverbike::wasOnGround
                 * IL_00C5: brfalse IL_015D
                 * IL_00CA: ldarg.0
                 * IL_00CB: ldfld     bool Hoverbike::jumpEnabled
                 * IL_00D0: brfalse IL_015D
                 * IL_00D5: ldarg.0
                 * IL_00D6: ldc.i4.0
                 * IL_00D7: stfld     bool Hoverbike::jumpReset
                 */
                else if (codes[i +  0].opcode == OpCodes.Stloc_0   // * IL_00AD: stloc.0
                    &&   codes[i +  1].opcode == OpCodes.Ldloc_0   // * IL_00AE: ldloc.0
                    &&   codes[i +  2].opcode == OpCodes.Brfalse   // * IL_00AF: brfalse IL_015D
                    &&   codes[i +  3].opcode == OpCodes.Ldarg_0   // * IL_00B4: ldarg.0
                    &&   codes[i +  4].opcode == OpCodes.Ldfld     // * IL_00B5: ldfld     bool Hoverbike::jumpReset
                    &&   codes[i +  5].opcode == OpCodes.Brfalse   // * IL_00BA: brfalse IL_015D
                    &&   codes[i +  6].opcode == OpCodes.Ldarg_0   // * IL_00BF: ldarg.0
                    &&   codes[i +  7].opcode == OpCodes.Ldfld     // * IL_00C0: ldfld     bool Hoverbike::wasOnGround
                    &&   codes[i +  8].opcode == OpCodes.Brfalse   // * IL_00C5: brfalse IL_015D
                    &&   codes[i +  9].opcode == OpCodes.Ldarg_0   // * IL_00CA: ldarg.0
                    &&   codes[i + 10].opcode == OpCodes.Ldfld     // * IL_00CB: ldfld     bool Hoverbike::jumpEnabled
                    &&   codes[i + 11].opcode == OpCodes.Brfalse   // * IL_00D0: brfalse IL_015D
                    &&   codes[i + 12].opcode == OpCodes.Ldarg_0   // * IL_00D5: ldarg.0
                    &&   codes[i + 13].opcode == OpCodes.Ldc_I4_0  // * IL_00D6: ldc.i4.0
                    &&   codes[i + 14].opcode == OpCodes.Stfld)    // * IL_00D7: stfld     bool Hoverbike::jumpReset
                {
                    // We're going to replace the codes at i+6 to i+7 with ldc.i4.1 and nop, placing 1 (true) on the stack instead of the result of the wasOnGround() method
                    wasOnGroundIndex = i + 6;
                    Log.LogDebug($"Located wasOnGround segment at {String.Format("0x{0:X4}", wasOnGroundIndex)}");
                    codes[wasOnGroundIndex + 0] = new CodeInstruction(OpCodes.Ldc_I4_1);
                    codes[wasOnGroundIndex + 1] = new CodeInstruction(OpCodes.Nop);
                }
            }

            if (Main.bLogTranspilers)
            {
                Log.LogDebug("Dump of Hoverbike.HoverEngines method, post-transpiler:");
                for (i = 0; i < codes.Count; i++)
                    Log.LogDebug(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");

                i = -1;
            }

            Log.LogDebug("Hoverbike.HoverEngine transpiler done");

            return codes.AsEnumerable();
        }

        public static bool WaterHoverMode(Hoverbike instance)
        {
            // This method is called in Hoverbike.HoverEngines(), made so via transpiler, and is only called once the code has already established that the Hoverbike is over water.
            // Also, since we want to fool the game into thinking the hoverbike is not over water if a valid travel module is installed, we need to invert the boolean.
            // This is because the return result of this method is assigned to the hoverbike's "over water" field; if we want the hoverbike to believe it is over land and thus is allowed
            // to boost and jump, we have to say "No, you're not over water" if a travel module is present.

            if (instance?.gameObject == null)
                return true;

            return !HoverbikeUpdater.StaticHasTravelModule(instance); //instance.modules.GetCount(Main.prefabHbWaterTravelModule.TechType) < 1;
        }
    }
#endif
}
