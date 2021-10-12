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
        protected static GameObject highCapTank;

        public HighCapacityBooster() : base("HighCapacityBooster", "High Capacity Booster Tank", "Booster tank with increased oxygen capacity.")
        {
            //CoroutineHost.StartCoroutine(PostPatchSetup());
            OnFinishedPatching += () =>
            {
                Main.AddSubstitution(this.TechType, TechType.SuitBoosterTank);
                Main.AddSubstitution(this.TechType, TechType.HighCapacityTank);
                Main.AddModTechType(this.TechType);
                Reflection.AddCompoundTech(this.TechType, new List<TechType>()
                {
                    TechType.SuitBoosterTank,
                    TechType.HighCapacityTank
                });

                Main.AddCustomOxyExclusion(this.TechType, true, true);
                Main.AddCustomOxyTank(this.TechType, -1f, icon);
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Tank;

        public override Vector2int SizeInInventory => new Vector2int(3, 4);

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override TechType RequiredForUnlock => TechType.Unobtanium;

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
            /*if (icon == null)
            {
                icon = SpriteManager.Get(TechType.HighCapacityTank, null);
            }
            return icon;*/
            return SpriteManager.Get(TechType.HighCapacityTank);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            CoroutineTask<GameObject> task;
            Oxygen oxy;
            if (prefab == null)
            {
                Log.LogDebug($"HighCapacityBooster.GetGameObjectAsync: getting SuitBoosterTank prefab");
                task = CraftData.GetPrefabForTechTypeAsync(TechType.SuitBoosterTank, verbose: true);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
                //HighCapacityBooster.prefab = prefab;
            }

            if (highCapTank == null)
            {
                Log.LogDebug($"HighCapacityBooster.GetGameObjectAsync: getting HighCapacityTank prefab");
                task = CraftData.GetPrefabForTechTypeAsync(TechType.HighCapacityTank, verbose: true);
                yield return task;
                highCapTank = GameObject.Instantiate(task.GetResult()); // The "capacity expansion" code in Customise Your Oxygen can't run unless the thing is instantiated. The prefabs can't be altered.
                                                                        // So unless we instantiate, we only get default capacities.
                ModPrefabCache.AddPrefab(highCapTank, false);
            }

            GameObject go = GameObject.Instantiate(prefab);
            Oxygen highCapOxygen = highCapTank.GetComponent<Oxygen>();
            //Oxygen highCapOxygen = task.GetResult().GetComponent<Oxygen>();
            if (highCapOxygen != null)
            {
                oxy = go.EnsureComponent<Oxygen>();
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

                GameObject.Destroy(highCapOxygen);
            }
            else
                Log.LogError($"Could not get Oxygen component of HighCapacityTank while generating HighCapacityBooster");

            float oxyCap = prefab.GetComponent<Oxygen>().oxygenCapacity;
            Log.LogDebug($"GameObject created with oxygenCapacity of {oxyCap}");
            gameObject.Set(go);
        }
    }
#endif
}
