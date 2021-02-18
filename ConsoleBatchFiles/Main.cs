using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Logger = QModManager.Utility.Logger;
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

#if BELOWZERO && DEBUG
        private static readonly FieldInfo entriesInfo = typeof(TechData).GetField("entries", BindingFlags.Static | BindingFlags.NonPublic);
        internal static string dumpPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "jsonvalues.raw");

        public static void ConsoleCommand_dumptechdata()
        {
            Dictionary<TechType, JsonValue> entries = (Dictionary<TechType, JsonValue>)entriesInfo.GetValue(null);

            if (entries is null)
            {
                Logger.Log(Logger.Level.Error, "Entries is NULL Reflection failed!!!!!!!!", null, true);
                return;
            }

            Logger.Log(Logger.Level.Debug, "Dumping TechData", null, true);
            using (StreamWriter writer = new StreamWriter(dumpPath))
            {
                writer.Write(JsonConvert.SerializeObject(entries, Formatting.Indented));
            }
            Logger.Log(Logger.Level.Debug, "Dumping done", null, true);
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
                Logger.Log(Logger.Level.Error, $"Could not find file {filePath}", null, true); 
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            foreach (string s in lines)
            {
                //Logger.Log(Logger.Level.Debug, $"Read line '{s}' from file", null, true);
                //DevConsole.InternalSendConsoleCommand(s);
                DevConsole.SendConsoleCommand(s);
            }
            //Logger.Log(Logger.Level.Debug, $"Done reading and executing {lines.Length} lines from file {filePath}", null, true);
        }

        [QModPatch]
        public static void Patch()
        {
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("batch", typeof(Main), nameof(ConsoleCommand_batch));
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("bat", typeof(Main), nameof(ConsoleCommand_batch));
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("exec", typeof(Main), nameof(ConsoleCommand_batch));
#if BELOWZERO && DEBUG
            ConsoleCommandsHandler.Main.RegisterConsoleCommand("dumptechdata", typeof(Main), nameof(ConsoleCommand_dumptechdata));
#endif
        }
    }
}
