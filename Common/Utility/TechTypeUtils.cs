using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Common
{
    public static class TechTypeUtils
    {
		private static Dictionary<string, GameObject> ModPrefabs = new Dictionary<string, GameObject>();
		private static Dictionary<string, TechType> ModTechTypes = new Dictionary<string, TechType>();

		public static void AddModTechType(TechType tech, GameObject prefab = null)
		{
			if (tech == TechType.None)
			{
				Log.LogError("AddModTechType called with TechType None!");
				return;
			}

			Log.LogDebug($"Adding mod TechType {tech.AsString()}");
			string key = tech.AsString(true);
			if (ModTechTypes.ContainsKey(key))
			{
				// Okay, so there's two possibilities here; one, AddModTechType is being called for the same TechType, and only the TechType, multiple times, which is an error.
				// Two, AddModTechType is being called a second time for a TechType to add a prefab. This is not an error.
				if (prefab == null)
				{
					Log.LogError($"AddModTechType called multiple times for key '{key}'");
					return;
				}
			}
			else
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
			if (ModPrefabs.TryGetValue(lowerKey, out GameObject modPrefab))
				return modPrefab;

			return null;
		}

		internal static bool TryGetModPrefab(string key, out GameObject modPrefab)
		{
			string lowerKey = key.ToLower();
			return ModPrefabs.TryGetValue(lowerKey, out modPrefab);
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
