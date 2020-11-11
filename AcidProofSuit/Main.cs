using AcidProofSuit.Module;
using System.Reflection;
using HarmonyLib;
using QModManager.API.ModLoading;

namespace AcidProofSuit
{
    [QModCore]
    public static class Main
    {
        public static bool bInAcid = false; // Whether or not the player is currently immersed in acid

        internal static AcidSuitPrefab suitPrefab = new AcidSuitPrefab();
        internal static AcidGlovesPrefab glovesPrefab = new AcidGlovesPrefab();
        //internal static AcidHelmetPrefab helmetPrefab = new AcidHelmetPrefab();

        [QModPatch]
        public static void Load()
        {
            
            suitPrefab.Patch();
            glovesPrefab.Patch();
            //helmetPrefab.Patch();

            Assembly assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }
}
