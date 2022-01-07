using System.Collections.Generic;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;

namespace PowerOverYourPower
{
    //[Menu("Power Over Your Power")]
	public class DWConfig : ConfigFile
	{
		//public float BatteryValue = 100f;
		//public float CellMultiplier = 2f;
		//public float IonMultiplier = 5f;

		public Dictionary<string, float> BatteryValues = new Dictionary<string, float>()
		{
			{ "Battery", 100f },
			{ "PowerCell", 200f },
			//{ "LithiumIonBattery", 200f },
			{ "PrecursorIonBattery", 500f },
			{ "PrecursorIonPowerCell", 1000f }
		};
	}
}