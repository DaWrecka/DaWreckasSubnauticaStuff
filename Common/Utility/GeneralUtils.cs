using System;
using System.Collections.Generic;
using System.Text;
using Logger = QModManager.Utility.Logger;

namespace Common
{
    class Log
    {
        public static void LogDebug(string message)
        {
            Logger.Log(Logger.Level.Debug, message);
        }

        public static void LogError(string message)
        {
            Logger.Log(Logger.Level.Error, message);
        }

        public static void LogWarning(string message)
        {
            Logger.Log(Logger.Level.Warn, message);
        }

        public static void LogInfo(string message)
        {
            Logger.Log(Logger.Level.Info, message);
        }
    }
}
