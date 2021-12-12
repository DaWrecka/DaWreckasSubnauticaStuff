using HarmonyLib;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Logger = QModManager.Utility.Logger;
#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Json = Oculus.Newtonsoft.Json;
#elif BELOWZERO
using Json = Newtonsoft.Json;
#endif


// Based HEAVILY on MrPurple6411's Copper from Scan code. For "based heavily" read "nicked almost wholesale".

namespace PartsFromScanning.Patches
{

    [HarmonyPatch(typeof(CraftData))]
    internal class CraftData_Patch
    {
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
#if SUBNAUTICA_STABLE
                //GameObject gameObject = CraftData.InstantiateFromPrefab(techType, false);
                GameObject prefabForTechType = CraftData.GetPrefabForTechType(techType, true);
                GameObject gameObject = (prefabForTechType != null) ? global::Utils.SpawnFromPrefab(prefabForTechType, null) : global::Utils.CreateGenericLoot(techType);
#elif BELOWZERO
                GameObject gameObject = CraftData.InstantiateFromPrefab(null, techType, false); // Coming back to this months later, I didn't think this worked in BZ, because it's not async. But apparently it does...
#endif
                Pickupable pickup = gameObject.GetComponent<Pickupable>();
                if (pickup != null)
                {
                    pickup.Initialize();

                    // This is kind of messy but it's an easy way to get the cross-game code running. In SN1 modules will always == null so the block won't run; but it'll still compile.
#if SUBNAUTICA_STABLE
                    Equipment modules = null;
#elif BELOWZERO
                    SeaTruckUpgrades upgrades = Player.main.GetComponentInParent<SeaTruckUpgrades>();
                    Equipment modules = upgrades?.modules;
#endif
                    if (modules != null && TechTypeHandler.TryGetModdedTechType("SeaTruckStorage", out TechType storageType))
                    {
                        HashSet<string> TruckSlotIDs = modules.equipment.Keys.ToSet<string>();
                        foreach (string slot in TruckSlotIDs)
                        {
                            if (modules.GetTechTypeInSlot(slot) == storageType)
                            {
                                InventoryItem item = modules.GetItemInSlot(slot);

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
#if !RELEASE
                                        Logger.Log(Logger.Level.Debug, $"Adding tech {techType} to Seatruck storage");
#endif
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Vehicle thisVehicle = Player.main.GetVehicle();
                        if (thisVehicle != null)
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
                                    Logger.Log(Logger.Level.Debug, $"Adding tech {techType.AsString().PadLeft(15)} to Exosuit storage; previous count {lastCount}, new count {techCount}");
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
                                //bool storageCheck = false;
                                List<IItemsContainer> containers = new List<IItemsContainer>();
                                seamoth.GetAllStorages(containers);
                                //for (int i = 0; i < 12; i++)
                                foreach (IItemsContainer storage in containers)
                                {
                                    try
                                    {
                                        //ItemsContainer storage = seamoth.GetStorageInSlot(i, TechType.VehicleStorageModule);
                                        InventoryItem newItem = new InventoryItem(pickup);
                                        if (storage is ItemsContainer iContainer)
                                        {
                                            int lastCount = iContainer.GetCount(techType);
                                            iContainer.AddItem(pickup);
                                            int techCount = iContainer.GetCount(techType);
#if !RELEASE
                                            Logger.Log(Logger.Level.Debug, $"Adding tech {techType.AsString().PadLeft(15)} to Seamoth storage; previous count {lastCount}, new count {techCount}");
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
                                        Logger.Log(Logger.Level.Debug, $"Exception adding tech {techType} to Seamoth storage: {e.ToString()}");
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
            }
#if !RELEASE
            Logger.Log(Logger.Level.Debug, $"Adding tech {techType} to player inventory");
#endif
            CraftData.AddToInventory(techType, count, bNoMessage, bSpawnIfCantAdd);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CraftData.AddToInventory))]
        private static bool PreAddToInventory(TechType techType, int num = 1, bool noMessage = false, bool spawnIfCantAdd = true)
        {
            if (techType == TechType.Titanium && num == 2 && !noMessage && spawnIfCantAdd)
            {
                TechType scannedFragment = TechType.None;
                RecipeData recipe = null;
                if (BlueprintHandTargetPatches.bIsDatabox)
                {
                    //if (!Main.config.bInterceptDataboxes)
                    //    return true;

                    scannedFragment = BlueprintHandTargetPatches.databoxUnlock;
                    if (Main.config.TryOverrideRecipe(scannedFragment, out recipe))
                    {
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Using OverrideRecipe: {Json.JsonConvert.SerializeObject(recipe, Json.Formatting.Indented)}");
#endif
                    }
                    else
                    {
                        recipe = CraftDataHandler.GetTechData(scannedFragment);
                    }
                }
                else
                {
                    scannedFragment = PDAScanner.scanTarget.techType;
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"Intercepting scan of fragment {scannedFragment.ToString()}");
#endif

                    if (PartsFromScanning.Main.config.TryOverrideRecipe(scannedFragment, out recipe))
                    {
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Using OverrideRecipe: {Json.JsonConvert.SerializeObject(recipe, Json.Formatting.Indented)}");
#endif
                    }
                    else if ((int)scannedFragment > 1112 && (int)scannedFragment < 1117)
                    {
                        // TechTypes 1113 to 1116 are Cyclops fragments, which have no blueprint associated, so we need to process them specially.
                        recipe = new RecipeData();
                        /*CyclopsHullFragment = 1113,
                        CyclopsBridgeFragment = 1114,
                        CyclopsEngineFragment = 1115,
                        CyclopsDockingBayFragment = 1116,*/

                        switch ((int)scannedFragment)
                        {
                            case 1113:
                                recipe.Ingredients.Add(new Ingredient(TechType.PlasteelIngot, 2));
                                recipe.Ingredients.Add(new Ingredient(TechType.Lead, 1));
                                break;
                            case 1114:
                                recipe.Ingredients.Add(new Ingredient(TechType.EnameledGlass, 3));
                                break;
                            case 1115:
                                recipe.Ingredients.Add(new Ingredient(TechType.Lubricant, 1));
                                recipe.Ingredients.Add(new Ingredient(TechType.AdvancedWiringKit, 1));
                                recipe.Ingredients.Add(new Ingredient(TechType.Lead, 1));
                                break;
                            case 1116:
                                recipe.Ingredients.Add(new Ingredient(TechType.PlasteelIngot, 2));
                                break;
                        }
                        recipe.Ingredients.Add(new Ingredient(TechType.Lead, 1));
                        recipe.Ingredients.Add(new Ingredient(TechType.PlasteelIngot, 1));
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Using recipe from manual override: {Json.JsonConvert.SerializeObject(recipe, Json.Formatting.Indented)}");
#endif
                    }
                    else
                    {
                        PDAScanner.EntryData entryData = PDAScanner.GetEntryData(scannedFragment);
                        if (entryData == null) // Sanity check; this should always be true
                        {
#if !RELEASE
                            Logger.Log(Logger.Level.Debug, $"Failed to find EntryData for fragment");
#endif
                            /*CraftData.AddToInventory(TechType.Titanium);
                            CraftData.AddToInventory(TechType.Titanium); // Adding them one-by-one so as to prevent it being caught by this very routine.*/
                            return true;
                        }
                        //Logger.Log(Logger.Level.Debug, $"Found entryData {entryData.ToString()}");
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Found entryData {Json.JsonConvert.SerializeObject(entryData, Json.Formatting.Indented)}");
#endif


                        //CraftData.AddToInventory(TechType.Titanium);
                        //CraftData.AddToInventory(TechType.Copper);
#if SUBNAUTICA_STABLE
                        recipe = CraftDataHandler.GetTechData(entryData.blueprint);
#elif BELOWZERO
                        recipe = CraftDataHandler.GetRecipeData(entryData.blueprint);
#endif
                        if (recipe == null)
                        {
#if !RELEASE
                            Logger.Log(Logger.Level.Debug, $"Failed to find blueprint for EntryData");
#endif
                            /*CraftData.AddToInventory(TechType.Titanium);
                            CraftData.AddToInventory(TechType.Titanium); // One-by-one again, as above.*/
                            return true;
                        }
                        //Logger.Log(Logger.Level.Debug, $"Found recipe {recipe.ToString()}");
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Using recipe from EntryData: {Json.JsonConvert.SerializeObject(recipe, Json.Formatting.Indented)}");
#endif
                    }
                }

                for (int i = 0; i < recipe.Ingredients.Count; i++)
                {
                    if (PartsFromScanning.Main.config.TrySubstituteIngredient(recipe.Ingredients[i].techType, out List<Ingredient> Substitutes))
                    {
                        foreach (Ingredient sub in Substitutes)
                            recipe.Ingredients.Add(sub);
                        recipe.Ingredients.RemoveAt(i); // Remove the current ingredient...
                        i--; // ...and make sure the loop continues at the item after this, not the one after that.
                    }
                }

                // I believe the easiest way to get a random item from the blueprint would be to make a list of techTypes; if an ingredient is used twice in the recipe, it will appear in the list twice.
                // That way, we can generate a random number where 0<=rnd<list.count, and select that item.
                List<TechType> bp = new List<TechType> { };
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
                    float thisWeight = Main.config.GetWeightForTechType(bp[i]);
                    TotalWeight += thisWeight;
                    WeightedItem thisWeightedItem = new WeightedItem(TotalWeight, bp[i]);
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"Adding item to drop list, TechType = {thisWeightedItem.tech.ToString()},   this weight = {thisWeight}, cumulative weight = {thisWeightedItem.Weight}"); 
#endif
                    BlueprintPairs.Add(thisWeightedItem);
                }

                // Now we should be able to pick a few random numbers between 0 and the list's total weight, and add those. We want to remove that entry afterwards, but that's not a big ask.
                System.Random rng = new System.Random();
                int numIngredients = Math.Min(PartsFromScanning.Main.config.GenerateGiftValue(), BlueprintPairs.Count);
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Generated a value for this scan of {numIngredients} components."); 
#endif

                int awards = 0;
                double r;
                for (int i = 0; i < numIngredients && BlueprintPairs.Count > 0; i++)
                {
                    r = rng.NextDouble() * TotalWeight;
                    for (int j = 0; j < BlueprintPairs.Count; j++)
                    {
                        //                                               This part is for sanity checking
                        //                                   ___________________________|______________________________
                        //                                  /                                                          \
                        if (r < BlueprintPairs[j].Weight || ((j + 1) == BlueprintPairs.Count && awards < numIngredients))
                        {
                            AddInventory(BlueprintPairs[j].tech, 1, false, true);
                            //CraftData.AddToInventory(BlueprintPairs[j].tech, 1, false, true);
                            awards++;
                            TotalWeight -= Main.config.GetWeightForTechType(BlueprintPairs[j].tech);
                            BlueprintPairs.RemoveAt(j);
                            break;
                        }
                    }
                }
                return false;
            }
            return true;
        }
    }
}