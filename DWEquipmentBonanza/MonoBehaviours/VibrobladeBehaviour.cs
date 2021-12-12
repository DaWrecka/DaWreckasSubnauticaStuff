using FMOD.Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace DWEquipmentBonanza.MonoBehaviours
{
    public class VibrobladeBehaviour : Knife
    {
        private const float FaunaDamageMultiplier = 4.5f;
        public VFXController fxControl;
        public override string animToolName => TechType.Knife.AsString(true);

        public override int GetUsesPerHit()
        {
            return 3;
        }

        public override void Awake()
        {
#if !RELEASE
            Logger.Log(Logger.Level.Debug, "VibrobladeBehaviour.Awake() executing");
#endif

            this.attackDist = 2f;
            this.bleederDamage = 90f;
            this.damage = 20f;
#if BELOWZERO
            this.spikeyTrapDamage = 9f;
#endif
            this.damageType = DamageType.Normal;
            this.socket = PlayerTool.Socket.RightHand;
            this.ikAimRightArm = true;
        }

        public override void OnToolUseAnim(GUIHand hand)
        {
            Vector3 position = new Vector3();
            GameObject closestObj = null;
#if SUBNAUTICA_STABLE
            UWE.Utils.TraceFPSTargetPosition(Player.main.gameObject, this.attackDist, ref closestObj, ref position);
#elif BELOWZERO
            Vector3 normal = new Vector3();

            UWE.Utils.TraceFPSTargetPosition(Player.main.gameObject, this.attackDist, ref closestObj, ref position, out normal);
#endif
            if (closestObj == null)
            {
                InteractionVolumeUser component = Player.main.gameObject.GetComponent<InteractionVolumeUser>();
                if (component != null && component.GetMostRecent() != null)
                    closestObj = component.GetMostRecent().gameObject;
            }



            if (closestObj != null)
            {
                LiveMixin ancestor = closestObj.FindAncestor<LiveMixin>();
                if (Knife.IsValidTarget(ancestor))
                {
                    if (ancestor != null)
                    {
                        bool wasAlive = ancestor.IsAlive();
                        float thisDamage = this.damage * (closestObj.GetComponent<Creature>() != null ? FaunaDamageMultiplier : 1f);
                        ancestor.TakeDamage(thisDamage, position, this.damageType);
                        this.GiveResourceOnDamage(closestObj, ancestor.IsAlive(), wasAlive);
                    }
#if SUBNAUTICA_STABLE
                    Utils.PlayFMODAsset(this.attackSound, this.transform);
                    VFXSurface component = closestObj.GetComponent<VFXSurface>();
                    Vector3 euler = MainCameraControl.main.transform.eulerAngles + new Vector3(300f, 90f, 0.0f);
                    VFXSurfaceTypeManager.main.Play(component, this.vfxEventType, position, Quaternion.Euler(euler), Player.main.transform);
                }
                else
                    closestObj = (GameObject)null;
#elif BELOWZERO
                }

                VFXSurface component = closestObj.GetComponent<VFXSurface>();
                Vector3 euler = MainCameraControl.main.transform.eulerAngles + new Vector3(300f, 90f, 0.0f);
                VFXSurfaceTypeManager.main.Play(component, this.vfxEventType, position, Quaternion.Euler(euler), Player.main.transform);
                VFXSurfaceTypes vfxSurfaceTypes = Utils.GetObjectSurfaceType(closestObj);
                if (vfxSurfaceTypes == VFXSurfaceTypes.none)
                    vfxSurfaceTypes = Utils.GetTerrainSurfaceType(position, normal, VFXSurfaceTypes.sand);
                EventInstance fmodEvent = Utils.GetFMODEvent(this.hitSound, this.transform.position);
                int num1 = (int)fmodEvent.setParameterValueByIndex(this.surfaceParamIndex, (float)vfxSurfaceTypes);
                int num2 = (int)fmodEvent.start();
                int num3 = (int)fmodEvent.release();
#endif
            }

#if SUBNAUTICA_STABLE
            if (!(closestObj == null) || !(hand.GetActiveTarget() == null))
                return;
            if (Player.main.IsUnderwater())
                Utils.PlayFMODAsset(this.underwaterMissSound, this.transform);
            else
                Utils.PlayFMODAsset(this.surfaceMissSound, this.transform);
#elif BELOWZERO
            Utils.PlayFMODAsset(Player.main.IsUnderwater() ? this.swingWaterSound : this.swingSound, this.transform.position);
#endif
        }
    }
}
