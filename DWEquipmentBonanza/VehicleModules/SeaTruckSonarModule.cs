using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    internal class SeaTruckSonarModule : SeaTruckUpgradeModule<SeaTruckSonarModule>
    {
        internal const float EnergyCost = 20f;

        public override EquipmentType EquipmentType => EquipmentType.SeaTruckModule;
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
        public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
        public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
        public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
        public override string[] StepsToFabricatorTab => new string[] { "SeaTruckUpgrade" };
        public override float CraftingTime => 10f;
        public override Vector2int SizeInInventory => new Vector2int(1, 1);
        protected override TechType prefabTemplate => TechType.SeaTruckUpgradeEnergyEfficiency;
        protected override TechType spriteTemplate => TechType.SeamothSonarModule;

        protected override GameObject ModifyPrefab(GameObject prefab)
        {
            ModPrefabCache.AddPrefab(prefab, false);
            return prefab;
        }
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Magnetite, 2),
                        new Ingredient(TechType.CopperWire, 1)
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
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                         // but it can still be instantiated. [unlike with SetActive(false)]
            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }

        public SeaTruckSonarModule() : base("SeaTruckSonarModule", "SeaTruck Sonar Module", "A dedicated system for detecting and displaying topographical data on the HUD.")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
                CraftDataHandler.SetEnergyCost(this.TechType, EnergyCost);
            };
        }
    }
#endif
}
