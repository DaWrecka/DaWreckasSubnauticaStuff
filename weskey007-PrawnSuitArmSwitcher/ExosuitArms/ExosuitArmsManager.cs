using HarmonyLib;
using System;

namespace PrawnSuitArmSwitcher.ExosuitArms
{
    public class ExosuitArmsManager : IExosuitArmsManager
    {
        public static ExosuitArmsManager CreateFromExosuit(Exosuit exosuit) => new ExosuitArmsManager(exosuit);

        protected Exosuit _exosuit;
        protected Sequence _sequence = new Sequence();

        protected ExosuitArmsManager(Exosuit exosuit)
        {
            _exosuit = exosuit;
        }

        public bool SetArm(string slotID, InventoryItem inventoryItem) => _exosuit.modules.AddItem(slotID, inventoryItem);

        public InventoryItem GetArm(string slotID) => _exosuit.modules.GetItemInSlot(slotID);

        public InventoryItem RemoveArm(string slotID) => _exosuit.modules.RemoveItem(slotID, false, false);

        public void MarkArmsForUpdate()
        {
            // Update GUI QuickSlots, it's not the cleanest way. But I couldn't find an alternative.
            uGUI_QuickSlots_HarmonyPatch.QuickSlotsUpdateTarget = _exosuit;
            _exosuit.MarkArmsDirty();
        }

        public void ResetArms()
        {
            foreach(var fieldName in ExosuitArmsStatics.ExosuitArmFieldNames)
            {
                IExosuitArm exosuitArm = GetIExosuitArm(fieldName);
                if (exosuitArm != null)
#if SN1
                    exosuitArm.Reset();
#elif BELOWZERO
                    exosuitArm.ResetArm();
#endif
            }

        }

        public IExosuitArm GetIExosuitArm(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return null;

            try
            {
                return (IExosuitArm) AccessTools
                                            .Field(_exosuit.GetType(), fieldName)
                                            .GetValue(_exosuit);
            }
            catch (Exception ex)
            {
                Logger.Log("ExosuitArmsManager.GetIExosuitArm Exception: " + ex);
                return null;
            }
        }
    }
}
