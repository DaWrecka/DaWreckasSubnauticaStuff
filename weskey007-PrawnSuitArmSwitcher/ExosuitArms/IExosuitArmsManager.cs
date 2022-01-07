namespace PrawnSuitArmSwitcher.ExosuitArms
{
    public interface IExosuitArmsManager
    {
        bool SetArm(string slotID, InventoryItem inventoryItem);

        InventoryItem GetArm(string slotID);

        InventoryItem RemoveArm(string slotID);

        void MarkArmsForUpdate();

        void ResetArms();

        IExosuitArm GetIExosuitArm(string slotID);
    }
}
