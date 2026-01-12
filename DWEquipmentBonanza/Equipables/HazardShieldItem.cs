using Nautilus.Crafting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nautilus.Assets.PrefabTemplates;
using System.Collections;
using DWEquipmentBonanza.MonoBehaviours;
using DWCommon.MonoBehaviours;




#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Handlers;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
	#if SN1
	//using Ingredient = CraftData\.Ingredient;
	#endif
#else
using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using RecipeData = SMLHelper.V2.Crafting.RecipeData
#endif
#if SN1
//using Sprite = Atlas.Sprite;

#endif


namespace DWEquipmentBonanza.Equipables
{
	internal class HazardShieldItem : Equipable
	{
		public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
		public override EquipmentType EquipmentType => EquipmentType.Chip;
		public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
		public override string[] StepsToFabricatorTab => new string[] { "Personal", DWConstants.ChipsMenuPath };
		public override TechType RequiredForUnlock => TechType.AdvancedWiringKit;

		protected override string templateClassId => String.Empty;
		protected override TechType templateType => TechType.Compass;
		private Sprite sprite;

		protected override Sprite GetItemSprite()
		{
			if (sprite == null)
			{
#if SN1
				sprite = SpriteManager.Get(TechType.CyclopsShieldModule);
#elif BELOWZERO		  
				sprite = SpriteManager.Get(TechType.CyclopsShieldModule, null);
#endif
			}

			return sprite;
		}


		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>
				{
					new Ingredient(TechType.Magnetite, 2),
					new Ingredient(TechType.AdvancedWiringKit, 1),
					new Ingredient(TechType.Polyaniline, 1),
#if SN1
					new Ingredient(TechType.PrecursorIonCrystal, 1),
#else
					new Ingredient(TechType.PrecursorIonBattery, 1),
#endif
				}
			};
		}

		public override void ModPrefab(GameObject prefab)
		{
			/*var storageObject = prefab.FindChild("StorageRoot") ?? new GameObject("StorageRoot", new Type[] { typeof(ChildObjectIdentifier) });
			storageObject.transform.SetParent(prefab.transform);
			var energyMixin = prefab.EnsureComponent<EnergyMixin>();
			energyMixin.storageRoot ??= storageObject.EnsureComponent<ChildObjectIdentifier>();
			energyMixin.defaultBattery = TechType.PrecursorIonBattery;
			energyMixin.compatibleBatteries = new List<TechType>() { TechType.PrecursorIonBattery };
			energyMixin.allowBatteryReplacement = false;*/
			prefab.EnsureComponent<HazardShieldComponent>().Initialise(this.TechType);
			prefab.EnsureComponent<RegeneratingPowerSource>().SetRegenParameters(0.5f, 0.5f);
		}

		public HazardShieldItem() : base("HazardShieldItem", "Hazard Shield", "Protects the user from hazardous environmental conditions, such as acid or extreme heat.")
		{
			OnFinishedPatching += () => {
				BatteryCharger.compatibleTech.Add(this.TechType);
				RegeneratingPowerSource.StaticSetRegenParameters(0.5f, 0.5f, this.TechType);
			};
		}
	}
}
