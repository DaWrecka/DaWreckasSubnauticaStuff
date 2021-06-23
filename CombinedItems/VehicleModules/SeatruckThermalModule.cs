using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace CombinedItems.VehicleModules
{
    internal class SeatruckThermalModule : Equipable
    {
        public override EquipmentType EquipmentType => EquipmentType.SeaTruckModule;
        public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
        public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
        public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
        public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
        public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
        public override string[] StepsToFabricatorTab => new string[] { "SeaTruckUpgrade" };
        public override float CraftingTime => 10f;
        public override Vector2int SizeInInventory => new Vector2int(1, 1);

        private static GameObject prefab;

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
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.SeaTruckUpgradeEnergyEfficiency);
                yield return task;
                prefab = GameObject.Instantiate<GameObject>(task.GetResult());
                // The code is handled by the SeatruckUpdater component, rather than anything here.
                prefab.SetActive(false);
            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.ExosuitThermalReactorModule);
        }

        public SeatruckThermalModule() : base("SeatruckThermalModule", "SeaTruck Thermal Charger", "Recharges SeaTruck power cells in hot zones. Does not stack.")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
            };
        }
    }
}
