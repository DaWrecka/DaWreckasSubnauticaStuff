using Common;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

namespace CombinedItems.MonoBehaviours
{
    internal class HoverbikeUpdater : MonoBehaviour
	{
		private Hoverbike parentHoverbike;
		//private float defaultWaterDampening;
		//private float defaultWaterOffset;
		private static Dictionary<string, object> defaultValues = new Dictionary<string, object>();
		struct HoverbikeField
		{
			public string fieldName;
			public BindingFlags bindingFlags;

			public HoverbikeField(string name, BindingFlags flags)
			{
				this.fieldName = name;
				this.bindingFlags = BindingFlags.Instance | flags; // There are no static values we're interested in for Hoverbike, so we add the Instance flag.
			}
		};
		private static List<HoverbikeField> hoverbikeFields = new List<HoverbikeField>() // This will be used to populate the defaultValues dictionary
		{
			new HoverbikeField(nameof(Hoverbike.enginePowerConsumption), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.boostDecay), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.hoverDist), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.forwardAccel), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.forwardBoostForce), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.waterDampening), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.waterLevelOffset), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.topSpeed), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.boostCooldown), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.forwardBoostForce), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.jumpCooldown), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.sidewaysTorque), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.boostCooldown), BindingFlags.Public)
		};

		internal struct EfficiencyModifierStruct
		{
			public TechType techType;
			public int maxUpgrades;
			public float efficiencyMultiplier;
			public int priority;

			public EfficiencyModifierStruct(TechType module, float EfficiencyMultiplier, int priority = 1, int MaxUpgrades = 1)
			{
				this.techType = module;
				this.maxUpgrades = MaxUpgrades;
				this.efficiencyMultiplier = EfficiencyMultiplier;
				this.priority = priority;
			}

			public override string ToString()
			{
				return $"(TechType.{techType.AsString()}, efficiencyMultiplier {efficiencyMultiplier}, priority {priority}, maxUpgrades {maxUpgrades})";
			}
		};

		internal struct MovementModifierStruct
		{
			public TechType techType;
			public int maxUpgrades;
			public float speedModifier;
			public float cooldownModifier;
			public int priority;

			public MovementModifierStruct(TechType module, float speedModifier, float cooldownModifier, int priority = 1, int MaxUpgrades = 1)
			{
				this.techType = module;
				this.maxUpgrades = MaxUpgrades;
				this.speedModifier = speedModifier;
				this.cooldownModifier = cooldownModifier;
				this.priority = priority;
			}

            public override string ToString()
            {
				return $"(TechType.{techType.AsString()}, speedMod {speedModifier}, cooldownMod {cooldownModifier}, priority {priority}, maxUpgrades {maxUpgrades})";
            }
        };

		//private static readonly Dictionary<TechType, EfficiencyModifierStruct> efficiencyMultipliers = new Dictionary<TechType, EfficiencyModifierStruct>();
		//private static readonly Dictionary<TechType, MovementModifierStruct> movementModifiers = new Dictionary<TechType, MovementModifierStruct>();
		private static readonly List<EfficiencyModifierStruct> efficiencyModifiers = new List<EfficiencyModifierStruct>();
		private static readonly List<MovementModifierStruct> movementModifiers = new List<MovementModifierStruct>();

		// The idea with obsoletedTechTypes is; If the key is installed, loop through the list.
		// If any TechType in the list is also installed, the key is considered "obsolete" and is rendered inactive.

		private bool bHasTravelModule;
		private const float moduleWaterDampening = 1f; // Movement is divided by this value when travelling over water.
													   // Don't set it below 1f, as that makes the Snowfox *more* manoeuvrable over water than over land.
		private const float moduleWaterOffset = 1f; // The default value for ground travel is 2m.
		internal float fSolarChargeMultiplier = 0.065f; // Multiplier applied to the local light amount to get amount of power regained from solar charger
														// default enginePowerConsumption = 0.06666667f so we want the solar charger to be a little bit less efficient than this.
														// Given that the hoverbike is going to be on the surface more often than not, depth is not exactly going to be a major factor, so this is mainly
														// based on the current light level.
		internal const float fMaxSolarDepth = 2f;
		private bool bBikeOverWater;
		private TechType techTypeWaterTravel => Main.prefabHbWaterTravelModule.TechType;
		private TechType techTypeSolarCharger => Main.prefabHbSolarCharger.TechType;
		private TechType techTypeHullModule => Main.prefabHbHullModule.TechType;
		private TechType techTypeEngineEfficiency => Main.prefabHbEngineModule.TechType;
		private TechType techTypeSpeed => Main.prefabHbSpeedModule.TechType;
		private TechType techTypeMobility => Main.prefabHbMobility.TechType;

		internal static bool AddEfficiencyMultiplier(TechType module, float multiplier, int priority = 1, int maxUpgrades = 1, bool bUpdateIfPresent = false)
		{
			// Multiple copies of a module stack, up to a maximum limit of maxUpgrades.
			for (int i = 0; i < efficiencyModifiers.Count; i++)
			{
				EfficiencyModifierStruct modifier = efficiencyModifiers[i];
				if (modifier.techType == module)
				{
					Log.LogDebug($"AddEfficiencyMultiplier called multiple times for TechType {module}; previous value was {modifier.ToString()}; new value is {multiplier} with maxUpgrades {maxUpgrades}; value "
						+ (bUpdateIfPresent ? "was " : "was not ") + "updated");
					if (bUpdateIfPresent)
						efficiencyModifiers[i] = new EfficiencyModifierStruct(module, multiplier, priority, maxUpgrades);
					return bUpdateIfPresent;
				}
			}

			efficiencyModifiers.Add(new EfficiencyModifierStruct(module, multiplier, priority, maxUpgrades));
			return true;

		}
		internal static bool AddMovementModifier(TechType module, float speedModifier, float cooldownModifier, int priority = 1, int maxUpgrades = 1, bool bUpdateIfPresent = false)
		{
			// Multiple copies of a module stack, up to a maximum limit of maxUpgrades.
			for (int i = 0; i < movementModifiers.Count; i++)
			{
				MovementModifierStruct modifier = movementModifiers[i];
				if (modifier.techType == module)
				{
					Log.LogDebug($"AddEfficiencyMultiplier called multiple times for TechType {module}; previous value was {modifier.ToString()}; new value speedModifier = {speedModifier}, cooldownModifier = {cooldownModifier}, maxUpgrades = {maxUpgrades}; value "
						+ (bUpdateIfPresent ? "was " : "was not ") + "updated");
					if (bUpdateIfPresent)
						movementModifiers[i] = new MovementModifierStruct(module, speedModifier, cooldownModifier, priority, maxUpgrades);
					return bUpdateIfPresent;
				}
			}

			movementModifiers.Add(new MovementModifierStruct(module, speedModifier, cooldownModifier, priority, maxUpgrades));
			return true;
		}

		internal bool TryGetDefaultFloat(string name, out float value)
		{
			if (defaultValues.TryGetValue(name, out object obj))
			{
				value = (float)obj;
				return true;
			}

			value = -2147483648f;
			return false;
		}

		public virtual void Initialise(ref Hoverbike vehicle)
		{
			parentHoverbike = vehicle;
			//defaultWaterDampening = vehicle.waterDampening;
			//defaultWaterOffset = vehicle.waterLevelOffset;
			foreach (HoverbikeField hoverbikeField in hoverbikeFields)
			{
				FieldInfo field = typeof(Hoverbike).GetField(hoverbikeField.fieldName, hoverbikeField.bindingFlags);
				if (field != null)
				{
					if (defaultValues.ContainsKey(hoverbikeField.fieldName))
						Log.LogError($"Tried to add key {hoverbikeField.fieldName} more than once!");
					else
					{
						defaultValues.Add(hoverbikeField.fieldName, (float)field.GetValue(vehicle));
						Log.LogDebug($"HoverbikeUpdate.Initialise(): Got default value of {defaultValues[hoverbikeField.fieldName]} for field name {hoverbikeField.fieldName}");
					}
				}
				else
				{
					Log.LogDebug($"Could not find field {hoverbikeField.fieldName} in class Hoverbike");
				}
			}
		}

		protected virtual int GetModuleCount(TechType techType, Hoverbike instance = null)
		{
			if (instance != null)
				return instance.modules.GetCount(techType);

			if (parentHoverbike == null || parentHoverbike.modules == null)
				return 0;

			return parentHoverbike.modules.GetCount(techType);
		}

		internal virtual void PreUpdate(Hoverbike instance = null)
		{
			if (parentHoverbike == null)
			{
				if (instance != null)
					Initialise(ref instance);
				else
					return;
			}
		}

		// Not to be confused with Unity's Update
		internal virtual void OnUpdate(Hoverbike instance = null)
		{
		}

		internal virtual void PostUpdate(Hoverbike instance = null)
		{
		}

		internal virtual void PreUpdateEnergy(Hoverbike instance = null)
		{
			if (parentHoverbike == null)
			{
				if (instance != null)
					parentHoverbike = instance;
				else
					return;
			}

			if (GetModuleCount(techTypeSolarCharger) > 0)
			{
				//if (Main.bVerboseLogging)
				//	Log.LogDebug("Solar charger found in Hoverbike");

				DayNightCycle main = DayNightCycle.main;
				if (main == null)
					return;

				float depthMultiplier = Mathf.Clamp01((fMaxSolarDepth + parentHoverbike.transform.position.y) / fMaxSolarDepth);
				float lightScalar = main.GetLocalLightScalar();
				float deltaTime = Time.deltaTime;

				//Log.LogDebug($"Charging Hoverbike battery with depthMultiplier of {depthMultiplier}, lightScalar = {lightScalar}, fSolarChargeMultiplier = {fSolarChargeMultiplier}, and deltaTime of {deltaTime}");
				parentHoverbike.energyMixin.AddEnergy(deltaTime * fSolarChargeMultiplier * depthMultiplier * lightScalar);
			}
		}

		internal virtual void PostUpdateEnergy(Hoverbike instance = null)
		{
			if (parentHoverbike == null)
			{
				if (instance != null)
					Initialise(ref instance);
				else
					return;
			}

			if (parentHoverbike.GetPilotingCraft() && bBikeOverWater)
				parentHoverbike.energyMixin.ConsumeEnergy(Time.deltaTime * parentHoverbike.enginePowerConsumption); // This effectively doubles power consumption when above water.
		}

		internal virtual void PostUpgradeModuleChange(int slotID, TechType techType, bool added, Hoverbike instance = null)
		{
			if (parentHoverbike == null)
			{
				if (instance != null)
					Initialise(ref instance);
				else
					return;
			}

			// My first instinct is always to use switch() in situations like this, but you can't use switch() with non-const types.
			if (techType == techTypeWaterTravel)
			{
				bHasTravelModule = added;
			}
			else if (techType == techTypeHullModule)
			{
				parentHoverbike.gameObject.EnsureComponent<HoverbikeStructuralIntegrityModifier>().SetActive(added);
			}
			else if (techType == techTypeMobility)
			{
				bHasTravelModule = added;
				if (GetModuleCount(TechType.HoverbikeJumpModule) < 1)
				{
					FieldInfo jump = typeof(Hoverbike).GetField("jumpEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
					if (jump != null)
						jump.SetValue(parentHoverbike, added);
				}
			}

			if (TryGetDefaultFloat("enginePowerConsumption", out float defaultPowerConsumption))
			{
				float effectiveEfficiency = 1f;
				int priority = 0;
				Log.LogDebug($"HoverbikeUpdate.PostUpgradeModuleChange(): applying efficiency modifiers");
				foreach (EfficiencyModifierStruct modifier in efficiencyModifiers)
				{
					Log.LogDebug($"Using modifier {modifier.ToString()}");
					int moduleCount = GetModuleCount(modifier.techType);
					if (moduleCount > 0)
					{
						moduleCount = Math.Min(moduleCount, modifier.maxUpgrades);  // This could've been included as part of the assignment, but this way we only do the Min() call if it's needed.
						if (modifier.priority > priority)
						{
							effectiveEfficiency = modifier.efficiencyMultiplier * moduleCount;
							priority = modifier.priority;
							Log.LogDebug($"Using modifier with higher priority {priority}; effectiveEfficiency now {modifier.efficiencyMultiplier}");
						}
						else if (modifier.priority == priority)
						{
							effectiveEfficiency *= modifier.efficiencyMultiplier * moduleCount;
						}
						// There is no else.
					}
				}
				float newConsumption = defaultPowerConsumption * effectiveEfficiency;
				Log.LogDebug($"HoverbikeUpdate.PostUpgradeModuleChange(): changed TechType {techType.AsString()}; found defaultPowerConsumption of {defaultPowerConsumption} and calculated new power consumption of {newConsumption}");
				parentHoverbike.enginePowerConsumption = newConsumption;
			}

			if (TryGetDefaultFloat(nameof(Hoverbike.forwardAccel), out float defaultForwardAccel)
				&& TryGetDefaultFloat(nameof(Hoverbike.forwardBoostForce), out float defaultBoost)
				&& TryGetDefaultFloat(nameof(Hoverbike.boostCooldown), out float defaultCooldown)
				&& TryGetDefaultFloat(nameof(Hoverbike.jumpCooldown), out float defaultJumpCooldown))
			{
				float speedMult = 1f;
				float cooldownMult = 1f;
				int priority = 0;

				Log.LogDebug($"HoverbikeUpdate.PostUpgradeModuleChange(): applying movement modifiers");
				foreach (MovementModifierStruct modifier in movementModifiers)
				{
					Log.LogDebug($"Using modifier {modifier.ToString()}");
					int moduleCount = GetModuleCount(modifier.techType);
					if (moduleCount > 0)
					{
						moduleCount = Math.Min(moduleCount, modifier.maxUpgrades);
						if (modifier.priority > priority)
						{
							speedMult = modifier.speedModifier * moduleCount;
							cooldownMult = modifier.cooldownModifier * moduleCount;
							priority = modifier.priority;
							Log.LogDebug($"Using modifier with higher priority {priority}; speedMult now {speedMult}, cooldownMult now {cooldownMult}");
						}
						else if (modifier.priority == priority)
						{
							speedMult *= modifier.speedModifier * moduleCount;
							cooldownMult *= modifier.cooldownModifier * moduleCount;
							Log.LogDebug($"Using modifier with equal priority {priority}; speedMult now {speedMult}, cooldownMult now {cooldownMult}");
						}
						// There is no else.
					}
				}
				parentHoverbike.forwardAccel = defaultForwardAccel * speedMult;
				parentHoverbike.forwardBoostForce = defaultBoost * speedMult;
				parentHoverbike.boostCooldown = defaultCooldown * cooldownMult;
				parentHoverbike.jumpCooldown = defaultJumpCooldown * cooldownMult;
			}
		}

		internal virtual void PrePhysicsMove(Hoverbike instance = null)
		{
			if (parentHoverbike == null)
			{
				if (instance != null)
					Initialise(ref instance);
				else
					return;
			}

			bBikeOverWater = !parentHoverbike.debugIgnoreWater && instance.transform.position.y < parentHoverbike.waterLevelOffset + 3f;

			if (bHasTravelModule)
			{
				if (parentHoverbike.GetPilotingCraft())
				{
					parentHoverbike.waterLevelOffset = moduleWaterOffset; // Makes the bike hover above the surface when piloted with the Water Travel Module
					parentHoverbike.waterDampening = moduleWaterDampening;

					return;
				}
			}

			if (TryGetDefaultFloat("waterOffset", out float defaultWaterOffset))
			{
				parentHoverbike.waterLevelOffset = defaultWaterOffset; // Let the hoverbike sink to water level when not piloted.
																	   // Otherwise it can be kind of hard to get on if the water offset is too high.
																	   // Of course this also runs if the module isn't installed.
			}
			if (TryGetDefaultFloat("waterDampening", out float defaultWaterDampening))
			{
				parentHoverbike.waterDampening = defaultWaterDampening; // Without this, the hoverbike will bob up and down on the water erratically when not piloted.
																		// And we have to do it in PhysicsMove because piloted/not piloted changes more often than PostUpgradeModuleChange.
			}
		}

		internal virtual void PostPhysicsMove(Hoverbike instance = null)
		{ }

		internal virtual void PostHoverEngines(Hoverbike instance = null)
		{
			if (parentHoverbike == null)
			{
				if (instance != null)
					Initialise(ref instance);
				else
					return;
			}
		}

		internal bool WaterHoverMode(Hoverbike instance = null)
		{
			// This method is only called if Hoverbike.HoverEngines() has already determined that the hoverbike is above water.
			// So we basically just need to return whether or not the Water Travel Module is installed.
			// We need to invert it though, as if the value is true, the code restricts the hoverbike's movement.
			if(parentHoverbike == null)
			{
				if (instance != null)
					parentHoverbike = instance;
				else
					return false;
			}

			return GetModuleCount(techTypeWaterTravel) < 1;
		}
	}
}
