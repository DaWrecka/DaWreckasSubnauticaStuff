using Common;
using Common.Utility;
using DWEquipmentBonanza.MonoBehaviours;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if SUBNAUTICA_STABLE
    using Sprite = Atlas.Sprite;
#endif

namespace DWEquipmentBonanza.VehicleModules
{
    public abstract class VehicleChargerModule<Y> : Equipable where Y : MonoBehaviour
    {
        public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
        public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
        public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
        public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.ChargerMenuPath };
        public override float CraftingTime => 10f;
        public override Vector2int SizeInInventory => new Vector2int(1, 1);
        protected virtual TechType template => TechType.None;
        public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
        public override EquipmentType EquipmentType => EquipmentType.VehicleModule;
        protected virtual float ChargerWeight => 1f;
        protected Sprite sprite;

#if SUBNAUTICA_STABLE
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

        public VehicleChargerModule(string classID,
            string friendlyName,
            string description) : base(classID, friendlyName, description)
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
                //VehicleUpdater.AddChargerType(this.TechType, ChargerWeight);
                //SeaTruckUpgradesPatches.AddMaxModuleOverride(this.TechType, MaxSolarModules);
            };
        }
    }
}
