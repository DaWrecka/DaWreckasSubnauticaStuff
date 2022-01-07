using HarmonyLib;
using PrawnSuitArmSwitcher.UserMessages;
using System;

namespace PrawnSuitArmSwitcher.ExosuitArms
{
    public class ExosuitArmsSwitcher : IExosuitArmsSwitcher
    {
        public static ExosuitArmsSwitcher CreateInstance(IExosuitArmsManager exosuitArmsManager,
                                                         IExosuitArmsStorage armsStorage,
                                                         IExosuitArmsListFactory armsListFactory,
                                                         IUserMessages userMessages)
            => new ExosuitArmsSwitcher(exosuitArmsManager, armsStorage, armsListFactory, userMessages);

        protected ExosuitArmsSwitcher(IExosuitArmsManager exosuitArmsManager,
                                      IExosuitArmsStorage armsStorage,
                                      IExosuitArmsListFactory armsListFactory,
                                      IUserMessages userMessages)
        {
            _exosuitArms = exosuitArmsManager;
            _armsStorage = armsStorage;
            _armsListFactory = armsListFactory;
            _userMessages = userMessages;
        }

        protected IExosuitArmsManager _exosuitArms;
        protected IExosuitArmsStorage _armsStorage;
        protected IExosuitArmsListFactory _armsListFactory;
        protected IUserMessages _userMessages;

        public bool CycleArm(string slotID)
        {
            var currentArm = _exosuitArms.GetArm(slotID);

            IExosuitArmsList armsList = new ExosuitArmsList();

            foreach (var arm in _armsStorage.GetAvailableArms())
                armsList.AddArm(arm);

            if (currentArm != null)
                armsList.AddArm(currentArm);

            armsList.Sort();
            
            bool canStoreArm = true;
            if (currentArm != null)
                canStoreArm = _armsStorage.HasRoomFor(currentArm);

            var nextArm = armsList.GetNextArm(currentArm);

            if (currentArm == null && nextArm == null)
            {
                _userMessages.ShowError("No PRAWN Suit Arm Upgrade found");
                return false;
            }
                
                
            if (nextArm == null && !canStoreArm)
            {
                nextArm = armsList.GetFirstArmNonCurrent(currentArm);

                if (nextArm == null)
                {
                    _userMessages.ShowError("Cannot store current PRAWN Suit Arm Upgrade, please make room and try again");
                    return false;
                }
                else
                {
                    _userMessages.ShowInfo("Skipped Default PRAWN Suit Arm Upgrade because there is no room to store the current one");
                }
            }

            _exosuitArms.ResetArms();

            InventoryItem removedArm = null;
            if (currentArm != null)
                removedArm = _exosuitArms.RemoveArm(slotID);
                
                

            if (nextArm != null)
            {
                if (!_armsStorage.RemoveItem(nextArm))
                {
                    _exosuitArms.MarkArmsForUpdate();
                    _userMessages.ShowError("Couldn't remove PRAWN Suit Arm Upgrade " + nextArm.item.GetTechName() + " from Arms Storage");
                    Logger.Log("Couldn't remove PRAWN Suit Arm Upgrade " + nextArm.item.GetTechName() + " from Arms Storage");
                    return false;
                }

                _exosuitArms.SetArm(slotID, nextArm);
            }

            if (removedArm != null)
                _armsStorage.AddItem(removedArm);

            _exosuitArms.MarkArmsForUpdate();

            return true;
        }

        private bool WouldStorageOverflow(bool canStoreArm, InventoryItem arm)
            => !canStoreArm && arm.item.GetTechType() == TechType.None;
    }
}
