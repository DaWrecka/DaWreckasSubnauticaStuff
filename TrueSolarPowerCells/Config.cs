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

		[Slider("Regeneration multiplier", 0.1f, 5f, DefaultValue = 1f, Id = "regenerationRate",
			Step = 0.05f, Format = "{0:F2}",
			Tooltip = "Recharge multiplier; amount of solar power is multiplied by this value before it is added to solar cells. Values below 1 will make the cells charge slower than normal, values above 1 will make the cells charge faster.")]
		public float regenerationRate = 1f;

	}
}
