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
#endif
using UnityEngine;
using System;
#if SN1
//using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza.VehicleModules
{
	internal class ExosuitGrappleUpgradeModule : Equipable
	{
#if NAUTILUS
		protected override TechType templateType => TechType.ExosuitThermalReactorModule;
		protected override string templateClassId => string.Empty;
#endif
		public override EquipmentType EquipmentType => EquipmentType.ExosuitModule;
		public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
		public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
		public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
		public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
		public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
		public override string[] StepsToFabricatorTab => new string[] { "ExosuitModules" };
		public override float CraftingTime => 10f;
		public override Vector2int SizeInInventory => new Vector2int(1, 2);

		//private static GameObject prefab;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.Polyaniline, 1),
						new Ingredient(TechType.WiringKit, 1),
						new Ingredient(TechType.Lithium, 1),
						new Ingredient(TechType.AramidFibers, 4)
					}
				)
			};
		}

		protected GameObject PreparePrefab(GameObject prefab)
		{
			GameObject obj = GameObject.Instantiate<GameObject>(prefab);

			// Editing prefab

			// Finalise prefab
			return obj;
		}

		protected override Sprite GetItemSprite()
		{
			return SpriteManager.Get(TechType.ExosuitGrapplingArmModule);
		}

		public ExosuitGrappleUpgradeModule() : base("ExosuitGrappleUpgradeModule", "Exosuit Grapple Upgrade Module", "Upgrades all equipped Grappling Arms, improving hook speed and pull speed")
		{
			//Console.WriteLine($"{this.ClassID} constructing");
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
			};
		}
	}
}
