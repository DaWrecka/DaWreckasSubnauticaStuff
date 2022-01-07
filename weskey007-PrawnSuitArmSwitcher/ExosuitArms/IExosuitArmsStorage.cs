using System.Collections.Generic;

namespace PrawnSuitArmSwitcher.ExosuitArms
{
    public interface IExosuitArmsStorage
    {
        IEnumerable<InventoryItem> GetAvailableArms();

        bool AddItem(InventoryItem inventoryItem);

        bool RemoveItem(InventoryItem inventoryItem);

        bool HasRoomFor(InventoryItem inventoryItem);
    }
}
