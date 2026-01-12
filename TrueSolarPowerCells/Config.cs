#if NAUTILUS
using Nautilus.Json;
using Nautilus.Options.Attributes;
#else
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Handlers;
#endif

namespace TrueSolarPowerCells
{
	public class Config : ConfigFile
	{
		[Slider("Cell capacity", 10, 75, DefaultValue = 25, Id = "regenerationThreshold",
			Step = 5,
			Tooltip = "Capacity of each solar cell.")]
		public float regenerationThreshold = 25f;

		[Slider("Solar regeneration multiplier", 0.1f, 5f, DefaultValue = 1f, Id = "regenerationRate",
			Step = 0.1f, Format = "{0:F1}",
			Tooltip = "Recharge multiplier; amount of solar power is multiplied by this value before it is added to solar cells. Values below 1 will make the cells charge slower than normal, values above 1 will make the cells charge faster.")]
		public float regenerationRate = 1f;

		[Slider("Constant regeneration amount", Min = 0f, Max = 5f, DefaultValue = 0f, Id = "baseRegenRate", Step = 0.1f, Format = "{0:F1}",
			Tooltip = "If non-zero, the solar cells will regenerate this much energy per second even at night")]
		public float baseRegenRate = 0f;
	}
}
