using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using System.Collections;
using System.Collections.Generic;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#if SN1
	//using Ingredient = CraftData\.Ingredient;
#endif
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
#endif
using UnityEngine;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
	internal class HoverbikeDurabilitySystem : HoverbikeUpgradeBase<HoverbikeDurabilitySystem>
	{
		public override float CraftingTime => 10f;
		protected override TechType spriteTemplate => TechType.VehicleArmorPlating; // Placeholder
		protected override TechType templateType => TechType.HoverbikeJumpModule;

		//private GameObject prefab;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(Main.GetModTechType("HoverbikeStructuralIntegrityModule"), 1),
						new Ingredient(Main.GetModTechType("HoverbikeSelfRepairModule"), 1),
						new Ingredient(TechType.PrecursorIonCrystal, 1),
						new Ingredient(TechType.Polyaniline, 1)
					}
				)
			};
		}

		public HoverbikeDurabilitySystem() : base("HoverbikeDurabilitySystem", "Hoverbike Durability System", "Energy field reduces incoming damage, and nanotech repair system passively repairs damage to Snowfox systems. Consumes energy while in use.")
		{
		}
	}
#endif
}
