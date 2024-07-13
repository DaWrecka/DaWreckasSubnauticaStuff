using BepInEx;
#if QMM
	using QModManager.API.ModLoading;
	using Logger = QModManager.Utility.Logger;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomiseYourTools
{
    [QModCore]
    public class Main
    {
        [QModPatch]
        public static void Load()
        {
        }
    }
}
