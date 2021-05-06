using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Common;
using UnityEngine;
using UWE;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;

namespace CombinedItems.Patches
{
    /*internal class FuelCell : Craftable
    {
        protected GameObject prefab;
        protected TechType template; // TechType to use as our template
        protected TechType basicBattery; // Basic version of the TechType for comparison.
        protected float differenceMultiplier = 0.5f; // The difference in capacities between template and basicBattery are multiplied by this value, which
                                             // is then added to the capacity of basicBattery to get the capacity of this cell.

        private const float defaultCapacity = 300f;

        public FuelCell(string classId = "FuelCell",
                string friendlyName = "Small Fuel Cell",
                string Description = "Small fuel cell, higher-capacity alternative to standard Alterra batteries",
                TechType thisTemplate = TechType.PrecursorIonBattery,
                TechType thisBasicBattery = TechType.Battery,
                bool bIsPowercell = false) : base(classId, friendlyName, Description)
        {
            this.template = thisTemplate;
            this.basicBattery = thisBasicBattery;
            if (bIsPowercell)
            {
                OnFinishedPatching += () =>
                {
                    BatteryChargerPatches.AddPowerCell(this.TechType);
                };
            }
            else
            {
                OnFinishedPatching += () =>
                {
                    BatteryChargerPatches.AddBattery(this.TechType);
                };
            }
        }

        public override TechType RequiredForUnlock => TechType.Polyaniline;
        public override Vector2int SizeInInventory => new Vector2int(1, 1);
        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
        public override string[] StepsToFabricatorTab => new string[] { "Resources", "Electronics" };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                Ingredients = new List<Ingredient>() {
                    new Ingredient(TechType.Battery, 1),
                    new Ingredient(TechType.Polyaniline, 1),
                    new Ingredient(TechType.DisinfectedWater, 1),
                    new Ingredient(TechType.Magnetite, 1)
                },
                craftAmount = 1
            };
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.Battery);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(this.template);
                yield return task;

                prefab = task.GetResult();
                prefab.SetActive(false); // Keep inactive until we're ready to use it

                task = CraftData.GetPrefabForTechTypeAsync(this.basicBattery);
                yield return task;

                Battery batteryTemplate = task.GetResult().GetComponent<Battery>();
                Battery thisBattery = prefab.GetComponent<Battery>();
                float newCapacity = defaultCapacity;
                if (batteryTemplate != null && thisBattery != null)
                {
                    float basicCap = batteryTemplate._capacity;
                    float thisCap = thisBattery._capacity;
                    newCapacity = (thisCap - basicCap) * differenceMultiplier + basicCap;
                }

                prefab.EnsureComponent<Battery>()._capacity = newCapacity;
                GameObject.DestroyImmediate(batteryTemplate);

                //foreach(SkinnedMeshRenderer smr in prefab.GetComponents<SkinnedMeshRenderer>())

                // Done editing prefab
                prefab.SetActive(true);
            }
            gameObject.Set(GameObject.Instantiate(prefab));
        }
    }

    internal class LargeFuelCell : FuelCell
    {
        internal LargeFuelCell() : base("LargeFuelCell", "Fuel Cell", "Fuel cell, higher-capacity alternative to standard Alterra power cells", TechType.PrecursorIonPowerCell, TechType.PowerCell, true)
        {
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                Ingredients = new List<Ingredient>() {
                new Ingredient(TechType.PowerCell, 1),
                new Ingredient(TechType.Polyaniline, 2),
                new Ingredient(TechType.DisinfectedWater, 1),
                new Ingredient(TechType.Magnetite, 1)
                },
                craftAmount = 1
            };
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.PowerCell);
        }
    }*/

    [HarmonyPatch(typeof(Battery))]
    internal class Batteries
    {
        private static bool bProcessingBatteries = false;
        // TODO: Make configurable
        internal static Dictionary<string, float> BatteryValues = new Dictionary<string, float>()
        {
            { "Battery", 150f },
            { "PowerCell", 300f },
            { "LithiumIonBattery", 300f },
            { "PrecursorIonBattery", 800f },
            { "PrecursorIonPowerCell", 1600f }
        };
        internal static Dictionary<TechType, float> typedBatteryValues;
        internal static List<Battery> pendingBatteryList = new List<Battery>();

        internal static void PostPatch()
        {
            foreach (KeyValuePair<string, float> kvp in BatteryValues)
            {
                TechType tt = TechTypeUtils.GetTechType(kvp.Key);
                if (tt == TechType.None)
                    Log.LogError($"No valid TechType found for string '{kvp.Key}'");
                else
                {
                    if (typedBatteryValues == null)
                        typedBatteryValues = new Dictionary<TechType, float>();

                    typedBatteryValues.Add(tt, kvp.Value);
                }
            }
        }

        internal static IEnumerator ProcessPendingBatteries()
        {
            if (bProcessingBatteries)
                yield break;

            bProcessingBatteries = true;

            while (pendingBatteryList.Count > 0)
            {
                for (int i = pendingBatteryList.Count - 1; i >= 0; i--)
                {
                    Battery b = pendingBatteryList[i];
                    TechTag tt = b.GetComponent<TechTag>();
                    if (tt != null)
                    {
                        TechType batteryTech = tt.type;
                        Log.LogDebug($"Deserialised battery at index {i} with TechType {batteryTech.AsString()}");
                        if (typedBatteryValues.TryGetValue(batteryTech, out float value))
                        {
                            Log.LogDebug($"Updating battery with new capacity {value}");
                            b._capacity = value;
                            b.OnAfterDeserialize();
                        }
                        pendingBatteryList.RemoveAt(i);
                    }
                    yield return new WaitForEndOfFrame();
                }
                yield return new WaitForSecondsRealtime(1f);
            }

            bProcessingBatteries = true;
            yield break;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Battery.OnProtoDeserialize))]
        public static void AddPendingBattery(ref Battery __instance)
        {
            if (!pendingBatteryList.Contains<Battery>(__instance))
            {
                Log.LogDebug($"Adding Battery instance {__instance.GetInstanceID()}");
                pendingBatteryList.Add(__instance);
            }

            CoroutineHost.StartCoroutine(ProcessPendingBatteries());
        }

        /*[HarmonyPostfix]
        [HarmonyPatch("Start")]
        public static void PostStart(Battery __instance)
        {
            bool bIsFull = (__instance.charge == __instance._capacity);
            TechType batteryTech = CraftData.GetTechType(__instance.gameObject);

            Log.LogDebug($"Batteries.PostAwake(): Found battery TechType {batteryTech.AsString()}");
            if (typedBatteryValues.ContainsKey(batteryTech))
            {
                float newCapacity = typedBatteryValues[batteryTech];
                if (__instance._capacity != newCapacity)
                {
                    Log.LogDebug($"Updating battery with new capacity {newCapacity}");
                    __instance._capacity = newCapacity;
                    if (bIsFull)
                        __instance._charge = newCapacity;
                }
            }
        }*/
    }
}
