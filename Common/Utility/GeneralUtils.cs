using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
#if QMM
	using Logger = QModManager.Utility.Logger;
#elif BEPINEX
	using BepInEx;
	using BepInEx.Logging;
#endif

namespace Common
{
	public static class Log
	{
#if QMM

		public static void LogDebug(string message, Exception ex = null, bool showOnScreen = false)
		{
#if !RELEASE
			Logger.Log(Logger.Level.Debug, message, ex, showOnScreen);
			//Console.WriteLine($"[{Assembly.GetExecutingAssembly().GetName().Name}:DEBUG] " + message);
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
#elif BEPINEX
		private static ManualLogSource logger;
		private static readonly AssemblyName ModName = Assembly.GetExecutingAssembly().GetName();

		static Log()
		{
			if(logger == null)
				logger = BepInEx.Logging.Logger.CreateLogSource(ModName.Name);
		}

		public static void InitialiseLog(string GUID)
		{
			if (logger == null)
				logger = BepInEx.Logging.Logger.CreateLogSource(GUID);

			logger?.LogInfo("Log initialised for mod " + GUID);
		}

		public static void LogDebug(string message, Exception ex = null, bool bShowOnScreen = false)
		{
#if !RELEASE
			logger.LogDebug(message);
#endif
			if (bShowOnScreen)
				ErrorMessage.AddDebug(message);
		}

		public static void LogError(string message, Exception ex = null, bool bShowOnScreen = false)
		{
			logger.LogError(message);
			if (bShowOnScreen)
				ErrorMessage.AddError(message);
		}

		public static void LogWarning(string message, Exception ex = null, bool bShowOnScreen = false)
		{
			logger.LogWarning(message);
			if (bShowOnScreen)
				ErrorMessage.AddWarning(message);
		}

		public static void LogInfo(string message, Exception ex = null, bool bShowOnScreen = false)
		{
			logger.LogInfo(message);
			if (bShowOnScreen)
				ErrorMessage.AddMessage(message);
		}
	}
#endif

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

	public static class GeneralUtils
	{
		public static void LogTranspiler(List<CodeInstruction> codes)
		{
			for (int i = 0; i < codes.Count; i++)
				Log.LogInfo(String.Format("0x{0:X4}", i) + $" : {codes[i].opcode.ToString()}	{(codes[i].operand != null ? codes[i].operand.ToString() : "")}");

		}
	}
}
