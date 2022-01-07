namespace PrawnSuitArmSwitcher.ExosuitArms
{
    public interface IExosuitArmsList
    {
        bool AddArm(InventoryItem newArm);

        void Sort();

        InventoryItem GetNextArm(InventoryItem currentArm);

        InventoryItem GetFirstArm();

        InventoryItem GetFirstArmNonCurrent(InventoryItem currentItem);
    }
}
