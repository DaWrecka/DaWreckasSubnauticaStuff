using Main = DWEquipmentBonanza.DWEBPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Crafting;
using Nautilus.Utility;
using Nautilus.Handlers;
using Ingredient = CraftData.Ingredient;
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#else
using RecipeData = SMLHelper.V2.Crafting.TechData;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using SMLHelper.V2.Handlers;
#endif
using System.Collections;
using UWE;
using Common;
using DWEquipmentBonanza.Patches;
using DWEquipmentBonanza.MonoBehaviours;
using System.Diagnostics;
using Common.Utility;
#if SN1
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

#if LEGACY
using Oculus.Newtonsoft;
using Oculus.Newtonsoft.Json;
#else
using Newtonsoft;
using Newtonsoft.Json;
#endif

namespace DWEquipmentBonanza.Equipables
{
	public class DiverPerimeterDefenceChip_Broken : PdaItem
	{
		private static bool bWaiting;
#if NAUTILUS
        protected override TechType templateType => TechType.MapRoomHUDChip;
        protected override string templateClassId => string.Empty;
#endif
        public static Sprite icon { get; private set; }
		public static GameObject prefab { get; private set; }

		public DiverPerimeterDefenceChip_Broken() : base("DiverPerimeterDefenceChip_Broken", "Diver Perimeter System (damaged)", $"Protects a diver from hostile fauna using electrical discouragement.\n\nChip has been discharged and is non-functional.")
		{
			//Console.WriteLine($"{this.ClassID} constructing");
			OnFinishedPatching += () =>
			{
				Main.AddModTechType(this.TechType);
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		public override Vector2int SizeInInventory => new Vector2int(1, 1);
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Equipment;
		public override bool UnlockedAtStart => false;
		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData();
		}

		protected IEnumerator PostPatchSetup()
		{
			if (bWaiting)
				yield break;

			bWaiting = true;

			while (bWaiting)
			{
				if (icon == null || icon == SpriteManager.defaultSprite)
				{
					icon = SpriteManager.Get(templateType);
				}
				else
					bWaiting = false;

				yield return new WaitForSecondsRealtime(0.5f);
			}
		}

		protected override Sprite GetItemSprite()
		{
			if (icon == null || icon == SpriteManager.defaultSprite)
			{
				icon = SpriteManager.Get(templateType);
			}
			return icon;
		}

#if NAUTILUS
#elif ASYNC
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				var task = CraftData.GetPrefabForTechTypeAsync(TechType.MapRoomHUDChip);
				yield return task;

				prefab = GameObject.Instantiate(task.GetResult());
				ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
			}

			gameObject.Set(prefab);
		}

#else
		public override GameObject GetGameObject()
        {
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
			if (prefab == null)
				prefab = GameObject.Instantiate(CraftData.GetPrefabForTechType(TechType.MapRoomHUDChip));

			ModPrefabCache.AddPrefab(prefab);

			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
			return prefab;
        }
#endif
	}

	abstract public class DiverPerimeterDefenceChipItemBase<T> : Equipable
	{
		internal static Dictionary<TechType, int> MaxDischargeDict = new Dictionary<TechType, int>();
		public static Sprite icon { get; protected set; }
		public static GameObject brokenPrefab { get; protected set; }
		public static GameObject prefab { get; protected set; }

		private bool bWaiting;

#if NAUTILUS
		protected override TechType templateType =>
#else
        public virtual TechType templateType =>
#endif
#if SN1
            TechType.SeamothElectricalDefense;
#elif BZ
			TechType.SeaTruckUpgradePerimeterDefense;
#endif
		protected virtual TechType prefabTechType => TechType.MapRoomHUDChip;
		public override Vector2int SizeInInventory => new Vector2int(1, 1);
		public override TechGroup GroupForPDA => TechGroup.Personal;
		public override TechCategory CategoryForPDA => TechCategory.Equipment;
		public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
		public override string[] StepsToFabricatorTab => new string[] { "Personal", DWConstants.ChipsMenuPath };
		public override float CraftingTime => 5f;
		public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
		public override EquipmentType EquipmentType => EquipmentType.Chip;
		protected virtual int MaxDischarges => 1;
		protected virtual bool bDestroyedOnDischarge => false;
		//protected virtual List<TechType> RequiredTech => new List<TechType>();
		internal static void AddChipData(TechType chip, int MaxDischarges)
		{
			MaxDischargeDict[chip] = MaxDischarges;
		}

		/*
		internal static int GetMaxDischarges(TechType chip)
		{
			if (MaxDischargeDict.TryGetValue(chip, out int value))
			{
				return value;
			}

			return 1;
		}*/

		public DiverPerimeterDefenceChipItemBase(string classId,
			string friendlyName,
			string description) : base(classId, friendlyName, description)
		{
            //Console.WriteLine($"{this.ClassID} constructing");
            OnFinishedPatching += () =>
			{
				DWEBPlugin.AddModTechType(this.TechType);
				InventoryPatches.AddChip(this.TechType, !this.bDestroyedOnDischarge);
				DiverPerimeterDefenceBehaviour.AddChipData(this.TechType, this.MaxDischarges, this.bDestroyedOnDischarge);
				/*
				if (CompoundTechsForUnlock.Count > 0)
				{
					Log.LogDebug($"{this.TechType.AsString()}.OnFinishedPatching(): Setting up CompoundTech with RequiredTech of:" + JsonConvert.SerializeObject(CompoundTechsForUnlock, Formatting.Indented));

					Reflection.AddCompoundTech(this.TechType, CompoundTechsForUnlock);
				}
				*/
				CoroutineHost.StartCoroutine(this.PostPatchSetup());
			};
		}

#if NAUTILUS
        public override void ModPrefab(GameObject gameObject)
        {
            base.ModPrefab(gameObject);
			var behaviour = gameObject.EnsureComponent<DiverPerimeterDefenceBehaviour>();
			behaviour.Initialise(this.TechType);
        }
#else
#if ASYNC
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTechType, verbose: true);
				yield return task;

				prefab = PreparePrefab(task.GetResult());
			}

			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
			gameObject.Set(prefab);
		}

#else
		public override GameObject GetGameObject()
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
			if (prefab == null)
			{
				prefab = PreparePrefab(CraftData.GetPrefabForTechType(prefabTechType));
			}

			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
			return prefab;
		}
#endif
        public virtual GameObject PreparePrefab(GameObject prefab)
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
			GameObject obj = GameObjectUtils.InstantiateInactive(prefab);

			DiverPerimeterDefenceBehaviour behaviour = obj.EnsureComponent<DiverPerimeterDefenceBehaviour>();
			behaviour.Initialise(this.TechType);
			ModPrefabCache.AddPrefab(obj, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.

			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
			return obj;
		}
#endif


        protected override Sprite GetItemSprite()
		{
			icon ??= (templateType != TechType.None ? SpriteUtils.Get(templateType, null) : null);
			return icon;
		}
		protected virtual IEnumerator PostPatchSetup()
		{
			if (DWEBPlugin.chipSlots.Count > 0)
				yield break;

			if (bWaiting)
				yield break;

			bWaiting = true;

			while (bWaiting)
			{
				if (icon == null || icon == SpriteManager.defaultSprite)
				{
					icon = SpriteManager.Get(templateType);
				}
				else
					bWaiting = false;

				yield return new WaitForSecondsRealtime(0.5f);
			}
			Log.LogDebug($"{this.TechType.AsString()}.PostPatchSetup(): sprite loaded, now waiting for chip slots");
			while (DWEBPlugin.chipSlots.Count < 1)
			{
				yield return new WaitForSecondsRealtime(0.5f);
			}
			Log.LogDebug($"{this.TechType.AsString()}.PostPatchSetup(): completed");
		}
	}

	public class DiverPerimeterDefenceChipItem : DiverPerimeterDefenceChipItemBase<DiverPerimeterDefenceChipItem>
	{
#if NAUTILUS
        protected override TechType templateType => TechType.MapRoomHUDChip;
        protected override string templateClassId => string.Empty;
#endif
        public DiverPerimeterDefenceChipItem(string classId = "DiverPerimeterDefenceChipItem",
			string friendlyName = "Diver Perimeter Defence System",
			string description = "Protects a diver from hostile fauna using electrical discouragement. Discharge damages the chip beyond repair.") : base(classId, friendlyName, description)
		{
			OnFinishedPatching += () =>
			{
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		public override EquipmentType EquipmentType => EquipmentType.Chip;
		public override string[] StepsToFabricatorTab => new string[] { "Personal", DWConstants.ChipsMenuPath };
		public override TechType RequiredForUnlock => TechType.Polyaniline;
		protected override bool bDestroyedOnDischarge => true;
		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.Battery, 1),
						new Ingredient(TechType.WiringKit, 1),
						new Ingredient(TechType.CopperWire, 1),
						new Ingredient(TechType.Polyaniline, 1)
					}
				)
			};
		}
	}

	public class DiverDefenceSystemMk2 : DiverPerimeterDefenceChipItemBase<DiverDefenceSystemMk2>
	{
#if NAUTILUS
        protected override TechType templateType => TechType.MapRoomHUDChip;
        protected override string templateClassId => string.Empty;
#endif
        public DiverDefenceSystemMk2(string classId = "DiverDefenceSystemMk2",
			string friendlyName = "Diver Defence System Mk2",
			string description = "Protects a diver from hostile fauna using electrical discouragement. Can be recharged multiple times.") : base(classId, friendlyName, description)
		{
			OnFinishedPatching += () =>
			{
				CoroutineHost.StartCoroutine(PostPatchSetup());
			};
		}

		/*protected override List<TechType> RequiredTech => new List<TechType>()
		{
			TechType.RadioTowerPPU,
			Main.GetModTechType("DiverPerimeterDefenceChipItem")
		};*/

		protected override IEnumerator PostPatchSetup()
		{
			yield return base.PostPatchSetup();

			BatteryCharger.compatibleTech.Add(this.TechType);
			InventoryPatches.AddChipRecharge(this.TechType);
			yield break;
		}

		public override TechType RequiredForUnlock => Main.GetModTechType("DiverPerimeterDefenceChipItem");

		protected override int MaxDischarges => 1;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
#if SN1
						new Ingredient(TechType.Polyaniline, 1),
#elif BELOWZERO
						new Ingredient(TechType.RadioTowerPPU, 1),
#endif
						new Ingredient(TechType.Nickel, 1),
						new Ingredient(TechType.Battery, 1),
                        new Ingredient(DWEBPlugin.GetModTechType("DiverPerimeterDefenceChipItem"), 1),
                    }
                )
			};
		}
	}

	public class DiverDefenceMk2_FromBrokenChip : Craftable
	{
#if NAUTILUS
        protected override TechType templateType => TechType.MapRoomHUDChip;
        protected override string templateClassId => string.Empty;
#endif
        public DiverDefenceMk2_FromBrokenChip() : base("DiverDefenceMk2_FromBrokenChip", "Diver Defence System Mk2", "Protects a diver from hostile fauna using electrical discouragement. Can be recharged multiple times.")
		{
			OnFinishedPatching += () =>
			{
                DWEBPlugin.AddModTechType(this.TechType);
			};
		}

        public override TechType RequiredForUnlock => DWEBPlugin.GetModTechType("DiverPerimeterDefenceChip_Broken");

        protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 0,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(DWEBPlugin.GetModTechType("DiverPerimeterDefenceChip_Broken"), 1),
#if SN1
						new Ingredient(TechType.Polyaniline, 1),
#elif BELOWZERO
						new Ingredient(TechType.RadioTowerPPU, 1),
#endif
						new Ingredient(TechType.Nickel, 1),
						new Ingredient(TechType.Battery, 1)
					}
				),
				LinkedItems = new List<TechType>()
				{
                    DWEBPlugin.GetModTechType("DiverDefenceSystemMk2")
				}
			};
		}
	}

	public class DiverDefenceSystemMk3 : DiverPerimeterDefenceChipItemBase<DiverDefenceSystemMk3>
	{
#if NAUTILUS
        protected override TechType templateType => TechType.MapRoomHUDChip;
        protected override string templateClassId => string.Empty;
#endif
        public DiverDefenceSystemMk3() : base("DiverDefenceSystemMk3", "Diver Defence System Mk3", "Protects a diver from hostile fauna using electrical discouragement. Can discharge multiple times per charge, and can be recharged multiple times.")
		{
		}

		protected override IEnumerator PostPatchSetup()
		{
			yield return base.PostPatchSetup();

			BatteryCharger.compatibleTech.Add(this.TechType);
			InventoryPatches.AddChipRecharge(this.TechType);
			yield break;
		}

		/*protected override List<TechType> RequiredTech => new List<TechType>()
		{
			TechType.PrecursorIonBattery,
			Main.GetModTechType("DiverDefenceSystemMk2")
		};*/


		public override TechType RequiredForUnlock => DWEBPlugin.GetModTechType("DiverDefenceSystemMk2");

		protected override int MaxDischarges => 5;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
#if SN1
						new Ingredient(TechType.PrecursorKey_Orange, 1),
						new Ingredient(TechType.PrecursorIonCrystal, 1),
#elif BELOWZERO
						new Ingredient(DWEBPlugin.GetModTechType("ShadowLeviathanSample"), 1),
						new Ingredient(TechType.PrecursorIonBattery, 1),
#endif
						new Ingredient(DWEBPlugin.GetModTechType("DiverDefenceSystemMk2"), 1),
                        new Ingredient(TechType.Nickel, 1),
                    }
                )
			};
		}
	}
}
