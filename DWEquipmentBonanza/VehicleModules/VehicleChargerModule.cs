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
        public override float CraftingTime => 10f;
        public override Vector2int SizeInInventory => new Vector2int(1, 1);

        protected static GameObject prefab;
        protected virtual TechType template => TechType.None;
        public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
        public override EquipmentType EquipmentType => EquipmentType.VehicleModule;
        protected virtual float ChargerWeight => 1f;
        protected Sprite sprite;

#if SUBNAUTICA_STABLE
        public override GameObject GetGameObject()
        {
            if (prefab == null && template != TechType.None)
            {
                prefab = CraftData.InstantiateFromPrefab(template);
                ModPrefabCache.AddPrefab(prefab, false);
                prefab.EnsureComponent<Y>();
            }

            return prefab;
        }

#elif BELOWZERO
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null && template != TechType.None)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(template);
                yield return task;
                prefab = GameObject.Instantiate<GameObject>(task.GetResult());
                // The code is handled by the SeatruckUpdater component, rather than anything here.
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                         // but it can still be instantiated. [unlike with SetActive(false)]

                prefab.EnsureComponent<Y>();
            }

            gameObject.Set(prefab);
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
