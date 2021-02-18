using HarmonyLib;
using Logger = QModManager.Utility.Logger;
using CustomiseYourStorage_BZ;
using System.Collections.Generic;

namespace CustomiseYourStorage_BZ
{
	[HarmonyPatch(typeof(Inventory))]
	internal class InventoryPatcher
	{
		[HarmonyPostfix]
		[HarmonyPatch("Awake")]
		public static void Postfix(ref Inventory __instance)
		{
			Vector2int invSize = Main.config.InventorySize;
#if !RELEASE
			Logger.Log(Logger.Level.Debug, $"Inventory.Awake() postfix: Resizing with values of ({invSize.ToString()})"); 
#endif
			__instance.container.Resize(invSize.x, invSize.y);
		}
	}
}
