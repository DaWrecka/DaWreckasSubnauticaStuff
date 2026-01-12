using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if BELOWZERO
	internal class FlashlightEnablerBZ : MonoBehaviour
	{
		private ToggleLights thisToggleLights;
		private GameObject lightsGrandParent;
		private bool bLightActive = true;
		private const float timerInterval = 0.1f;

		public void Start()
		{
			thisToggleLights = this.gameObject.GetComponent<ToggleLights>();
			thisToggleLights.lightsCallback += OnLightsToggled;
			base.InvokeRepeating("UpdateLights", timerInterval, timerInterval);
		}

		private void UpdateLights()
		{
			lightsGrandParent = thisToggleLights.lightsParent.transform.parent.gameObject;
			if (lightsGrandParent != null && lightsGrandParent.activeSelf != bLightActive)
				lightsGrandParent.SetActive(bLightActive);
		}

		public void OnLightsToggled(bool active)
		{
			// For some reason, the lights_parent of the prefab is *not* added to the UltimateHelmet GameObject. Instead, the UltimateHelmet uses the same lights_parent from the prefab.
			// When this is enabled by the FlashlightHelmet component, it is enabled properly - but its parent is *not* enabled, and as a result the light is not visible in-game.
			// This method is intended as a stop-gap; once I can figure out how to get the lights_parent on the UltimateHelmet, this will be removed.
			this.bLightActive = active;
			thisToggleLights.lightsParent.transform.parent.gameObject.SetActive(active);
		}
	}
#endif
}
