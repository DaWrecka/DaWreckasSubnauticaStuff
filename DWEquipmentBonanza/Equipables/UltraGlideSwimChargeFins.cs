using Main = DWEquipmentBonanza.DWEBPlugin;
using DWEquipmentBonanza.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using UWE;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Crafting;
using Nautilus.Utility;
using Nautilus.Handlers;
#if SN1
	//using Ingredient = CraftData\.Ingredient;
#endif
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#else
using RecipeData = SMLHelper.V2.Crafting.TechData;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using SMLHelper.V2.Handlers;
#endif

#if LEGACY
//using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
using Oculus.Newtonsoft;
using Oculus.Newtonsoft.Json;
#else
using Newtonsoft;
using Newtonsoft.Json;
#endif

#if SN1
//using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif
using Common.Utility;

namespace DWEquipmentBonanza.Equipables
{
	public class DWUltraGlideSwimChargeFins : Equipable
	{
		protected static GameObject prefab;
		protected static Sprite icon;
		protected static GameObject swimChargePrefab;
		private static string friendlyName => "Ultra Glide Swim Charge Fins";
		private static string description => "Ultra Glide Fins with the additional tool-charging circuits of the Swim Charge Fins";
		private const float speedModifier = 3f;

#if NAUTILUS
		protected override TechType templateType => TechType.UltraGlideFins;
		protected override string templateClassId => string.Empty;
#endif

		public DWUltraGlideSwimChargeFins() : base("DWUltraGlideSwimChargeFins", friendlyName, description)
		{
			//Console.WriteLine($"{this.ClassID} constructing");
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				EquipmentPatches.AddSubstitution(this.TechType, TechType.SwimChargeFins);
				UnderwaterMotorPatches.AddSpeedModifier(this.TechType, speedModifier);
				Reflection.AddCompoundTech(this.TechType, new List<TechType>()
				{
					TechType.SwimChargeFins,
					TechType.UltraGlideFins
				});
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		public override bool UnlockedAtStart => false;
		public override EquipmentType EquipmentType => EquipmentType.Foots;
		public override Vector2int SizeInInventory => new(2, 2);
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Equipment;
		public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
		public override string[] StepsToFabricatorTab => new string[] { DWConstants.FinsMenuPath };
		public override float CraftingTime => 5f;
		public override QuickSlotType QuickSlotType => QuickSlotType.None;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>()
				{
					new Ingredient(TechType.SwimChargeFins, 1),
					new Ingredient(TechType.UltraGlideFins, 1),
					new Ingredient(TechType.Lubricant, 2),
					new Ingredient(TechType.HydrochloricAcid, 1)
				}
			};
		}

		protected override Sprite GetItemSprite()
		{
			return icon ??= SpriteUtils.Get(TechType.SwimChargeFins, null);
		}

		private IEnumerator PostPatchSetup()
		{
/*#if SN1
			while (icon == null)
			{
				icon = SpriteManager.GetWithNoDefault(TechType.SwimChargeFins);
				yield return new WaitForEndOfFrame();
			}
#elif BELOWZERO*/
			while (!SpriteManager.hasInitialized)
				yield return new WaitForEndOfFrame();
			icon = SpriteManager.Get(TechType.SwimChargeFins);
//#endif

			yield break;
		}

		public GameObject PreparePrefab(GameObject upperPrefab)
		{
			GameObject go = GameObject.Instantiate<GameObject>(upperPrefab);
			go.EnsureComponent<UpdateSwimCharge>();

#if !NAUTILUS
			ModPrefabCache.AddPrefab(go, false);
#endif
			return go;
		}
#if NAUTILUS
#elif ASYNC
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.UltraGlideFins);
				yield return task;

				prefab = PreparePrefab(task.GetResult());
			}

			gameObject.Set(prefab);
		}

#else
		public override GameObject GetGameObject()
		{
			if (prefab == null)
			{
				prefab = PreparePrefab(CraftData.GetPrefabForTechType(TechType.UltraGlideFins));
			}
			return prefab;
		}

#endif

	}
}
