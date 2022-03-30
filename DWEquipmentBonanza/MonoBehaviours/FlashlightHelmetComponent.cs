using Common;
using Common.Interfaces;
using DWEquipmentBonanza.Equipables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
	public class FlashlightHelmetComponent : MonoBehaviour,
#if SUBNAUTICA_STABLE
		IInventoryDescriptionSN1,
#elif BELOWZERO
		IInventoryDescription,
#endif
		IEquippable
	{
		public static GameObject flashlightPrefab { get; internal set; }
		public static GameObject lightsParent { get; internal set; }
		private const float eyeOffset = 0.1f;
		//public Light pointLight { get; internal set; }
		//public Light spotLight { get; internal set; }
		public ToggleLights toggleLights { get; internal set; }
		public Collider mainCollider { get; internal set; }
		public GameObject thisObject { get; internal set; }
		//public GameObject pickupableModel { get; internal set; }
		private readonly HashSet<Renderer> pickupableModels = new HashSet<Renderer>();
		public GameObject storageRoot { get; internal set; }
		//public Quaternion lastTargetRotation { get; private set; }
		//public Quaternion lastActualRotation { get; private set; }
		//public bool bLastUpdateToggleLights { get; private set; }
		public bool bLightsActive => (toggleLights != null ? toggleLights.GetLightsActive() : false);
		private GameObject lightSocket;

		public static void SetFlashlightPrefab(GameObject prefab)
		{
			flashlightPrefab ??= prefab;
		}

		private GameObject GetEquipmentModel(Player main, TechType forType)
		{
			for (int index1 = main.equipmentModels.Length - 1; index1 >= 0; --index1)
			{
				Player.EquipmentType equipmentModel1 = main.equipmentModels[index1];
				for (int index2 = equipmentModel1.equipment.Length - 1; index2 >= 0; --index2)
				{
					Player.EquipmentModel equipmentModel2 = equipmentModel1.equipment[index2];
					if (equipmentModel2.techType == forType)
						return equipmentModel2.model;
				}
			}
			return null;
		}

		public string GetInventoryDescription()
		{
			List<string> args = new List<string>();
			string arg0 = ""; // "Diver Perimeter Defence Chip";
			string arg1 = ""; // "Protects a diver from hostile fauna using electrical discouragement. Discharge damages the chip beyond repair.";
			TechType thisType = CraftData.GetTechType(this.gameObject);
			if (thisType != TechType.None)
			{
				arg0 = Language.main.Get(thisType);
				arg1 = Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(thisType));
			}
			//Log.LogDebug($"For techType of {this.techType.AsString()} got arg0 or '{arg0}' and arg1 of '{arg1}'");
			//args.Add(string.Format("{0}\n", arg1));

			//return String.Join("\n", args);
			return string.Format("{0}\n", arg1);
		}

		public void Awake()
		{
			this.lightSocket = GetEquipmentModel(Player.main, TechType.Rebreather);
			if (this.lightSocket != null)
				return;
			this.lightSocket = Inventory.main.cameraSocket.gameObject;
		}

		public void Start()
		{
			Log.LogDebug($"FlashlightHelmetComponent.Start()");

			thisObject = this.gameObject;
			this.storageRoot = this.gameObject.FindChild("StorageRoot") ?? new GameObject("StorageRoot", new Type[] { typeof(ChildObjectIdentifier) });

			var lightsParent = FlashlightHelmetComponent.lightsParent ??= Player.main.gameObject.FindChild("HeadFlashlightParent") ?? new GameObject("HeadFlashlightParent");
			lightsParent.transform.SetParent(Player.main.gameObject.transform);
			lightsParent.name = "HeadLampParent";
			var spotLightObject = lightsParent.FindChild("HeadFlashLight_spot") ?? new GameObject("HeadFlashLight_spot", new Type[] { typeof(Light) });
			spotLightObject.transform.SetParent(lightsParent.transform);
			var spotLight = spotLightObject.EnsureComponent<Light>();
			if (spotLight == null)
			{
				Log.LogError($"Error creating spotLight component on FlashlightHelmet object");
			}
			else
			{
				spotLightObject.name = "HeadFlashLight_spot";
				spotLight.type = LightType.Spot;
				spotLight.spotAngle = 60f;
				spotLight.innerSpotAngle = 45.07401f;
				spotLight.color = new Color(0.992f, 0.992f, 0.996f, 1);
				spotLight.range = 50;
				spotLight.shadows = LightShadows.Hard;
			}

			var pointLightObject = lightsParent.FindChild("HeadFlashLight_point") ?? new GameObject("HeadFlashLight_point", new Type[] { typeof(Light) });
			pointLightObject.transform.SetParent(lightsParent.transform);
			var pointLight = pointLightObject.EnsureComponent<Light>();
			if (pointLight == null)
			{
				Log.LogError($"Error creating pointLight component on FlashlightHelmet object");
			}
			else
			{
				pointLightObject.name = "HeadFlashLight_point";
				pointLight.type = LightType.Point;
				pointLight.intensity = 0.9f;
				pointLight.range = 12;
			}

			this.toggleLights = this.gameObject.EnsureComponent<ToggleLights>();
			this.toggleLights.energyMixin = this.gameObject.EnsureComponent<EnergyMixin>();
			this.toggleLights.energyMixin.storageRoot = this.storageRoot.GetComponent<ChildObjectIdentifier>();
			this.toggleLights.energyMixin.OnCraftEnd(TechType.None);
			this.toggleLights.lightsParent = FlashlightHelmetComponent.lightsParent;
			var toggleLightsPrefab = FlashlightHelmetComponent.flashlightPrefab.GetComponent<ToggleLights>();
			this.toggleLights.lightsOnSound = toggleLightsPrefab.lightsOnSound;
			this.toggleLights.lightsOffSound = toggleLightsPrefab.lightsOffSound;
			this.toggleLights.energyPerSecond = 0f;
			this.mainCollider = gameObject.GetComponent<BoxCollider>();
		}

		public void OnEquip(GameObject sender, string slot)
		{
			if (slot != "Head")
				return;
			this.toggleLights ??= this.gameObject.EnsureComponent<ToggleLights>();
			//this.pickupableModel ??= this.gameObject.FindChild("Rebreather");
			this.mainCollider = this.gameObject.GetComponent<Collider>();

			if(this.mainCollider != null)
				this.mainCollider.enabled = false;
			this.gameObject.SetActive(true);
			//ModelPlug.PlugIntoSocket(this.lightModelPlug, this.lightSocket.transform);
			if (this.toggleLights == null)
				Log.LogError($"toggleLights is null!");
			else
			{
				this.toggleLights.SetLightsActive(false);
				this.toggleLights.SetLightsActive(true);
			}

			foreach (Renderer R in this.gameObject.GetComponentsInChildren<Renderer>())
			{
				R.enabled = false;
			}
		}

		public void OnUnequip(GameObject sender, string slot)
		{
			Log.LogDebug($"FlashlightHelmetComponent.OnUnequip: sender = {sender.ToString()}, slot = {slot}");

			if (slot != "Head")
				return;
			//ModelPlug.PlugIntoSocket(this.lightModelPlug, this.transform);

			Log.LogDebug($"FlashlightHelmetComponent.OnUnequip: getting ToggleLights"); 
			this.toggleLights ??= this.gameObject.GetComponent<ToggleLights>();

			this.gameObject.SetActive(false);
			Collider C = this.gameObject.GetComponent<BoxCollider>();
			if (C is not null)
				this.gameObject.GetComponent<BoxCollider>().enabled = true;
			else
			{
				Log.LogDebug($"FlashlightHelmetComponent.OnUnequip: No BoxCollider found");
			}

			if (this.toggleLights == null)
				Log.LogDebug($"FlashlightHelmetComponent.OnUnequip: toggleLights is null!");
			else
				this.toggleLights.SetLightsActive(false);

			Log.LogDebug($"FlashlightHelmetComponent.OnUnequip: Seting renderers active");
			foreach (Renderer R in this.gameObject.GetComponentsInChildren<Renderer>())
			{
				R.enabled = true;
			}
			if(this.mainCollider != null)
				this.mainCollider.enabled = true;
		}

		public void UpdateEquipped(GameObject sender, string slot)
		{
			if (MainCameraControl.main.enabled)
			{
				var lastTargetRotation = MainCamera.camera.transform.rotation;
				lightsParent.transform.rotation = lastTargetRotation;
				var lastActualRotation = lightsParent.transform.rotation;
				lightsParent.transform.position = MainCamera.camera.transform.position + (MainCamera.camera.transform.forward * eyeOffset);
			}
			else
				lightsParent.transform.localRotation = Quaternion.identity;
			if (Inventory.main.GetHeldObject() != null || Player.main.isPiloting)
			{
				//bLastUpdateToggleLights = false;
				return;
			}

			//bLastUpdateToggleLights = true;
			this.toggleLights.CheckLightToggle();
		}
	}
}
