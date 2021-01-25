using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Handlers;

namespace HabitatBuilderSpeed.Configuration
{
    [Menu("Builder Speed")]
    public class DWConfig : ConfigFile
    {
        private const float MAX_MULT = 5f;
        private const float MIN_MULT = 0.1f;
        private float _builderMultiplier = 1f; // Minimum number of ingredients received from scanning an existing fragment

        [Slider("Speed multiplier", MIN_MULT, MAX_MULT, DefaultValue = 1, Id = "SpeedMult", Step = 0.05f, Format = "{0:F2}"), OnChange(nameof(OnSliderChange))]
        public float builderMultiplier {
            get
            {
                return System.Math.Max(_builderMultiplier, MIN_MULT);
            }

            set
            {
                this._builderMultiplier = System.Math.Max(MIN_MULT, System.Math.Min(value, MAX_MULT));
            }
        }

        private void OnSliderChange(SliderChangedEventArgs e)
        {
        }

    }
}
