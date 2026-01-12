using Common;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if BEPINEX
using BepInEx;
using BepInEx.Logging;
using Nautilus.Handlers;
#elif QMM
	using QModManager.API.ModLoading;
	using Logger = QModManager.Utility.Logger;
	using SMLHelper.V2.Handlers;
#endif

using System;
using System.Collections;
using UnityEngine;
using UWE;
using System.Text;
#if BELOWZERO
using Newtonsoft.Json;
#endif

namespace ConsoleBatchFiles
{
#if BEPINEX
	[BepInPlugin(GUID, pluginName, version)]

#if BELOWZERO
	[BepInProcess("SubnauticaZero.exe")]
#elif SN1
	[BepInProcess("Subnautica.exe")]
#endif
	public class BatchFilesPlugin : BaseUnityPlugin
	{
#elif QMM
	[QModCore]
	public static class BatchFilesPlugin
	{
#endif
		#region[Declarations]
		public const string
				MODNAME = "BatchFiles",
				AUTHOR = "dawrecka",
				GUID = "com." + AUTHOR + "." + MODNAME,
				version = "1.21.0.5";
		private const string pluginName = "Console Batch Files";
		//private const string CustomOxygenGUID = "com." + AUTHOR + "." + "CustomiseOxygen";
		#endregion

		internal static string ModPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		private static string Executing = "";
		private static bool bPickupablesLogging = false;

#if BELOWZERO
		private static void OnConsoleCommand_lock(NotificationCenter.Notification n)
		{
			if (n != null && n.data != null)
			{
				string text = (string)n.data[0];
				if (text == "all")
				{
					List<TechType> list = new List<TechType>(KnownTech.GetTech());
					for (int i = 0; i < list.Count; i++)
					{
						KnownTech.Remove(list[i]);
					}
					return;
				}
				TechType techType;
				if (UWE.Utils.TryParseEnum<TechType>(text, out techType) && CraftData.IsAllowed(techType))
				{
					bool flag = false | KnownTech.Remove(techType);
					PDAScanner.RemoveAllEntriesWhichUnlocks(techType);
					ErrorMessage.AddDebug("Locked " + Language.main.Get(techType.AsString(false)));
				}
			}
		}
#endif

		public static void OnConsoleCommand_pickupables(NotificationCenter.Notification n)
		{
			if (bPickupablesLogging)
			{
				ErrorMessage.AddMessage("Already running");
				return;
			}
			CoroutineHost.StartCoroutine(LogPickupables());
		}

		public static IEnumerator LogPickupables()
		{
			bPickupablesLogging = true;
			ErrorMessage.AddMessage("Pickupables command running");
			StringBuilder sbPickups = new StringBuilder($"Pickupable TechTypes:\n");
			StringBuilder sbBadPrefabs = new StringBuilder($"TechTypes without prefabs:\n");
			StringBuilder sbNotPickups = new StringBuilder($"Non-pickupable TechTypes:\n");
			foreach (TechType tech in Enum.GetValues(typeof(TechType)))
			{

				string LogString = $"{tech.AsString()}";
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(tech);
				yield return task;

				GameObject prefab = task.GetResult();
				if (prefab == null)
				{
					sbBadPrefabs.AppendLine(LogString);
					continue;
				}
				Pickupable pickup = prefab.GetComponent<Pickupable>();
				if (pickup == null)
				{
					sbNotPickups.AppendLine(LogString);
					continue;
				}

				LogString += $"{pickup.GetTechName()}";
				sbPickups.AppendLine(LogString);
			}

			Log.LogInfo(sbPickups.ToString());
			Log.LogInfo(sbNotPickups.ToString());
			Log.LogInfo(sbBadPrefabs.ToString());

			ErrorMessage.AddMessage("Pickupables command done");
			bPickupablesLogging = false;
			yield break;
		}

		public static void ConsoleCommand_batch(string BatchName)
		{
			if(BatchName.SplitByChar('.').Length < 2)
			{
				// The above should return at least two entries if the name already contains an extension. If it doesn't, append a .txt to the end.
				BatchName += ".txt";
			}
			// This command takes the name of a file in the mod directory and attempts to parse it as separate lines, which are passed to the DevConsole separately.
			string filePath = Path.Combine(ModPath, BatchName);
			if (!File.Exists(filePath))
			{
				ErrorMessage.AddMessage($"Could not find file {filePath}"); 
				return;
			}

			string[] lines = File.ReadAllLines(filePath);
			/*foreach (string s in lines)
			{
				//Log.LogDebug($"Read line '{s}' from file", null, true);
				//DevConsole.InternalSendConsoleCommand(s);
				DevConsole.SendConsoleCommand(s);
			}*/
			CoroutineHost.StartCoroutine(ExecuteScript(BatchName, lines));
			//Log.LogDebug($"Done reading and executing {lines.Length} lines from file {filePath}", null, true);
		}

		private static IEnumerator ExecuteScript(string filename, string[] lines)
		{
			if (!String.IsNullOrEmpty(Executing))
			{
				ErrorMessage.AddMessage($"Script {Executing} already running, please wait before starting another.");
				yield break;
			}

			Executing = filename;
			ErrorMessage.AddMessage($"Executing script {filename}");

			foreach (string s in lines)
			{
				//Log.LogDebug($"Read line '{s}' from file", null, true);
				ErrorMessage.AddMessage(s);
				//DevConsole.InternalSendConsoleCommand(s);
				string[] args = s.Split(new string[] { " " }, 2, System.StringSplitOptions.RemoveEmptyEntries);

				// Process special commands "wait" and "waitRT"
				// These differ in that "wait" is affected by the day/night speed and also by game pause, while "waitRT" is not.
				if (args[0].Substring(0, 4).ToLower() == "wait")
				{
					if (Single.TryParse(args[1], out float delay))
					{
						bool bWaitRT = (args[0].Substring(4, 2).ToLower() == "rt");
						if (bWaitRT)
						{
							ErrorMessage.AddMessage($"Waiting for {delay} seconds realtime...");
							yield return new WaitForSecondsRealtime(delay);
						}
						else
						{
							ErrorMessage.AddMessage($"Waiting for {delay} seconds in-game...");
							yield return new WaitForSeconds(delay);
						}
					}
					else
						ErrorMessage.AddMessage($"Could not parse '{args[1]}' as number");
				}
				else
				{
					DevConsole.SendConsoleCommand(s);
					yield return new WaitForEndOfFrame();
				}
			}

			Executing = "";
			yield break;
		}
#if QMM
		[QModPatch]
#endif
		public void Start()
		{
#if QMM
			ConsoleCommandsHandler.Main.RegisterConsoleCommand("batch", typeof(BatchFilesPlugin), nameof(ConsoleCommand_batch));
			ConsoleCommandsHandler.Main.RegisterConsoleCommand("bat", typeof(BatchFilesPlugin), nameof(ConsoleCommand_batch));
			ConsoleCommandsHandler.Main.RegisterConsoleCommand("exec", typeof(BatchFilesPlugin), nameof(ConsoleCommand_batch));
			ConsoleCommandsHandler.Main.RegisterConsoleCommand("pickupables", typeof(BatchFilesPlugin), nameof(OnConsoleCommand_pickupables));
#if BELOWZERO
			ConsoleCommandsHandler.Main.RegisterConsoleCommand("lock", typeof(BatchFilesPlugin), nameof(OnConsoleCommand_lock));
#endif
#else
			ConsoleCommandsHandler.RegisterConsoleCommand("batch", typeof(BatchFilesPlugin), nameof(ConsoleCommand_batch), new Type[] { typeof(string) });
			ConsoleCommandsHandler.RegisterConsoleCommand("bat", typeof(BatchFilesPlugin), nameof(ConsoleCommand_batch), new Type[] { typeof(string) });
			ConsoleCommandsHandler.RegisterConsoleCommand("exec", typeof(BatchFilesPlugin), nameof(ConsoleCommand_batch), new Type[] { typeof(string) });
			ConsoleCommandsHandler.RegisterConsoleCommand("pickupables", typeof(BatchFilesPlugin), nameof(OnConsoleCommand_pickupables), new Type[] { typeof(string) });
	#if BELOWZERO
			ConsoleCommandsHandler.RegisterConsoleCommand("lock", typeof(BatchFilesPlugin), nameof(OnConsoleCommand_lock), new Type[] { typeof(string) });
	#endif
#endif
		}
	}
}
