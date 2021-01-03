using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using QModManager.API.ModLoading;
//using SMLHelper.V2.Crafting;
using System.Reflection;
#if SUBNAUTICA_STABLE
#elif BZ || SUBNAUTICA_EXP
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
#endif
using LitJson;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace NamedVehiclePrompts
{
    [QModCore]
    public class Main
    {
        internal static string ModPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static string languageFile = ModPath + "/Assets/language.json";
        internal static Dictionary<string, string> vehiclePromptDict = new Dictionary<string, string>();

        [QModPatch]
        public static void Load()
        {
            if (!LoadLanguageFile())
            {
                Logger.Log(Logger.Level.Error, $"Failed loading language file {languageFile}");
            }
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }

        // Attempt to get the text for a vehicle prompt.
        // VehicleName should be "Seamoth", "Exosuit" or "Cyclops". Any other value will return false.
        public static bool TryGetVehiclePrompt(string initialKey, string Language, string VehicleName, out string prompt)
        {
            string targetKey = Language + initialKey;
            bool result = vehiclePromptDict.TryGetValue(targetKey, out prompt);
            if (result)
            {
                prompt = prompt.Replace("<vehicle>", VehicleName);
            }

            Logger.Log(Logger.Level.Debug, $"Main.TryGetVehiclePrompt: got prompt value of {prompt} with key {targetKey}");

            //return vehiclePromptDict.TryGetValue(targetKey, out prompt);
            return result;
        }

        private static bool LoadLanguageFile()
        {
            if (!File.Exists(languageFile))
            {
                Logger.Log(Logger.Level.Error, "File does not exist");
                return false;
            }

            JsonData jsonData;
            using (StreamReader streamReader = new StreamReader(languageFile))
            {
                try
                {
                    jsonData = JsonMapper.ToObject(streamReader);
                    //Logger.Log(Logger.Level.Debug, $"Read JSON data of: {JsonConvert.SerializeObject(jsonData, Oculus.Newtonsoft.Json.Formatting.Indented)}");
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    return false;
                }
            }

            foreach (string text in jsonData.Keys)
            {
                Main.vehiclePromptDict[text] = (string)jsonData[text];
            }
            return true;
        }
    }
}