using HarmonyLib;
using Logger = QModManager.Utility.Logger;
using CustomiseYourStorage_BZ;
using System.Collections.Generic;
using Common;

namespace CustomiseYourStorage_BZ
{
	[HarmonyPatch(typeof(Inventory))]
	internal class InventoryPatcher
	{
		[HarmonyPostfix]
		[HarmonyPatch("Awake")]
		public static void Postfix(ref Inventory __instance)
		{
			//Vector2int invSize = Main.config.InventorySize;
			int X = Main.config.InvWidth;
			int Y = Main.config.InvHeight;
#if !RELEASE
			Logger.Log(Logger.Level.Debug, $"Inventory.Awake() postfix: Resizing with values of ({X}, {Y})"); 
#endif
			__instance.container.Resize(X, Y);
			__instance.container.onResize += OnInventoryResize;
		}

		internal static void OnInventoryResize(int width, int height)
		{
			int forceWidth = System.Math.Max(width, Main.config.InvWidth);
			int forceHeight = System.Math.Max(height, Main.config.InvHeight);

			if (forceWidth > width || forceHeight > height)
			{
				Log.LogDebug($"Inventory was resized to ({width}, {height})! Forcing resize to ({forceWidth}, {forceHeight})");
				Inventory.main.container.Resize(forceWidth, forceHeight);
			}
		}
	}
}
