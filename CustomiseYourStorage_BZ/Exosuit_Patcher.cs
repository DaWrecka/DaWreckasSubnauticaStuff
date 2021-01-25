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
    [HarmonyPatch(typeof(Exosuit), "UpdateStorageSize")]
    class Exosuit_Patcher
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Exosuit __instance)
        {
            var exoConfig = Main.config.ExosuitConfig;
            int moduleCount = __instance.modules.GetCount(TechType.VehicleStorageModule);
            Logger.Log(Logger.Level.Debug, $"Exosuit.UpdateStorageSize prefixed with ExosuitConfig of ({exoConfig.ToString()}); Number of VehicleStorageModule units found: {moduleCount}");
            int height = exoConfig.height + (exoConfig.heightPerModule * moduleCount);
            __instance.storageContainer.Resize(exoConfig.width, height);

            return false;
        }
    }
}
