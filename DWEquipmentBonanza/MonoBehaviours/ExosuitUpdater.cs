using DWEquipmentBonanza.VehicleModules;
using DWEquipmentBonanza.Patches;
using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;
using System.Collections;

namespace DWEquipmentBonanza.MonoBehaviours
{
	internal class ExosuitUpdater : VehicleUpdater, ISerializationCallbackReceiver
	{
		private float GroundSprintMult = 1.25f; // Base force multiplier applied when using the Sprint Module on ground
		private float WaterSprintMult = 1.1f; // Base force multiplier applied when using the Sprint Module while jumping.
		private float JetUpgradeMult = 1.1f; // Additional multiplier applied to the above if the jump jet upgrade is installed.
		private float fLastForce = 1f;
		protected float defaultForwardForce;
		//protected Exosuit parentVehicle;
		protected bool bJetsUpgraded;
		//protected Equipment exosuitModules => parentVehicle != null ? parentVehicle.modules : null;


		[NonSerialized]
		private Vector3 lastMoveDirection;

        protected override void Start()
        {
            base.Start();
        }

        public override void Initialise(ref MonoBehaviour vehicle)
		{
			base.Initialise(ref vehicle);
			if (vehicle is Exosuit exosuit)
			{
				parentVehicle = vehicle;
				defaultForwardForce = exosuit.forwardForce;
			}
		}

		public override int GetModuleCount(TechType techType)
		{
			if (parentVehicle is Vehicle v)
				return v.modules.GetCount(techType);

			return 0;
		}

		internal float GetForceMultiplier(bool bSprinting, bool bUnderwaterJumping)
		{
			if (bSprinting && ExosuitPatches.GetThrustPower(parentVehicle as Exosuit) > 0f)
			{
				if (bUnderwaterJumping)
					return WaterSprintMult * (bJetsUpgraded ? JetUpgradeMult : 1f);

				return GroundSprintMult * (bJetsUpgraded ? JetUpgradeMult : 1f);
			}

			return 1f;
		}

		public override void OnAfterDeserialize()
		{
			CoroutineHost.StartCoroutine(OnAfterDeserializeCoroutine());
		}

		internal override void PostUpdate(MonoBehaviour __instance = null)
		{
			if (parentVehicle == null)
				parentVehicle = __instance;

			if (parentVehicle is Exosuit parentExosuit && Player.main.currentMountedVehicle == parentExosuit)
			{
				lastMoveDirection = AvatarInputHandler.main.IsEnabled() ? GameInput.GetMoveDirection() : Vector3.zero;

				bool bExosuitSprint = ExosuitPatches.ExosuitIsSprinting(parentExosuit, lastMoveDirection);
				//float forceMultiplier = 1f * (bExosuitSprint ? (bJetsUpgraded ? 2.5f : 2f) : 1f); // These constants will likely be tweaked, but they're here for testing
				float forceMultiplier = GetForceMultiplier(bExosuitSprint, ExosuitPatches.ExosuitIsJumping(parentExosuit, lastMoveDirection) && parentExosuit.IsUnderwater());
				parentExosuit.forwardForce = defaultForwardForce * forceMultiplier;
				fLastForce = forceMultiplier;
			}
		}

		internal override void PostOverrideAcceleration(ref Vector3 acceleration)
		{
			Exosuit parentExosuit = parentVehicle as Exosuit;

			if (ExosuitPatches.ExosuitIsSprinting(parentExosuit, lastMoveDirection))
			{
				float thrust = GetForceMultiplier(true, ExosuitPatches.ExosuitIsJumping(parentExosuit, lastMoveDirection) && parentExosuit.IsUnderwater());
				acceleration.x *= thrust;
				acceleration.z *= thrust;
			}
		}

		internal override void ApplyPhysicsMove()
		{
		}

		public override void OnBeforeSerialize()
		{
		}

		internal override void PostUpgradeModuleChange(int slotID, TechType techType, bool added, MonoBehaviour instance)
		{
			base.PostUpgradeModuleChange(slotID, techType, added, instance);
			if (instance is Vehicle v)
			{
				defaultForwardForce = v.forwardForce;
				if (techType == TechType.ExosuitJetUpgradeModule)
					bJetsUpgraded = GetModuleCount(TechType.ExosuitJetUpgradeModule) > 0;
			}
		}

		internal override void PostUpgradeModuleUse(MonoBehaviour instance, TechType tt, int slotID) { }

		internal override bool PreQuickSlotIsToggled(MonoBehaviour instance, int slotID) { return true; }

	}
}
