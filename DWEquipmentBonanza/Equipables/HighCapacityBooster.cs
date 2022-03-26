using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using UnityEngine;
using UWE;
using Logger = QModManager.Utility.Logger;
using FMODUnity;
using Common;

namespace DWEquipmentBonanza.Equipables
{
#if BELOWZERO
    internal class HighCapacityBooster : Equipable
    {
        protected static Sprite icon;
        protected static GameObject prefab;
        //protected static GameObject highCapTank;

        protected virtual List<TechType> Substitutions => new List<TechType>()
        {
            TechType.SuitBoosterTank,
            TechType.HighCapacityTank
        };

        protected virtual List<TechType> CompoundUnlocks => new List<TechType>()
        {
            TechType.SuitBoosterTank,
            TechType.HighCapacityTank
        };

        public HighCapacityBooster(string classId = "HighCapacityBooster",
            string friendlyName = "High Capacity Booster Tank",
            string description = "Booster tank with increased oxygen capacity.") : base(classId, friendlyName, description)
        {
            //CoroutineHost.StartCoroutine(PostPatchSetup());
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
                if (Substitutions != null && Substitutions.Count > 0)
                {
                    foreach (var tt in Substitutions)
                        Main.AddSubstitution(this.TechType, tt);
                }
                if (CompoundUnlocks != null && CompoundUnlocks.Count > 0)
                {
                    Reflection.AddCompoundTech(this.TechType, CompoundUnlocks);
                }

                Main.AddCustomOxyExclusion(this.TechType, true, true);
                Main.AddCustomOxyTank(this.TechType, -1f, icon);
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Tank;
        public override Vector2int SizeInInventory => new Vector2int(3, 4);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
		public override bool UnlockedAtStart => false;
        public override TechCategory CategoryForPDA => TechCategory.Equipment;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.TankMenuPath };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.HighCapacityTank, 1),
                        new Ingredient(TechType.WiringKit, 1)
                    }
                )
            };
        }

        protected virtual IEnumerator PostPatchSetup()
        {
            while (icon == null)
            {
                icon = SpriteManager.Get(TechType.HighCapacityTank, null);
                yield return new WaitForEndOfFrame();
            }
        }
        protected override Sprite GetItemSprite()
        {
            return icon ??= SpriteManager.Get(TechType.HighCapacityTank, null);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            // We're not using a separate PreparePrefab() method, because this object only exists in BelowZero.
            // In the event 

            CoroutineTask<GameObject> task;
            Oxygen oxy;
            if (Main.HighCapacityTankPrefab == null)
            {
                Log.LogDebug($"HighCapacityBooster.GetGameObjectAsync: getting HighCapacityTank prefab");
                task = CraftData.GetPrefabForTechTypeAsync(TechType.HighCapacityTank, verbose: true);
                yield return task;
                Main.HighCapacityTankPrefab = GameObject.Instantiate(task.GetResult()); // The "capacity expansion" code in Customise Your Oxygen can't run unless the thing is instantiated. The prefabs can't be altered.
                                                                        // So unless we instantiate, we only get default capacities.
                ModPrefabCache.AddPrefab(Main.HighCapacityTankPrefab, false);
            }

            if (prefab == null)
            {
                Log.LogDebug($"HighCapacityBooster.GetGameObjectAsync: getting SuitBoosterTank prefab");
                task = CraftData.GetPrefabForTechTypeAsync(TechType.SuitBoosterTank, verbose: true);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.

                //GameObject go = GameObject.Instantiate(prefab);
                //Oxygen highCapOxygen = task.GetResult().GetComponent<Oxygen>();
                if (Main.HighCapacityTankPrefab.TryGetComponent<Oxygen>(out Oxygen highCapOxygen))
                {
                    oxy = prefab.EnsureComponent<Oxygen>();
                    if (oxy != null)
                    {
                        float oxygenCapacity = highCapOxygen.oxygenCapacity;
                        Log.LogDebug($"Found Oxygen component with capacity of {oxygenCapacity} for prefab HighCapacityTank and existing oxygen capacity of {oxy.oxygenCapacity} for prefab HighCapacityBooster.");
                        oxy.oxygenCapacity = oxygenCapacity;
                    }
                    else
                    {
                        Log.LogError($"Could not get Oxygen component of SuitBoosterTank while generating HighCapacityBooster");
                    }
                }
                else
                    Log.LogError($"Could not get Oxygen component of HighCapacityTank while generating HighCapacityBooster");

            }

            float oxyCap = -1f;
            if(prefab.TryGetComponent<Oxygen>(out oxy))
                oxyCap = oxy.oxygenCapacity;
            Log.LogDebug($"GameObject created with oxygenCapacity of {oxyCap}");

            gameObject.Set(prefab);
        }

    }

    internal class IonBoosterTank : HighCapacityBooster
    {
        private const float boostOxygenUsePerSecond = 2f;
        protected override List<TechType> CompoundUnlocks => null;
        public override TechType RequiredForUnlock => TechType.SeaTruckTeleportationModule;

        public IonBoosterTank() : base("IonBoosterTank", "Ion Booster Tank", "Booster tank upgraded using Architect hybrid technology; consumes less oxygen while boosting")
        {

        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("HighCapacityBooster"), 1),
                        new Ingredient(TechType.PrecursorIonCrystal, 2),
                        new Ingredient(TechType.Kyanite, 2)
                    }
                )
            };
        }
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            // We're not using a separate PreparePrefab() method, because this object only exists in BelowZero.
            // In the event 

            CoroutineTask<GameObject> task;
            Oxygen oxy;
            if (Main.HighCapacityTankPrefab == null)
            {
                Log.LogDebug($"HighCapacityBooster.GetGameObjectAsync: getting HighCapacityTank prefab");
                task = CraftData.GetPrefabForTechTypeAsync(TechType.HighCapacityTank, verbose: true);
                yield return task;
                Main.HighCapacityTankPrefab = GameObject.Instantiate(task.GetResult()); // The "capacity expansion" code in Customise Your Oxygen can't run unless the thing is instantiated. The prefabs can't be altered.
                                                                                        // So unless we instantiate, we only get default capacities.
                ModPrefabCache.AddPrefab(Main.HighCapacityTankPrefab, false);
            }

            if (prefab == null)
            {
                Log.LogDebug($"HighCapacityBooster.GetGameObjectAsync: getting SuitBoosterTank prefab");
                task = CraftData.GetPrefabForTechTypeAsync(TechType.SuitBoosterTank, verbose: true);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.

                //GameObject go = GameObject.Instantiate(prefab);
                Oxygen highCapOxygen = Main.HighCapacityTankPrefab.GetComponent<Oxygen>();
                //Oxygen highCapOxygen = task.GetResult().GetComponent<Oxygen>();
                if (highCapOxygen != null)
                {
                    oxy = prefab.EnsureComponent<Oxygen>();
                    if (oxy != null)
                    {
                        float oxygenCapacity = highCapOxygen.oxygenCapacity;
                        Log.LogDebug($"Found Oxygen component with capacity of {oxygenCapacity} for prefab HighCapacityTank and existing oxygen capacity of {oxy.oxygenCapacity} for prefab IonBoosterTank.");
                        oxy.oxygenCapacity = oxygenCapacity;
                    }
                    else
                    {
                        Log.LogError($"Could not get Oxygen component of SuitBoosterTank while generating IonBoosterTank");
                    }
                }
                else
                    Log.LogError($"Could not get Oxygen component of HighCapacityTank while generating IonBoosterTank");

                var booster = prefab.GetComponent<SuitBoosterTank>();
                if (booster != null)
                {
                    booster.boostOxygenUsePerSecond = boostOxygenUsePerSecond;
                }
                else
                {
                    Log.LogError($"Could not get SuitBoosterTank component of SuitBoosterTank prefab while generating IonBoosterTank");
                }
            }

            oxy = prefab.GetComponent<Oxygen>();
            float oxyCap = oxy != null ? oxy.oxygenCapacity : -1f;
            Log.LogDebug($"IonBoosterTank created with oxygenCapacity of {oxyCap}");
            var postBooster = prefab.GetComponent<SuitBoosterTank>();
            if (postBooster != null)
            {
                if (postBooster.boostOxygenUsePerSecond != boostOxygenUsePerSecond)
                {
                    Log.LogDebug($"Editing SuitBoosterTnk component");
                    postBooster.boostOxygenUsePerSecond = boostOxygenUsePerSecond;
                }
            }
            else
            {
                Log.LogError($"Could not get SuitBoosterTank component of SuitBoosterTank prefab while generating IonBoosterTank");
            }

            gameObject.Set(prefab);
        }
    }
#endif
}
