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

namespace DWEquipmentBonanza.Spawnables
{
	internal class FlashlightBatteryComponent : MonoBehaviour,
		IBattery
	{
		public float charge
		{
			get
			{
				return 1f;
			}
			set { }
		}
		internal TechType techType { get; }
		public float capacity => 1f;

		internal virtual void Initialise(TechType newTechType) { }

		public string GetChargeValueText()
		{
			// The StringBuilder method in which this value is used adds a LF character before adding the result of this method to the StringBuilder. For this reason we pass a Backspace character, \b - let's see if it works.
			return "\b";
		}
	}

	internal class FlashlightBattery : Equipable
	{
#if NAUTILUS
		protected override TechType templateType => TechType.PrecursorIonBattery;
		protected override string templateClassId => string.Empty;
#endif
		public override EquipmentType EquipmentType => EquipmentType.BatteryCharger;
		public override QuickSlotType QuickSlotType => QuickSlotType.None;
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Electronics;
		public override TechType RequiredForUnlock => TechType.BatteryAcidOld;
		public override CraftTree.Type FabricatorType => CraftTree.Type.None;
		public override string[] StepsToFabricatorTab => new string[] { "" };
		public override float CraftingTime => 10f;
		public override Vector2int SizeInInventory => new Vector2int(1, 1);

		//private static GameObject prefab;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData();
		}

		protected GameObject PreparePrefab(GameObject prefab)
		{
			GameObject obj = GameObject.Instantiate<GameObject>(prefab);

			// Editing prefab

			if (prefab.TryGetComponent<Battery>(out Battery B))
				GameObject.DestroyImmediate(B);
			prefab.EnsureComponent<FlashlightBatteryComponent>();


			// Finalise prefab
#if NAUTILUS
#else
			ModPrefabCache.AddPrefab(obj, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
													 // but it can still be instantiated. [unlike with SetActive(false)]
#endif
			return obj;
		}

		protected override Sprite GetItemSprite()
		{
#if SN1
			return SpriteManager.Get(TechType.SeamothElectricalDefense);
#elif BELOWZERO
			return SpriteManager.Get(TechType.SeaTruckUpgradePerimeterDefense);
#endif
		}

		public FlashlightBattery() : base("FlashlightBattery", "FlashlightBattery", "How did you get this?")
		{
			//Console.WriteLine($"{this.ClassID} constructing");
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
			};
		}
	}
}
