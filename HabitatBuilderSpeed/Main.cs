using HabitatBuilderSpeed.Configuration;
using HarmonyLib;
using System.Reflection;

#if NAUTILUS
using Nautilus.Crafting;
using Nautilus.Handlers;
using RecipeData = Nautilus.Crafting.RecipeData;
using Ingredient = CraftData.Ingredient;
#else
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
#endif

#if BEPINEX
using BepInEx;
using BepInEx.Logging;
#elif QMM
    using QModManager.API.ModLoading;
    using Logger = QModManager.Utility.Logger;
#endif

#if SN1
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace HabitatBuilderSpeed
{
#if BEPINEX
    [BepInPlugin(GUID, pluginName, version)]
    [BepInProcess("Subnautica.exe")]
    public class BuilderSpeedPlugin : BaseUnityPlugin
    {
#elif QMM
    [QModCore]
	public static class BuilderSpeedPlugin
    {
#endif
    #region[Declarations]
    public const string
        MODNAME = "BuilderSpeed",
        AUTHOR = "dawrecka",
        GUID = "com." + AUTHOR + "." + MODNAME;
    internal const string pluginName = "Habitat Builder Speed";
    public const string version = "1.0.0.0";
    #endregion

    private static readonly Harmony harmony = new Harmony(GUID);

        internal static DWConfig config { get; } = OptionsPanelHandler.RegisterModOptions<DWConfig>();

#if QMM
    [QModPatch]
#endif
        public void Start()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }
}

