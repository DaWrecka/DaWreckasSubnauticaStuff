using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using CustomiseYourStorage_BZ.Configuration;
using Logger = QModManager.Utility.Logger;

namespace CustomiseYourStorage_BZ
{
	[HarmonyPatch(typeof(Exosuit))]
	class Exosuit_Patcher
	{
		[HarmonyPrefix]
		[HarmonyPatch("UpdateStorageSize")]
		public static bool UpdateStorageSize(ref Exosuit __instance)
		{
			//var exoConfig = Main.config.ExosuitConfig;
			var X = Main.config.ExosuitWidth;
			var Y = Main.config.ExosuitHeight;
			var perModule = Main.config.ExosuitModuleHeight;
			int moduleCount = __instance.modules.GetCount(TechType.VehicleStorageModule);
			var height = Y + (moduleCount * perModule);
#if !RELEASE
			Logger.Log(Logger.Level.Debug, $"Exosuit.UpdateStorageSize prefixed with Exosuit config of ({X}, {Y}, {perModule}); Number of VehicleStorageModule units found: {moduleCount}"); 
#endif
			__instance.storageContainer.Resize(X, height);

			return false;
		}

		[HarmonyPatch("IsAllowedToRemove")]
		[HarmonyPrefix]
		public static bool IsAllowedToRemove(ref Exosuit __instance, ref bool __result, Pickupable pickupable, bool verbose)
		{
			if (pickupable.GetTechType() == TechType.VehicleStorageModule)
			{
				__result = __instance.storageContainer.container.HasRoomFor(Main.config.ExosuitWidth, Main.config.ExosuitModuleHeight);
				if (verbose && !__result)
				{
					ErrorMessage.AddDebug(Language.main.Get("ExosuitStorageShrinkError"));
				}
				return false;
			}

			return true;
		}


	}
}
