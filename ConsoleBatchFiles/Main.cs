using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Logger = QModManager.Utility.Logger;
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
        public const string version = "0.8.0.0";

        internal static string ModPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
                Logger.Log(Logger.Level.Error, $"Could not find file {filePath}", null, true); 
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            /*foreach (string s in lines)
            {
                //Logger.Log(Logger.Level.Debug, $"Read line '{s}' from file", null, true);
                //DevConsole.InternalSendConsoleCommand(s);
                DevConsole.SendConsoleCommand(s);
            }*/
            CoroutineHost.StartCoroutine(ExecuteScript(lines));
            //Logger.Log(Logger.Level.Debug, $"Done reading and executing {lines.Length} lines from file {filePath}", null, true);
        }

        private static IEnumerator ExecuteScript(string[] lines)
        {
            foreach (string s in lines)
            {
                //Logger.Log(Logger.Level.Debug, $"Read line '{s}' from file", null, true);
                //DevConsole.InternalSendConsoleCommand(s);
                DevConsole.SendConsoleCommand(s);
                yield return new WaitForEndOfFrame();
            }

            yield break;
        }

        [QModPatch]
        public static void Patch()
        {
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("batch", typeof(Main), nameof(ConsoleCommand_batch));
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("bat", typeof(Main), nameof(ConsoleCommand_batch));
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("exec", typeof(Main), nameof(ConsoleCommand_batch));
        }
    }
}
