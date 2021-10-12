using System;
using System.Collections.Generic;
using System.Text;
using Logger = QModManager.Utility.Logger;

namespace Common
{
	class Log
	{
		public static void LogDebug(string message, Exception ex = null, bool showOnScreen = false)
		{
#if !RELEASE
			Logger.Log(Logger.Level.Debug, message, ex, showOnScreen);
#endif
		}

		public static void LogError(string message, Exception ex = null, bool bShowOnScreen = false)
		{
			Logger.Log(Logger.Level.Error, message, ex, bShowOnScreen);
		}

		public static void LogWarning(string message, Exception ex = null, bool bShowOnScreen = false)
		{
			Logger.Log(Logger.Level.Warn, message, ex, bShowOnScreen);
		}

		public static void LogInfo(string message, Exception ex = null, bool bShowOnScreen = false)
		{
			Logger.Log(Logger.Level.Info, message, ex, bShowOnScreen);
		}
	}
}
