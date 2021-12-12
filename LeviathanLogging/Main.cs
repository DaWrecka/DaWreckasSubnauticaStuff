using Common;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Json;
using SMLHelper.V2.Json.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LeviathanLogging
{
    [FileName("LeviathanData")]
    internal class LeviathanDataFile : SaveDataCache
    {
        internal struct LeviathanRecord
        {
            public string creatureType;
            public Vector3 leashPosition;

            public LeviathanRecord(string newType, Vector3 newLeash)
            {
                creatureType = newType;
                leashPosition = newLeash;
            }
        }

        internal Dictionary<string, LeviathanRecord> LeviathanData;

        internal void Init()
        {
            if (LeviathanData == null)
                LeviathanData = new Dictionary<string, LeviathanRecord>();
        }

        internal void OnExit()
        {
            Save();
        }

        internal bool AddLeviathan(string Id, string creature, Vector3 newLeash)
        {
                Log.LogDebug($"Recording Leviathan data:             Id {Id}, creatureType '{creature.PadLeft(20)}', leashPosition {newLeash.ToString()} ");

            if (LeviathanData.TryGetValue(Id, out LeviathanRecord value))
            {
                Log.LogWarning($"Leviathan data already recorded for id {Id}: creatureType '{value.creatureType.PadLeft(20)}', leashPosition = {value.leashPosition.ToString()}");
                return false;
            }

            LeviathanData.Add(Id, new LeviathanRecord(creature, newLeash));
            Save();
            return true;
        }
    }

    [QModCore]
    public class Main
    {
        internal const string version = "1.0.0.0";

        internal static LeviathanDataFile saveData;
        //internal static LeviathanDataFile config { get; } = OptionsPanelHandler.RegisterModOptions<LeviathanDataFile>();

        [QModPatch]
        public static void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
            saveData = SaveDataHandler.Main.RegisterSaveDataCache<LeviathanDataFile>();
            saveData.Init();
        }
    }
}
