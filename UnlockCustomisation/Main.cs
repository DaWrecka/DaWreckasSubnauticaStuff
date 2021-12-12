using Common;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace UnlockCustomisation
{
    [QModCore]
    public class Main
    {
        public static UCConfig config { get; } = OptionsPanelHandler.RegisterModOptions<UCConfig>();
        private static readonly Assembly myAssembly = Assembly.GetExecutingAssembly();

        [QModPatch]
        public static void Load()
        {
            var harmony = new Harmony($"DaWrecka_{myAssembly.GetName().Name}");
            harmony.PatchAll(myAssembly);
            config.Patch();
        }
    }

}
