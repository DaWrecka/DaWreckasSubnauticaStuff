using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;

namespace HabitatBuilderSpeed.Configuration
{
    [Menu("Builder Speed")]
    public class DWConfig : ConfigFile
    {
        private const float MAX_MULT = 10f;
        private const float MIN_MULT = 0.1f;
        [Slider("Speed multiplier", MIN_MULT, MAX_MULT, DefaultValue = 1, Id = "SpeedMult"), OnChange(nameof(OnSliderChange))]
        public float builderMultiplier = 1f; // Minimum number of ingredients received from scanning an existing fragment

        private void OnSliderChange(SliderChangedEventArgs e)
        {
        }
    }
}
