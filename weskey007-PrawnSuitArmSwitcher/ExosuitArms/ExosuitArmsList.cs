using System.Collections.Generic;
using System.Linq;

namespace PrawnSuitArmSwitcher.ExosuitArms
{
    public class ExosuitArmsList : IExosuitArmsList
    {
        public ExosuitArmsList()
        {
            _arms = new List<InventoryItem>();
        }

        protected List<InventoryItem> _arms;

        public bool AddArm(InventoryItem newArm)
        {
            var newTechType = newArm.item.GetTechType();

            foreach (var arm in _arms)
                if (newTechType == arm.item.GetTechType())
                    return false;

            _arms.Add(newArm);
            return true;
        }

        // Sort using the tech name, this isn't very efficient because it calls a bunch of string functions
        // However there shouldn't be more than 4-5 items in the list anyway and this way it is always sorted the same
        public void Sort() => _arms.Sort((arm1, arm2) => string.Compare(arm1.item.GetTechName(), arm2.item.GetTechName()));

        public InventoryItem GetNextArm(InventoryItem currentArm)
        {
            if (currentArm == null)
                // if there isn't a current arm, return the first item from the list
                return GetFirstArm();

            bool foundCurrent = false;

            foreach (var arm in _arms)
                if (foundCurrent)
                    return arm;
                else if (IsSameType(currentArm, arm))
                    foundCurrent = true;

            return null;
        }

        public InventoryItem GetFirstArm() => _arms.FirstOrDefault();

        public InventoryItem GetFirstArmNonCurrent(InventoryItem currentItem)
        {
            foreach (var arm in _arms)
                if (!IsSameType(currentItem, arm))
                    return arm;

            return null;
        }

        // If there is no current arm then the techtype will never be in the inventory anyway, however the check is to prevent weird exceptions
        private bool IsSameType(InventoryItem currentArm, InventoryItem newArm)
            => (currentArm != null && currentArm.item.GetTechType() == newArm.item.GetTechType());
    }
}
