using SMLHelper.V2.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /*internal abstract void PreUpdate(Hoverbike instance);
        internal abstract void Update(Hoverbike instance);
        internal abstract void PostUpdate(Hoverbike instance);

        internal abstract void PostUpgradeModuleChange(Hoverbike instance);*/

        protected override Sprite GetItemSprite()
        {
            if (sprite == null || sprite == SpriteManager.defaultSprite)
            {
                sprite = SpriteManager.Get(spriteTemplate);
            }

            return sprite;
        }


        public HoverbikeUpgradeBase(string classID, string Title, string Description) : base(classID, Title, Description)
        {
        }
    }
#endif
}
