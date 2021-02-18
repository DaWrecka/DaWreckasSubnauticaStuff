using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombinedItems;
using HarmonyLib;
using QModManager.Utility;
using UnityEngine;
using CombinedItems.ExosuitModules;
using System.Reflection;

namespace CombinedItems.Patches
{
    [HarmonyPatch(typeof(Exosuit))]
    public class ExosuitPatches
    {
        private static readonly FieldInfo jumpJetUpgrade = typeof(Exosuit).GetField("jumpJetsUpgraded", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo jetsActive = typeof(Exosuit).GetProperty("jetsActive", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo timeJetsActiveChanged = typeof(Exosuit).GetField("timeJetsActiveChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo thrustPower = typeof(Exosuit).GetField("thrustPower", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo thrustConsumption = typeof(Exosuit).GetField("thrustConsumption", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo onGround = typeof(Exosuit).GetField("onGround", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo timeOnGround = typeof(Exosuit).GetField("timeOnGround", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo jetDownLastFrame = typeof(Exosuit).GetField("jetDownLastFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo powersliding = typeof(Exosuit).GetField("powersliding", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo areFXPlaying = typeof(Exosuit).GetField("areFXPlaying", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo ApplyJumpForceMethod = typeof(Exosuit).GetMethod("ApplyJumpForce", BindingFlags.Instance | BindingFlags.NonPublic);
        protected static bool JumpJetsUpgraded(Exosuit instance) { return (bool)jumpJetUpgrade.GetValue(instance); }
        protected static bool JetsActive(Exosuit instance) => (bool)jetsActive.GetValue(instance);
        protected static void JetsActive(Exosuit instance, bool value) { jetsActive.SetValue(instance, value); }
        protected static float TimeJetsActiveChanged(Exosuit instance) => (float)timeJetsActiveChanged.GetValue(instance);
        protected static float ThrustPower(Exosuit instance) => (float)thrustPower.GetValue(instance);
        protected static void ThrustPower(Exosuit instance, float value) { thrustPower.SetValue(instance, value); }
        protected static float ThrustConsumption(Exosuit instance) => (float)thrustConsumption.GetValue(instance);
        protected static bool OnGround(Exosuit instance) => (bool)onGround.GetValue(instance);
        protected static float TimeOnGround(Exosuit instance) => (float)timeOnGround.GetValue(instance);
        protected static bool JetDownLastFrame(Exosuit instance) => (bool)jetDownLastFrame.GetValue(instance);
        protected static void JetDownLastFrame(Exosuit instance, bool value) { jetDownLastFrame.SetValue(instance, value); }
        protected static bool Powersliding(Exosuit instance) => (bool)powersliding.GetValue(instance);
        protected static bool AreFXPlaying(Exosuit instance) => (bool)areFXPlaying.GetValue(instance);
        protected static void AreFXPlaying(Exosuit instance, bool value) { areFXPlaying.SetValue(instance, value); }
        protected static void ApplyJumpForce(Exosuit instance) { ApplyJumpForceMethod.Invoke(instance, new object[] { }); }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void PreStart(Exosuit __instance)
        {
            // Exosuit actually has a GetArmPrefab method - but the motherfucker is private.
            // WWWWWWWWWWWWWHHHHHHHHHHHHHHHHHHHHHHHHHYYYYYYYYYYYYYYYYYYYYYYYYYYYYY

            int count = __instance.armPrefabs.Length;
            int clawIndex = -1;

            for (int i = 0; i < count; i++)
            {
                if (__instance.armPrefabs[i].techType == Main.prefabLightningClaw.TechType)
                {
                    //Log.LogDebug($"Lightning claw prefab already found at index {i}");
                    return;
                }
                else if (__instance.armPrefabs[i].techType == TechType.ExosuitClawArmModule)
                    clawIndex = i; // Save this index for later use
            }

            GameObject armPrefab = null;
            if (clawIndex > -1)
                armPrefab = GameObject.Instantiate(__instance.armPrefabs[clawIndex].prefab);

            if (armPrefab != null)
            {
                // Since we're making a variant of Claw Arm, we don't actually have to do much here.
                // Just remove the base ExosuitClawArm component, and swap in our ExosuitLightningClaw prefab
                GameObject.DestroyImmediate(armPrefab.GetComponent<ExosuitClawArm>());
                armPrefab.AddComponent<ExosuitLightningClaw>();
                armPrefab.SetActive(true);
                Array.Resize(ref __instance.armPrefabs, count + 1);
                __instance.armPrefabs[count] = new Exosuit.ExosuitArmPrefab()
                {
                    prefab = armPrefab,
                    techType = Main.prefabLightningClaw.TechType
                };
            }
            else
            {
                //Log.LogDebug($"Failed to find arm prefab in Exosuit prefab");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Exosuit.HasClaw))]
        public static bool PostHasClaw(bool __result, Exosuit __instance)
        {

            return __result || __instance.leftArmType == Main.prefabLightningClaw.TechType || __instance.rightArmType == Main.prefabLightningClaw.TechType;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnUpgradeModuleChange")]
        public static void PostUpgradeChange(ref Exosuit __instance, int slotID, TechType techType, bool added)
        {
            if (techType == Main.prefabLightningClaw.TechType)
                __instance.MarkArmsDirty();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OverrideAcceleration")]
        public static void PostOverrideAcceleration(ref Exosuit __instance, ref Vector3 acceleration)
        {
            if (__instance.modules.GetCount(Main.prefabExosuitSprintModule.TechType) > 0 && GameInput.GetButtonDown(GameInput.Button.Sprint))
            {
                float thrust = JumpJetsUpgraded(__instance) ? 2f : 1f;
                acceleration.x *= thrust;
                acceleration.z *= thrust;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void PostUpdate(ref Exosuit __instance)
        {
            /*bool bSprint = (__instance.modules.GetCount(Main.prefabExosuitSprintModule.TechType) > 0 && GameInput.GetButtonDown(GameInput.Button.Sprint));
            if (bSprint)
            {
                float fthrustConsumption = ThrustConsumption(__instance);
                bool bOnGround = OnGround(__instance);
                float fTimeOnGround = TimeOnGround(__instance);
                bool bPowersliding = Powersliding(__instance);
                float fTimeJetsActiveChanged = TimeJetsActiveChanged(__instance);
                bool bJetsActive = JetsActive(__instance);
                bool bJetsDownLastFrame = JetDownLastFrame(__instance);
                bool pilotingMode = __instance.GetPilotingMode();
                bool docked = pilotingMode && !__instance.docked;

                if (pilotingMode)
                {
                    Player.main.transform.localPosition = Vector3.zero;
                    Player.main.transform.localRotation = Quaternion.identity;
                    Vector3 vector = AvatarInputHandler.main.IsEnabled() ? GameInput.GetMoveDirection() : Vector3.zero;
                    bool bJumping = vector.y > 0f;
                    bool bPoweredup = __instance.IsPowered() && __instance.liveMixin.IsAlive();
                    if (bPoweredup)
                    {
                        ThrustPower(__instance, Mathf.Clamp01(ThrustPower(__instance) - Time.deltaTime * fthrustConsumption));
                        if ((bOnGround || Time.time - fTimeOnGround <= 1f) && !bJetsDownLastFrame && bJumping)
                        {
                            ApplyJumpForce(__instance);
                        }
                        JetsActive(__instance, true);
                    }
                    else
                    {
                        JetsActive(__instance, false);
                        float num = Time.deltaTime * fthrustConsumption * 0.7f;
                        if (bOnGround)
                        {
                            num = Time.deltaTime * fthrustConsumption * 4f;
                        }
                        ThrustPower(__instance, Mathf.Clamp01(ThrustPower(__instance) + num));
                    }
                    JetDownLastFrame(__instance, bPoweredup);
                    __instance.footStepSounds.soundsEnabled = !bPowersliding;
                    __instance.movementEnabled = !bPowersliding;
                    ProfilingUtils.BeginSample("UpdateJetFX");
                    if (fTimeJetsActiveChanged + 0.3f <= Time.time)
                    {
                        if ((bJetsActive || bPowersliding) && ThrustPower(__instance) > 0f)
                        {
                            __instance.loopingJetSound.Play();
                            __instance.fxcontrol.Play(0);
                            AreFXPlaying(__instance, true);
                        }
                        else if (AreFXPlaying(__instance))
                        {
                            __instance.loopingJetSound.Stop();
                            __instance.fxcontrol.Stop(0);
                            AreFXPlaying(__instance, false);
                        }
                    }
                }
            }*/
        }
        /*
            * Methods which may need to be patched to accomodate future plans:

            private bool GetIsGrappling()
            {
                this.arms.Clear();
                base.GetComponentsInChildren<ExosuitGrapplingArm>(this.arms);
                for (int i = 0; i < this.arms.Count; i++)
                {
                    if (this.arms[i].GetIsGrappling())
                    {
                        return true;
                    }
                }
                return false;
            }

            // This method appears to be called in all circumstances - as long as the vehicle's controlSheme (sic) is either Submarine or Mech.
            protected override void OverrideAcceleration(ref Vector3 acceleration)
            {
                if (!this.onGround)
                {
                    float num = this.jumpJetsUpgraded ? 0.3f : 0.22f;
                    acceleration.x *= num;
                    acceleration.z *= num;
                }
            }
            */
    } 
}
