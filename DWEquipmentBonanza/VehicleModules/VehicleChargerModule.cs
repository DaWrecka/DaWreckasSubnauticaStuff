using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using Common.Utility;
using DWEquipmentBonanza.MonoBehaviours;
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
#else
using RecipeData = SMLHelper.V2.Crafting.TechData;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using SMLHelper.V2.Handlers;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if SN1
//using Sprite = Atlas.Sprite;
#endif

namespace DWEquipmentBonanza.VehicleModules
{
	public abstract class VehicleModule : Equipable
	{
		public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
		public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
		public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
		public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
		public override string[] StepsToFabricatorTab => new string[] { DWConstants.ChargerMenuPath };
		public override float CraftingTime => 5f;
		public override Vector2int SizeInInventory => new(1, 1);
		public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
		public override EquipmentType EquipmentType => EquipmentType.VehicleModule;
		protected virtual float ChargerWeight => 1f;
		protected Sprite sprite;

		public VehicleModule(string classID,
			string friendlyName,
			string description) : base(classID, friendlyName, description)
		{
		}

		public override void FinalisePrefab(CustomPrefab prefab)
		{
			base.FinalisePrefab(prefab);
			prefab.SetVehicleUpgradeModule(EquipmentType, QuickSlotType)
				.WithOnModuleAdded(OnModuleAdded)
				.WithOnModuleRemoved(OnModuleRemoved)
				.WithOnModuleToggled(OnModuleToggled)
				.WithOnModuleUsed(OnModuleUsed);
		}

		protected virtual void OnModuleAdded(Vehicle inst, int slotId)
		{ }

		protected virtual void OnModuleUsed(Vehicle inst, int slotID, float charge, float chargeScalar)
		{ }

		// Toggled, Removed
		protected virtual void OnModuleToggled(Vehicle inst, int slotID, float energyCost, bool state)
		{ }

		protected virtual void OnModuleRemoved(Vehicle inst, int slotID)
		{ }
	}

	public abstract class VehicleChargerModule<Y> : VehicleModule where Y : MonoBehaviour
	{
#if NAUTILUS
		protected override TechType templateType => TechType.SeamothSolarCharge;
		protected override string templateClassId => string.Empty;

		public override void ModPrefab(GameObject gameObject)
		{
			base.ModPrefab(gameObject);

			gameObject.EnsureComponent<Y>();
		}

		public override void FinalisePrefab(CustomPrefab prefab)
		{
			base.FinalisePrefab(prefab);
			prefab.SetVehicleUpgradeModule(EquipmentType, QuickSlotType)
				.WithOnModuleAdded(OnModuleAdded)
				.WithOnModuleRemoved(OnModuleRemoved)
				.WithOnModuleToggled(OnModuleToggled)
				.WithOnModuleUsed(OnModuleUsed);
		}


#elif SN1	
		public override GameObject GetGameObject()
		{
			GameObject modPrefab;

			if (template != TechType.None)
			{
				if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
				{

					modPrefab = GameObjectUtils.InstantiateInactive(CraftData.GetPrefabForTechType(template));
					//ModPrefabCache.AddPrefab(modPrefab, false);
					modPrefab.EnsureComponent<Y>();
					TechTypeUtils.AddModTechType(this.TechType, modPrefab);
				}
			}
			else
				modPrefab = null;

			return modPrefab;
		}

#elif BELOWZERO
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			GameObject modPrefab;

			if (template != TechType.None)
			{
				if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
				{
					CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(template);
					yield return task;
					modPrefab = GameObjectUtils.InstantiateInactive(task.GetResult());
					//ModPrefabCache.AddPrefab(modPrefab, false);
					modPrefab.EnsureComponent<Y>();
					TechTypeUtils.AddModTechType(this.TechType, modPrefab);
				}
			}
			else
				modPrefab = null;


			gameObject.Set(modPrefab);
		}
#endif

		protected override Sprite GetItemSprite()
		{
			sprite ??= ImageUtils.LoadSpriteFromFile(Path.Combine(Main.AssetsFolder, "VehicleCharger", $"{ClassID}.png"));
			return sprite;
		}

#if !NAUTILUS
		protected virtual void OnFinishedPatch()
		{
			Main.AddModTechType(this.TechType);
		}
#endif

		public VehicleChargerModule(string classID,
			string friendlyName,
			string description) : base(classID, friendlyName, description)
		{
			//Console.WriteLine($"{this.ClassID} constructing");
#if !NAUTILUS
			OnFinishedPatching += OnFinishedPatch;
#endif
		}
	}
}
