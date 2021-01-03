using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using UnityEngine;
using SMLHelper.V2.Handlers;
using Logger = QModManager.Utility.Logger;

#if SUBNAUTICA
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace UpgradedBlades
{
    internal class Vibroblade : Equipable
    {
        public Vibroblade(string classId = "Vibroblade", string friendlyName = "Vibroblade", string description = "Survival knife with high-frequency oscillator inflicts horrific damage with even glancing blows") : base(classId, friendlyName, description)
        {

        }

        public override EquipmentType EquipmentType => EquipmentType.Hand;
        public override Vector2int SizeInInventory => new Vector2int(1, 1);
        public override TechType RequiredForUnlock => TechType.Diamond;
        public override TechGroup GroupForPDA => TechGroup.Personal;
        public override TechCategory CategoryForPDA => TechCategory.Tools;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { "KnifeMenu" };
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public override float CraftingTime => base.CraftingTime*2;

        public override GameObject GetGameObject()
        {
            var prefab = CraftData.GetPrefabForTechType(TechType.DiamondBlade);
            var obj = Object.Instantiate(prefab);
            if (obj == null)
            {
                Logger.Log(Logger.Level.Error, "Failed to instantiate GameObject for prefab DiamondBlade");
                return null;
            }

            Knife component = obj.EnsureComponent<Knife>();
            if (component != null)
            {
                component.damage = 55f;
                component.attackDist = 2f;
                component.socket = PlayerTool.Socket.RightHand;
                component.ikAimRightArm = true;
            }
            else
                Logger.Log(Logger.Level.Debug, $"Could not ensure Knife component in Vibroblade prefab");
            return obj;
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.Knife, 1),
                    new Ingredient(TechType.Diamond, 2),
                    new Ingredient(TechType.Quartz, 1),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.Magnetite, 1),
                    new Ingredient(TechType.WiringKit, 1)
                })
            };

            return recipe;
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}Icon.png");
        }
    }

}
