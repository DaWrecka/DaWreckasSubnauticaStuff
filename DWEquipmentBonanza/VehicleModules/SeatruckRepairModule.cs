using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;
using DWEquipmentBonanza.MonoBehaviours;
using SMLHelper.V2.Utility;
using System.IO;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    internal class SeatruckRepairModule : Equipable
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

        private static GameObject prefab;
        private static Sprite sprite;

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

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                //TaskResult<GameObject> prefabResult = new TaskResult<GameObject>();
                //yield return CraftData.InstantiateFromPrefabAsync(TechType.SeaTruckUpgradeEnergyEfficiency, prefabResult, false);
                //prefab = prefabResult.Get();

                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.SeaTruckUpgradeEnergyEfficiency, true);
                yield return task;
                prefab = GameObject.Instantiate(task.GetResult());

                prefab.name = ClassID;
                //prefab.EnsureComponent<VehicleRepairComponent>();
                // The code is handled by the SeatruckUpdater component, rather than anything here.
                //ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                         // but it can still be instantiated. [unlike with SetActive(false)]
            }

            gameObject.Set(prefab);
        }

        protected override Sprite GetItemSprite()
        {
            sprite ??= ImageUtils.LoadSpriteFromFile(Path.Combine(Main.AssetsFolder, $"{ClassID}.png")); ;
            return sprite;
        }

        public SeatruckRepairModule() : base("SeatruckRepairModule", "SeaTruck Repair Module", "Passively repairs damaged Seatruck and modules for modest energy cost; in active mode, rapidly repairs damage, but at significant energy cost")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
                bool success = SeaTruckUpdater.AddRepairModuleType(this.TechType);
                Log.LogDebug(($"Finished patching {this.TechType.AsString()}, added successfully: {success}"));
            };
        }
    }
#endif
}
