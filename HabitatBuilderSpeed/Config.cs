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

namespace HabitatBuilderSpeed.Configuration
{
    [Menu("Builder Speed")]
    public class DWConfig : ConfigFile
    {
        private const float MAX_MULT = 5f;
        private const float MIN_MULT = 0.10f;
        internal float _builderMultiplier = 1f;

        [Slider("Speed multiplier", MIN_MULT, MAX_MULT, DefaultValue = 1f, Id = "SpeedMult", Step = 0.1f, Format = "{0:F1}",
            Tooltip = "Time required to construct/deconstruct is multiplied by this value. Values lower than 1 make the Habitat Builder faster, values above 1 make it slower.\r\nValues lower than 0.5 are allowed, but may result in losing resources on deconstruction, and thus are not recommended."),
            OnChange(nameof(OnSliderChange))]
        public float builderMultiplier {
            get => this._builderMultiplier;
            set
            {
                this._builderMultiplier = Mathf.Clamp(value, MIN_MULT, MAX_MULT);
            }
        }

        private void OnSliderChange(SliderChangedEventArgs e)
        {
            
        }

    }
}
