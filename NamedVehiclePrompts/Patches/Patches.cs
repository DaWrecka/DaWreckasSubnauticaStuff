using HarmonyLib;
using Logger = QModManager.Utility.Logger;

namespace NamedVehiclePrompts.Patches
{
    [HarmonyPatch(typeof(CinematicModeTrigger), nameof(CinematicModeTrigger.OnHandHover))]
    public static class CinematicModeTrigger_OnHandHover_Patch
    {
        private static int msgTimer = 0;
        private const bool bDebug = false;

        [HarmonyPrefix]
        public static bool Prefix(ref CinematicModeTrigger __instance, GUIHand hand)
        {
            bool bLog = false;
            string handText = __instance.handText;
            if (msgTimer > 0)
                msgTimer--;
            else
            {
                bLog = bDebug && Logger.DebugLogsEnabled;
                if (bLog)
                {
                    if (bLog) ErrorMessage.AddMessage($"CinematicModeTrigger_OnHandHover_Patch.Prefix() running, handText = {handText}");
                    msgTimer = 30;
                }
            }

            if (handText != "EnterCyclops" && handText != "LeaveCyclops")
                return true;

            if (!__instance.showIconOnHandHover)
            {
                if (bLog) ErrorMessage.AddMessage($"showIconOnHandHover == false");
                return true;
            }
            if (PlayerCinematicController.cinematicModeCount > 0)
            {
                if (bLog) ErrorMessage.AddMessage($"cinematicModeCount > 0");
                return true;
            }
            if (string.IsNullOrEmpty(__instance.handText))
            {
                if (bLog) ErrorMessage.AddMessage($"string handText IsNullOrEmpty");
                return true;
            }

            // transform.parent will be the CyclopsMeshAnimated; we need to go up one more level to get the Cyclops-MainPrefab, at which point we can get at the SubRoot
            UnityEngine.Transform transformGrandparent = __instance.transform.parent.parent;
            SubRoot subRoot = null;
            if(transformGrandparent != null)
            {
                subRoot = transformGrandparent.GetComponent<SubRoot>();
            }
            else
            {
                if (bLog) ErrorMessage.AddMessage($"Couldn't get transformRoot in __instance");
                return true;
            }

            if (subRoot == null)
            {
                if (bLog) ErrorMessage.AddMessage($"Couldn't get SubRoot in transformRoot");
                return true;
            }

            string subName = subRoot.GetSubName();
            string language = Language.main.GetCurrentLanguage();

            //if (bLog) ErrorMessage.AddMessage($"calling TryGetVehiclePrompt with parameters initialKey='{handText}', language='{language}', VehicleName='{subName}'");
            if (Main.TryGetVehiclePrompt(handText, Language.main.GetCurrentLanguage(), subName, out string prompt))
            {
                if (bLog) ErrorMessage.AddMessage($"TryGetVehiclePrompt returned true with value for prompt of '{prompt}'");
#if SUBNAUTICA_LEGACY
                HandReticle.main.SetInteractText(prompt);
#elif SUBNAUTICA_LL || BELOWZERO
                HandReticle.main.SetText(HandReticle.TextType.Hand, prompt, true);
#endif
                HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
                return false;
            }
            else
            {
                if (bLog) ErrorMessage.AddMessage($"TryGetVehiclePrompt returned false");
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(DockedVehicleHandTarget), nameof(DockedVehicleHandTarget.OnHandHover))]
    public static class DockedVehicleHandTarget_OnHandHover_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref DockedVehicleHandTarget __instance, GUIHand hand)
        {
#if SN1
            Vehicle dockedVehicle = __instance.dockingBay.GetDockedVehicle();
#elif BELOWZERO
#endif
            if (!(dockedVehicle != null))
            {
                HandReticle.main.SetInteractInfo("NoVehicleDocked");
                return false;
            }
            bool crushDanger = false;
            CrushDamage crushDamage = dockedVehicle.crushDamage;
            if (crushDamage != null)
            {
                float crushDepth = crushDamage.crushDepth;
                if (Ocean.main.GetDepthOf(Player.main.gameObject) > crushDepth)
                {
                    crushDanger = true;
                }
            }
            string text = (dockedVehicle is Exosuit) ? "EnterExosuit" : "EnterSeamoth";
            bool result;
            string prompt = "";
            string vehicleName = dockedVehicle.GetName();
            result = Main.TryGetVehiclePrompt(text, Language.main.GetCurrentLanguage(), dockedVehicle.GetName(), out prompt);
            //Log.LogDebug($"DockedVehicleHandTarget_OnHandHover_Patch.Prefix(): with docked vehicle {vehicleName}, called OnHandHover with text {text}; Main.TryGetVehiclePrompt({text}) returned prompt '{prompt}'", null, true);
            if (result)
            {
                text = prompt;
            }
            if (crushDanger)
            {
                HandReticle.main.SetInteractText(text, "DockedVehicleDepthWarning");
                return false;
            }
            EnergyMixin component = dockedVehicle.GetComponent<EnergyMixin>();
            LiveMixin liveMixin = dockedVehicle.liveMixin;
            if (component.charge < component.capacity)
            {
                string format = Language.main.GetFormat<float, float>("VehicleStatusFormat", liveMixin.GetHealthFraction(), component.GetEnergyScalar());
                HandReticle.main.SetInteractText(text, format, true, false, HandReticle.Hand.Left);
            }
            else
            {
                string format2 = Language.main.GetFormat<float>("VehicleStatusChargedFormat", liveMixin.GetHealthFraction());
                HandReticle.main.SetInteractText(text, format2, true, false, HandReticle.Hand.Left);
            }
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);

            return false;
        }
    }

    /*[HarmonyPatch(typeof(CinematicModeTrigger), nameof(CinematicModeTrigger.OnHandHover))]
    public static class CinematicModeTrigger_OnHandHover_Patch
    {
        static int timer = 0;

        [HarmonyPrefix]
        public static bool Prefix(ref UseableDiveHatch __instance, GUIHand hand)
        {
        }
    }*/

    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.OnHandHover))]
    public static class Vehicle_OnHandHover_Patch
    {
        //private static int timer = 0;

        [HarmonyPrefix]
        public static bool Prefix(Vehicle __instance, GUIHand hand)
        {
            string vehicleName = __instance.GetName();
            string handLabel = __instance.handLabel;
            string vehicleClass = handLabel;//.Replace("Enter", "");
            string result = "";
            bool success = Main.TryGetVehiclePrompt(vehicleClass, Language.main.GetCurrentLanguage(), vehicleName, out result);
            //Language.main.TryGet(vehicleClass, out result);

            if (success)
            {
                if (!__instance.GetPilotingMode() && __instance.enabled /*&& __instance.GetEnabled()*/)
                {
                    HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
                    HandReticle.main.SetInteractText(result);
                }
                return false;
            }

            return true;
        }
    }

}
