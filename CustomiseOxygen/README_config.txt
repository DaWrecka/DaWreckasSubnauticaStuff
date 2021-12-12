Explanations of settings found within the config.json:

Configurable from GUI
    "bAllowAutoRefill": Takes the values true or false. If true, oxygen tanks refill by themselves when within breathable atmosphere. If false, oxygen tanks must be refilled at the Fabricator, under a new tab in the Personal category. False is referred to as "Manual mode" in the mod config and the remainder of this document.

    "baseOxyMultiplier": Takes a floating-point value which must be enclosed in quotes, e.g. "1", "2.5", etc. All oxygen tanks without specific overrides (see CapacityOverrides, below) will have their default capacity multiplied by this number. For example, if this value is 2.0, High Capacity Tanks will contain 360 seconds of oxygen. This multiplier is applied to all tanks, unless that tank has a specific capacity set in the Capacity overrides tables.

    "refillableMultiplier": A floating-point number like baseOxyMultiplier. This is applied when bAllowAutoRefill is set to false. If baseOxyMultiplier is 1.0, a High Capacity Tank has a capacity of 180 by default; if refillableMultiplier is 3.0 then this becomes 540.
	
Note that both of these numeric values are ignored if the tank has a specific override configured in CapacityOverrides, below.

Only configurable from config.json
    "defaultTankCapacities": When a new tank is encountered, its TechType - the identifier that separates it from all other spawnable items in the game - is written to this table, along with the tank's default capacity. This is for user reference only - values in defaultTankCapacities are never used by the mod.
	"CapacityOverrides": This is where specific oxygen tanks can have their capacities overridden solidly. If a tank has a value in this table, both baseOxyMultiplier and refillableMultiplier are ignored, and the capacity specified in this table takes absolute preference. The DoubleTank, SuitBoosterTank, and PlasteelTank are encouraged to have equal capacities, but this is not required.
	"manualCapacityOverrides": as with CapacityOverrides, except these values are only used when the mod is set to Manual Refilling mode.
	
===========================================================
By way of example, imagine a config.json with these values:
{
    "bAllowAutoRefill": false,
    "baseOxyMultiplier": "2",
    "refillableMultiplier": "2.5",
    "manualCapacityOverrides": {
        "DoubleTank": "120",
        "SuitBoosterTank": "120",
        "PlasteelTank": "120"
    }
}

What this means is:
*) the DoubleTank, SuitBoosterTank, and PlasteelTank will all hold 120 seconds of oxygen. They must be refilled at Fabricators.
*) the standard Tank, normal capacity 30, will have its capacity multiplied by 2 (baseOxyMultiplier) and then multiplied again by 2.5. (refillableMultiplier) Its final capacity will therefore be 150 seconds of oxygen. It won't refill by itself, and will need to be refilled at the fabricator.
*) the HighCapacityTank, normal capacity 180, will be multiplied by 2 and 2.5, for a final value of 900. It won't refill by itself.

The TechType SuitBoosterTank is only valid in BelowZero, but including it in the config for the Subnautica version will cause no problems; those entries will simply be ignored.