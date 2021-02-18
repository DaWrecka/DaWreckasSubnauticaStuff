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
            var exoConfig = Main.config.ExosuitConfig;
            int moduleCount = __instance.modules.GetCount(TechType.VehicleStorageModule);
#if !RELEASE
            Logger.Log(Logger.Level.Debug, $"Exosuit.UpdateStorageSize prefixed with ExosuitConfig of ({exoConfig.ToString()}); Number of VehicleStorageModule units found: {moduleCount}"); 
#endif
            int height = exoConfig.height + (exoConfig.heightPerModule * moduleCount);
            __instance.storageContainer.Resize(exoConfig.width, height);

            return false;
        }

        [HarmonyPatch("IsAllowedToRemove")]
        [HarmonyPrefix]
        public static bool IsAllowedToRemove(ref Exosuit __instance, ref bool __result, Pickupable pickupable, bool verbose)
        {
            if (pickupable.GetTechType() == TechType.VehicleStorageModule)
            {
                __result = __instance.storageContainer.container.HasRoomFor(Main.config.ExosuitConfig.width, Main.config.ExosuitConfig.heightPerModule);
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
