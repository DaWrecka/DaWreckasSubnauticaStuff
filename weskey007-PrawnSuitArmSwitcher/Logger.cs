using System;

namespace PrawnSuitArmSwitcher
{
    public static class Logger
    {
        private static string LogTag = "[PrawnSuitArmSwitcher] ";

        public static void Log(string message) => Console.WriteLine(LogTag + message);
    }
}
