using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza.VehicleModules
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

        private static GameObject prefab;

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

#if SUBNAUTICA_STABLE
        public override GameObject GetGameObject()
        {
            if (prefab == null)
            {
                prefab = CraftData.InstantiateFromPrefab(TechType.ExosuitThermalReactorModule);
                ModPrefabCache.AddPrefab(prefab, false);
            }

            return prefab;
        }
#elif BELOWZERO

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                TaskResult<GameObject> instResult = new TaskResult<GameObject>();
                yield return CraftData.InstantiateFromPrefabAsync(techType, instResult, false);

                //GameObject go = instResult.Get();
                //prefab = GameObject.Instantiate<GameObject>(task.GetResult());
                prefab = instResult.Get();
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)]
                                                         // but it can still be instantiated. [unlike with SetActive(false)]

            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }
#endif

        protected override Sprite GetItemSprite()
        {
#if SUBNAUTICA_STABLE
            return SpriteManager.Get(TechType.SeamothElectricalDefense);
#elif BELOWZERO
            return SpriteManager.Get(TechType.SeaTruckUpgradePerimeterDefense);
#endif
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
