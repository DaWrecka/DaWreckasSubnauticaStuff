﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWE;

namespace CombinedItems.Patches
{
    [HarmonyPatch(typeof(SpikeyTrap))]
    public class SpikeyTrapPatches
    {
        [HarmonyPatch("Start")]
        public static void PostAwake(ref SpikeyTrap __instance)
        {
            __instance.detachDamage = Main.config.SpikeyTrapTentacleHealth;
        }
    }
}
