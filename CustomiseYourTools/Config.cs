using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomiseYourTools
{
    public class Config : ConfigFile
    {
        [Slider(DefaultValue = 50f, Step = 0.5f, Format = "{0:F1}", Label = "Propulsion Cannon shoot force", Max = 200f, Min = 10f, Tooltip = "Force applied to objects fired from the handheld Propulsion Cannon")]
        public float PropCannonShootForce = 50f;

        [Slider(DefaultValue = 140f, Step = 0.5f, Format = "{0:F1}", Label = "Propulsion Cannon attract force", Max = 400f, Min = 35f, Tooltip = "Force applied to objects as they are pulled towards the handheld Propulsion Cannon muzzle")]
        public float PropCannonAttractionForce = 140f;

        [Slider(DefaultValue = 0.02f, Step = 0.002f, Format = "{0:F3}", Label = "Propulsion Cannon mass scaling factor", Max = 1f, Min = 0.001f, Tooltip = "Objects picked up by the handheld Propulsion Cannon have their mass multiplied by that value")]
        public float PropCannonMassScalingFactor = 0.02f;

        [Slider(DefaultValue = 18f, Step = 1f, Format = "{0:F0}", Label = "Propulsion Cannon pickup distance", Max = 72f, Min = 6f, Tooltip = "handheld Propulsion Cannon maximum pickup range, in metres")]
        public float PropCannonPickupDistance = 18f;

        [Slider(DefaultValue = 1200f, Step = 5f, Format = "{0:F0}", Label = "Propulsion Cannon max mass", Max = 4800f, Min = 300f, Tooltip = "Objects with a mass greater than this value cannot be picked up using the handheld Propulsion Cannon")]
        public float PropCannonMaxMass = 0.02f;

        [Slider(DefaultValue = 120f, Step = 1f, Format = "{0:F0}", Label = "Propulsion Cannon max AABB volume", Max = 480f, Min = 30f, Tooltip = "Unknown")]
        public float PropCannonMaxAABBVolume = 0.02f;

        // Welder
        [Slider(DefaultValue = 0.4f, Step = 0.05f, Format = "{0:F2}", Label = "Repair tool energy cost", Max = 2f, Min = 0.1f, Tooltip = "Repair tool energy cost per interval")]
        public float WelderEnergyCost = 0.4f;

        [Slider(DefaultValue = 10f, Step = 0.5f, Format = "{0:F1}", Label = "Repair tool health per weld", Max = 40f, Min = 1f, Tooltip = "Health restored by repair tool per interval")]
        public float WelderHealthPerWeld = 0.4f;

#if BELOWZERO
        // Thumper
        [Slider(DefaultValue = 0.32f, Step = 0.01f, Format = "{0:F2}", Label = "Thumper energy cost", Max = 1.5f, Min = 0.1f, Tooltip = "Thumper energy consumption in units per second")]
        public float ThumperEnergyPerSecond = 0.32f;

        [Slider(DefaultValue = 35f, Step = 0.5f, Format = "{0:F1}", Label = "Thumper effect radius", Max = 140f, Min = 10f, Tooltip = "Radius of Thumper effect, in metres")]
        public float ThumperEffectRadius = 35f;

        [Slider(DefaultValue = 2.5f, Step = 0.01f, Format = "{0:F2}", Label = "Thumper energy cost", Max = 10f, Min = 0.5f, Tooltip = "Interval between Thumper impulses")]
        public float ThumperImpulseInterval = 2.5f;
#endif

        // Laser cutter
        [Slider(DefaultValue = 1f, Step = 0.01f, Format = "{0:F2}", Label = "Laser cutter energy cost", Max = 4f, Min = 0.25f, Tooltip = "Laser cutter energy consumption")]
        public float LaserCutterEnergyCost = 1f;

        [Slider(DefaultValue = 25f, Step = 0.1f, Format = "{0:F1}", Label = "Laser cutter health per weld", Max = 4f, Min = 0.25f, Tooltip = "Laser cutter damage per usage interval")]
        public float LaserCutterHealthPerWeld = 1f;



        // Exosuit Prop Cannon
        [Slider(DefaultValue = 50f, Step = 0.5f, Format = "{0:F1}", Label = "Exosuit Prop Cannon shoot force", Max = 200f, Min = 10f, Tooltip = "Force applied to objects fired from the handheld Propulsion Cannon")]
        public float ExosuitPropCannonShootForce = 50f;

        [Slider(DefaultValue = 140f, Step = 0.5f, Format = "{0:F1}", Label = "Exosuit Prop Cannon attract force", Max = 400f, Min = 35f, Tooltip = "Force applied to objects as they are pulled towards the handheld Propulsion Cannon muzzle")]
        public float ExosuitPropCannonAttractionForce = 140f;

        [Slider(DefaultValue = 0.02f, Step = 0.002f, Format = "{0:F3}", Label = "Exosuit Prop Cannon mass scaling factor", Max = 1f, Min = 0.001f, Tooltip = "Objects picked up by the handheld Propulsion Cannon have their mass multiplied by that value")]
        public float ExosuitPropCannonMassScalingFactor = 0.02f;

        [Slider(DefaultValue = 18f, Step = 1f, Format = "{0:F0}", Label = "Exosuit Prop Cannon pickup distance", Max = 72f, Min = 6f, Tooltip = "handheld Propulsion Cannon maximum pickup range, in metres")]
        public float ExosuitPropCannonPickupDistance = 18f;

        [Slider(DefaultValue = 1200f, Step = 5f, Format = "{0:F0}", Label = "Exosuit Prop Cannon max mass", Max = 4800f, Min = 300f, Tooltip = "Objects with a mass greater than this value cannot be picked up using the handheld Propulsion Cannon")]
        public float ExosuitPropCannonMaxMass = 0.02f;

        [Slider(DefaultValue = 120f, Step = 1f, Format = "{0:F0}", Label = "Exosuit Prop Cannon max AABB volume", Max = 480f, Min = 30f, Tooltip = "Unknown")]
        public float ExosuitPropCannonMaxAABBVolume = 0.02f;

    }
}
