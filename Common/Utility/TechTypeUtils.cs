using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Common
{
    class TechTypeUtils
    {
		public static Dictionary<string, GameObject> ModPrefabs = new Dictionary<string, GameObject>();
		public static Dictionary<string, TechType> ModTechTypes = new Dictionary<string, TechType>();

		internal static void AddModTechType(TechType tech, GameObject prefab = null)
		{
			Log.LogDebug($"Adding mod TechType {tech.AsString()}");
			string key = tech.AsString(true);
			if (!ModTechTypes.ContainsKey(key))
			{
				ModTechTypes.Add(key, tech);
			}
			if (prefab != null)
			{
				ModPrefabs[key] = prefab;
			}
		}

		public static bool TryGetModTechType(string key, out TechType techType)
		{
			techType = GetModTechType(key);
			return (techType != TechType.None);
		}

		public static TechType GetModTechType(string key)
		{
			string lowerKey = key.ToLower();
			TechType tt;
			if (ModTechTypes.TryGetValue(lowerKey, out tt))
				return tt;

			return GetTechType(key);
		}

		internal static GameObject GetModPrefab(string key)
		{
			string lowerKey = key.ToLower();
			GameObject modPrefab;
			if (ModPrefabs.TryGetValue(lowerKey, out modPrefab))
				return modPrefab;

			return null;
		}
		// Useful function provided by PrimeSonic. Ta!
		public static TechType GetTechType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return TechType.None;

            // Look for a known TechType
            if (TechTypeExtensions.FromString(value, out TechType tType, true))
                return tType;

            //  Not one of the known TechTypes - is it registered with SMLHelper?
            if (TechTypeHandler.TryGetModdedTechType(value, out TechType custom))
                return custom;

            return TechType.None;
        }
    }
}
