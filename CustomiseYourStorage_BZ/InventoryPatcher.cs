#if QMM
using Logger = QModManager.Utility.Logger;
#endif
using Common;
using HarmonyLib;

namespace CustomiseYourStorage
{
	[HarmonyPatch(typeof(Inventory))]
	internal class InventoryPatcher
	{
		[HarmonyPostfix]
		[HarmonyPatch("Awake")]
		public static void Postfix(ref Inventory __instance)
		{
			//Vector2int invSize = Main.config.InventorySize;
			int X = CustomiseStoragePlugin.config.InvWidth;
			int Y = CustomiseStoragePlugin.config.InvHeight;
#if !RELEASE
			Log.LogDebug($"Inventory.Awake() postfix: Resizing with values of ({X}, {Y})"); 
#endif
			__instance.container.Resize(X, Y);
			__instance.container.onResize += OnInventoryResize;
		}

		internal static void OnInventoryResize(int width, int height)
		{
			int forceWidth = System.Math.Max(width, CustomiseStoragePlugin.config.InvWidth);
			int forceHeight = System.Math.Max(height, CustomiseStoragePlugin.config.InvHeight);

			if (forceWidth > width || forceHeight > height)
			{
				Log.LogDebug($"Inventory was resized to ({width}, {height})! Forcing resize to ({forceWidth}, {forceHeight})");
				Inventory.main.container.Resize(forceWidth, forceHeight);
			}
		}
	}
}
