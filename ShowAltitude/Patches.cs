using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if !LEGACY
using TMPro;
#endif

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
			// 2024-12-27: So it turns out that at some point after I first wrote this method, the Ocean.GetDepthOf method
			//   returns 0 for any value above sea level. Since vehicles get depth using that method, there's no point
			//   in going through the vehicle go get the player's depth
			int depth = int.MinValue;
			if (Player.main != null)
			{
#if LEGACY
					depth = Mathf.FloorToInt(Ocean.main.GetOceanLevel() - Player.main.gameObject.transform.position.y);
#else
				depth = Mathf.FloorToInt(Ocean.GetOceanLevel() - Player.main.gameObject.transform.position.y);
#endif
			}

			if (__instance._depthMode == uGUI_DepthCompass.DepthMode.Player)
			{
				if (depth != LastPlayerDepth)
				{
					LastPlayerDepth = depth;
					string depthText = (depth < 0 ? "+" : "") + IntStringCache.GetStringForInt(Math.Abs(depth));

					__instance.depthText.text = depthText;
					__instance.suffixText.text = __instance._meterSuffix;

				}
			}
			else if (__instance._depthMode == uGUI_DepthCompass.DepthMode.Submersible)
			{
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
