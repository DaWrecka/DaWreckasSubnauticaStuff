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
        internal float _builderMultiplier = 1f;

        [Slider("Speed multiplier", MIN_MULT, MAX_MULT, DefaultValue = 1, Id = "SpeedMult", Step = 0.05f, Format = "{0:F2}",
            Tooltip = "Time required to construct/deconstruct is multiplied by this value. Values lower than 1 make the Habitat Builder faster, values above 1 make it slower.\r\nValues lower than 0.5 are allowed, but not recommended!"),
            OnChange(nameof(OnSliderChange))]
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
