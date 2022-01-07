namespace PrawnSuitArmSwitcher.ExosuitArms
{
    public class ExosuitArmsListFactory : IExosuitArmsListFactory
    {
        public IExosuitArmsList Create() => new ExosuitArmsList();
    }
}
