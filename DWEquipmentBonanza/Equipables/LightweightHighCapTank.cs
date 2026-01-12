using Main = DWEquipmentBonanza.DWEBPlugin;
using System;
using System.Collections.Generic;
using System.Collections;
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
using UWE;
#if BEPINEX
	using BepInEx;
	using BepInEx.Logging;
#elif QMM
	using QModManager.API.ModLoading;
#endif
using Common;
using Common.Utility;
using DWEquipmentBonanza.Patches;

#if SN1
using FMODUnity;
//using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

#if LEGACY
using Oculus.Newtonsoft;
using Oculus.Newtonsoft.Json;
#else
using Newtonsoft;
using Newtonsoft.Json;
using DWEquipmentBonanza.MonoBehaviours;
using Nautilus.Assets.PrefabTemplates;
#endif

namespace DWEquipmentBonanza.Equipables
{
	public class PlasteelHighCapTank : Equipable
	{
#if NAUTILUS
		protected override TechType templateType => TechType.PlasteelTank;
		protected override string templateClassId => string.Empty;
#endif
		protected static Sprite icon;
		protected static GameObject prefab;
		private static float oxygenValue = -1f;

		public PlasteelHighCapTank() : base("PlasteelHighCapTank", "Plasteel Ultra Capacity Tank", "Lightweight tank with high oxygen capacity")
		{
			//Console.WriteLine($"{this.ClassID} constructing");
			OnFinishedPatching += () =>
			{
				Main.AddSubstitution(this.TechType, TechType.PlasteelTank);
				Main.AddSubstitution(this.TechType, TechType.HighCapacityTank);
				Main.AddModTechType(this.TechType);
				Reflection.AddCompoundTech(this.TechType, new List<TechType>()
				{
					TechType.PlasteelTank,
					TechType.HighCapacityTank
				});
				CoroutineHost.StartCoroutine(PostPatchSetup());

				UnderwaterMotorPatches.AddSpeedModifier(this.TechType, -0.10625f);
				Main.AddCustomOxyExclusion(this.TechType, false, true);
				Main.AddCustomOxyTank(this.TechType, -1f, this.GetItemSprite());
			};
		}

		public override EquipmentType EquipmentType => EquipmentType.Tank;

		public override Vector2int SizeInInventory => new Vector2int(3, 4);

		public override QuickSlotType QuickSlotType => QuickSlotType.None;

		public override bool UnlockedAtStart => false;

		public override TechCategory CategoryForPDA => TechCategory.Equipment;

		public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;

		public override string[] StepsToFabricatorTab => new string[] { DWConstants.TankMenuPath };

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.HighCapacityTank, 1),
						new Ingredient(TechType.PlasteelTank, 1),
						new Ingredient(TechType.Lubricant, 2),
						new Ingredient(TechType.WiringKit, 1)
					}
				)
			};
		}

		protected virtual IEnumerator PostPatchSetup()
		{
			while (icon == null)
			{
				icon ??= SpriteUtils.Get(TechType.HighCapacityTank, null);

				yield return new WaitForSecondsRealtime(0.5f);
			}

			CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HighCapacityTank);
			yield return task;
		}
		protected override Sprite GetItemSprite()
		{
			return icon ??= SpriteUtils.Get(TechType.HighCapacityTank, null);
		}

#if NAUTILUS
		public override void ModifyClone(CloneTemplate clone)
		{
			clone.ModifyPrefabAsync += SetUpPrefabsCoroutine;
		}

		public IEnumerator SetUpPrefabsCoroutine(GameObject prefabToModify)
		{
			if (prefabToModify == null)
			{
				//Log.LogError($"FlashlightHelmet.PreparePrefab called with null prefab!");
				yield break;
			}

			Oxygen oxygen = prefabToModify.GetComponent<Oxygen>();

			if (oxygenValue == -1f)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HighCapacityTank);
				yield return task;

				var HighCapOxygen = task.GetResult().GetComponent<Oxygen>();
				oxygenValue = HighCapOxygen != null ? HighCapOxygen.oxygenCapacity : -1f;
			}

			if (oxygenValue != -1f)
			{
				oxygen.oxygenCapacity = oxygenValue;
			}
		}

#else
		private GameObject PreparePrefab(GameObject prefab)
		{
			GameObject go = GameObject.Instantiate(prefab);
			//ModPrefabCache.AddPrefab(go, false);
			return go;
		}

#if ASYNC
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				Log.LogDebug($"LightweightHighCapTank.GetGameObjectAsync: getting HighCapacityTank prefab");
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HighCapacityTank, verbose: true);
				yield return task;

				prefab = PreparePrefab(task.GetResult());
			}

			float oxyCap = prefab.GetComponent<Oxygen>().oxygenCapacity;
			Log.LogDebug($"GameObject created with oxygenCapacity of {oxyCap}");
			gameObject.Set(prefab);
		}
#else
		public override GameObject GetGameObject()
		{
			if (prefab == null)
			{
				prefab = PreparePrefab(CraftData.GetPrefabForTechType(TechType.HighCapacityTank));
			}

			return prefab;
		}
#endif
#endif
	}
}
