using System.Collections.Generic;
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

namespace FuelCells
{
	[Menu("Fuel Cells")]
	public class DWConfig : ConfigFile
	{
		private const int defaultCapacity = 400;
		private const int minCapacity = 200;
		private const int maxCapacity = 800;
		private const float defaultMultiplier = 2.25f;
		private const float minMultiplier = 1.5f;
		private const float maxMultiplier = 3f;

		// Declared as floats because the Battery class uses floats, so using floats here is the most straightforward way to avoid rounding errors.
		[Slider("Base capacity of Small Fuel Cell", minCapacity, maxCapacity, DefaultValue = defaultCapacity, Id = "batteryCap", Step = 50, Tooltip = "Capacity of the Small Fuel Cell. The capacity of the full-sized Fuel Cell is a multiple of this value.\nGame must be restarted for changes to this setting to take effect."), OnChange(nameof(OnSliderChange))]
		public float smallFuelCellCap = 400f;
		[Slider("Large Fuel Cell capacity multiplier", minMultiplier, maxMultiplier, DefaultValue = defaultMultiplier, Id = "batteryMult", Step = 0.05f, Format = "{0:F2}", Tooltip = "The capacity of the large Fuel Cell is equal to the Small Fuel Cell capacity, above, multiplied by this value.\nGame must be restarted for changes to this setting to take effect."), OnChange(nameof(OnSliderChange))]
		public float cellMultiplier = 2.25f;
		internal float cellCap => smallFuelCellCap * cellMultiplier;

		public Dictionary<string, float> BatteryValues = new Dictionary<string, float>()
		{
#if BATTERYPATCHING
			{ "Battery", 100f },
			{ "PowerCell", 200f },
#endif
			{ "LithiumIonBattery", 200f },
#if BATTERYPATCHING
			{ "PrecursorIonBattery", 500f },
			{ "PrecursorIonPowerCell", 1000f }
#endif
		};

		private void OnSliderChange(SliderChangedEventArgs e)
		{
			//if (e.Id == "batteryCap")
			//{
			//	OnLoad();
			//}
		}

		internal void OnLoad()
		{
		}
	}
}