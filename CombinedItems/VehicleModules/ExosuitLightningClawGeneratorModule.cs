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
    internal class ExosuitLightningClawGeneratorModule : Equipable
    {
        public override EquipmentType EquipmentType => EquipmentType.ExosuitModule;
        public override QuickSlotType QuickSlotType => QuickSlotType.Passive;
        public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
        public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
        public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
        public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
        public override string[] StepsToFabricatorTab => new string[] { "ExosuitModules" };
        public override float CraftingTime => 10f;
        public override Vector2int SizeInInventory => new Vector2int(1, 2);

        private GameObject prefab;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Polyaniline, 1),
                        new Ingredient(TechType.WiringKit, 1),
                        new Ingredient(TechType.Battery, 1),
                        new Ingredient(TechType.AluminumOxide, 1)
                    }
                )
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ExosuitThermalReactorModule);
                yield return task;
                prefab = GameObject.Instantiate<GameObject>(task.GetResult());

            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.SeaTruckUpgradePerimeterDefense);
        }

        public ExosuitLightningClawGeneratorModule() : base("ExosuitLightningClawGeneratorModule", "Exosuit Lightning Claw Generator", "An electrical pulse generator which ties into the Exosuit's claw arm, electrocuting anything struck by it.")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
            };
        }
    }
}
