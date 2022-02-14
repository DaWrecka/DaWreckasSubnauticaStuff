using System.Collections.Generic;
using Logger = QModManager.Utility.Logger;
using System.IO;
using System.Reflection;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Handlers;

#if SUBNAUTICA_STABLE
	using Oculus.Newtonsoft.Json;
	using Oculus.Newtonsoft.Json.Serialization;
	using Oculus.Newtonsoft.Json.Converters;
#elif BELOWZERO
	using Newtonsoft.Json;
	using Newtonsoft.Json.Serialization;
	using Newtonsoft.Json.Converters;
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

		[Slider("Knife damage", KNIFE_DAMAGE_MIN, KNIFE_DAMAGE_MAX, DefaultValue = KNIFE_DAMAGE_DEFAULT, Id = "KnifeDamage",
			Step = 0.1f, Format = "{0:F1}",
			Tooltip = "Base damage dealt by Survival Knife (UWE default: 20)\nNote that this will affect how many times a harvestable plant can be struck with a Knife before it is destroyed, and thus how many resources you will get.")]
		public float KnifeDamage = KNIFE_DAMAGE_DEFAULT;


		[Slider("Heatblade damage", HEATBLADE_DAMAGE_MIN, HEATBLADE_DAMAGE_MAX, DefaultValue = HEATBLADE_DAMAGE_DEFAULT, Id = "HeatbladeDamage",
			Step = 0.1f, Format = "{0:F1}",
			Tooltip = "Base damage dealt by Heatblade. (UWE default: 20.0)\nNote that this will affect how many times a harvestable plant can be struck with a HeatBlade before it is destroyed, and thus how many resources you will get.")]
		public float HeatbladeDamage = HEATBLADE_DAMAGE_DEFAULT;

#if BELOWZERO
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

#if SUBNAUTICA_STABLE
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

#endif
		public delegate void OnConfigChange(DWConfig config, bool isEvent = false);
		public delegate void OnQuit(DWConfig config);

		public DWConfig() : base()
		{
			IngameMenuHandler.RegisterOnQuitEvent(this.OnQuitEvent);
		}

		private void OnSliderChange(SliderChangedEventArgs e)
		{
			if (onOptionChanged != null)
				onOptionChanged.Invoke(this);
		}

		public void OnQuitEvent()
		{
			if (onQuit != null)
				onQuit.Invoke(this);

			onOptionChanged = null;
			onQuit = null;
		}
	}
}