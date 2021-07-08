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

namespace DWEquipmentBonanza.MonoBehaviours
{
	internal class ExosuitUpdater : VehicleUpdater
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

		internal override void Initialise(ref Vehicle vehicle)
		{
			if (vehicle is Exosuit exosuit)
			{
				parentVehicle = vehicle;
				defaultForwardForce = vehicle.forwardForce;
				Log.LogDebug($"ExosuitUpdater.Initialise(): Found Exosuit with forwardForce of {defaultForwardForce}");
			}
		}

		protected override int GetModuleCount(TechType techType)
		{
			if (parentVehicle == null || parentVehicle.modules == null)
				return 0;

			return parentVehicle.modules.GetCount(techType);
		}

		internal override void PostUpgradeModuleChange(TechType changedTechType = TechType.None)
		{
			if (parentVehicle != null)
			{
				defaultForwardForce = parentVehicle != null ? parentVehicle.forwardForce : 0f;
				if (changedTechType == TechType.ExosuitJetUpgradeModule)
					bJetsUpgraded = GetModuleCount(TechType.ExosuitJetUpgradeModule) > 0;
			}
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

		internal override void PreUpdate()
		{
			if (parentVehicle is Exosuit parentExosuit)
			{
				lastMoveDirection = AvatarInputHandler.main.IsEnabled() ? GameInput.GetMoveDirection() : Vector3.zero;

				bool bExosuitSprint = ExosuitPatches.ExosuitIsSprinting(parentExosuit, lastMoveDirection);
				//float forceMultiplier = 1f * (bExosuitSprint ? (bJetsUpgraded ? 2.5f : 2f) : 1f); // These constants will likely be tweaked, but they're here for testing
				float forceMultiplier = GetForceMultiplier(bExosuitSprint, ExosuitPatches.ExosuitIsJumping(parentExosuit, lastMoveDirection) && parentExosuit.IsUnderwater());
				parentVehicle.forwardForce = defaultForwardForce * forceMultiplier;
				//if(forceMultiplier != fLastForce)
				//    Log.LogDebug($"ExosuitUpdater.PostUpdate(): Applying forwardForce of {parentVehicle.forwardForce} to Exosuit with defaultForwardForce of {defaultForwardForce}");
				fLastForce = forceMultiplier;
			}
		}

		internal override void Update()
		{
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
	}
}
