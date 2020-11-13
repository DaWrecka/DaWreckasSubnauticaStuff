//using System;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Reflection;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using UnityEngine;

#if SUBNAUTICA
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
#endif

namespace AcidProofSuit.Module
{
    internal class AcidGlovesPrefab : Equipable
    {
        public AcidGlovesPrefab() : base("AcidGloves", "Brine Gloves", "Reinforced dive gloves with an acid-resistant layer")
        {
        }

        public override EquipmentType EquipmentType => EquipmentType.Gloves;

        public override Vector2int SizeInInventory => new Vector2int(2, 2);

        public override TechType RequiredForUnlock => TechType.Workbench;

        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;

        public override string[] StepsToFabricatorTab => new string[] { "Suits" };

        public override TechGroup GroupForPDA => TechGroup.Personal;

        public override TechCategory CategoryForPDA => TechCategory.Equipment;

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override GameObject GetGameObject()
        {
            return Object.Instantiate(CraftData.GetPrefabForTechType(TechType.ReinforcedGloves));
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(/*new Ingredient[3]
                {
                    new Ingredient(TechType.CreepvinePiece, 1),
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.ReinforcedGloves, 1)
                }*/)
            };
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
        }
    }

    internal class AcidHelmetPrefab : Equipable
    {
        public AcidHelmetPrefab() : base("AcidHelmet", "Brine Helmet", "Rebreather treated with an acid-resistant layer")
        {
        }

        public override EquipmentType EquipmentType => EquipmentType.Head;

        public override Vector2int SizeInInventory => new Vector2int(2, 2);

        public override TechType RequiredForUnlock => TechType.Workbench;

        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;

        public override string[] StepsToFabricatorTab => new string[] { "Suits" };

        public override TechGroup GroupForPDA => TechGroup.Personal;

        public override TechCategory CategoryForPDA => TechCategory.Equipment;

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override GameObject GetGameObject()
        {
            return Object.Instantiate(CraftData.GetPrefabForTechType(TechType.Rebreather));
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(/*new Ingredient[3]
                {
                    new Ingredient(TechType.CreepvinePiece, 1),
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.Rebreather, 1)
                }*/)
            };
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
        }
    }

    internal class AcidSuitPrefab : Equipable
    {
        public AcidSuitPrefab() : base("AcidSuit", "Brine Suit", "Reinforced dive suit with an acid-resistant layer")
        {
        }

        public override EquipmentType EquipmentType => EquipmentType.Body;

        public override Vector2int SizeInInventory => new Vector2int(2, 2);

        public override TechType RequiredForUnlock => TechType.ReinforcedDiveSuit;

        public override TechGroup GroupForPDA => TechGroup.Personal;

        public override TechCategory CategoryForPDA => TechCategory.Equipment;

        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;

        public override string[] StepsToFabricatorTab => new string[] { "Personal", "Equipment" };

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override GameObject GetGameObject()
        {
            return Object.Instantiate(CraftData.GetPrefabForTechType(TechType.ReinforcedDiveSuit));
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[6]
                {
                    new Ingredient(TechType.AramidFibers, 3),
                    new Ingredient(TechType.Diamond, 2),
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.WiringKit, 1)
                }),
                LinkedItems = new List<TechType>() {
                    Main.glovesPrefab.TechType,
                    Main.helmetPrefab.TechType
                }
            };
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
        }
    }
}
