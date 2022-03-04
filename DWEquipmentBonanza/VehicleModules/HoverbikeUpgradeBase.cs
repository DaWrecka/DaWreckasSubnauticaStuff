using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    internal abstract class HoverbikeUpgradeBase<T> : Equipable
    {
        protected static GameObject prefab;
        protected static Sprite sprite;
        protected virtual TechType spriteTemplate { get; }

        public override EquipmentType EquipmentType => EquipmentType.HoverbikeModule;
        public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
        public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
        public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
        public override TechType RequiredForUnlock => TechType.Hoverbike;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
        public override string[] StepsToFabricatorTab => new string[] { "Upgrades", "HoverbikeUpgrades" };
        public override Vector2int SizeInInventory => new Vector2int(1, 1);
        protected abstract TechType prefabTemplate { get; }

        protected override Sprite GetItemSprite()
        {
            try
            {
                sprite ??= ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}Icon.png") ?? SpriteManager.Get(spriteTemplate, null);
            }
            catch
            {
                sprite ??= SpriteManager.Get(spriteTemplate, null);
            }

            return sprite;
        }

        protected virtual GameObject ModifyPrefab(GameObject original)
        {
            return original;
        }

        public override System.Collections.IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefabTemplate == TechType.None)
                yield break;

            CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate);
            yield return task;

            prefab = ModifyPrefab(ModPrefabCache.AddPrefabCopy(task.GetResult()));
            gameObject.Set(prefab);
        }

        public HoverbikeUpgradeBase(string classID, string Title, string Description) : base(classID, Title, Description)
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
            };
        }
    }
#endif
}
