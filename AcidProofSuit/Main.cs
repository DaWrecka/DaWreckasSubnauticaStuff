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
        internal static AcidHelmetPrefab helmetPrefab = new AcidHelmetPrefab();

        // This function was stol*cough*take*cough*nicked wholesale from FCStudios
        public static object GetPrivateField<T>(this T instance, string fieldName, BindingFlags bindingFlags = BindingFlags.Default)
        {
            return typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | bindingFlags).GetValue(instance);
        }

        [QModPatch]
        public static void Load()
        {
            
            suitPrefab.Patch();
            glovesPrefab.Patch();
            helmetPrefab.Patch();

            Assembly assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }
}
