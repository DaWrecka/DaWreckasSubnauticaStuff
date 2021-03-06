﻿using HarmonyLib;
using Newtonsoft.Json;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

// Based HEAVILY on MrPurple6411's Copper from Scan code. For "based heavily" read "nicked almost wholesale".

namespace IngredientsFromScanning_BZ.Patches
{
#if !BELOWZERO
    [HarmonyPatch(typeof(PDAScanner), nameof(PDAScanner.CanScan), new Type[] { typeof(GameObject) })]
    internal class PDAScanner_CanScan_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result, GameObject go)
        {
            if (!Main.config.bOverrideMapRoom)
            {
                return true;
            }

            __result = false;
            //Logger.Log(Logger.Level.Debug, $"PDAScanner.CanScan Override: checking GameObject: {JsonConvert.SerializeObject(go.GetInstanceID(), Newtonsoft.Json.Formatting.Indented)}");

            UniqueIdentifier component = go.GetComponent<UniqueIdentifier>();
            if (component != null)
            {
                TechType techType = CraftData.GetTechType(go);
                string id = component.Id;
                //if (!PDAScanner.fragments.ContainsKey(id) && !PDAScanner.complete.Contains(techType))
                //{
                //return true;
                __result = true;
                //}
            }
            return false;
        }
    }
#endif

    [HarmonyPatch(typeof(BlueprintHandTarget))]
    public class BlueprintHandTargetPatches
    {
        public static bool bIsDatabox { get; private set; }
        public static TechType databoxUnlock { get; private set; }

        [HarmonyPatch(nameof(BlueprintHandTarget.UnlockBlueprint))]
        [HarmonyPrefix]
        internal static bool PreUnlockBlueprint(BlueprintHandTarget __instance)
        {
            databoxUnlock = __instance.unlockTechType;
            bIsDatabox = true;
            return true;
        }

        [HarmonyPatch(nameof(BlueprintHandTarget.UnlockBlueprint))]
        [HarmonyPostfix]
        internal static void PostUnlockBlueprint()
        {
            databoxUnlock = TechType.None;
            bIsDatabox = false;
        }
    }

    [HarmonyPatch(typeof(CraftData), nameof(CraftData.AddToInventory))]
    internal class CraftData_AddToInventory_Patch
    {
        struct WeightedItem
        {
            public WeightedItem(float w, TechType t)
            {
                this.Weight = w;
                this.tech = t;
            }
            public float Weight { get; set; }
            public TechType tech { get; set; }

        }

        private static void AddInventory(TechType techType, int count = 1, bool bNoMessage = false, bool bSpawnIfCantAdd = true)
        {
            // Ripped<cough>based upon MrPurple6411's method Deconstruct_Patch from the BuilderModule
            if (Player.main.isPiloting)
            {
                GameObject gameObject = CraftData.InstantiateFromPrefab(null, techType, false);
                Pickupable component = gameObject.GetComponent<Pickupable>();
                component?.Initialize();

                SeaTruckUpgrades upgrades = Player.main.GetComponentInParent<SeaTruckUpgrades>();
                if (upgrades != null)
                {
                    if (!TechTypeHandler.TryGetModdedTechType("SeaTruckStorage", out TechType storageType))
                        return;

                    HashSet<string> TruckSlotIDs = upgrades.modules.equipment.Keys.ToSet<string>();
                    foreach (string slot in TruckSlotIDs)
                    {
                        if (upgrades.modules.GetTechTypeInSlot(slot) == storageType)
                        {
                            InventoryItem item = upgrades.modules.GetItemInSlot(slot);

                            if (item.item.TryGetComponent(out SeamothStorageContainer seamothStorageContainer))
                            {
                                string name = Language.main.Get(component.GetTechName());
                                ErrorMessage.AddMessage(Language.main.GetFormat("VehicleAddedToStorage", name));

                                uGUI_IconNotifier.main.Play(component.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                                var inventory = new InventoryItem(component);
                                seamothStorageContainer.container.UnsafeAdd(item);
                                component.PlayPickupSound();
                                return;
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

                            if (storageContainer.container.HasRoomFor(component))
                            {
                                string name = Language.main.Get(component.GetTechName());
                                ErrorMessage.AddMessage(Language.main.GetFormat("VehicleAddedToStorage", name));

                                uGUI_IconNotifier.main.Play(component.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                                var item = new InventoryItem(component);
                                storageContainer.container.UnsafeAdd(item);
                                component.PlayPickupSound();
                                return;
                            }
                        }
                        else if (thisVehicle is SeaMoth seamoth)
                        {
                            bool storageCheck = false;
                            for (int i = 0; i < 12; i++)
                            {
                                try
                                {
                                    ItemsContainer storage = seamoth.GetStorageInSlot(i, TechType.VehicleStorageModule);
                                    if (storage != null && storage.HasRoomFor(component))
                                    {
                                        string name = Language.main.Get(component.GetTechName());
                                        ErrorMessage.AddMessage(Language.main.GetFormat("VehicleAddedToStorage", name));

                                        uGUI_IconNotifier.main.Play(component.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);

                                        var item = new InventoryItem(component);
                                        storage.UnsafeAdd(item);
                                        component.PlayPickupSound();
                                        storageCheck = true;
                                        break;
                                    }
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                            if (storageCheck)
                            {
                                return;
                            }
                        }
                    }
                }
            }
            CraftData.AddToInventory(techType, count, bNoMessage, bSpawnIfCantAdd);
        }

        [HarmonyPrefix]
        private static bool Prefix(TechType techType, int num = 1, bool noMessage = false, bool spawnIfCantAdd = true)
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
                        Logger.Log(Logger.Level.Debug, $"Using OverrideRecipe: {JsonConvert.SerializeObject(recipe, Newtonsoft.Json.Formatting.Indented)}");
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

                    if (IngredientsFromScanning_BZ.Main.config.TryOverrideRecipe(scannedFragment, out recipe))
                    {
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Using OverrideRecipe: {JsonConvert.SerializeObject(recipe, Newtonsoft.Json.Formatting.Indented)}");
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
                        Logger.Log(Logger.Level.Debug, $"Using recipe from manual override: {JsonConvert.SerializeObject(recipe, Newtonsoft.Json.Formatting.Indented)}");
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
                        Logger.Log(Logger.Level.Debug, $"Found entryData {JsonConvert.SerializeObject(entryData, Newtonsoft.Json.Formatting.Indented)}");
#endif


                        //CraftData.AddToInventory(TechType.Titanium);
                        //CraftData.AddToInventory(TechType.Copper);
                        recipe = CraftDataHandler.GetRecipeData(entryData.blueprint);
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
                        Logger.Log(Logger.Level.Debug, $"Using recipe from EntryData: {JsonConvert.SerializeObject(recipe, Newtonsoft.Json.Formatting.Indented)}");
#endif
                    }
                }

                for (int i = 0; i < recipe.Ingredients.Count; i++)
                {
                    if (IngredientsFromScanning_BZ.Main.config.TrySubstituteIngredient(recipe.Ingredients[i].techType, out List<Ingredient> Substitutes))
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
                    float thisWeight = IngredientsFromScanning_BZ.Main.config.GetWeightForTechType(bp[i]);
                    TotalWeight += thisWeight;
                    WeightedItem thisWeightedItem = new WeightedItem(TotalWeight, bp[i]);
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"Adding item to drop list, TechType = {thisWeightedItem.tech.ToString()},   this weight = {thisWeight}, cumulative weight = {thisWeightedItem.Weight}"); 
#endif
                    BlueprintPairs.Add(thisWeightedItem);
                }

                // Now we should be able to pick a few random numbers between 0 and the list's total weight, and add those. We want to remove that entry afterwards, but that's not a big ask.
                System.Random rng = new System.Random();
                int numIngredients = Math.Min(IngredientsFromScanning_BZ.Main.config.GenerateGiftValue(), BlueprintPairs.Count);
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
#if !RELEASE
                            Logger.Log(Logger.Level.Debug, $"With randomised weight of {r}, adding tech {BlueprintPairs[j].tech} to player inventory"); 
#endif
                            AddInventory(BlueprintPairs[j].tech, 1, false, true);
                            //CraftData.AddToInventory(BlueprintPairs[j].tech, 1, false, true);
                            awards++;
                            TotalWeight -= IngredientsFromScanning_BZ.Main.config.GetWeightForTechType(BlueprintPairs[j].tech);
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