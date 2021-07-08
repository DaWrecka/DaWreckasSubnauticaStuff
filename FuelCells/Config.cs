using System.Collections.Generic;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;

namespace FuelCells
{
    [Menu("Fuel Cells")]
	public class DWConfig : ConfigFile
	{
		private const int defaultCapacity = 400;
		private const int minCapacity = 200;
		private const int maxCapacity = 800;
		private const float cellMultiplier = 2.25f;

		// Declared as floats because the Battery class uses floats, so using floats here is the most straightforward way to avoid rounding errors.
		[Slider("Base capacity of Small Fuel Cell", minCapacity, maxCapacity, DefaultValue = defaultCapacity, Id = "batteryCap", Step = 50, Tooltip = "Capacity of the Small Fuel Cell. The capacity of the full-sized Fuel Cell is a multiple of this value.\nGame must be restarted for changes to this setting to take effect."), OnChange(nameof(OnSliderChange))]
		public float smallFuelCellCap = 400;
		internal float cellCap = 900;

		public Dictionary<string, float> BatteryValues = new Dictionary<string, float>()
		{
			{ "Battery", 150f },
			{ "PowerCell", 300f },
			{ "LithiumIonBattery", 300f },
			{ "PrecursorIonBattery", 800f },
			{ "PrecursorIonPowerCell", 1600f }
		};

		private void OnSliderChange(SliderChangedEventArgs e)
		{
			if (e.Id == "batteryCap")
			{
				OnLoad();
			}
		}

		internal void OnLoad()
		{
			cellCap = smallFuelCellCap * cellMultiplier;
		}
	}
}