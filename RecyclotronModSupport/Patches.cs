using Common;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace RecyclotronModSupport.Patches
{
    [HarmonyPatch(typeof(Recyclotron))]
    class Recyclotron_GetIngredients_Patch
    {
		private static readonly FieldInfo RecyclotronIngredients = typeof(Recyclotron).GetField("ingredients", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly FieldInfo RecyclotronBatteryTech = typeof(Recyclotron).GetField("batteryTech", BindingFlags.Static | BindingFlags.NonPublic);
		/*private static readonly MethodInfo IsUsedBatteryMethod = typeof(Recyclotron).GetMethod("IsUsedBattery", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly MethodInfo IsUsedTorpedoArmMethod = typeof(Recyclotron).GetMethod("IsUsedBattery", BindingFlags.Instance | BindingFlags.NonPublic);

		private static bool IsUsedBattery(GameObject obj)
		{
			Battery component = obj.GetComponent<Battery>();
			return component != null && (double)component.charge < (double)component.capacity * 0.97;
		}

		private static bool IsUsedTorpedoArm(Pickupable p)
		{
			if (p.GetTechType() == TechType.ExosuitTorpedoArmModule)
			{
				SeamothStorageContainer component = p.GetComponent<SeamothStorageContainer>();
				return component != null && component.container.GetCount(TechType.WhirlpoolTorpedo) != 2;
			}
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch("IsAllowedToAdd")]
		public static bool IsAllowedToAdd(ref Recyclotron __instance, bool __result, Pickupable pickupable, bool verbose)
		{
			string text = null;
			bool flag;
			TechType pickupTech = pickupable.GetTechType();
			ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(pickupTech);
			RecipeData recipeData = CraftDataHandler.GetRecipeData(pickupTech);
			Log.LogDebug($"Prefix IsAllowedToAdd executing: Pickupable TechType is {pickupTech.AsString()}");
			Log.LogDebug("TechData Ingredients list is as follows: " + (ingredients == null ? "null" : JsonConvert.SerializeObject(ingredients, Formatting.Indented, new StringEnumConverter()
			{
				NamingStrategy = new CamelCaseNamingStrategy(),
				AllowIntegerValues = true
			})));
			Log.LogDebug("CraftDataHandler Ingredients list is as follows: " + (recipeData == null ? "null" : JsonConvert.SerializeObject(recipeData, Formatting.Indented, new StringEnumConverter()
			{
				NamingStrategy = new CamelCaseNamingStrategy(),
				AllowIntegerValues = true
			})));

			if (Recyclotron.bannedTech.Contains(pickupTech) || TechData.GetCraftAmount(pickupTech) > 1)
			{
				Log.LogDebug($"Item is banned tech, or has craftAmount > 1");
				text = "RecyclotronErrorItemNotAllowed";
				flag = false;
			}
			else if (__instance.storageContainer.container.count > 0)
			{
				Log.LogDebug($"Recyclotron is not empty");
				text = "RecyclotronErrorNotEmpty";
				flag = false;
			}
			else if (!__instance.storageContainer.container.HasRoomForComponents(pickupTech))
			{
				Log.LogDebug("Not enough storage space for components");
				text = "RecyclotronErrorNotEnoughSpace";
				flag = false;
			}
			else if (IsUsedBattery(pickupable.gameObject) || IsUsedTorpedoArm(pickupable))
			{
				Log.LogDebug("Item is or contains used battery or torpedo arm");
				text = "RecyclotronErrorUsedItem";
				flag = false;
			}
			else
			{
				flag = (recipeData != null && recipeData.Ingredients.Count > 0);
				if (!flag)
				{
					text = "RecyclotronErrorItemNotAllowed";
					Log.LogDebug("Could not get recipe for TechType");
				}
			}
			if (!string.IsNullOrEmpty(text) && verbose)
			{
				ErrorMessage.AddMessage(Language.main.Get(text));
			}
			__result = flag;
			return false;
		}*/


		[HarmonyPrefix]
		[HarmonyPatch(nameof(Recyclotron.GetIngredients))]
        public static bool PreGetIngredients(ref Recyclotron __instance, ref List<Ingredient> __result)
        {
#if !RELEASE
			Logger.Log(Logger.Level.Debug, $"Recyclotron_GetIngredients_Patch executing"); 
#endif
			List<Ingredient> ingredients = (List<Ingredient>)RecyclotronIngredients.GetValue(null);
			if (ingredients == null)
			{
#if !RELEASE
				Logger.Log(Logger.Level.Error, $"Failed to acquire List ingredients through Reflection"); 
#endif
				return true;
			}

			Logger.Log(Logger.Level.Debug, $"List ingredients received through Reflection"); 
			HashSet<TechType> batteryTech = (HashSet<TechType>)RecyclotronBatteryTech.GetValue(null);
			if (batteryTech == null)
			{
#if !RELEASE
				Logger.Log(Logger.Level.Debug, $"Failed to acquire HashSet batteryTech through Reflection"); 
#endif
				return true;
			}

			Logger.Log(Logger.Level.Debug, $"HashSet batteryTech received through Reflection"); 
#if !RELEASE
			Logger.Log(Logger.Level.Debug, $"Reflection values received"); 
#endif

			ingredients.Clear();
			if (__instance.GetWasteCount() == 1)
			{
#if !RELEASE
				Logger.Log(Logger.Level.Debug, $"Recyclotron found one item in waste list");
#endif
				Pickupable pickup = __instance.GetCurrent().inventoryItem.item;
				GameObject gameObject = pickup.gameObject;

				if (gameObject)
				{
					TechType tt = pickup.GetTechType();
					Logger.Log(Logger.Level.Debug, $"Item in waste list has TechType {tt.AsString(false)}"); 
					ReadOnlyCollection<Ingredient> readOnlyCollection = TechData.GetIngredients(tt);
					if (readOnlyCollection == null) // Try the SMLHelper method instead
					{
#if !RELEASE
						Logger.Log(Logger.Level.Debug, $"TechData.GetIngredients failed for TechType {tt.AsString(false)}, attempting SMLHelper"); 
#endif
						List<Ingredient> ingredientsList = CraftDataHandler.GetRecipeData(tt)?.Ingredients;
						if (ingredientsList != null)
							readOnlyCollection = new ReadOnlyCollection<Ingredient>(ingredientsList);
						else
						{
#if !RELEASE
							Logger.Log(Logger.Level.Debug, $"Failed to get ingredients list for TechType {tt.AsString(false)} using SMLHelper"); 
#endif
						}
					}
					if (readOnlyCollection != null)
					{
						foreach (Ingredient ingredient in readOnlyCollection)
						{
#if !RELEASE
							Logger.Log(Logger.Level.Debug, $"Processing Ingredients member {ingredient.techType}"); 
#endif
							if (!batteryTech.Contains(ingredient.techType))
							{
								ingredients.Add(ingredient);
							}
						}
					}
					EnergyMixin component = gameObject.GetComponent<EnergyMixin>();
					if (component)
					{
						GameObject battery = component.GetBattery();
						if (battery)
						{
							ingredients.Add(new Ingredient(CraftData.GetTechType(battery), 1));
						}
					}
				}
			}
			__result = ingredients;
			return false;
		}
	}
}