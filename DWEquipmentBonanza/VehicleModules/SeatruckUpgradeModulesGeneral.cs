using Common;
using DWEquipmentBonanza.MonoBehaviours;
using DWEquipmentBonanza.Patches;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    internal abstract class SeaTruckUpgradeModule<T> : Equipable
    {
        public override EquipmentType EquipmentType => EquipmentType.SeaTruckModule;
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
        public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
        public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
        public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
        public override string[] StepsToFabricatorTab => new string[] { "SeaTruckUpgrade" };
        public override float CraftingTime => 5f;
        public override Vector2int SizeInInventory => new Vector2int(1, 1);
        protected abstract TechType prefabTemplate { get; }
        protected virtual TechType spriteTemplate => TechType.None;

        protected static Sprite sprite;

        protected virtual void OnFinishedPatch(TechType thisType)
        {
            Main.AddModTechType(thisType);
        }

        protected override Sprite GetItemSprite()
        {
            try
            {
                sprite ??= ImageUtils.LoadSpriteFromFile(Path.Combine(Main.AssetsFolder, $"{ClassID}.png"));
            }
            catch
            {
                if (spriteTemplate != TechType.None)
                    sprite ??= SpriteManager.Get(spriteTemplate, null);
                else
                    Log.LogError($"Could not find a file named {Path.Combine(Main.AssetsFolder, $"{ClassID}.png")} and no spriteTemplate was set");
            }

            return sprite;
        }

        protected virtual GameObject ModifyPrefab(GameObject prefab)
        {
            ModPrefabCache.AddPrefab(prefab, false);
            return prefab;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject modPrefab;

            if (TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
                gameObject.Set(modPrefab);
                yield break;
            }
            else if (prefabTemplate != TechType.None)
            {
                //TaskResult<GameObject> prefabResult = new TaskResult<GameObject>();
                //yield return CraftData.InstantiateFromPrefabAsync(TechType.SeaTruckUpgradeEnergyEfficiency, prefabResult, false);
                //prefab = prefabResult.Get();

                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate, true);
                yield return task;

                modPrefab = ModifyPrefab(GameObject.Instantiate(task.GetResult()));

                modPrefab.name = ClassID;
                //prefab.EnsureComponent<VehicleRepairComponent>();
                // The code is handled by the SeatruckUpdater component, rather than anything here.
                // but it can still be instantiated. [unlike with SetActive(false)]
            }
            else
                modPrefab = null;

            gameObject.Set(modPrefab);
        }

        public SeaTruckUpgradeModule(string classId, string friendlyName, string description) : base(classId, friendlyName, description)
        {
            OnFinishedPatching += () => this.OnFinishedPatch(this.TechType);
        }
    }

    internal class SeatruckRepairModule : SeaTruckUpgradeModule<SeatruckRepairModule>
    {
        /*public override EquipmentType EquipmentType => EquipmentType.SeaTruckModule;
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
        public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
        public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
        public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
        public override string[] StepsToFabricatorTab => new string[] { "SeaTruckUpgrade" };
        public override float CraftingTime => 5f;
        public override Vector2int SizeInInventory => new Vector2int(1, 1);

        private static GameObject prefab;
        private static Sprite sprite;*/

        protected override TechType prefabTemplate => TechType.SeaTruckUpgradeHorsePower;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Kyanite, 2),
                        new Ingredient(TechType.Polyaniline, 2),
                        new Ingredient(TechType.WiringKit, 1)
                    }
                )
            };
        }

        protected override GameObject ModifyPrefab(GameObject prefab)
        {
            var newGO = GameObject.Instantiate(prefab);
            ModPrefabCache.AddPrefab(newGO, false);
            return newGO; // This module doesn't need to modify the prefab at all
        }

        protected override void OnFinishedPatch(TechType thisType)
        {
            base.OnFinishedPatch(thisType);
            bool success = SeaTruckUpdater.AddRepairModuleType(thisType);
            Log.LogDebug(($"Finished patching {thisType.AsString()}, added successfully: {success}"));
        }

        public SeatruckRepairModule() : base("SeatruckRepairModule", "SeaTruck Repair Module", "Passively repairs damaged Seatruck and modules for modest energy cost; in active mode, rapidly repairs damage, but at significant energy cost")
        {
        }
    }

    internal abstract class SeaTruckHorsepowerUpgradeBase<T> : SeaTruckUpgradeModule<T>
    {
        protected override TechType prefabTemplate => TechType.SeaTruckUpgradeHorsePower;
        protected override TechType spriteTemplate => TechType.SeaTruckUpgradeHorsePower;
        public override TechType RequiredForUnlock => TechType.SeaTruckUpgradeHorsePower;
        protected abstract float weightMultiplier { get; } // Vanilla Horsepower Upgrade reduces weight to 0.65 of normal

        protected override GameObject ModifyPrefab(GameObject prefab)
        {
            ModPrefabCache.AddPrefab(prefab, false);
            return prefab; // This module doesn't need to modify the prefab at all
        }

        protected override void OnFinishedPatch(TechType thisType)
        {
            base.OnFinishedPatch(thisType);
            bool success = HorsepowerPatches.RegisterHorsepowerModifier(thisType, weightMultiplier);
            //Main.AddSubstitution(thisType, TechType.SeaTruckUpgradeHorsePower);
            //Main.AddUVSpeedModifier(thisType, 0f, 0f);
            Log.LogDebug(($"Finished patching {thisType.AsString()}, added successfully: {success}"));
        }

        public SeaTruckHorsepowerUpgradeBase(string classId, string friendlyName, string description) : base(classId, friendlyName, description)
        { }
    }

    internal class SeaTruckUpgradeHorsepower2 : SeaTruckHorsepowerUpgradeBase<SeaTruckUpgradeHorsepower2>
    {
        //protected override float speedMultiplier => 1.25f;
        protected override float weightMultiplier => 0.5f;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.SeaTruckUpgradeHorsePower, 1),
                        new Ingredient(TechType.Benzene, 2),
                        new Ingredient(TechType.ReactorRod, 1),
                        new Ingredient(TechType.WiringKit, 1)
                    }
                )
            };
        }

        public SeaTruckUpgradeHorsepower2() : base("SeaTruckUpgradeHorsepower2", "SeaTruck Horsepower Upgrade Mk2", "Further improves SeaTruck engine power, reducing the impact of a long train. Does not stack.")
        {
        }
    }

    internal class SeaTruckUpgradeHorsepower3 : SeaTruckHorsepowerUpgradeBase<SeaTruckUpgradeHorsepower3>
    {
        //protected override float speedMultiplier => 1.50f;
        protected override float weightMultiplier => 0.4f;
        public override TechType RequiredForUnlock => Main.GetModTechType("SeaTruckUpgradeHorsepower2");
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(Main.GetModTechType("SeaTruckUpgradeHorsepower2"), 1),
                        new Ingredient(TechType.AluminumOxide, 1),
                        new Ingredient(TechType.RadioTowerPPU, 1),
                        new Ingredient(TechType.PrecursorIonCrystal, 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1)
                    }
                )
            };
        }

        public SeaTruckUpgradeHorsepower3() : base("SeaTruckUpgradeHorsepower3", "SeaTruck Horsepower Upgrade Mk3", "Maximally improves SeaTruck engine power and minimises the effect of a long train on the Seatruck's speed. Does not stack.")
        {
        }
    }
#endif
}
