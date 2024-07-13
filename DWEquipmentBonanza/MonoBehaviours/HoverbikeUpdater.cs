using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if BELOWZERO
	internal class HoverbikeUpdater : MonoBehaviour
	{
		private Hoverbike parentHoverbike;
		//private float defaultWaterDampening;
		//private float defaultWaterOffset;
		private static Dictionary<string, float> defaultValues = new Dictionary<string, float>();
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
			new HoverbikeField(nameof(Hoverbike.jumpCooldown), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.sidewaysTorque), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.timeBeforePowerDown), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.animationInterpolationSpeed), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.stabilitySpeed), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.stability), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.boyancy), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.emptyVehicleHeight), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.impactCompensatorHeight), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.hoverForce), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.verticalBoostForce), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.jumpDecay), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.gravity), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.verticalDampening), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.horizontalDampening), BindingFlags.Public),
			new HoverbikeField(nameof(Hoverbike.constantForceDampening), BindingFlags.Public),
		};

		// An EfficiencyModifier is associated with a module by way of the dictionary;
		// The TechType is added as key, and its EfficiencyModule dictates the modifer for that TechType
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
		private static float SelfRepairRate = 0.5f; // Amount of health restored per second when Self Repair is active.
		private static float SelfRepairEnergyConsumption = 0.5f; // Energy consumed per second when Self Repair is active
		private static float SelfRepairDisableThreshold = 0.1f; // If battery power is lower than this fraction, disable Self Repair.
		private static readonly Dictionary<TechType, EfficiencyModifier> efficiencyModifiers = new Dictionary<TechType, EfficiencyModifier>();
		private static readonly Dictionary<TechType, MovementModifier> movementModifiers = new Dictionary<TechType, MovementModifier>();

		private static float moduleWaterDampening = 1f; // Movement is divided by this value when travelling over water. UWE default is 10f.
														// Don't set it below 1f, as that makes the Snowfox *more* manoeuvrable over water than over land.
		private static float moduleWaterOffset = 1f; // The default value for ground travel is 2m.
		internal static float fSolarChargeMultiplier = 0.05f; // Multiplier applied to the local light amount to get amount of power regained from solar charger
															  // default enginePowerConsumption = 0.06666667f so we want the solar charger to be a little bit less efficient than this.
															  // Given that the hoverbike is going to be on the surface more often than not, depth is not exactly going to be a major factor, so this is mainly
															  // based on the current light level.
		internal const float fMaxSolarDepth = 2f;
		private static TechType techTypeWaterTravel => Main.GetModTechType("HoverbikeWaterTravelModule");// Main.prefabHbWaterTravelModule.TechType;
		private static TechType techTypeSolarCharger => Main.GetModTechType("HoverbikeSolarChargerModule");// Main.prefabHbSolarCharger.TechType;
		private static TechType techTypeHullModule => Main.GetModTechType("HoverbikeStructuralIntegrityModule");// Main.prefabHbHullModule.TechType;
		private static TechType techTypeEngineEfficiency => Main.GetModTechType("HoverbikeEngineEfficiencyModule");// Main.prefabHbEngineModule.TechType;
		private static TechType techTypeSpeed => Main.GetModTechType("HoverbikeSpeedModule");// Main.prefabHbSpeedModule.TechType;
		private static TechType techTypeMobility => Main.GetModTechType("HoverbikeMobilityUpgrade");// Main.prefabHbMobility.TechType;
		private static TechType techTypeRepair => Main.GetModTechType("HoverbikeSelfRepairModule");
		private static TechType techTypeDurability => Main.GetModTechType("HoverbikeDurabilitySystem");
		private static TechType techTypeBoostUpgrade => Main.GetModTechType("HoverbikeBoostUpgradeModule");
		private static TechType techTypeQuantumLocker => Main.GetModTechType("HoverbikeQuantumLocker");

		public bool bHasTravelModule { get; protected set; }
		public bool bHasSelfRepair { get; protected set; }
		public bool bBikeOverWater { get; protected set; }
		public bool bConfigCoroutineState { get; protected set; }

		// Operational values: Shield
		public bool bHasShield { get; protected set; }
		public float ShieldStrength { get; protected set; }
		public float NextShieldRecharge { get; protected set; }
		// Configurable values: Shield
		public float MaxShieldStrength { get; protected set; }
		public float ShieldRechargeDelay { get; protected set; }
		public float ShieldChargeRate { get; protected set; }
		public float ShieldEnergyConsumeRate { get; protected set; }

		// Operational values: Upgraded boost
		private const float defaultBoostDuration = 6f;
		public bool bHasBoostUpgrade { get; protected set; }
		public bool isBoosting { get; protected set; }
		public bool isOverheated { get; protected set; }
		public float thisBoostDuration { get; protected set; } // How long we have been boosting?
		// Configurable values: Upgraded boost
		public static float boostUpgradeDuration { get; protected set; } // Maximum sustained duration of upgraded boost
		public static float cooldownRate { get; protected set; } // How fast we cool down normally
		public static float cooldownRateOverheated { get; protected set; } // How fast we cool down following an overheat event

		// Operational and configurable values: Snowfox Quantum Locker
		public KeyCode activationKey { get; protected set; }
		public bool bHasQuantumLocker { get; protected set; }
		private bool bLastHotkeyState = false; 

		private void ApplyValues(DWConfig config, bool isEvent = false)
		{
			//if(isEvent)
			//	ErrorMessage.AddMessage("HoverbikeUpdater.ApplyValues() event received");
			SelfRepairRate = config.HoverbikeSelfRepairRate;
			SelfRepairEnergyConsumption = config.HoverbikeSelfRepairEnergyConsumption;
			SelfRepairDisableThreshold = config.HoverbikeSelfRepairDisableThreshold * 0.01f;
			moduleWaterDampening = config.SnowfoxWaterModuleDampening;
			moduleWaterOffset = config.SnowfoxWaterModuleOffset;
			fSolarChargeMultiplier = config.SnowfoxSolarMultiplier;
			boostUpgradeDuration = config.SnowfoxBoostDuration;
			cooldownRate = config.SnowfoxCooldownRatePct * 0.01f; // The config value is expressed as a value between 0.5 and 200, where 200 means 2x rate, so we multiply by 0.01 to get a usable value.
			cooldownRateOverheated = config.SnowfoxCooldownRateOverheatPct * 0.01f;
			MaxShieldStrength = (config.SnowfoxMaxShield != 0 ? config.SnowfoxMaxShield : 1f); // Make sure MaxShieldStrength is not zero
			ShieldRechargeDelay = config.SnowfoxShieldRechargeDelay;
			ShieldChargeRate = config.SnowfoxShieldRechargeRate * 0.01f;
			ShieldEnergyConsumeRate = config.SnowfoxShieldEnergyRate * 0.01f;
			activationKey = config.SnowfoxQuantumTrigger;
			UWE.CoroutineHost.StartCoroutine(ApplyValuesCoroutine(config, isEvent));
		}

		private IEnumerator ApplyValuesCoroutine(DWConfig config, bool isEvent)
		{
			if (bConfigCoroutineState)
				yield break;

			bConfigCoroutineState = true;
			while (parentHoverbike == null)
			{
				parentHoverbike = gameObject?.GetComponent<Hoverbike>();
				yield return new WaitForEndOfFrame();
			}

			if(parentHoverbike?.gameObject != null && parentHoverbike.gameObject.TryGetComponent<DealDamageOnImpact>(out DealDamageOnImpact ddoi))
				ddoi.damageBases = config.bSnowfoxDamageBases;
			bConfigCoroutineState = false;
		}

		// Absorb incoming damage, returning any damage left over after the shield is depleted.
		// Current implementation allows the shield to absorb any amount of damage, so long as it is not fully depleted.
		internal float ShieldAbsorb(float incomingDamage)
		{
			if (bHasShield && ShieldStrength > 0f && incomingDamage > 0f)
			{
				NextShieldRecharge = Time.time + ShieldRechargeDelay;
				float absorbedDamage = Mathf.Min(ShieldStrength, incomingDamage);
				ShieldStrength -= absorbedDamage;
				incomingDamage = 0f;
			}

			return incomingDamage;
		}

		internal static bool AddEfficiencyMultiplier(TechType module, float multiplier, int priority = 1, int maxUpgrades = 1, bool bUpdateIfPresent = false)
		{
			// Multiple copies of a module stack, up to a maximum limit of maxUpgrades.
			//for (int i = 0; i < efficiencyModifiers.Count; i++)
			if (efficiencyModifiers.TryGetValue(module, out EfficiencyModifier modifier))
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
			if (movementModifiers.TryGetValue(module, out MovementModifier modifier))
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

		public void Start()
		{
			//DevConsole.RegisterConsoleCommand(this, "hoverbikeparams", false, false);
		}

		/*private void OnConsoleCommand_hoverbikeparams(NotificationCenter.Notification n)
		{
			if (n != null && n.data != null)
			{
				if (n.data.Count < 1)
				{
					ErrorMessage.AddMessage("Usage: hoverbikeparams <mass multiplier> <forward acceleration multiplier> <boost force multiplier>")
				}
			}
		}*/

		internal bool TryGetDefaultFloat(string name, out float value)
		{
			if (defaultValues.TryGetValue(name, out float obj))
			{
				value = (float)obj;
				return true;
			}

			value = Mathf.NegativeInfinity;
			return false;
		}

		public virtual void Initialise(ref Hoverbike vehicle, DWConfig config = null)
		{
			parentHoverbike = vehicle;
			boostUpgradeDuration = defaultBoostDuration;
			//defaultWaterDampening = vehicle.waterDampening;
			//defaultWaterOffset = vehicle.waterLevelOffset;
			foreach (HoverbikeField hoverbikeField in hoverbikeFields)
			{
				FieldInfo field = typeof(Hoverbike).GetField(hoverbikeField.fieldName, hoverbikeField.bindingFlags);
				if (field != null)
				{
					if (!defaultValues.ContainsKey(hoverbikeField.fieldName))
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

			if (gameObject != null)
			{
				LiveMixin hoverbikeHealth = null;
				float defaultHealth;

				if (config != null)
				{
					config.onOptionChanged += this.ApplyValues;
					ApplyValues(config, false);
				}

				if (gameObject.TryGetComponent<LiveMixin>(out hoverbikeHealth))
				{
					bool gotHealth = Main.defaultHealth.TryGetValue(TechType.Hoverbike, out defaultHealth);
					if (gotHealth)
					{
						float instanceHealthPct = Mathf.Min(hoverbikeHealth.GetHealthFraction(), 1f);
						float maxHealth = defaultHealth * Main.config.HoverbikeHealthMult;

						hoverbikeHealth.data.maxHealth = maxHealth;
						hoverbikeHealth.health = maxHealth * instanceHealthPct;
					}
					else
					{
						Log.LogError("Could not get default health for TechType Hoverbike");
					}
				}
				else
				{
					Log.LogError($"Could not get LiveMixin for Hoverbike object");
					return;
				}

				// We're going to do something a little off-spec here... We're going to replace the main bike's BoxCollider with a CapsuleCollider.
				// Finding it is a little tricky, but not massively-so.
				// Hoverbike has four BoxColliders; two of them have parents named Collider. Only one of them has a grandparent named Collision.
				foreach (var box in parentHoverbike.gameObject.GetComponentsInChildren<BoxCollider>())
				{
					//Console.WriteLine(box.gameObject.transform.parent.name);
					if (box.gameObject.transform.parent.name == "Collision")
					{
						box.enabled = false;
						var capsule = box.gameObject.EnsureComponent<CapsuleCollider>();
						capsule.enabled = true;
						break;
					}
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
			if (bHasQuantumLocker && parentHoverbike != null && parentHoverbike.isPiloting)
			{
				bool hotkeyState = Input.GetKeyDown(activationKey);
				if (!bLastHotkeyState)
				{
					if (hotkeyState)
					{
						if (bHasQuantumLocker)
						{
							var playerTransform = Player.main.gameObject?.transform;
							var QuantumStorage = QuantumLockerStorage.GetStorageContainer(false);
							if (playerTransform != null && QuantumStorage != null)
								QuantumStorage.Open(playerTransform);
						}
					}
				}

				bLastHotkeyState = hotkeyState;
			}
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
				if (parentHoverbike.liveMixin.GetHealthFraction() < 1f
					&& parentHoverbike.energyMixin.GetEnergyScalar() > SelfRepairDisableThreshold
					&& parentHoverbike.energyMixin.ConsumeEnergy(SelfRepairEnergyConsumption * deltaTime))
				{
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

			if (bHasShield && NextShieldRecharge < Time.time)
			{
				float rechargeAmount = Mathf.Min(MaxShieldStrength - ShieldStrength, Time.deltaTime * ShieldChargeRate * MaxShieldStrength);
				float energyConsume = rechargeAmount * ShieldEnergyConsumeRate;
				if (rechargeAmount > 0f && parentHoverbike.energyMixin.ConsumeEnergy(energyConsume))
				{
					ShieldStrength += rechargeAmount;
				}
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
				CheckStructuralIntegrity();
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
				CheckSelfRepair();
			}
			else if (techType == techTypeDurability)
			{
				CheckSelfRepair();
				CheckStructuralIntegrity();
			}
			else if (techType == techTypeBoostUpgrade)
			{
				this.bHasBoostUpgrade = parentHoverbike.modules.GetCount(techTypeBoostUpgrade) > 0;
			}
			else if (techType == techTypeQuantumLocker)
			{
				string slot = SeaTruckUpgrades.slotIDs[slotID];
				VehicleQuantumLockerComponent vQL = null;
				try
				{
					InventoryItem item = parentHoverbike?.modules.GetItemInSlot(slot);
					vQL = item.item.gameObject.GetComponent<VehicleQuantumLockerComponent>();
				}
				catch
				{

				}
				if (vQL != null)
				{
					vQL.ToggleQuantumStorage(added);
				}
				this.bHasQuantumLocker = parentHoverbike.modules.GetCount(techTypeQuantumLocker) > 0;
			}

			if (TryGetDefaultFloat("enginePowerConsumption", out float defaultPowerConsumption))
			{
				float effectiveEfficiency = 1f;
				int priority = 0;
				//Log.LogDebug($"HoverbikeUpdate.PostUpgradeModuleChange(): applying efficiency modifiers");
				//foreach (EfficiencyModifier modifier in efficiencyModifiers)
				foreach (KeyValuePair<TechType, EfficiencyModifier> modifierPair in efficiencyModifiers)
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
				foreach (KeyValuePair<TechType, MovementModifier> modifierPair in movementModifiers)
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

		protected virtual void CheckStructuralIntegrity()
		{
			bool bStructuralIntegrityActive = (parentHoverbike.modules.GetCount(techTypeHullModule) + parentHoverbike.modules.GetCount(techTypeDurability)) > 0;
			parentHoverbike.gameObject.EnsureComponent<HoverbikeStructuralIntegrityModifier>().SetActive(bStructuralIntegrityActive);
			if(parentHoverbike.gameObject.TryGetComponent<DealDamageOnImpact>(out DealDamageOnImpact ddoi))
			{
				ddoi.mirroredSelfDamageFraction = (bStructuralIntegrityActive ? 0.1f : 1f);
			}
			bHasShield = parentHoverbike.modules.GetCount(techTypeDurability) > 0;
		}

		protected virtual void CheckSelfRepair()
		{
			bHasSelfRepair = (parentHoverbike.modules.GetCount(techTypeRepair) + parentHoverbike.modules.GetCount(techTypeDurability)) > 0;
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
			if (parentHoverbike == null)
			{
				if (instance != null)
					parentHoverbike = instance;
				else
					return false;
			}

			return HasTravelModule();
		}

		private static float upgradedBoostMultiplier = 0.5f; // If upgraded boost is active, the boost force is set to the normal boost force multiplied by this value.

		public bool HandleBoost(Hoverbike hoverbike, bool bRequestBoost)
		{
			float deltaTime = Time.deltaTime;

			if (parentHoverbike == null && hoverbike != null)
				Initialise(ref hoverbike);

			if (bHasBoostUpgrade)
			{
				if (bRequestBoost && !isOverheated)
				{
					if (!isBoosting)
					{
						isBoosting = true;
						hoverbike.boostFxControl.Play();
						hoverbike.sfx_boost.Play();
						//hoverbike.SetBoostButtonState(false);
						Player.main.playerAnimator.SetTrigger("hovercraft_button_3");
					}

					thisBoostDuration = Mathf.MoveTowards(thisBoostDuration, boostUpgradeDuration, deltaTime);
					if (thisBoostDuration >= boostUpgradeDuration)
					{
						isOverheated = true;
						hoverbike.boostReset = false;
					}
					else
					{
						hoverbike.boostFuel = defaultValues.GetOrDefault("forwardBoostForce", 28600f) * upgradedBoostMultiplier;
					}
				}
				else
				{
					if(isBoosting && !isOverheated)
					isBoosting = false;
					thisBoostDuration = Mathf.MoveTowards(thisBoostDuration, 0f, deltaTime * (isOverheated ? cooldownRateOverheated : cooldownRate));
					if (thisBoostDuration <= 0f)
					{
						isOverheated = false;
						hoverbike.boostReset = true;
					}
					if (!isOverheated)
						hoverbike.boostReset = true;
				}
			}
			else
			{
				// We don't really need to do anything here... Our method's call replaces the point where the vanilla code checks the value of boostReset, so we can return that and there we go.
				// In essence, at this point we want our method to return the result of "Should the normal boost happen?"
				return bRequestBoost && hoverbike.boostReset;
			}

			return false;
		}

		// TODO: Figure out how to make the bar flash at "danger" levels.
		// "danger" levels in this case would be about 25% shield strength, 80% of maximum boost duration - or at any percentage if the boost is overheated.
		// This could be done by altering the alpha of the colour as a function of time; the problem I have is figuring out the algorithm.
		public static readonly AnimationCurve gradientBoostNormal = new AnimationCurve(
			new Keyframe(0f, 1f),
			new Keyframe(0.5f, 1f),
			new Keyframe(0.52f, 0f),
			new Keyframe(0.54f, 1f),
			new Keyframe(0.56f, 0f),
			new Keyframe(0.58f, 1f),
			new Keyframe(0.6f, 0f),
			new Keyframe(0.62f, 1f),
			new Keyframe(0.64f, 0f),
			new Keyframe(0.66f, 1f),
			new Keyframe(0.68f, 0f),
			new Keyframe(0.7f, 1f),
			new Keyframe(0.71f, 0f),
			new Keyframe(0.72f, 1f),
			new Keyframe(0.73f, 0f),
			new Keyframe(0.74f, 1f),
			new Keyframe(0.75f, 0f),
			new Keyframe(0.76f, 1f),
			new Keyframe(0.77f, 0f),
			new Keyframe(0.78f, 1f),
			new Keyframe(0.79f, 0f),
			new Keyframe(0.8f, 1f),
			new Keyframe(0.81f, 0f),
			new Keyframe(0.82f, 1f),
			new Keyframe(0.83f, 0f),
			new Keyframe(0.84f, 1f),
			new Keyframe(0.85f, 0f),
			new Keyframe(0.86f, 1f),
			new Keyframe(0.87f, 0f),
			new Keyframe(0.88f, 1f),
			new Keyframe(0.89f, 0f),
			new Keyframe(0.9f, 1f),
			new Keyframe(0.91f, 0f),
			new Keyframe(0.92f, 1f),
			new Keyframe(0.93f, 0f),
			new Keyframe(0.94f, 1f),
			new Keyframe(0.95f, 0f),
			new Keyframe(0.96f, 1f),
			new Keyframe(0.97f, 0f),
			new Keyframe(0.98f, 1f),
			new Keyframe(0.99f, 0f),
			new Keyframe(1f, 1f)
		);

		public static readonly AnimationCurve gradientBoostOverheated = new AnimationCurve(
			new Keyframe(0f, 0f),
			new Keyframe(0.1f, 1f),
			new Keyframe(0.2f, 0f),
			new Keyframe(0.3f, 1f),
			new Keyframe(0.4f, 0f),
			new Keyframe(0.5f, 1f),
			new Keyframe(0.6f, 0f),
			new Keyframe(0.7f, 1f),
			new Keyframe(0.8f, 0f),
			new Keyframe(0.9f, 1f),
			new Keyframe(1f, 0f)
		);

		private float hudTime;

		public bool HUDUpdate(HoverbikeHUD hud)
		{
			if (!hud.hudActive)
				return false;

			hudTime = (hudTime + Time.deltaTime) % 1f;
			if (bHasShield)
			{
				float shieldPct = Mathf.Clamp01(ShieldStrength / MaxShieldStrength);
				hud.speedBar.fillAmount = shieldPct;
				hud.speedBar.color = new Color(1f, 1f, 1f, (shieldPct < 0.4 ? gradientBoostOverheated.Evaluate(hudTime) : 1f));
			}
			else
				hud.speedBar.fillAmount = Mathf.Clamp(Mathf.Abs(hud.hoverbike.rb.velocity.magnitude), 0.0f, hud.hoverbike.topSpeed) / hud.hoverbike.topSpeed;

			if (bHasBoostUpgrade)
			{
				float boostPct = thisBoostDuration / boostUpgradeDuration;
				float boostColour = isOverheated ? 0f : 1 - boostPct;
				hud.boostBar.fillAmount = Mathf.Clamp(boostPct, 0f, 1f);
				//hud.boostBar.color = new Color(1f, boostColour, boostColour);
				if (isOverheated)
				{
					hud.boostBar.color = new Color(1f, 0f, 0f, gradientBoostOverheated.Evaluate(hudTime));
					parentHoverbike.goButtonColor = Color.red;
				}
				else
				{
					hud.boostBar.color = new Color(1f, boostColour, boostColour, gradientBoostNormal.Evaluate(boostPct));
					parentHoverbike.goButtonColor = hud.boostBar.color;
				}
			}
			else
			{
				if (hud.boostBar.fillAmount < hud.boostGoValue)
					hud.boostBar.fillAmount = Mathf.Lerp(hud.boostBar.fillAmount, 1f, Time.deltaTime * 15f);
				if (hud.boostBar.fillAmount > hud.boostGoValue)
					hud.boostBar.fillAmount = Mathf.Lerp(hud.boostBar.fillAmount, 0.0f, Time.deltaTime);
			}

			return false;
		}


	}
#endif
}
