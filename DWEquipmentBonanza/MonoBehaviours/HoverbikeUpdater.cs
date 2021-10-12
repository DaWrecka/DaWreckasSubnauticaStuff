using Common;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if BELOWZERO
    internal class HoverbikeUpdater : MonoBehaviour
	{
		private const float SelfRepairRate = 0.5f; // Amount of health restored per second when Self Repair is active.
		private const float SelfRepairEnergyConsumption = 0.5f; // Energy consumed per second when Self Repair is active
		private const float SelfRepairDisableThreshold = 0.1f; // If battery power is lower than this fraction, disable Self Repair.
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

		internal struct EfficiencyModifier
		{
			//public TechType techType;
			public int maxUpgrades;
			public float efficiencyMultiplier;
			public int priority;

			public EfficiencyModifier(float EfficiencyMultiplier, int priority = 1, int MaxUpgrades = 1)
			{
				//this.techType = module;
				this.maxUpgrades = MaxUpgrades;
				this.efficiencyMultiplier = EfficiencyMultiplier;
				this.priority = priority;
			}

			public override string ToString()
			{
				return $"(efficiencyMultiplier {efficiencyMultiplier}, priority {priority}, maxUpgrades {maxUpgrades})";
			}
		};

		internal struct MovementModifier
		{
			//public TechType techType;
			public int maxUpgrades;
			public float speedModifier;
			public float cooldownModifier;
			public int priority;

			public MovementModifier(float speedModifier, float cooldownModifier, int priority = 1, int MaxUpgrades = 1)
			{
				//this.techType = module;
				this.maxUpgrades = MaxUpgrades;
				this.speedModifier = speedModifier;
				this.cooldownModifier = cooldownModifier;
				this.priority = priority;
			}

            public override string ToString()
            {
				return $"(speedMod {speedModifier}, cooldownMod {cooldownModifier}, priority {priority}, maxUpgrades {maxUpgrades})";
            }
        };

		//private static readonly List<EfficiencyModifierStruct> efficiencyModifiers = new List<EfficiencyModifierStruct>();
		//private static readonly List<MovementModifierStruct> movementModifiers = new List<MovementModifierStruct>();
		private static readonly Dictionary<TechType, EfficiencyModifier> efficiencyModifiers = new Dictionary<TechType, EfficiencyModifier>();
		private static readonly Dictionary<TechType, MovementModifier> movementModifiers = new Dictionary<TechType, MovementModifier>();

		private bool bHasTravelModule;
		private bool bHasSelfRepair;
		private const float moduleWaterDampening = 1f; // Movement is divided by this value when travelling over water. UWE default is 10f.
													   // Don't set it below 1f, as that makes the Snowfox *more* manoeuvrable over water than over land.
		private const float moduleWaterOffset = 1f; // The default value for ground travel is 2m.
		internal float fSolarChargeMultiplier = 0.05f; // Multiplier applied to the local light amount to get amount of power regained from solar charger
														// default enginePowerConsumption = 0.06666667f so we want the solar charger to be a little bit less efficient than this.
														// Given that the hoverbike is going to be on the surface more often than not, depth is not exactly going to be a major factor, so this is mainly
														// based on the current light level.
		internal const float fMaxSolarDepth = 2f;
		private bool bBikeOverWater;
		private static TechType techTypeWaterTravel => Main.GetModTechType("HoverbikeWaterTravelModule");// Main.prefabHbWaterTravelModule.TechType;
		private static TechType techTypeSolarCharger => Main.GetModTechType("HoverbikeSolarChargerModule");// Main.prefabHbSolarCharger.TechType;
		private static TechType techTypeHullModule => Main.GetModTechType("HoverbikeStructuralIntegrityModule");// Main.prefabHbHullModule.TechType;
		private static TechType techTypeEngineEfficiency => Main.GetModTechType("HoverbikeEngineEfficiencyModule");// Main.prefabHbEngineModule.TechType;
		private static TechType techTypeSpeed => Main.GetModTechType("HoverbikeSpeedModule");// Main.prefabHbSpeedModule.TechType;
		private static TechType techTypeMobility => Main.GetModTechType("HoverbikeMobilityUpgrade");// Main.prefabHbMobility.TechType;
		private static TechType techTypeRepair => Main.GetModTechType("HoverbikeSelfRepairModule");
		private static TechType techTypeDurability => Main.GetModTechType("HoverbikeDurabilitySystem");

		internal static bool AddEfficiencyMultiplier(TechType module, float multiplier, int priority = 1, int maxUpgrades = 1, bool bUpdateIfPresent = false)
		{
			// Multiple copies of a module stack, up to a maximum limit of maxUpgrades.
			//for (int i = 0; i < efficiencyModifiers.Count; i++)
			if(efficiencyModifiers.TryGetValue(module, out EfficiencyModifier modifier))
			{
				//EfficiencyModifierStruct modifier = efficiencyModifiers[i];
				//if (modifier.techType == module)
				//{
					Log.LogDebug($"AddEfficiencyMultiplier called multiple times for TechType {module}; previous value was {modifier.ToString()}; new value is {multiplier} with maxUpgrades {maxUpgrades}; value "
						+ (bUpdateIfPresent ? "was " : "was not ") + "updated");
					if (bUpdateIfPresent)
						efficiencyModifiers.Remove(module);
					else
						return false;
				//}
			}

			efficiencyModifiers.Add(module, new EfficiencyModifier(multiplier, priority, maxUpgrades));
			return true;

		}
		internal static bool AddMovementModifier(TechType module, float speedModifier, float cooldownModifier, int priority = 1, int maxUpgrades = 1, bool bUpdateIfPresent = false)
		{
			// Multiple copies of a module stack, up to a maximum limit of maxUpgrades.
			if(movementModifiers.TryGetValue(module, out MovementModifier modifier))
			//for (int i = 0; i < movementModifiers.Count; i++)
			{
				//MovementModifierStruct modifier = movementModifiers[i];
				//if (modifier.techType == module)
				//{
					Log.LogDebug($"AddMovementModifier called multiple times for TechType {module}; previous value was {modifier.ToString()}; new value speedModifier = {speedModifier}, cooldownModifier = {cooldownModifier}, maxUpgrades = {maxUpgrades}; value "
						+ (bUpdateIfPresent ? "was " : "was not ") + "updated");
					if (bUpdateIfPresent)
						movementModifiers.Remove(module);
					else
						return false;
				//}
			}

			movementModifiers.Add(module, new MovementModifier(speedModifier, cooldownModifier, priority, maxUpgrades));
			return true;
		}

		internal bool TryGetDefaultFloat(string name, out float value)
		{
			if (defaultValues.TryGetValue(name, out object obj))
			{
				value = (float)obj;
				return true;
			}

			value = Mathf.NegativeInfinity;
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

		protected static int StaticGetModuleCount(TechType techType, Hoverbike instance = null)
		{
			if (instance == null)
				return 0;

			return instance.modules.GetCount(techType);
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
			float deltaTime = Time.deltaTime;
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

				DayNightCycle dayNightCycle = DayNightCycle.main;
				if (dayNightCycle == null)
					return;

				float depthMultiplier = Mathf.Clamp01((fMaxSolarDepth + parentHoverbike.transform.position.y) / fMaxSolarDepth);
				float lightScalar = dayNightCycle.GetLocalLightScalar();

				//Log.LogDebug($"Charging Hoverbike battery with depthMultiplier of {depthMultiplier}, lightScalar = {lightScalar}, fSolarChargeMultiplier = {fSolarChargeMultiplier}, and deltaTime of {deltaTime}");
				parentHoverbike.energyMixin.AddEnergy(deltaTime * fSolarChargeMultiplier * depthMultiplier * lightScalar);
			}
			if (bHasSelfRepair)
			{
				if(parentHoverbike.liveMixin.GetHealthFraction() < 1f && parentHoverbike.energyMixin.GetEnergyScalar() > SelfRepairDisableThreshold)
				{
					parentHoverbike.energyMixin.ConsumeEnergy(SelfRepairEnergyConsumption * deltaTime);
					parentHoverbike.liveMixin.AddHealth(SelfRepairRate * deltaTime);
				}
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

		internal static bool StaticHasTravelModule(Hoverbike instance)
		{
			if (instance == null)
				return false;

			return (StaticGetModuleCount(techTypeMobility, instance) + StaticGetModuleCount(techTypeWaterTravel, instance)) > 0;
		}

		internal bool HasTravelModule(Hoverbike instance = null)
		{
			if (instance != null)
				return StaticHasTravelModule(instance);

			return StaticHasTravelModule(parentHoverbike);
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
				bHasTravelModule = HasTravelModule();
			}
			else if (techType == techTypeHullModule)
			{
				parentHoverbike.gameObject.EnsureComponent<HoverbikeStructuralIntegrityModifier>().SetActive(added);
			}
			else if (techType == techTypeMobility)
			{
				bHasTravelModule = HasTravelModule();
				if (GetModuleCount(TechType.HoverbikeJumpModule) < 1)
				{
					FieldInfo jump = typeof(Hoverbike).GetField("jumpEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
					if (jump != null)
						jump.SetValue(parentHoverbike, added);
				}
			}
			else if (techType == techTypeRepair)
			{
				bHasSelfRepair = added;
			}
			else if (techType == techTypeDurability)
			{
				bHasSelfRepair = added;
				parentHoverbike.gameObject.EnsureComponent<HoverbikeStructuralIntegrityModifier>().SetActive(added);
			}

			if (TryGetDefaultFloat("enginePowerConsumption", out float defaultPowerConsumption))
			{
				float effectiveEfficiency = 1f;
				int priority = 0;
				//Log.LogDebug($"HoverbikeUpdate.PostUpgradeModuleChange(): applying efficiency modifiers");
				//foreach (EfficiencyModifier modifier in efficiencyModifiers)
				foreach(KeyValuePair<TechType, EfficiencyModifier> modifierPair in efficiencyModifiers)
				{
					EfficiencyModifier modifier = modifierPair.Value;
					//Log.LogDebug($"Using modifier {modifier.ToString()}");
					//int moduleCount = GetModuleCount(modifier.techType);
					int moduleCount = GetModuleCount(modifierPair.Key);
					if (moduleCount > 0)
					{
						moduleCount = Math.Min(moduleCount, modifier.maxUpgrades);  // This could've been included as part of the assignment, but this way we only do the Min() call if it's needed.
						if (modifier.priority > priority)
						{
							effectiveEfficiency = modifier.efficiencyMultiplier * moduleCount;
							priority = modifier.priority;
							//Log.LogDebug($"Using modifier with higher priority {priority}; effectiveEfficiency now {modifier.efficiencyMultiplier}");
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

				//Log.LogDebug($"HoverbikeUpdate.PostUpgradeModuleChange(): applying movement modifiers");
				//foreach (MovementModifierStruct modifier in movementModifiers)
				foreach(KeyValuePair<TechType, MovementModifier> modifierPair in movementModifiers)
				{
					//Log.LogDebug($"Using modifier {modifier.ToString()}");
					MovementModifier modifier = modifierPair.Value;
					int moduleCount = GetModuleCount(modifierPair.Key);
					if (moduleCount > 0)
					{
						moduleCount = Math.Min(moduleCount, modifier.maxUpgrades);
						if (modifier.priority > priority)
						{
							speedMult = modifier.speedModifier * moduleCount;
							cooldownMult = modifier.cooldownModifier * moduleCount;
							priority = modifier.priority;
							//Log.LogDebug($"Using modifier with higher priority {priority}; speedMult now {speedMult}, cooldownMult now {cooldownMult}");
						}
						else if (modifier.priority == priority)
						{
							speedMult *= modifier.speedModifier * moduleCount;
							cooldownMult *= modifier.cooldownModifier * moduleCount;
							//Log.LogDebug($"Using modifier with equal priority {priority}; speedMult now {speedMult}, cooldownMult now {cooldownMult}");
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

			return HasTravelModule();
		}
	}
#endif
}
