using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
    internal class HoverbikeBoostUpgradeModule : HoverbikeUpgradeBase<HoverbikeBoostUpgradeModule>
    {
        protected override TechType spriteTemplate => TechType.HoverbikeJumpModule;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.WiringKit, 1),
                    new Ingredient(TechType.ComputerChip, 1),
                    new Ingredient(TechType.GenericRibbon, 1),
                    new Ingredient(TechType.Nickel, 1),
                    new Ingredient(TechType.Magnetite, 1)
                }
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HoverbikeJumpModule);
                yield return task;
                prefab = GameObject.Instantiate<GameObject>(task.GetResult());
                ModPrefabCache.AddPrefab(prefab, false);
            }

            gameObject.Set(prefab);
        }

        public HoverbikeBoostUpgradeModule() : base("HoverbikeBoostUpgradeModule", "Snowfox Boost Upgrade", "Reworks boost system, allowing for continuous boost. Be aware of overheating.")
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
            };
        }
    }
#endif
}
