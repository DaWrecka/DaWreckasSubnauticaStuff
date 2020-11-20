﻿using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using UnityEngine;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Interfaces;

#if SUBNAUTICA
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
#endif

namespace AcidProofSuit.Module
{
    internal class AcidGlovesPrefab : Equipable
    {
        public override EquipmentType EquipmentType => EquipmentType.Gloves;

        public override Vector2int SizeInInventory => new Vector2int(2, 2);

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
                Ingredients = new List<Ingredient>()
            };
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
        }

        public AcidGlovesPrefab() : base("AcidGloves", "Brine Gloves", "Reinforced dive gloves with an acid-resistant layer")
        {
        }
    }

    internal class AcidHelmetPrefab : Equipable
    {
        public AcidHelmetPrefab() : base("AcidHelmet", "Brine Helmet", "Rebreather treated with an acid-resistant layer")
        {
        }

        public override EquipmentType EquipmentType => EquipmentType.Head;

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
                Ingredients = new List<Ingredient>()
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
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.AramidFibers, 3),
                    new Ingredient(TechType.Diamond, 2),
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.WiringKit, 1)
                })
            };

            recipe.LinkedItems.Add(Main.glovesPrefab.TechType);
            recipe.LinkedItems.Add(Main.helmetPrefab.TechType);

            return recipe;
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
        }
    }

    abstract class bpSupplemental : Craftable
    {
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;

        public override string[] StepsToFabricatorTab => new string[] { "BodyMenu" };

        public virtual QuickSlotType QuickSlotType => QuickSlotType.None;

        public override GameObject GetGameObject()
        {
            return Object.Instantiate(CraftData.GetPrefabForTechType(TechType.ReinforcedDiveSuit));
        }

        public bpSupplemental(string classId, string friendlyName, string description) : base(classId, friendlyName, description)
        {
            /*OnStartedPatching += () =>
            {
                CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "BodyMenu", "Suit Upgrades", SpriteManager.Get(TechType.Stillsuit));
            };*/

            OnFinishedPatching += () =>
            {
                SpriteHandler.RegisterSprite(base.TechType, SpriteManager.Get(TechType.ReinforcedDiveSuit));
            };
        }
    }

    internal class bpSupplemental_Suits : bpSupplemental
    {
        // This is the recipe that allows already-crafted suits to be used; requires a full Radiation Suit, Reinforced Dive Suit, and Rebreather, plus the HCl, Creepvine and Sulphur of the base recipe.
        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.RadiationGloves, 1),
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.RadiationSuit, 1),
                    new Ingredient(TechType.ReinforcedDiveSuit, 1),
                    new Ingredient(TechType.ReinforcedGloves, 1),
                    new Ingredient(TechType.Rebreather, 1)
                })
            };

            recipe.LinkedItems.Add(Main.glovesPrefab.TechType);
            recipe.LinkedItems.Add(Main.helmetPrefab.TechType);
            recipe.LinkedItems.Add(Main.suitPrefab.TechType);

            return recipe;
        }

        public bpSupplemental_Suits() : base("bpSupplemental_Suits", "Brine Suit", "Reinforced dive suit with layers of acid and radiation protection.")
        {
        }
    }

    internal class bpSupplemental_OnlyRadSuit : bpSupplemental
    {
        // This is the recipe that uses an existing Rad Suit only.
        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.RadiationGloves, 1),
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.RadiationSuit, 1),
                    new Ingredient(TechType.AramidFibers, 2),
                    new Ingredient(TechType.Diamond, 2),
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.WiringKit, 1)
                })
            };

            recipe.LinkedItems.Add(Main.glovesPrefab.TechType);
            recipe.LinkedItems.Add(Main.helmetPrefab.TechType);
            recipe.LinkedItems.Add(Main.suitPrefab.TechType);

            return recipe;
        }

        public bpSupplemental_OnlyRadSuit() : base("bpSupplemental_OnlyRadSuit", "Brine Suit", "Reinforced dive suit with layers of acid and radiation protection.")
        {
        }
    }

    internal class bpSupplemental_OnlyRebreather : bpSupplemental
    {
        // This is the recipe that uses an existing Rebreather only
        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.AramidFibers, 2),
                    new Ingredient(TechType.Diamond, 2),
                    new Ingredient(TechType.Titanium, 1),
                    new Ingredient(TechType.Rebreather, 1)
                })
            };

            recipe.LinkedItems.Add(Main.glovesPrefab.TechType);
            recipe.LinkedItems.Add(Main.helmetPrefab.TechType);
            recipe.LinkedItems.Add(Main.suitPrefab.TechType);

            return recipe;
        }

        public bpSupplemental_OnlyRebreather() : base("bpSupplemental_OnlyRebreather", "Brine Suit", "Reinforced dive suit with layers of acid and radiation protection.")
        {
        }
    }

    internal class bpSupplemental_OnlyReinforcedSuit : bpSupplemental
    {
        // This is the recipe that uses an existing Reinforced Dive Suit only
        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.AramidFibers, 2),
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.ReinforcedDiveSuit, 1),
                    new Ingredient(TechType.ReinforcedGloves, 1),
                    new Ingredient(TechType.WiringKit, 1)
                })
            };

            recipe.LinkedItems.Add(Main.glovesPrefab.TechType);
            recipe.LinkedItems.Add(Main.helmetPrefab.TechType);
            recipe.LinkedItems.Add(Main.suitPrefab.TechType);

            return recipe;
        }

        public bpSupplemental_OnlyReinforcedSuit() : base("bpSupplemental_OnlyReinforcedSuit", "Brine Suit", "Reinforced dive suit with layers of acid and radiation protection.")
        {
        }
    }

    internal class bpSupplemental_RebreatherRad : bpSupplemental
    {
        // This is the recipe that uses an existing Rebreather and Rad Suit
        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.RadiationGloves, 1),
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.RadiationSuit, 1),
                    new Ingredient(TechType.AramidFibers, 2),
                    new Ingredient(TechType.Diamond, 2),
                    new Ingredient(TechType.Titanium, 1),
                    new Ingredient(TechType.Rebreather, 1)
                })
            };

            recipe.LinkedItems.Add(Main.glovesPrefab.TechType);
            recipe.LinkedItems.Add(Main.helmetPrefab.TechType);
            recipe.LinkedItems.Add(Main.suitPrefab.TechType);

            return recipe;
        }

        public bpSupplemental_RebreatherRad() : base("bpSupplemental_RebreatherRad", "Brine Suit", "Reinforced dive suit with layers of acid and radiation protection.")
        {
        }
    }

    internal class bpSupplemental_RebreatherReinforced : bpSupplemental
    {
        // This is the recipe that uses an existing Rebreather and Reinforced Dive Suit
        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.AramidFibers, 1),
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.ReinforcedDiveSuit, 1),
                    new Ingredient(TechType.ReinforcedGloves, 1),
                    new Ingredient(TechType.Rebreather, 1)
                })
            };

            recipe.LinkedItems.Add(Main.glovesPrefab.TechType);
            recipe.LinkedItems.Add(Main.helmetPrefab.TechType);
            recipe.LinkedItems.Add(Main.suitPrefab.TechType);

            return recipe;
        }

        public bpSupplemental_RebreatherReinforced() : base("bpSupplemental_RebreatherReinforced", "Brine Suit", "Reinforced dive suit with layers of acid and radiation protection.")
        {
        }
    }

    internal class bpSupplemental_RadReinforced : bpSupplemental
    {
        // This is the recipe that uses an existing Rad Suit and Reinforced Dive Suit
        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.RadiationGloves, 1),
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.RadiationSuit, 1),
                    new Ingredient(TechType.ReinforcedDiveSuit, 1),
                    new Ingredient(TechType.ReinforcedGloves, 1),
                    new Ingredient(TechType.AramidFibers, 1),
                    new Ingredient(TechType.WiringKit, 1)
                })
            };

            recipe.LinkedItems.Add(Main.glovesPrefab.TechType);
            recipe.LinkedItems.Add(Main.helmetPrefab.TechType);
            recipe.LinkedItems.Add(Main.suitPrefab.TechType);

            return recipe;
        }

        public bpSupplemental_RadReinforced() : base("bpSupplemental_RadReinforced", "Brine Suit", "Reinforced dive suit with layers of acid and radiation protection.")
        {
        }
    }
}
