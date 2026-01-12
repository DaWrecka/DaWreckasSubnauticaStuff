using Main = DWEquipmentBonanza.DWEBPlugin;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
using UnityEngine;
using System.Collections;
using DWEquipmentBonanza.Patches;
using DWEquipmentBonanza.MonoBehaviours;
using Common;
using System;
using Nautilus.Assets.PrefabTemplates;

#if SN1
using FMOD.Studio;
//using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza
{

	internal class Vibroblade : Equipable
	{
		//protected static GameObject prefab;
		protected static GameObject hbPrefab;

		public Vibroblade(string classId = "Vibroblade", string friendlyName = "Vibroblade", string description = "Hardened survival blade with high-frequency oscillator inflicts horrific damage with even glancing blows") : base(classId, friendlyName, description)
		{
			Log.LogDebug($"{this.ClassID} constructed");
#if !NAUTILUS
			OnFinishedPatching += () =>
			{
				var diamondBlade = new RecipeData()
				{
					craftAmount = 1,
					Ingredients = new List<Ingredient>()
					{
						new Ingredient(TechType.Knife, 1),
						new Ingredient(TechType.Diamond, 1)
					}
				};

	#if SN1	
				CraftDataHandler.SetTechData(TechType.DiamondBlade, diamondBlade);
				CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.DiamondBlade, new string[] { DWConstants.KnifeMenuPath });
	#endif
				Main.AddModTechType(this.TechType);
				PlayerPatch.AddSubstitution(this.TechType, TechType.Knife);
			};
#endif
		}

#if NAUTILUS
	#if SN1
		protected override TechType templateType => TechType.DiamondBlade;
	#else
		protected override TechType templateType => TechType.Knife;
	#endif
		protected override string templateClassId => null;
#endif
		public override EquipmentType EquipmentType => EquipmentType.Hand;
		public override Vector2int SizeInInventory => new Vector2int(1, 1);
		public override TechType RequiredForUnlock => TechType.Diamond;
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Tools;
		public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
		public override string[] StepsToFabricatorTab => new string[] { DWConstants.KnifeMenuPath };
		public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
		public override float CraftingTime => base.CraftingTime*1.5f;

		private static GameObject SetupPrefab(GameObject activePrefab)
		{
#if NAUTILUS
			var obj = activePrefab;
#else
			var obj = GameObject.Instantiate(activePrefab);
#endif
			if (obj == null)
			{
				return null;
			}

			Knife knife = obj.GetComponent<Knife>();

			VibrobladeBehaviour blade = obj.EnsureComponent<VibrobladeBehaviour>();
			if (blade != null)
			{
				if (hbPrefab != null)
				{
					HeatBlade hb = hbPrefab.GetComponent<HeatBlade>();
					blade.fxControl = hb.fxControl;
					blade.vfxEventType = hb.vfxEventType;
				}
				if (knife != null)
				{
#if SN1
					blade.attackSound = knife.attackSound;
					blade.underwaterMissSound = knife.underwaterMissSound;
					blade.surfaceMissSound = knife.surfaceMissSound;
#endif
					blade.mainCollider = knife.mainCollider;
					blade.drawSound = knife.drawSound;
					blade.firstUseSound = knife.firstUseSound;
					blade.hitBleederSound = knife.hitBleederSound;
					if (hbPrefab == null)
						blade.vfxEventType = knife.vfxEventType;
					GameObject.DestroyImmediate(knife);
				}
				blade.attackDist = 2f;
				blade.damageType = DamageType.Normal;
				blade.socket = PlayerTool.Socket.RightHand;
				blade.ikAimRightArm = true;
#if BELOWZERO
				blade.bleederDamage = 90f;
#endif
			}
			else
			{
#if !RELEASE
				//Log.LogDebug($"Could not ensure VibrobladeBehaviour component in Vibroblade prefab");
#endif
			}

#if !NAUTILUS
			ModPrefabCache.AddPrefab(obj, false);
#endif
			return obj;
		}

#if NAUTILUS
		public override void ModifyClone(CloneTemplate clone)
		{
			clone.ModifyPrefabAsync += ModPrefabAsync;
		}

		public IEnumerator ModPrefabAsync(GameObject gameObject)
		{
			if (hbPrefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HeatBlade);
				yield return task;
				hbPrefab = task.GetResult();

			}

			gameObject = SetupPrefab(gameObject);
			PlayerPatch.AddSubstitution(this.TechType, TechType.Knife);
		}

#elif ASYNC
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HeatBlade);
				yield return task;
				hbPrefab = task.GetResult();

				task = CraftData.GetPrefabForTechTypeAsync(TechType.Knife);
				yield return task;

				prefab = SetupPrefab(task.GetResult());
			}
			gameObject.Set(prefab);
		}

#else
		public override GameObject GetGameObject()
		{
			if (prefab == null)
			{
				GameObject dbPrefab = CraftData.GetPrefabForTechType(TechType.DiamondBlade);
				hbPrefab = CraftData.GetPrefabForTechType(TechType.HeatBlade);

				prefab = SetupPrefab(dbPrefab);
			}

			return prefab;
		}
#endif

		protected override RecipeData GetBlueprintRecipe()
		{
			RecipeData recipe = new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
				{
					new Ingredient(TechType.Knife, 1),
					new Ingredient(TechType.Diamond, 2),
					new Ingredient(TechType.Battery, 1),
					new Ingredient(TechType.Quartz, 1),
					new Ingredient(TechType.Aerogel, 1),
					new Ingredient(TechType.Magnetite, 1),
					new Ingredient(TechType.WiringKit, 1)
				})
			};

			return recipe;
		}

		protected override Sprite GetItemSprite()
		{
			return ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}Icon.png");
		}
	}
}
