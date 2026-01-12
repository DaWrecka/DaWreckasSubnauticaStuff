#if NAUTILUS
using Nautilus.Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Common.NautilusHelper
{
	public abstract class ModPrefab
	{
		/*
		private static readonly Dictionary<string, ModPrefab> FileNameDictionary = new Dictionary<string, ModPrefab>(StringComparer.InvariantCultureIgnoreCase);
		private static readonly Dictionary<string, ModPrefab> ClassIdDictionary = new Dictionary<string, ModPrefab>(StringComparer.InvariantCultureIgnoreCase);
		private static readonly List<ModPrefab> PreFabsList = new List<ModPrefab>();
		internal static bool ModPrefabsPatched = false;

		internal static void Add(ModPrefab prefab)
		{
			FileNameDictionary.Add(prefab.PrefabFileName, prefab);
			ClassIdDictionary.Add(prefab.ClassID, prefab);
			PreFabsList.Add(prefab);
			ModPrefabsPatched = false;
		}

		internal static IEnumerable<ModPrefab> Prefabs => PreFabsList;
		internal static bool TryGetFromFileName(string classId, out ModPrefab prefab)
		{
			if (string.IsNullOrEmpty(classId))
			{
				prefab = null;
				return false;
			}
			return FileNameDictionary.TryGetValue(classId, out prefab);
		}

		internal static bool TryGetFromClassId(string classId, out ModPrefab prefab)
		{
			if (string.IsNullOrEmpty(classId))
			{
				prefab = null;
				return false;
			}
			return ClassIdDictionary.TryGetValue(classId, out prefab);
		}

		public string ClassID { get; protected set; }
		public string PrefabFileName { get; protected set; }
		public TechType TechType { get; protected set; }
		internal readonly Assembly Mod;

		protected ModPrefab(string classId, string prefabFileName, TechType techType = TechType.None)
		{
			this.ClassID = classId;
			this.PrefabFileName = prefabFileName;
			this.TechType = techType;

			Mod = GetType().Assembly;
		}

		protected virtual void ProcessPrefab(GameObject go)
		{
			if (go.activeInHierarchy) // inactive prefabs don't need to be removed by cache
				ModPrefabCache.AddPrefab(go);

			go.name = this.ClassID;

			if (this.TechType != TechType.None)
			{
				if (go.GetComponent<TechTag>() is TechTag tag)
				{
					tag.type = this.TechType;
				}

				if (go.GetComponent<Constructable>() is Constructable cs)
				{
					cs.techType = this.TechType;
				}
			}

			if (go.GetComponent<PrefabIdentifier>() is PrefabIdentifier pid)
			{
				pid.ClassId = this.ClassID;
			}
		}

		internal GameObject GetGameObjectInternal()
		{
			GameObject go = GetGameObject();
			if (go == null)
				return null;

			ProcessPrefab(go);
			return go;
		}

		internal IEnumerator GetGameObjectInternalAsync(IOut<GameObject> gameObject)
		{
			var taskResult = new TaskResult<GameObject>();
			yield return GetGameObjectAsync(taskResult);

			GameObject go = taskResult.Get();
			if (go == null)
				yield break;

			ProcessPrefab(go);
			gameObject.Set(go);
		}

		/// <summary>
		/// Gets the prefab game object. Set up your prefab components here.
		/// The <see cref="TechType"/> and ClassID are already handled.
		/// </summary>
		/// <returns>The game object to be instantiated into a new in-game entity.</returns>
		public virtual GameObject GetGameObject() => null;

		/// <summary>
		/// Gets the prefab game object asynchronously. Set up your prefab components here.
		/// The <see cref="TechType"/> and ClassID are already handled.
		/// </summary>
		/// <param name="gameObject"> The game object to be instantiated into a new in-game entity. </param>
		public virtual IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject) => null;
		*/
	}
}
#endif
