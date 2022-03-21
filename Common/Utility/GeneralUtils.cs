using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace Common
{
	public static class Log
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

	public static class Extensions
	{
		public static T FindComponentInChildWithTag<T>(this GameObject parent, string tag) where T : Component
		{
			Transform t = parent.transform;
			foreach (Transform tr in t)
			{
				if (tr.tag == tag)
					return tr.GetComponent<T>();
			}

			return null;
		}

		public static T FindComponentInChildWithName<T>(this GameObject parent, string name) where T : Component
		{
			Transform t = parent.transform;
			foreach (Transform tr in t)
			{
				if (tr.name == name)
					return tr.GetComponent<T>();
			}

			return null;
		}
	}
}
