namespace PrawnSuitArmSwitcher.UserMessages
{
    public interface IUserMessages
    {
        void ShowError(string message);
        void ShowWarning(string message);
        void ShowInfo(string message);
        void ShowDebug(string message);
    }
}
