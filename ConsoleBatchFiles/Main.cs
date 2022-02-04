using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Logger = QModManager.Utility.Logger;
using System;
using System.Collections;
using UnityEngine;
using UWE;
#if BELOWZERO
using Newtonsoft.Json;
#endif

namespace ConsoleBatchFiles
{
    [QModCore]
    public static class Main
    {
        public const string version = "1.1.0.0";

        internal static string ModPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string Executing = "";
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
                //Logger.Log(Logger.Level.Debug, $"Read line '{s}' from file", null, true);
                //DevConsole.InternalSendConsoleCommand(s);
                DevConsole.SendConsoleCommand(s);
            }*/
            CoroutineHost.StartCoroutine(ExecuteScript(BatchName, lines));
            //Logger.Log(Logger.Level.Debug, $"Done reading and executing {lines.Length} lines from file {filePath}", null, true);
        }

        private static IEnumerator ExecuteScript(string filename, string[] lines)
        {
            if (Executing != "")
            {
                ErrorMessage.AddMessage($"Script {Executing} already running, please wait before starting another.");
                yield break;
            }

            Executing = filename;
            ErrorMessage.AddMessage($"Executing script {filename}");

            foreach (string s in lines)
            {
                //Logger.Log(Logger.Level.Debug, $"Read line '{s}' from file", null, true);
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

        [QModPatch]
        public static void Patch()
        {
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("batch", typeof(Main), nameof(ConsoleCommand_batch));
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("bat", typeof(Main), nameof(ConsoleCommand_batch));
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("exec", typeof(Main), nameof(ConsoleCommand_batch));
#if BELOWZERO
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("lock", typeof(Main), nameof(OnConsoleCommand_lock));
#endif
        }
    }
}
