using HarmonyLib;
using PrawnSuitArmSwitcher.ExosuitArms;
using PrawnSuitArmSwitcher.UserMessages;

namespace PrawnSuitArmSwitcher
{
    [HarmonyPatch(typeof(Exosuit))]
    public class ExosuitSlotKeyDown_HarmonyPatch
    {
        private static readonly IExosuitArmsListFactory _armsListFactory = new ExosuitArmsListFactory();
        private static readonly IUserMessages _userMessages = new SimpleUserMessages();

        [HarmonyPatch(nameof(Exosuit.SlotKeyDown))]
        public static bool Prefix(int slotID, Exosuit __instance)
        {
            switch (slotID)
            {
                case 0:
                    CycleArm(__instance, ExosuitArmsStatics.LeftArm);
                    break;
                case 1:
                    CycleArm(__instance, ExosuitArmsStatics.RightArm);
                    break;

                default:
                    break;
            }

            return true;
        }

        public static void CycleArm(Exosuit exosuit, string slotID)
        {
            IExosuitArmsManager manager = ExosuitArmsManager.CreateFromExosuit(exosuit);
            IExosuitArmsStorage storage = ExosuitArmsStorage.CreateInstance(Inventory.Get().container);
            IExosuitArmsSwitcher switcher = ExosuitArmsSwitcher.CreateInstance(manager, storage, _armsListFactory, _userMessages);

            switcher.CycleArm(slotID); 
        }
    }
}
