using HarmonyLib;
using Newtonsoft.Json;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace RecyclotronModSupport.Patches
{
    [HarmonyPatch(typeof(Recyclotron), nameof(Recyclotron.GetIngredients))]
    class Recyclotron_GetIngredients_Patch
    {
		private static readonly FieldInfo RecyclotronIngredients = typeof(Recyclotron).GetField("ingredients", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly FieldInfo RecyclotronBatteryTech = typeof(Recyclotron).GetField("batteryTech", BindingFlags.Static | BindingFlags.NonPublic);

		[HarmonyPrefix]
        public static bool Prefix(ref Recyclotron __instance, ref List<Ingredient> __result)
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