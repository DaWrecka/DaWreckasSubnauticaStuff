﻿using HarmonyLib;
using Logger = QModManager.Utility.Logger;
using CustomiseYourStorage;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using UWE;
using UnityEngine;
using System.IO;
using Common;

namespace CustomiseYourStorage.Patches
{
	internal class InventoryModMarker : MonoBehaviour
	{
		// This exists purely as a marker to say "Hey, you've already modified this container, don't need to do anything else"
		// As such, it doesn't have any custom code because it doesn't need any - it just has to *be*.
	}

	[HarmonyPatch(typeof(StorageContainer), "Awake")]
	internal class StoragePatcher
	{
		[HarmonyPostfix]
		private static void Postfix(ref StorageContainer __instance)
		{
			string name = __instance.gameObject.name;
			var techType = CraftData.GetTechType(__instance.gameObject);
			if (techType == TechType.Exosuit || techType == TechType.FiltrationMachine)
			{
				// the Exosuit and Filtration Machine require special processing, which we patch in their respective classes.
				return;
			}

			string ContainerID = (techType.ToString() + "." + name).Replace("(Clone)", "");
			string lowerID = ContainerID.ToLower();

#if SUBNAUTICA_STABLE
			if(lowerID == "escapepod.storagecontainer")
#elif BELOWZERO
			if (lowerID == "none.storagecontiner") //(sic)
#endif
			{
				// Special processing for the Lifepod storage locker; Note the mis-spelling of the BZ string above.
				//Vector2int newLifepodLockerSize = Main.config.LifepodLockerSize;
				int X = Main.config.DroppodWidth;
				int Y = Main.config.DroppodHeight;
#if !RELEASE
				Logger.Log(Logger.Level.Debug, $"Setting LifePod locker to size ({X}, {Y})"); 
#endif
				__instance.Resize(X, Y);
				if (Main.config.defaultLifepodLockerInventoryTypes.Count > 0)
				{
					CoroutineHost.StartCoroutine(AddLifepodInventory(__instance, Main.config.defaultLifepodLockerInventoryTypes));
				}
				return;
			}

#if SUBNAUTICA_STABLE
			if (lowerID == "none.submarine_locker_01_door")
			{
				int x = Main.config.CyclopsWidth;
				int y = Main.config.CyclopsHeight;
				Logger.Log(Logger.Level.Debug, $"Setting Cyclops locker to size ({x}, {y})");
				__instance.Resize(x, y);
				return;
			}
#endif

			if (techType == TechType.None)
			{
#if !RELEASE
				Logger.Log(Logger.Level.Debug, $"Container {name} has TechType of None and is not the Lifepod locker, skipping"); 
#endif
				return;
			}

			if (Main.StorageBlacklist.Contains(techType))
			{
#if !RELEASE
				Logger.Log(Logger.Level.Debug, $"Container TechType {techType} present on blacklist, skipping"); 
#endif
				return;
			}

			// We allow the identification strings in the config to be mixed-case for the sake of readability.
			// It makes this function a tiny bit harder, but makes life easier for the end-user, since they don't need to worry about case.

			Vector2int NewSize;
			//if (Main.config.TryGetModSize(ContainerID, out NewSize))
			if (Main.config.TryGetModSize(lowerID, out NewSize))
			{
#if !RELEASE
				Logger.Log(Logger.Level.Debug, $"Configuration for storage container {ContainerID} was found with value of ({NewSize.x}, {NewSize.y})"); 
#endif
				__instance.Resize(NewSize.x, NewSize.y);
				return;
			}

			if (NewSize.x != 0 && NewSize.y != 0)
			{
				// TryGetModSize returned false, but with a non-zero vector; this should mean that the value was found, but with default value.
#if !RELEASE
				Logger.Log(Logger.Level.Debug, $"Storage container {ContainerID} was found, but with default values; nothing to do."); 
#endif
				return;
			}
#if !RELEASE

			Logger.Log(Logger.Level.Info, $"Storage container identifier {ContainerID} was not found in configuration settings; using default values"); 
#endif
			Main.config.AddContainer(lowerID, __instance.width, __instance.height);
		}

		public static IEnumerator AddLifepodInventory(StorageContainer container, List<TechType> newTechTypes)
		{
			if (!Main.config.useDropPodInventory)
				yield break;

			if (AlreadyInitialised())
				yield break;

			foreach (TechType tt in newTechTypes)
			{
				Log.LogDebug($"Adding item {tt.AsString()} to drop pod locker");
#if SUBNAUTICA_STABLE
				GameObject go = CraftData.InstantiateFromPrefab(tt);
				InventoryItem inventoryItem2 = new InventoryItem(go.GetComponent<Pickupable>());
#elif BELOWZERO
				TaskResult<GameObject> result = new TaskResult<GameObject>();
				yield return CraftData.InstantiateFromPrefabAsync(tt, result, false);
				InventoryItem inventoryItem2 = new InventoryItem(result.Get().GetComponent<Pickupable>());
#endif

				inventoryItem2.item.Initialize();
				container.container.UnsafeAdd(inventoryItem2);
			}
			yield break;
		}

		static bool AlreadyInitialised()
		{
			var file = Path.Combine(SaveLoadManager.GetTemporarySavePath(), "CustomisedStorageInit");
			if (File.Exists(file))
			{
				// already initialized, return to prevent from spawn duplications.
				Log.LogDebug("Customised Storage already initialized in the current save.");
				return true;
			}

			File.Create(file);

			Log.LogDebug("Customised Storage newly-initialized in the current save.");
			return false;
		}
	}

	[HarmonyPatch(typeof(SeamothStorageContainer), "Init")]
	class SeamothStorageContainer_Init_Patch
	{
		[HarmonyPostfix]
		private static void Postfix(SeamothStorageContainer __instance)
		{
			string name = __instance.gameObject.name;
			var techType = CraftData.GetTechType(__instance.gameObject);
			string ContainerID = (techType.ToString() + "." + name).Replace("(Clone)", "");
			string lowerID = ContainerID.ToLower();

			if (techType == TechType.None)
			{
#if !RELEASE
				Logger.Log(Logger.Level.Info, $"Container {name} has TechType of None and is not the Lifepod locker, skipping"); 
#endif
				return;
			}


			Vector2int NewSize;
			if (Main.config.TryGetModSize(ContainerID, out NewSize))
			{
#if !RELEASE
				Logger.Log(Logger.Level.Debug, $"Configuration for items container {ContainerID} was found with value of ({NewSize.x}, {NewSize.y})"); 
#endif
				__instance.container.Resize(NewSize.x, NewSize.y);
				return;
			}

			if (NewSize.x * NewSize.y != 0)
			{
				// TryGetNewSize returned false, but with a non-zero vector; this should mean that the value was found, but with default value.
#if !RELEASE
				Logger.Log(Logger.Level.Debug, $"Items container {ContainerID} was found, but with default values; nothing to do."); 
#endif
				return;
			}
#if !RELEASE

			Logger.Log(Logger.Level.Info, $"Items container identifier {ContainerID} was not found in configuration settings; using default values"); 
#endif
			Main.config.AddContainer(lowerID, __instance.width, __instance.height);
		}
	}

	[HarmonyPatch(typeof(FiltrationMachine), "Start")]
	class FiltrationMachine_Start_Patch
	{
		private static void Postfix(FiltrationMachine __instance)
		{
			int maxSalt = Main.config.FiltrationSalt;
			int maxWater = Main.config.FiltrationWater;
			Vector2int newContainerSize = new Vector2int(Main.config.FiltrationWidth, Main.config.FiltrationHeight);
#if !RELEASE
			Logger.Log(Logger.Level.Debug, $"Reconfiguring Filtration Machine {__instance.gameObject.name} with configuration values of: maxSalt {maxSalt}, maxWater {maxWater}, new size ({newContainerSize.x}, {newContainerSize.y})"); 
#endif

			__instance.maxSalt = maxSalt;
			__instance.maxWater = maxWater;
			__instance.storageContainer.Resize(newContainerSize.x, newContainerSize.y);
		}
	}

	[HarmonyPatch(typeof(BaseBioReactor))]
	[HarmonyPatch("get_container")]
	class BaseBioReactor_get_container_Patch
	{
		private static readonly FieldInfo BaseBioReactor_container = typeof(BaseBioReactor).GetField("_container", BindingFlags.NonPublic | BindingFlags.Instance);

		private static void Postfix(BaseBioReactor __instance)
		{
			ItemsContainer container = (ItemsContainer)BaseBioReactor_container.GetValue(__instance);
			container.Resize(Main.config.BioreactorWidth, Main.config.BioreactorHeight);
		}
	}
}
