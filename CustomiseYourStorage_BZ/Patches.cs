using HarmonyLib;
using Logger = QModManager.Utility.Logger;
using CustomiseYourStorage_BZ;
using System.Collections.Generic;
using System.Reflection;

namespace CustomiseYourStorage_BZ.Patches
{
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
			if (lowerID == "none.storagecontiner")
			{
				// Special processing for the Lifepod storage locker
				Vector2int newLifepodLockerSize = Main.config.LifepodLockerSize;
				Logger.Log(Logger.Level.Debug, $"Setting LifePod locker to size ({newLifepodLockerSize})");
				__instance.Resize(newLifepodLockerSize.x, newLifepodLockerSize.y);
				return;
			}

			if (techType == TechType.None)
			{
				Logger.Log(Logger.Level.Debug, $"Container {name} has TechType of None and is not the Lifepod locker, skipping");
				return;
			}

			if (Main.StorageBlacklist.Contains(techType))
			{
				Logger.Log(Logger.Level.Debug, $"Container TechType {techType} present on blacklist, skipping");
				return;
			}

			// We allow the identification strings in the config to be mixed-case for the sake of readability.
			// It makes this function a little harder, but makes life easier for the end-user, since they don't need to worry about case.

			Vector2int NewSize;
			//if (Main.config.TryGetModSize(ContainerID, out NewSize))
			if (Main.config.TryGetModSize(lowerID, out NewSize))
			{
				Logger.Log(Logger.Level.Debug, $"Configuration for storage container {ContainerID} was found with value of ({NewSize.ToString()})");
				__instance.Resize(NewSize.x, NewSize.y);
				return;
			}

			if (NewSize.x != 0 && NewSize.y != 0)
			{
				// TryGetModSize returned false, but with a non-zero vector; this should mean that the value was found, but with default value.
				Logger.Log(Logger.Level.Debug, $"Storage container {ContainerID} was found, but with default values; nothing to do.");
				return;
			}

			Logger.Log(Logger.Level.Info, $"Storage container identifier {ContainerID} was not found in configuration settings; using default values");
			Main.config.AddContainer(lowerID, __instance.width, __instance.height);
		}
	}

	[HarmonyPatch(typeof(SeamothStorageContainer), "Init")]
	class SeamothStorageContainer_Init_Patch
	{
		[HarmonyPostfix]
		private static void Postfix(SeamothStorageContainer __instance)
		{
			/*__instance.width = Mod.config.SeamothStorage.width;
			__instance.height = Mod.config.SeamothStorage.height;*/

			string name = __instance.gameObject.name;
			var techType = CraftData.GetTechType(__instance.gameObject);
			string ContainerID = (techType.ToString() + "." + name).Replace("(Clone)", "");
			string lowerID = ContainerID.ToLower();

			if (techType == TechType.None)
			{
				Logger.Log(Logger.Level.Info, $"Container {name} has TechType of None and is not the Lifepod locker, skipping");
				return;
			}

			// We allow the identification strings in the config to be mixed-case for the sake of readability.
			// It makes this function a little harder, but makes life easier for the end-user, since they don't need to worry about case.

			Vector2int NewSize;
			if (Main.config.TryGetModSize(ContainerID, out NewSize))
			//if (Main.config.TryGetModSize(lowerID, out NewSize))
			{
				Logger.Log(Logger.Level.Debug, $"Configuration for items container {ContainerID} was found with value of ({NewSize.ToString()})");
				__instance.container.Resize(NewSize.x, NewSize.y);
				return;
			}

			if (NewSize.x * NewSize.y != 0)
			{
				// TryGetNewSize returned false, but with a non-zero vector; this should mean that the value was found, but with default value.
				Logger.Log(Logger.Level.Debug, $"Items container {ContainerID} was found, but with default values; nothing to do.");
				return;
			}

			Logger.Log(Logger.Level.Info, $"Items container identifier {ContainerID} was not found in configuration settings; using default values");
			Main.config.AddContainer(lowerID, __instance.width, __instance.height);
		}
	}

	[HarmonyPatch(typeof(FiltrationMachine), "Start")]
	class FiltrationMachine_Start_Patch
	{
		private static void Postfix(FiltrationMachine __instance)
		{
			int maxSalt = Main.config.FiltrationConfig.maxSalt;
			int maxWater = Main.config.FiltrationConfig.maxWater;
			Vector2int newContainerSize = Main.config.FiltrationConfig.containerSize;
			Logger.Log(Logger.Level.Debug, $"Reconfiguring Filtration Machine {__instance.gameObject.name} with configuration values of: maxSalt {maxSalt}, maxWater {maxWater}, new size ({newContainerSize.ToString()})");

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
			container.Resize(Main.config.BioreactorSize.x, Main.config.BioreactorSize.y);
		}
	}
}
