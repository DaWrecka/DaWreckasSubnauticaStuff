using CombinedItems.MonoBehaviours;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using UnityEngine;
using UWE;
using Logger = QModManager.Utility.Logger;
using FMODUnity;
using CombinedItems.Patches;

namespace CombinedItems.Equipables
{
	class PowerglideEquipable: Equipable
	{
		private static GameObject prefab;
		private static Sprite icon;
		private const float PowerglideColourR = 0f;
		private const float PowerglideColourG = 2f;
		private const float PowerglideColourB = 0f;

		public PowerglideEquipable() : base("PowerglideEquipable", "Powerglide", $"Hold {GameInput.GetBinding(GameInput.Device.Keyboard, GameInput.Button.Sprint, GameInput.BindingSet.Primary)} for dramatic speed bonus with increased energy consumption")
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				PlayerToolPatches.AddToolSubstitution(this.TechType, TechType.Seaglide);
			};
		}

		new public bool UnlockedAtStart => false;

		public override EquipmentType EquipmentType => EquipmentType.Hand;
		public override Vector2int SizeInInventory => new Vector2int(2, 3);
		public override TechGroup GroupForPDA => TechGroup.Machines;
		public override TechCategory CategoryForPDA => TechCategory.Machines;
		public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
		public override string[] StepsToFabricatorTab => new string[] { "Machines" };
		public override float CraftingTime => 5f;
		public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
		//public override TechType RequiredForUnlock => Main.powerglideFrag.TechType;
		public override TechType RequiredForUnlock => Main.GetModTechType("PowerglideFragment");
		public override string DiscoverMessage => $"{this.FriendlyName} Unlocked!";
		public override bool AddScannerEntry => true;
		public override int FragmentsToScan => 4;
		public override float TimeToScanFragment => 5f;
		public override bool DestroyFragmentOnScan => true;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.WiringKit, 1),
						new Ingredient(TechType.CopperWire, 2),
						new Ingredient(TechType.Lubricant, 1),
						new Ingredient(TechType.PlasteelIngot, 1),
						new Ingredient(TechType.Polyaniline, 1),
						new Ingredient(TechType.Nickel, 1)
					}
				)
			};
		}

		protected override Sprite GetItemSprite()
		{
			if (icon == null || icon == SpriteManager.defaultSprite)
			{
				icon = SpriteManager.Get(TechType.Seaglide);
			}
			return icon;
		}

		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.Seaglide, true);
				yield return task;

				prefab = GameObject.Instantiate(task.GetResult());
				prefab.EnsureComponent<PowerglideBehaviour>();
				prefab.SetActive(false);

				/*
				MeshRenderer[] meshRenderers = prefab.GetAllComponentsInChildren<MeshRenderer>();
				SkinnedMeshRenderer[] skinnedMeshRenderers = prefab.GetAllComponentsInChildren<SkinnedMeshRenderer>();
				Color powerGlideColour = new Color(PowerglideColourR, PowerglideColourG, PowerglideColourB);

				foreach (MeshRenderer mr in meshRenderers)
				{
					// MeshRenderers have the third-person mesh, apparently?
					if (mr.name.Contains("SeaGlide_01_TP"))
					{
						mr.material.color = powerGlideColour;
					}
				}

				foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
				{
					if (smr.name.Contains("SeaGlide_Geo"))
					{
						smr.material.color = powerGlideColour;
					}
				}
				*/
				prefab.SetActive(true);
			}

			GameObject go = GameObject.Instantiate(prefab);
			go.EnsureComponent<PowerglideBehaviour>();
			gameObject.Set(go);
		}
	}
}
