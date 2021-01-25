using HarmonyLib;
using Logger = QModManager.Utility.Logger;
using CustomiseYourStorage_BZ;
using System.Collections.Generic;

namespace CustomiseYourStorage_BZ
{
	[HarmonyPatch(typeof(Inventory), "Awake")]
	internal class InventoryPatcher
	{
		[HarmonyPostfix]
		public static void Postfix(ref Inventory __instance)
		{
			Vector2int invSize = Main.config.InventorySize;
			Logger.Log(Logger.Level.Debug, $"Inventory.Awake() postfix: Resizing with values of ({invSize.ToString()})");
			__instance.container.Resize(invSize.x, invSize.y);
		}
	}
}
