using DWEquipmentBonanza.Patches;
using SMLHelper.V2.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using UWE;
using SMLHelper.V2.Crafting;

#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
using Oculus.Newtonsoft;
using Oculus.Newtonsoft.Json;
#elif BELOWZERO
using Newtonsoft;
using Newtonsoft.Json;
#endif
using Common.Utility;

namespace DWEquipmentBonanza.Equipables
{
    public class DWUltraGlideSwimChargeFins : Equipable
    {
		protected static GameObject prefab;
		protected static Sprite icon;
		protected static GameObject swimChargePrefab;
		internal static string friendlyName => "Ultra Glide Swim Charge Fins";
		internal static string description => "Ultra Glide Fins with the additional tool-charging circuits of the Swim Charge Fins";

		public DWUltraGlideSwimChargeFins() : base("DWUltraGlideSwimChargeFins", friendlyName, description)
		{
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				EquipmentPatch.AddSubstitution(this.TechType, TechType.SwimChargeFins);
				UnderwaterMotorPatches.AddSpeedModifier(this.TechType, 3f);
				Reflection.AddCompoundTech(this.TechType, new List<TechType>()
				{
					TechType.SwimChargeFins,
					TechType.UltraGlideFins
				});
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		new public bool UnlockedAtStart => false;

		public override EquipmentType EquipmentType => EquipmentType.Foots;
		public override Vector2int SizeInInventory => new Vector2int(2, 2);
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
			if (icon == null)
			{
				icon = Common.Utility.SpriteUtils.GetSpriteWithNoDefault(TechType.SwimChargeFins);
			}

			return icon;
        }

        private IEnumerator PostPatchSetup()
		{
#if SUBNAUTICA_STABLE
			while (icon == null)
			{
				icon = SpriteManager.GetWithNoDefault(TechType.SwimChargeFins);
				yield return new WaitForEndOfFrame();
			}
#elif BELOWZERO
			while (!SpriteManager.hasInitialized)
				yield return new WaitForEndOfFrame();
			icon = SpriteManager.Get(TechType.SwimChargeFins);
#endif

			yield break;
		}

#if SUBNAUTICA_STABLE
		public override GameObject GetGameObject()
        {
			if (prefab == null)
			{
				prefab = PreparePrefab(CraftData.GetPrefabForTechType(TechType.UltraGlideFins));
			}
            return prefab;
        }
#endif
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

		public GameObject PreparePrefab(GameObject upperPrefab)
		{
			GameObject go = GameObject.Instantiate<GameObject>(upperPrefab);
			go.EnsureComponent<UpdateSwimCharge>();

			ModPrefabCache.AddPrefab(go, false);
			return go;
		}
    }
}
