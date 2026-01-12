using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if NAUTILUS
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using Nautilus.Handlers;
#else
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Handlers;
#endif
using UnityEngine;
using System;

#if LEGACY
	using Oculus.Newtonsoft.Json;
	using Oculus.Newtonsoft.Json.Serialization;
	using Oculus.Newtonsoft.Json.Converters;
#else
	using Newtonsoft.Json;
	using Newtonsoft.Json.Serialization;
	using Newtonsoft.Json.Converters;
using Nautilus.Utility;
using DWEquipmentBonanza.Patches;
#endif

namespace DWEquipmentBonanza
{
	[Menu("DW's Equipment Bonanza")]
	public class DWConfig : ConfigFile
	{
		public event OnConfigChange onOptionChanged;
		public event OnQuit onQuit;

		private const float SPIKEY_TRAP_HEALTH_MIN = 1f;
		private const float SPIKEY_TRAP_HEALTH_MAX = 15f;
		private const float SPIKEY_TRAP_HEALTH_DEFAULT = 7f;
		private const float KNIFE_DAMAGE_MIN = 20f;
		private const float KNIFE_DAMAGE_MAX = 60f;
		private const float KNIFE_DAMAGE_DEFAULT = 20f;
		private const float KNIFE_TENTACLE_DAMAGE_MIN = 2f;
		private const float KNIFE_TENTACLE_DAMAGE_MAX = 10f;
		private const float KNIFE_TENTACLE_DAMAGE_DEFAULT = 2f;
		private const float HEATBLADE_DAMAGE_MIN = 20f;
		private const float HEATBLADE_DAMAGE_MAX = 80f;
		private const float HEATBLADE_DAMAGE_DEFAULT = 20f;
		private const float HEATBLADE_TENTACLE_DAMAGE_MIN = 2f;
		private const float HEATBLADE_TENTACLE_DAMAGE_MAX = 10f;
		private const float HEATBLADE_TENTACLE_DAMAGE_DEFAULT = 2f;
		private const float VEHICLE_HEALTH_MIN = 0.2f;
		private const float VEHICLE_HEALTH_MAX = 5f;
		private const float HOVERPAD_REPAIR_RATE = 5f;
		private const float HOVERPAD_REPAIR_RATE_MIN = 1f;
		private const float HOVERPAD_REPAIR_RATE_MAX = 10f;
		private const float HOVERPAD_RECHARGE_RATE = 1f;
		private const float HOVERPAD_RECHARGE_RATE_MIN = 0.1f;
		private const float HOVERPAD_RECHARGE_RATE_MAX = 10f;
		private const string EASY = "Easy";
		private const string HARD = "Hard";
		private const string RIDICULOUS = "Ridiculous";
		private const string INSANE = "Insane";

		[Choice("Vehicle charger difficulty", new string[] { EASY, HARD, RIDICULOUS, INSANE })] //, OnChange(nameof(OnChoiceChanged))]
		public string ChargeDifficulty;

		[Choice("Self-repair charger difficulty", new string[] { EASY, HARD, RIDICULOUS, INSANE })] //, OnChange(nameof(OnChoiceChanged))]
		public string SelfRepairDifficulty;

		[Slider("Knife damage", KNIFE_DAMAGE_MIN, KNIFE_DAMAGE_MAX, DefaultValue = KNIFE_DAMAGE_DEFAULT, Id = "KnifeDamage",
			Step = 0.1f, Format = "{0:F1}",
			Tooltip = "Base damage dealt by Survival Knife (UWE default: 20)\nNote that this will affect how many times a harvestable plant can be struck with a Knife before it is destroyed, and thus how many resources you will get.")]
		public float KnifeDamage = KNIFE_DAMAGE_DEFAULT;


		[Slider("Heatblade damage", HEATBLADE_DAMAGE_MIN, HEATBLADE_DAMAGE_MAX, DefaultValue = HEATBLADE_DAMAGE_DEFAULT, Id = "HeatbladeDamage",
			Step = 0.1f, Format = "{0:F1}",
			Tooltip = "Base damage dealt by Heatblade. (UWE default: 20.0)\nNote that this will affect how many times a harvestable plant can be struck with a HeatBlade before it is destroyed, and thus how many resources you will get.")]
		public float HeatbladeDamage = HEATBLADE_DAMAGE_DEFAULT;

#if BELOWZERO
		[Keybind("Snowfox Quantum Storage trigger key")]
		public KeyCode SnowfoxQuantumTrigger = KeyCode.C;

		[Toggle(Id = "SnowfoxDamageBasesToggle", Label = "Snowfox can damage bases", Tooltip = "If enabled, the Snowfox can damage bases if it touches them")]
		public bool bSnowfoxDamageBases = false;

		[Slider("Spikey Trap tentacle health", SPIKEY_TRAP_HEALTH_MIN, SPIKEY_TRAP_HEALTH_MAX, DefaultValue = SPIKEY_TRAP_HEALTH_DEFAULT, Id = "SpikeyTrapHealth",
			Step = 1f,
			Tooltip = "Amount of damage that must be inflicted on a tentacle to convince a Spikey Trap to let go (UWE default: 7)")]
		public float SpikeyTrapTentacleHealth = SPIKEY_TRAP_HEALTH_DEFAULT;

		[Slider("Knife Tentacle damage", KNIFE_TENTACLE_DAMAGE_MIN, KNIFE_TENTACLE_DAMAGE_MAX, DefaultValue = KNIFE_TENTACLE_DAMAGE_DEFAULT, Id = "KnifeTentacleDamage",
			Step = 0.1f, Format = "{0:F1}",
			Tooltip = "Damage dealt by Survival Knife to a Spikey Trap tentacle; see above (UWE default: 2.0)")]
		public float KnifeTentacleDamage = KNIFE_TENTACLE_DAMAGE_DEFAULT;

		[Slider("Heatblade Tentacle damage", HEATBLADE_TENTACLE_DAMAGE_MIN, HEATBLADE_TENTACLE_DAMAGE_MAX, DefaultValue = HEATBLADE_TENTACLE_DAMAGE_DEFAULT, Id = "HeatbladeTentacleDamage",
			Step = 0.1f, Format = "{0:F1}",
			Tooltip = "Damage dealt by Heatblade to a Spikey Trap tentacle; see above (UWE default: 2.0)")]
		public float HeatbladeTentacleDamage = HEATBLADE_TENTACLE_DAMAGE_DEFAULT;

#endif

		[Toggle("Absolute values on HUD", Tooltip = "If enabled, vehicle HUDs (Exosuit, Seatruck and Hoverbike) will show absolute values on the HUD, instead of percentages. (so a Hoverbike with a full, unmodified Ion Battery would show 500 energy, instead of 100)")]
		public bool bHUDAbsoluteValues = true;

		[Slider("Exosuit health multiplier", VEHICLE_HEALTH_MIN, VEHICLE_HEALTH_MAX, DefaultValue = 1f, Id = "ExosuitHealthMult",
			Step = 0.05f, Format = "{0:F2}",
			Tooltip = "Maximum health of Exosuit; The default health of 600 is multiplied by this value.\nThe game must be restarted for this change to take effect.")]
		public float ExosuitHealthMult = 1f;

#if SN1
		[Slider("SeaMoth health multiplier", VEHICLE_HEALTH_MIN, VEHICLE_HEALTH_MAX, DefaultValue = 1f, Id = "SeaMothHealthMult",
			Step = 0.05f, Format = "{0:F2}",
			Tooltip = "Maximum health of SeaMoth; The default health of 200 is multiplied by this value.\nThe game must be restarted for this change to take effect.")]
		public float SeaMothHealthMult = 1f;

#elif BELOWZERO
		[Slider("SeaTruck vehicle health multiplier", VEHICLE_HEALTH_MIN, VEHICLE_HEALTH_MAX, DefaultValue = 1f, Id = "SeatruckVehicleHealthMult",
		Step = 0.05f, Format = "{0:F2}",
			Tooltip = "Maximum health of the SeaTruck vehicle (not modules); The default health of 500 is multiplied by this value.\nThe game must be restarted for this change to take effect.")]
		public float SeatruckVehicleHealthMult = 1f;

		[Slider("SeaTruck modules health multiplier", VEHICLE_HEALTH_MIN, VEHICLE_HEALTH_MAX, DefaultValue = 1f, Id = "SeatruckModulesHealthMult",
			Step = 0.05f, Format = "{0:F2}",
			Tooltip = "Maximum health of SeaTruck modules; The default health of 500 is multiplied by this value.\nThe game must be restarted for this change to take effect.")]
		public float SeatruckModulesHealthMult = 1f;

		[Slider("Snowfox health multiplier", VEHICLE_HEALTH_MIN, VEHICLE_HEALTH_MAX, DefaultValue = 1f, Id = "HoverbikeHealthMult",
			Step = 0.05f, Format = "{0:F2}",
			Tooltip = "Maximum health of Snowfox; The default health of 200 is multiplied by this value.\nThe game must be restarted for this change to take effect.")]
		public float HoverbikeHealthMult = 1f;

		[Slider("Hoverpad repair rate", HOVERPAD_REPAIR_RATE_MIN, HOVERPAD_REPAIR_RATE_MAX, DefaultValue = HOVERPAD_REPAIR_RATE, Id = "HoverpadRepairRate",
			Step = 0.5f, Format = "{0:F1}",
			Tooltip = "How much health the Snowfox regains per tick while sitting on a Hoverpad. (by default, one tick = 1 second)"), OnChange(nameof(OnSliderChange))]
		public float healAmountPerTick = HOVERPAD_REPAIR_RATE;

		[Slider("Hoverpad recharge rate", HOVERPAD_RECHARGE_RATE_MIN, HOVERPAD_RECHARGE_RATE_MAX, DefaultValue = HOVERPAD_RECHARGE_RATE, Id = "HoverpadRechargeRate",
			Step = 0.05f, Format = "{0:F2}",
			Tooltip = "How much energy the Snowfox's battery regains per tick while sitting on a Hoverpad. (by default, one tick = 1 second)"), OnChange(nameof(OnSliderChange))]
		public float rechargeAmountPerTick = HOVERPAD_RECHARGE_RATE;

		[Slider("Snowfox Self-Repair speed", 0.01f, 5f, DefaultValue = 0.5f, Id = "SnowfoxSelfRepairRate",
			Step = 0.05f, Format = "{0:F2}",
			Tooltip = "Number of units of health restored by the Snowfox Self-Repair Module (and variants) per second"), OnChange(nameof(OnSliderChange))]
		public float HoverbikeSelfRepairRate = 0.5f; // Amount of health restored per second when Self Repair is active.
		[Slider("Snowfox Self-Repair energy consumption", 0.01f, 5f, DefaultValue = 0.5f, Id = "SnowfoxSelfRepairEnergy",
			Step = 0.05f, Format = "{0:F2}",
			Tooltip = "Units of energy consumed by the Snowfox Self-Repair Module (and variants) per second while active"), OnChange(nameof(OnSliderChange))]
		public float HoverbikeSelfRepairEnergyConsumption = 0.5f; // Energy consumed per second when Self Repair is active
		[Slider("Snowfox Self-Repair disable threshold", 1f, 50f, DefaultValue = 10f, Id = "SnowfoxSelfRepairDisableThreshold",
			Step = 1f, Format = "{0:F0}",
			Tooltip = "The Snowfox Self-Repair Module will disable itself if battery power drops below this percentage"), OnChange(nameof(OnSliderChange))]
		public float HoverbikeSelfRepairDisableThreshold = 0.1f; // If battery power is lower than this fraction, disable Self Repair.

		[Slider("Snowfox Water Travel water dampening", 0.5f, 2f, DefaultValue = 1f, Id = "SnowfoxWaterModuleDampening",
			Step = 0.1f, Format = "{0:F1}",
			Tooltip = "A Snowfox with a Water Travel Module (or variants) travelling over water has its movement characteristics divided by this value when travelling over water. UWE default is 10f.\nDon't set it below 1f unless you want to make the Snowfox *more* manoeuvrable over water than over land."), OnChange(nameof(OnSliderChange))]
		public float SnowfoxWaterModuleDampening = 1f; // Movement is divided by this value when travelling over water. UWE default is 10f.
													   // Don't set it below 1f, as that makes the Snowfox *more* manoeuvrable over water than over land.
		[Slider("Snowfox Water Travel water offset", 0.5f, 2f, DefaultValue = 1f, Id = "SnowfoxWaterModuleOffset",
			Step = 0.1f, Format = "{0:F1}",
			Tooltip = "A Snowfox with a Water Travel Module (or variants) travelling over water hovers this many metres above water."), OnChange(nameof(OnSliderChange))]
		public float SnowfoxWaterModuleOffset = 1f; // The default value for ground travel is 2m.

		[Slider("Snowfox Solar Module efficiency", 0.2f, 1f, DefaultValue = 0.5f, Id = "SnowfoxSolarEfficiency",
			Step = 0.05f, Format = "{0:F2}",
			Tooltip = "Maximum amount of energy regenerated by the Snowfox Solar Module per second\nMovement drains 0.06666... units per second, assuming no efficiency module."), OnChange(nameof(OnSliderChange))]
		public float SnowfoxSolarMultiplier = 0.5f; // Multiplier applied to the local light amount to get amount of power regained from solar charger

		[Slider("Snowfox upgraded boost duration", 5f, 20f, DefaultValue = 6f, Id = "SnowfoxBoostDuration",
			Step = 0.5f, Format = "{0:F1}",
			Tooltip = "Maximum length of a boost if Snowfox is upgraded with the Snowfox Boost Upgrade"), OnChange(nameof(OnSliderChange))]
		public float SnowfoxBoostDuration = 6f;

		[Slider("Snowfox boost normal cooldown rate", 0.5f, 300f, DefaultValue = 150f, Id = "SnowfoxCooldownRatePct",
			Step = 0.5f, Format = "{0:F1}",
			Tooltip = "Rate of cooling on the upgraded Snowfox boost, as a percentage of time passed. (100 = 100% of time passed is applied to cooling)\nA value of 100 means that 12 seconds of boost requires 12 seconds without boost to cool down fully, a value of 200 means 12 seconds of boost requires 6 seconds of cooling."), OnChange(nameof(OnSliderChange))]
		public float SnowfoxCooldownRatePct = 150f;

		[Slider("Snowfox boost overheated cooldown rate", 0.5f, 300f, DefaultValue = 75f, Id = "SnowfoxCooldownRateOverheatPct",
			Step = 0.5f, Format = "{0:F1}",
			Tooltip = "Rate of cooling on the upgraded Snowfox boost if it was allowed to overheat, as a percentage of time passed.\nA value of 100 means that 12 seconds of boost requires 12 seconds without boost to cool down fully, a value of 200 means 12 seconds of boost requires 6 seconds of cooling."), OnChange(nameof(OnSliderChange))]
		public float SnowfoxCooldownRateOverheatPct = 75f;

		[Slider("Snowfox Durability Upgrade max field strength", 1f, 300f, DefaultValue = 100f, Id = "SnowfoxMaxShield",
			Step = 1f, Format = "{0:F0}",
			Tooltip = "Maximum amount of damage that can be absorbed by the Snowfox Durability Upgrade's integrity field from a full charge"), OnChange(nameof(OnSliderChange))]
		public float SnowfoxMaxShield = 200f;

		[Slider("Snowfox Durability Upgrade recharge delay", 1f, 10f, DefaultValue = 6f, Id = "SnowfoxShieldRechargeDelay",
			Step = 0.5f, Format = "{0:F1}",
			Tooltip = "Delay in seconds between the Snowfox taking damage and the Durability Upgrade's shield beginning to recharge."), OnChange(nameof(OnSliderChange))]
		public float SnowfoxShieldRechargeDelay = 6f;

		[Slider("Snowfox Durability Upgrade recharge rate", 1f, 150f, DefaultValue = 25f, Id = "SnowfoxShieldRechargeRate",
			Step = 1f, Format = "{0:F0}",
			Tooltip = "Amount of Durability Upgrade shield that recharges per second, as a percentage of its maximum. A value of 100 means it recharges fully in one second."), OnChange(nameof(OnSliderChange))]
		public float SnowfoxShieldRechargeRate = 25f;

		[Slider("Snowfox Durability Upgrade energy consumption rate", 1f, 150f, DefaultValue = 25f, Id = "SnowfoxShieldEnergyRate",
			Step = 1f, Format = "{0:F0}",
			Tooltip = "Amount of energy consumed per second by Durability Upgrade while recharging, as a percentage of the recharge rate. A value of 100 means that one unit of energy is consumed for every point of shield strength recharged."), OnChange(nameof(OnSliderChange))]
		public float SnowfoxShieldEnergyRate = 25f;

		[Slider("Drillable drill damage", DefaultValue = 5f, Format = "{0:F1}", Id = "DrillableDamage", Min = 5f, Max = 50f, Step = 0.1f, Tooltip = "Damage done to drillables by the Exosuit or Seatruck drill arms (UWE default 5)"), OnChange(nameof(OnSliderChange))]
		public float DrillableDamage = 5f;
#endif
		public delegate void OnConfigChange(DWConfig config, bool isEvent = false);
		public delegate void OnQuit(DWConfig config);

		public DWConfig() : base()
		{
			if (this.OnQuitEvent != null)
#if NAUTILUS
				SaveUtils.RegisterOnQuitEvent(this.OnQuitEvent);
#else
			
				IngameMenuHandler.RegisterOnQuitEvent(this.OnQuitEvent);
#endif
			if (string.IsNullOrEmpty(ChargeDifficulty))
				ChargeDifficulty = EASY;
			if (string.IsNullOrEmpty(SelfRepairDifficulty))
				SelfRepairDifficulty = EASY;
		}

		private void OnSliderChange(SliderChangedEventArgs e)
		{
			if (onOptionChanged != null)
				onOptionChanged.Invoke(this);

			if (e.Id == "DrillableDamage")
				DrillablePatches.ExosuitDrillDamage = e.Value;
		}

		/*
		private void OnChoiceChanged(ChoiceChangedEventArgs e)
		{
		}
		*/

		public void OnQuitEvent()
		{
			if (onQuit != null)
				onQuit.Invoke(this);

			onOptionChanged = null;
			onQuit = null;
		}
	}
}