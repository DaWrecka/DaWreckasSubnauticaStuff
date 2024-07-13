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

#if SN1
        public static float powerGlideForce = 4500f;
#elif BELOWZERO
		public static float powerGlideForce = 3500f;
#endif
		public static float powerLerpRate = 900f;
		public float powerSeaglideForce;
		public static Color PowerGlideColour = new Color(1f, 0f, 1f);

		public bool bIsUnderwater { get; private set; }
		public bool bhasEnergy { get; private set; }
		public bool bInputHeld { get; private set; }

		private void OnConsoleCommand_powerglideforce(NotificationCenter.Notification n)
		{
			float force;
			float rate;
			if (n != null && n.data != null)
			{
				if (n.data.Count == 1)
				{
					string text = (string)n.data[0];
#if SN1
                    if (float.TryParse(text, out force))
#elif BELOWZERO
					if (DevConsole.ParseFloat(n, 0, out force, 0f))
#endif
					{
						powerGlideForce = force;
						powerLerpRate = force * 0.2f;
					}
					else
					{
						ErrorMessage.AddError($"Could not parse '{n.data[0]}' as number");
					}
				}
				else if (n.data.Count == 2)
				{
					string text = (string)n.data[0];
					string text2 = (string)n.data[1];
#if SN1
                    bool try0 = float.TryParse(text, out force);
					bool try1 = float.TryParse(text2, out rate);
#elif BELOWZERO
					bool try0 = DevConsole.ParseFloat(n, 0, out force, 0f);
					bool try1 = DevConsole.ParseFloat(n, 1, out rate, 0f);
#endif
					if (try0 && try1)
					{
						powerGlideForce = force;
						powerLerpRate = rate;
					}
					else
					{
						if (!try0)
							ErrorMessage.AddError($"Could not parse '{text}' as number");
						if (!try1)
							ErrorMessage.AddError($"Could not parse '{text2}' as number");
					}
				}
			}
		}

		public void Awake()
		{
			tool = gameObject.GetComponent<Seaglide>();
			power = gameObject.GetComponent<EnergyMixin>();
			DevConsole.RegisterConsoleCommand(this, "powerglideforce", false, false);
		}

		public void PostUpdate(Seaglide instance)
		{
			tool = instance;
		}

		public void FixedUpdate()
		{ 
			if (tool == null)
			{
				//if (instance != null)
				//  tool = instance;
				//else
					return;
			}

			/*
		public bool bBoostActive { get; private set; }
		public bool bhasEnergy { get; private set; }
		public bool bInputHeld { get; private set; }
			 */
			bIsUnderwater = Player.main.IsUnderwaterForSwimming();
			bhasEnergy = tool.HasEnergy();
			bInputHeld = GameInput.GetButtonHeld(GameInput.Button.Sprint);
			bool powerGlideActive = bIsUnderwater && bhasEnergy && bInputHeld;
			
			tool.powerGlideParam = Mathf.Lerp(tool.powerGlideParam, powerGlideActive ? 1f : 0f, Time.deltaTime * 3f);
			powerSeaglideForce = Mathf.Lerp(powerSeaglideForce, powerGlideActive ? powerGlideForce : 0f, Time.deltaTime * powerLerpRate);
			tool.powerGlideForce = powerSeaglideForce;

			// For some reason, relying on the legacy code has stopped working in SN1, but still works in BZ.
			if (powerGlideActive)
			{
				Player.main.gameObject.GetComponent<Rigidbody>().AddForce(MainCamera.camera.transform.forward * powerSeaglideForce, ForceMode.Force);
			}

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
