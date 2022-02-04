using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShowAltitude
{
    [HarmonyPatch]
    public class Patches
    {
        public static int LastPlayerDepth { get; private set; }
        public static int LastSubmersibleDepth { get; private set; }

        [HarmonyPatch(typeof(uGUI_DepthCompass), nameof(uGUI_DepthCompass.UpdateDepth))]
        [HarmonyPostfix]
        public static void PostUpdateDepth(uGUI_DepthCompass __instance)
        {
            if (__instance._depthMode == uGUI_DepthCompass.DepthMode.Player)
            {
                int depth = int.MinValue;
                if (Player.main != null)
                {
#if SUBNAUTICA
                    depth = Mathf.FloorToInt(Ocean.main.GetOceanLevel() - Player.main.gameObject.transform.position.y);
#elif BELOWZERO
                    depth = Mathf.FloorToInt(Ocean.GetOceanLevel() - Player.main.gameObject.transform.position.y);
#endif
                    if (depth != LastPlayerDepth)
                    {
                        LastPlayerDepth = depth;
                        string depthText = (depth < 0 ? "+" : "") + IntStringCache.GetStringForInt(Math.Abs(depth));

                        __instance.depthText.text = depthText;
                        __instance.suffixText.text = __instance._meterSuffix;

                    }
                }
            }
            else if (__instance._depthMode == uGUI_DepthCompass.DepthMode.Submersible)
            {
                int depth = 0;
#if SUBNAUTICA_STABLE
                Vehicle vehicle = Player.main.GetVehicle();
                if (vehicle != null)
                {
                    int crushDepth = 0;
                    vehicle.GetDepth(out depth, out crushDepth);
                }

#elif BELOWZERO

                CrushDamage crushDamage = Player.main.GetCrushDamage();
                if (crushDamage != null && (Player.main.GetVehicle() != null || Player.main.IsPilotingSeatruck()))
                {
                    depth = Mathf.FloorToInt(crushDamage.GetDepth() + (Player.main != null && Player.main.lilyPaddlerHypnosis.IsHypnotized() ? Player.main.lilyPaddlerHypnosis.GetDepthError() : 0));
                }
#endif

                if (LastSubmersibleDepth != depth)
                {
                    LastSubmersibleDepth = depth;
                    string depthText = (depth < 0 ? "+" : "") + IntStringCache.GetStringForInt(Math.Abs(depth));
                    __instance.submersibleDepth.text = depthText;
                }
            }
        }
    }
}
