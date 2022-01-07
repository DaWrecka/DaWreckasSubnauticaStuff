using SMLHelper.V2.Handlers;
using System.Collections.Generic;

namespace PrawnSuitArmSwitcher.ExosuitArms
{
    public class ExosuitArmsStorage : IExosuitArmsStorage
    {
        public static ExosuitArmsStorage CreateInstance(IItemsContainer container) => new ExosuitArmsStorage(container);

        protected ExosuitArmsStorage(IItemsContainer container)
        {
            _armsContainer = container;
        }

        protected IItemsContainer _armsContainer;

        public IEnumerable<InventoryItem> GetAvailableArms()
        {
            foreach (var possibleArm in _armsContainer)
#if SUBNAUTICA_STABLE
                if (CraftData.GetEquipmentType(possibleArm.item.GetTechType()) == EquipmentType.ExosuitArm)
#elif BELOWZERO
                if(TechData.GetEquipmentType(possibleArm.item.GetTechType()) == EquipmentType.ExosuitArm)
#endif
                    yield return possibleArm;
        }

        public bool AddItem(InventoryItem inventoryItem) => _armsContainer.AddItem(inventoryItem);

        public bool RemoveItem(InventoryItem inventoryItem) => _armsContainer.RemoveItem(inventoryItem, true, false);

        public bool HasRoomFor(InventoryItem inventoryItem) => _armsContainer.HasRoomFor(inventoryItem.item, null);
    }
}
