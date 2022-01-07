namespace PrawnSuitArmSwitcher.UserMessages
{
    public class SimpleUserMessages : IUserMessages
    {
        public void ShowError(string message) => ErrorMessage.AddError(message);

        public void ShowInfo(string message) => ErrorMessage.AddMessage(message);

        public void ShowWarning(string message) => ErrorMessage.AddWarning(message);

        public void ShowDebug(string message) => ErrorMessage.AddDebug(message);
    }
}
