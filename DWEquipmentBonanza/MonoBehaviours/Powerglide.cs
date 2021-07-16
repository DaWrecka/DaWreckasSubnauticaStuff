using Common;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using DWEquipmentBonanza.Equipables;

namespace DWEquipmentBonanza.MonoBehaviours
{
    internal class PowerglideBehaviour : MonoBehaviour
    {
        private Seaglide tool;
        private EnergyMixin power;

        public static float powerGlideForce = 3500f;
        public static float powerLerpRate = 900f;
        public float powerSeaglideForce;
        public static Color PowerGlideColour = new Color(1f, 0f, 1f);

        private void OnConsoleCommand_powerglideforce(NotificationCenter.Notification n)
        {
#if SUBNAUTICA_STABLE
#elif BELOWZERO
            float force;
            float rate;
            if (n != null && n.data != null)
            {
                if (n.data.Count == 1)
                {
                    if (DevConsole.ParseFloat(n, 0, out force, 0f))
                    {
                        powerGlideForce = force;
                        powerLerpRate = force * 0.2f;
                    }
                }
                else if (n.data.Count == 2)
                {
                    if(DevConsole.ParseFloat(n, 0, out force, 0f) && DevConsole.ParseFloat(n, 1, out rate, 0f))
                    {
                        powerGlideForce = force;
                        powerLerpRate = rate;
                    }
                }
            }
#endif
        }

        public void Awake()
        {
            tool = gameObject.GetComponent<Seaglide>();
            power = gameObject.GetComponent<EnergyMixin>();
            DevConsole.RegisterConsoleCommand(this, "powerglideforce", false, false);
        }

        public void PostUpdate(Seaglide instance)
        {
            if (tool == null)
            {
                if (instance != null)
                    tool = instance;
                else
                    return;
            }

            tool.powerGlideActive = Player.main.IsUnderwaterForSwimming()
                && tool.HasEnergy()
                && GameInput.GetButtonHeld(GameInput.Button.Sprint);
            
            tool.powerGlideParam = Mathf.Lerp(tool.powerGlideParam, tool.powerGlideActive ? 1f : 0f, Time.deltaTime * 3f);
            powerSeaglideForce = Mathf.Lerp(powerSeaglideForce, tool.powerGlideActive ? powerGlideForce : 0f, Time.deltaTime * powerLerpRate);
            tool.powerGlideForce = powerSeaglideForce;
            MeshRenderer[] meshRenderers = tool.GetAllComponentsInChildren<MeshRenderer>();
            SkinnedMeshRenderer[] skinnedMeshRenderers = tool.GetAllComponentsInChildren<SkinnedMeshRenderer>();

            foreach (MeshRenderer mr in meshRenderers)
            {
                // MeshRenderers have the third-person mesh, apparently?
                if (mr.name.Contains("SeaGlide_01_TP"))
                {
                    mr.material.color = PowerglideBehaviour.PowerGlideColour;
                }
                
            }

            foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
            {
                if (smr.name.Contains("SeaGlide_geo"))
                {
                    smr.material.color = PowerglideBehaviour.PowerGlideColour;
                }
            }
        }
    }
}
