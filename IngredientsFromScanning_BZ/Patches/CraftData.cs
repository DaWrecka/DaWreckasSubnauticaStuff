using HarmonyLib;
#if NAUTILUS
using Nautilus.Crafting;
using Nautilus.Handlers;
using Ingredient = CraftData.Ingredient;
#else
    using SMLHelper.V2.Crafting;
    using SMLHelper.V2.Handlers;
    #if SN1
        using RecipeData = SMLHelper.V2.Crafting.TechData;
    #endif
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Nautilus.Extensions;
#if QMM
using Logger = QModManager.Utility.Logger;
#endif


#if SUBNAUTICA_LEGACY
using Json = Oculus.Newtonsoft.Json;
#else
using Json = Newtonsoft.Json;
#endif

using Common;

// Based HEAVILY on MrPurple6411's Copper from Scan code. For "based heavily" read "nicked almost wholesale".

namespace PartsFromScanning.Patches
{

    [HarmonyPatch(typeof(CraftData))]
    internal class CraftData_Patch
    {
        private static readonly HashSet<TechType> blacklistedItems = new() // Items that will not appear in the final recipe
        {
            TechType.AcidMushroom,
            TechType.WhiteMushroom,
            TechType.StalkerTooth,
            TechType.Floater,
            TechType.Bladderfish,
            TechType.GasPod,
        };

        private static readonly HashSet<TechType> nonSimplifyItems = new() // Items that won't be simplified
        {
            TechType.Titanium,
            TechType.Copper,
            TechType.Silver,
            TechType.Gold,
            TechType.Glass,
            TechType.Lubricant,
            TechType.Benzene,
            TechType.Silicone,
            TechType.HydrochloricAcid,
            TechType.ReactorRod,
            TechType.Polyaniline,
            TechType.PrecursorKey_Blue,
            TechType.PrecursorKey_Purple,
            TechType.PrecursorKey_Orange,
            TechType.ComputerChip,
            TechType.Aerogel
        };

        private static readonly Dictionary<string, string> TechTypeOverrides = new()
        {
            { "TechPistolFragment", "TechPistol" }

        };

        private static TechType GetOverrideTechType(TechType query)
        {
            if (TechTypeOverrides.TryGetValue(query.ToString(), out string value))
            {
                TechType overrideTech = TechTypeUtils.GetTechType(value);
                return overrideTech;
            }

            return query;
        }

        protected static RecipeData AgnosticGetRecipe(TechType tt)
        {
            tt = GetOverrideTechType(tt);
            Log.LogDebug($"Retrieving recipe for TechType {tt.ToString()}");

            if (tt == TechType.None)
            {
                Log.LogError($"AgnosticGetRecipe: No TechType specified");
                return null;
            }
#if NAUTILUS
            var recipe = CraftDataHandler.GetRecipeData(tt);
            if (recipe == null)
                recipe = CraftDataHandler.GetModdedRecipeData(tt);

            if (recipe == null)
            {
                
            }
            return recipe;
            /*
            var recipe = CraftData.Get(tt);

            if (recipe == null)
            {
                return null;
            }
            return recipe.ConvertToRecipeData();*/


#elif SN1
            var recipe = CraftDataHandler.GetTechData(tt);
            return recipe;
#elif BELOWZERO
            var recipe = CraftDataHandler.GetRecipeData(tt);
            return recipe;
#endif
        }

        struct WeightedItem
        {
            public WeightedItem(float w, TechType t)
            {
                this.Weight = w;
                this.tech = t;
            }
            public float Weight { get; private set; }
            public TechType tech { get; private set; }

        }

        private static void AddInventory(TechType techType, int count = 1, bool bNoMessage = false, bool bSpawnIfCantAdd = true)
        {
            // Ripped<cough>based upon MrPurple6411's method Deconstruct_Patch from the BuilderModule
            if (Player.main.isPiloting)
            {
#if ASYNC
                GameObject gameObject = CraftData.InstantiateFromPrefab(null, techType, false); // Coming back to this months later, I didn't think this worked in BZ, because it's not async. But apparently it does...
#else
                //GameObject gameObject = CraftData.InstantiateFromPrefab(techType, false);
                GameObject prefabForTechType = CraftData.GetPrefabForTechType(techType, true);
                GameObject gameObject = (prefabForTechType != null) ? global::Utils.SpawnFromPrefab(prefabForTechType, null) : global::Utils.CreateGenericLoot(techType);
#endif
                Pickupable pickup = gameObject.GetComponent<Pickupable>();
                if (pickup != null)
                {
                    pickup.Initialize();

                    // This is kind of messy but it's an easy way to get the cross-game code running. In SN1 modules will always == null so the block won't run; but it'll still compile.
                    Vehicle thisVehicle = Player.main.GetVehicle();
#if SN1
                    if (thisVehicle != null)
#elif BELOWZERO
                    SeaTruckUpgrades upgrades = Player.main.GetComponentInParent<SeaTruckUpgrades>();
                    Equipment modules = upgrades?.modules;
                    if (modules != null && TechTypeHandler.TryGetModdedTechType("SeaTruckStorage", out TechType storageType))
                    {
                        //HashSet<string> TruckSlotIDs = modules.equipment.Keys.ToSet<string>();
                        List<string> TruckSlotIDs = null;
                        modules.GetSlots(EquipmentType.SeaTruckModule, TruckSlotIDs);
                        foreach (string slot in TruckSlotIDs)
                        {
                            InventoryItem item = modules.GetItemInSlot(slot);
                            if (item.item.GetTechType() == storageType)
                            {
                                //InventoryItem item = modules.GetItemInSlot(slot);

                                if (item.item.TryGetComponent(out SeamothStorageContainer seamothStorageContainer))
                                {
                                    InventoryItem newItem = new InventoryItem(pickup);
                                    if (seamothStorageContainer.container.AddItem(newItem) != null)
                                    {
                                        string name = Language.main.Get(pickup.GetTechName());
                                        ErrorMessage.AddMessage(Language.main.GetFormat("VehicleAddedToStorage", name));

                                        //uGUI_IconNotifier.main.Play(pickup.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                                        uGUI_IconNotifier.main.Play(techType, uGUI_IconNotifier.AnimationType.From, null);
                                        pickup.PlayPickupSound();
                                        Log.LogDebug($"Adding tech {techType} to Seatruck storage");
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    else if(thisVehicle != null)
#endif
                    {
                        if (thisVehicle is Exosuit exo)
                        {
                            StorageContainer storageContainer = exo.storageContainer;

                            if (storageContainer != null)
                            {
                                int lastCount = storageContainer.container.GetCount(techType);
                                storageContainer.container.AddItem(pickup);
                                int techCount = storageContainer.container.GetCount(techType);
#if !RELEASE
                                Log.LogDebug($"Adding tech {techType.AsString().PadLeft(15)} to Exosuit storage; previous count {lastCount}, new count {techCount}");
#endif
                                if (techCount - lastCount == 1)
                                {
                                    string name = Language.main.Get(pickup.GetTechName());
                                    ErrorMessage.AddMessage(Language.main.GetFormat("VehicleAddedToStorage", name));

                                    uGUI_IconNotifier.main.Play(pickup.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                                    pickup.PlayPickupSound();
                                    return;
                                }
                            }
                        }

                        else if (thisVehicle is SeaMoth seamoth)
                        {
                            // We don't need to do any excluding here for BZ; normally this block will never run, and if it does it's because the user has taken steps
                            
                            List<IItemsContainer> containers = new List<IItemsContainer>();
                            seamoth.GetAllStorages(containers);
                            //for (int i = 0; i < 12; i++)
                            InventoryItem newItem = new InventoryItem(pickup);
                            foreach (IItemsContainer storage in containers)
                            {
                                try
                                {
                                    //ItemsContainer storage = seamoth.GetStorageInSlot(i, TechType.VehicleStorageModule);
                                    if (storage is ItemsContainer iContainer)
                                    {
                                        int lastCount = iContainer.GetCount(techType);
                                        iContainer.AddItem(pickup);
                                        int techCount = iContainer.GetCount(techType);
#if !RELEASE
                                        Log.LogDebug($"Adding tech {techType.AsString().PadLeft(15)} to Seamoth storage; previous count {lastCount}, new count {techCount}");
#endif
                                        if (techCount - lastCount == 1)
                                        {
                                            string name = Language.main.Get(pickup.GetTechName());
                                            ErrorMessage.AddMessage(Language.main.GetFormat("VehicleAddedToStorage", name));
                                            uGUI_IconNotifier.main.Play(pickup.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);

                                            pickup.PlayPickupSound();
                                            //storageCheck = true;
                                            return;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
#if !RELEASE
                                    Log.LogDebug($"Exception adding tech {techType} to Seamoth storage: {e.ToString()}");
#endif
                                    continue;
                                }
                            }
                            /*if (storageCheck)
                            {
                                return;
                            }*/
                        }
                    }
                }
            }
#if !RELEASE
            Log.LogDebug($"Adding tech {techType} to player inventory");
#endif
            CraftData.AddToInventory(techType, count, bNoMessage, bSpawnIfCantAdd);
        }

        private static List<Ingredient> GetIngredients(TechType tt)
        {
            var recipe = AgnosticGetRecipe(tt);
            if (recipe == null)
                return null;

            return recipe.Ingredients;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CraftData.AddToInventory))]
        private static bool PreAddToInventory(TechType techType, int num = 1, bool noMessage = false, bool spawnIfCantAdd = true)
        {
            if (techType == TechType.Titanium && num == 2 && !noMessage && spawnIfCantAdd)
            {
                TechType scannedFragment = TechType.None;
                RecipeData recipe = null;
                RecipeData baseRecipe = null;
                if (BlueprintHandTargetPatches.bIsDatabox)
                {
                    //if (!Main.config.bInterceptDataboxes)
                    //    return true;

                    scannedFragment = BlueprintHandTargetPatches.databoxUnlock;
                    if (PartsFromScanningPlugin.config.TryOverrideRecipe(scannedFragment, out baseRecipe))
                    {
#if !RELEASE
                        Log.LogDebug($"Using OverrideRecipe: {Json.JsonConvert.SerializeObject(baseRecipe, Json.Formatting.Indented)}");
#endif
                    }
                    else
                    {
                        baseRecipe = AgnosticGetRecipe(scannedFragment);
                    }
                }
                else
                {
                    var scanTarget = PDAScanner.scanTarget;
                    scannedFragment = scanTarget.techType;
#if !RELEASE
                    Log.LogDebug($"Intercepting scan of fragment {scannedFragment.ToString()}");
#endif

                    if (PartsFromScanningPlugin.config.TryOverrideRecipe(scannedFragment, out baseRecipe))
                    {
#if !RELEASE
                        Log.LogDebug($"Using OverrideRecipe: {Json.JsonConvert.SerializeObject(baseRecipe, Json.Formatting.Indented)}");
#endif
                    }
                    else if ((int)scannedFragment > 1112 && (int)scannedFragment < 1117)
                    {
                        // TechTypes 1113 to 1116 are Cyclops fragments, which have no blueprint associated, so we need to process them specially.
                        baseRecipe = new RecipeData();
                        /*CyclopsHullFragment = 1113,
                        CyclopsBridgeFragment = 1114,
                        CyclopsEngineFragment = 1115,
                        CyclopsDockingBayFragment = 1116,*/

                        switch ((int)scannedFragment)
                        {
                            case 1113:
                                baseRecipe.Ingredients.Add(new Ingredient(TechType.PlasteelIngot, 2));
                                baseRecipe.Ingredients.Add(new Ingredient(TechType.Lead, 1));
                                break;
                            case 1114:
                                baseRecipe.Ingredients.Add(new Ingredient(TechType.EnameledGlass, 3));
                                break;
                            case 1115:
                                baseRecipe.Ingredients.Add(new Ingredient(TechType.Lubricant, 1));
                                baseRecipe.Ingredients.Add(new Ingredient(TechType.AdvancedWiringKit, 1));
                                baseRecipe.Ingredients.Add(new Ingredient(TechType.Lead, 1));
                                break;
                            case 1116:
                                baseRecipe.Ingredients.Add(new Ingredient(TechType.PlasteelIngot, 2));
                                break;
                        }
                        baseRecipe.Ingredients.Add(new Ingredient(TechType.Lead, 1));
                        baseRecipe.Ingredients.Add(new Ingredient(TechType.PlasteelIngot, 1));
#if !RELEASE
                        Log.LogDebug($"Using recipe from manual override: {Json.JsonConvert.SerializeObject(baseRecipe, Json.Formatting.Indented)}");
#endif
                    }
                    else
                    {
                        PDAScanner.EntryData entryData = PDAScanner.GetEntryData(scannedFragment);
                        if (entryData == null) // Sanity check; this should always be true
                        {
#if !RELEASE
                            Log.LogDebug($"Failed to find EntryData for fragment");
#endif
                            /*CraftData.AddToInventory(TechType.Titanium);
                            CraftData.AddToInventory(TechType.Titanium); // Adding them one-by-one so as to prevent it being caught by this very routine.*/
                            return true;
                        }
                        //Log.LogDebug($"Found entryData {entryData.ToString()}");
#if !RELEASE
                        Log.LogDebug($"Found entryData {Json.JsonConvert.SerializeObject(entryData, Json.Formatting.Indented)}");
#endif


                        //CraftData.AddToInventory(TechType.Titanium);
                        //CraftData.AddToInventory(TechType.Copper);
                        baseRecipe = AgnosticGetRecipe(entryData.blueprint);
                        if (baseRecipe == null)
                        {
#if !RELEASE
                            Log.LogDebug($"Failed to find blueprint for EntryData");
#endif
                            /*CraftData.AddToInventory(TechType.Titanium);
                            CraftData.AddToInventory(TechType.Titanium); // One-by-one again, as above.*/



                            return true;
                        }
                        //Log.LogDebug($"Found recipe {recipe.ToString()}");
#if !RELEASE
                        Log.LogDebug($"Using recipe from EntryData: {Json.JsonConvert.SerializeObject(baseRecipe, Json.Formatting.Indented)}");
#endif
                    }
                }

                recipe = GetSimplifiedRecipe(baseRecipe);
                // I believe the easiest way to get a random item from the blueprint would be to make a list of techTypes; if an ingredient is used twice in the recipe, it will appear in the list twice.
                // That way, we can generate a random number where 0<=rnd<list.count, and select that item.
                List<TechType> bp = new List<TechType>();
                for (int i = 0; i < recipe.Ingredients.Count; i++)
                {
                    for (int j = 0; j < recipe.Ingredients[i].amount; j++)
                        bp.Add(recipe.Ingredients[i].techType);
                }

                // Now build up weights
                List<WeightedItem> BlueprintPairs = new List<WeightedItem>();
                float TotalWeight = 0f;
                //Logger.Log(Logger.Level.Error, "Unidentified Vehicle Type!");
                for (int i = 0; i < bp.Count; i++)
                {
                    float thisWeight = PartsFromScanningPlugin.config.GetWeightForTechType(bp[i]);
                    TotalWeight += thisWeight;
                    WeightedItem thisWeightedItem = new WeightedItem(TotalWeight, bp[i]);
#if !RELEASE
                    Log.LogDebug($"Adding item to drop list, TechType = {thisWeightedItem.tech.ToString()},   this weight = {thisWeight}, cumulative weight = {thisWeightedItem.Weight}"); 
#endif
                    BlueprintPairs.Add(thisWeightedItem);
                }

                // Now we should be able to pick a few random numbers between 0 and the list's total weight, and add those. We want to remove that entry afterwards, but that's not a big ask.
                System.Random rng = new System.Random();
                int numIngredients = Math.Min(PartsFromScanning.PartsFromScanningPlugin.config.GenerateGiftValue(), BlueprintPairs.Count);
#if !RELEASE
                Log.LogDebug($"Generated a value for this scan of {numIngredients} components."); 
#endif

                int awards = 0;
                double r;
                for (int i = 0; i < numIngredients && BlueprintPairs.Count > 0; i++)
                {
                    r = rng.NextDouble() * TotalWeight;
                    for (int j = 0; j < BlueprintPairs.Count; j++)
                    {
                        //                                               This part is for sanity checking
                        //                                   ___________________________|_______________________________
                        //                                  /                                                           \
                        if (r < BlueprintPairs[j].Weight || ((j + 1) == BlueprintPairs.Count && awards < numIngredients))
                        {
                            AddInventory(BlueprintPairs[j].tech, 1, false, true);
                            //CraftData.AddToInventory(BlueprintPairs[j].tech, 1, false, true);
                            awards++;
                            TotalWeight -= PartsFromScanningPlugin.config.GetWeightForTechType(BlueprintPairs[j].tech);
                            BlueprintPairs.RemoveAt(j);
                            break;
                        }
                    }
                }
                return false;
            }
            return true;
        }

        private static RecipeData GetSimplifiedRecipe(RecipeData baseRecipe)
        {
            System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            if (baseRecipe == null)
            {
                Log.LogError($"{thisMethod.ReflectedType.Name}.{thisMethod.Name} called with null recipe!");
                return null;
            }

            if (baseRecipe.Ingredients == null || baseRecipe.Ingredients.Count < 1)
            {
                Log.LogError($"{thisMethod.ReflectedType.Name}.{thisMethod.Name} called with invalid recipe!");
                return null;
            }

            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");

            var recipe = new RecipeData(baseRecipe.Ingredients);
            recipe.craftAmount = baseRecipe.craftAmount;
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                Ingredient baseIngredient = baseRecipe.Ingredients[i];
                TechType item = recipe.Ingredients[i].techType;

                if (blacklistedItems.Contains(item))
                {
                    recipe.Ingredients.RemoveAt(i--);
                    continue;
                }
                if (nonSimplifyItems.Contains(item))
                {
                    Log.LogDebug($"Using Ingredient(techType: {item.AsString()}, amount: {baseIngredient.amount}");
                    continue;
                }

                var subRecipe = AgnosticGetRecipe(item);
                if (subRecipe != null)
                {
                    Log.LogDebug($" Found sub-recipe for techType {item.AsString()}");
                    foreach (Ingredient I in subRecipe.Ingredients)
                    {
                        if (blacklistedItems.Contains(I.techType))
                            continue;
                        // This line doesn't work with Nautilus, as the amount property is read-only.
                        //I.amount *= baseIngredient.amount;
                        Log.LogDebug($"     Adding sub-Ingredient(techType: {I.techType.AsString()}, amount: {I.amount}");
                        for(int j = 0; j < I.amount; j++)
                            recipe.Ingredients.Add(I);
                    }
                    recipe.Ingredients.RemoveAt(i--);
                }
                else
                {
                    Log.LogDebug($"No sub-recipe found for Ingredient(techType: {item.AsString()}, amount: {baseIngredient.amount}");
                }
            }

            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                if (PartsFromScanning.PartsFromScanningPlugin.config.TrySubstituteIngredient(recipe.Ingredients[i].techType, out List<Ingredient> Substitutes))
                {
                    foreach (Ingredient sub in Substitutes)
                    {
                        recipe.Ingredients.Add(sub);
                    }
                    recipe.Ingredients.RemoveAt(i); // Remove the current ingredient...
                    i--; // ...and make sure the loop continues at the item after this, not the one after that.
                }
            }

            return recipe;
        }
    }
}