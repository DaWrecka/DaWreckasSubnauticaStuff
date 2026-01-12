using Main = DWEquipmentBonanza.DWEBPlugin;
using System.Collections.Generic;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Nautilus.Utility;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#if SN1
	//using Ingredient = CraftData\.Ingredient;
#endif
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
#endif
using DWEquipmentBonanza.MonoBehaviours;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
	internal class SeatruckThermalModule : SeatruckChargerModule<VehicleThermalChargerMk1>
	{
		protected override TechType templateType => TechType.SeaTruckUpgradeEnergyEfficiency;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.Sulphur, 2),
						new Ingredient(TechType.Polyaniline, 2),
						new Ingredient(TechType.WiringKit, 1)
					}
				)
			};
		}

		public SeatruckThermalModule() : base("SeatruckThermalModule", "SeaTruck Thermal Charger", "Recharges SeaTruck power cells in hot zones. Limited stacking ability.")
		{
		}
	}

	internal class SeatruckThermalModuleMk2 : SeatruckChargerModule<VehicleThermalChargerMk2>
	{
		protected override float ChargerWeight => 1.5f;
		protected override TechType templateType => TechType.SeaTruckUpgradeEnergyEfficiency;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(Main.GetModTechType("SeatruckThermalModule"), 1),
						new Ingredient(TechType.Kyanite, 2),
						new Ingredient(TechType.Battery, 2),
						new Ingredient(TechType.AdvancedWiringKit, 1)
					}
				)
			};
		}

		public SeatruckThermalModuleMk2() : base("SeatruckThermalModuleMk2", "SeaTruck Thermal Charger Mk2", "Recharges SeaTruck power cells in hot zones, and contains an internal backup battery. Limited stacking ability.")
		{
		}
	}
#endif
}
