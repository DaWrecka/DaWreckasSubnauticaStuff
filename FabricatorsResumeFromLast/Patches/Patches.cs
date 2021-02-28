using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Gendarme;
using UnityEngine;
using UnityEngine.UI;

namespace FabricatorsResumeFromLast.Patches
{
    [HarmonyPatch(typeof(uGUI_CraftingMenu), nameof(uGUI_CraftingMenu.Open))]
    class uGUI_CraftingMenu_Patch
    {
    }
}
