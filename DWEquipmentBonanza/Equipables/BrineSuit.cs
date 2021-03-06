using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using UnityEngine;
using SMLHelper.V2.Handlers;
using System.Collections;
using Common;
using DWEquipmentBonanza.Patches;

#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza
{
#if SUBNAUTICA_STABLE
    internal class AcidGloves : Equipable
    {
        private static Sprite itemSprite;
        private static GameObject prefab;

        public override EquipmentType EquipmentType => EquipmentType.Gloves;

        public override Vector2int SizeInInventory => new Vector2int(2, 2);

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override GameObject GetGameObject()
        {
            System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
            if (prefab == null)
            {
                prefab = ModifyAndInstantiateGameObject(CraftData.GetPrefabForTechType(TechType.ReinforcedGloves));
            }


            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
            return prefab;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ReinforcedGloves);
                yield return task;

                prefab = ModifyAndInstantiateGameObject(task.GetResult());
            }

            gameObject.Set(prefab);
        }

        protected GameObject ModifyAndInstantiateGameObject(GameObject prefab)
        {
            var obj = GameObject.Instantiate(prefab);
            Shader shader = Shader.Find("MarmosetUBER");
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.name == "reinforced_suit_01_gloves")
                {
                    renderer.sharedMaterial.shader = shader;
                    renderer.material.shader = shader;

                    renderer.sharedMaterial.mainTexture = Main.glovesTexture;
                    renderer.material.mainTexture = Main.glovesTexture;

                    renderer.sharedMaterial.SetTexture("_Illum", Main.glovesIllumTexture);
                    renderer.material.SetTexture("_Illum", Main.glovesIllumTexture);

                    renderer.sharedMaterial.SetTexture("_SpecTex", Main.glovesTexture);
                    renderer.material.SetTexture("_SpecTex", Main.glovesTexture);
                }
            }
            return obj;
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
            if (itemSprite == null || itemSprite == SpriteManager.defaultSprite)
            {
                itemSprite = ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
            }

            return itemSprite;
        }

        public AcidGloves() : base("AcidGloves", "Brine Gloves", "Reinforced dive gloves with an acid-resistant layer")
        {
            OnFinishedPatching += () =>
            {
                TechTypeUtils.AddModTechType(this.TechType);
                EquipmentPatch.AddSubstitutions(this.TechType, new HashSet<TechType>() { TechType.RadiationGloves, TechType.ReinforcedGloves });
            };
        }
    }

    internal class AcidHelmet : Equipable
    {
        public static Texture2D texture;
        public static Texture2D illumTexture;
        private static Sprite itemSprite;
        private static GameObject prefab;

        public AcidHelmet() : base("AcidHelmet", "Brine Helmet", "Rebreather treated with an acid-resistant layer")
        {
            OnFinishedPatching += () =>
            {
                TechTypeUtils.AddModTechType(this.TechType);
                EquipmentPatch.AddSubstitution(this.TechType, TechType.Rebreather);
                EquipmentPatch.AddSubstitution(this.TechType, TechType.RadiationHelmet);
                texture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidHelmetskin.png"));
                illumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidHelmetillum.png"));
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Head;

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override Vector2int SizeInInventory => new Vector2int(2, 2);

        public override GameObject GetGameObject()
        {
            System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
            if (prefab == null)
            {
                prefab = ModifyAndInstantiateGameObject(CraftData.GetPrefabForTechType(TechType.ReinforcedGloves));
            }

            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
            return prefab;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ReinforcedGloves);
                yield return task;

                prefab = ModifyAndInstantiateGameObject(task.GetResult());
            }

            gameObject.Set(prefab);
        }

        protected GameObject ModifyAndInstantiateGameObject(GameObject prefab)
        {
            var obj = Object.Instantiate(prefab);
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Shader shader = Shader.Find("MarmosetUBER");
            foreach (var renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    material.shader = shader; // apply the shader
                    material.mainTexture = texture; // apply the main texture
                    material.SetTexture(ShaderPropertyID._Illum, illumTexture); // apply the illum texture
                    material.SetTexture("_SpecTex", texture); // apply the spec texture
                }
            }
            return obj;
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
            if (itemSprite == null || itemSprite == SpriteManager.defaultSprite)
            {
                itemSprite = ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
            }

            return itemSprite;
        }
    }

    internal class AcidSuit : Equipable
    {
        private static Sprite itemSprite;
        private static GameObject prefab;

        public AcidSuit(string classId = "AcidSuit", string friendlyName = "Brine Suit", string description = "Reinforced dive suit with an acid-resistant layer") : base(classId, friendlyName, description)
        {
            OnFinishedPatching += () =>
            {
                TechTypeUtils.AddModTechType(this.TechType);
                EquipmentPatch.AddSubstitutions(this.TechType, new HashSet<TechType>() { TechType.RadiationSuit, TechType.ReinforcedDiveSuit });
                Main.AddDiveSuit(this.TechType, 800f, 0.85f, 15f);
                KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
                compound.techType = this.TechType;
                compound.dependencies = new List<TechType>()
                {
                    TechType.ReinforcedDiveSuit,
                    TechType.RadiationSuit
                };
                Reflection.AddCompoundTech(compound);
            };
        }
        protected virtual float maxDepth => 800f;

        public override EquipmentType EquipmentType => EquipmentType.Body;
        public override Vector2int SizeInInventory => new Vector2int(2, 2);
        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override TechGroup GroupForPDA => TechGroup.Personal;
        public override TechCategory CategoryForPDA => TechCategory.Equipment;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
        public override string[] StepsToFabricatorTab => new string[] { "Personal", "Equipment" };
        public override QuickSlotType QuickSlotType => QuickSlotType.None;

#if SUBNAUTICA_STABLE
        public override GameObject GetGameObject()
        {
            System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");
            if (prefab == null)
            {
                prefab = ModifyAndInstantiateGameObject(CraftData.GetPrefabForTechType(TechType.ReinforcedGloves));
            }

            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");

            return prefab;
        }
#endif

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ReinforcedGloves);
                yield return task;

                prefab = ModifyAndInstantiateGameObject(task.GetResult());
            }

            gameObject.Set(prefab);
        }

        protected GameObject ModifyAndInstantiateGameObject(GameObject prefab)
        {
            var obj = Object.Instantiate(prefab);
            Shader shader = Shader.Find("MarmosetUBER");
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.name == "reinforced_suit_01")
                {
                    // apply the shader
                    renderer.sharedMaterial.shader = shader;
                    renderer.material.shader = shader;

                    // apply the main texture
                    renderer.sharedMaterial.mainTexture = Main.suitTexture;
                    renderer.material.mainTexture = Main.suitTexture;
                    renderer.materials[1].mainTexture = Main.suitTexture;

                    // apply the spec map
                    renderer.sharedMaterial.SetTexture("_SpecTex", Main.suitTexture);
                    renderer.material.SetTexture("_SpecTex", Main.suitTexture);
                    renderer.materials[1].SetTexture("_SpecTex", Main.suitTexture);

                    // apply the illum map
                    renderer.sharedMaterial.SetTexture(ShaderPropertyID._Illum, Main.suitIllumTexture);
                    renderer.material.SetTexture(ShaderPropertyID._Illum, Main.suitIllumTexture);
                    renderer.materials[1].SetTexture(ShaderPropertyID._Illum, Main.suitIllumTexture);
                }
            }
            return obj;
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    /*new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.CreepvinePiece, 2),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.AramidFibers, 3),
                    new Ingredient(TechType.Diamond, 2),
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.WiringKit, 1)*/
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.FiberMesh, 1),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.RadiationGloves, 1),
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.RadiationSuit, 1),
                    new Ingredient(TechType.ReinforcedDiveSuit, 1),
                    new Ingredient(TechType.ReinforcedGloves, 1),
                    new Ingredient(TechType.Rebreather, 1)
                }),
                LinkedItems = new List<TechType>()
                {
                    TechTypeUtils.GetModTechType("AcidGloves"),
                    TechTypeUtils.GetModTechType("AcidHelmet")
                }
            };

            //recipe.LinkedItems.Add(AcidGlovesPrefab.TechTypeID);
            //recipe.LinkedItems.Add(AcidHelmetPrefab.TechTypeID);

            return recipe;
        }

        protected override Sprite GetItemSprite()
        {
            if (itemSprite == null || itemSprite == SpriteManager.defaultSprite)
            {
                itemSprite = ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
            }

            return itemSprite;
        }
    }

    abstract class Blueprint : Craftable
    {
        // A base class for all of the modification recipes that use existing suit pieces
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;

        public override string[] StepsToFabricatorTab => new string[] { "BodyMenu" };

        public virtual QuickSlotType QuickSlotType => QuickSlotType.None;

        public override GameObject GetGameObject()
        {
            return CraftData.GetPrefabForTechType(TechType.ReinforcedDiveSuit);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ReinforcedDiveSuit);
            yield return task;

            gameObject.Set(task.GetResult());
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.ReinforcedDiveSuit);
        }

        public Blueprint(string classId, string friendlyName, string description) : base(classId, friendlyName, description)
        {
            OnFinishedPatching += () =>
            {
            };
        }
    }

    /*internal class Blueprint_Suits : Blueprint
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
                    new Ingredient(TechType.FiberMesh, 1),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.RadiationGloves, 1),
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.RadiationSuit, 1),
                    new Ingredient(TechType.ReinforcedDiveSuit, 1),
                    new Ingredient(TechType.ReinforcedGloves, 1),
                    new Ingredient(TechType.Rebreather, 1)
                })
            };

            recipe.LinkedItems.Add(TechTypeUtils.GetModTechType("AcidGloves"));
            recipe.LinkedItems.Add(TechTypeUtils.GetModTechType("AcidHelmet"));
            recipe.LinkedItems.Add(TechTypeUtils.GetModTechType("AcidSuit"));

            return recipe;
        }

        public Blueprint_Suits() : base("Blueprint_Suits", "Brine Suit", "Reinforced dive suit with layers of acid and radiation protection.")
        {
        }
    }*/

    internal class NitrogenBrineSuit2 : AcidSuit
    {
        public static string title = "Brine Suit Mk2";
        public static string description = "Upgraded dive suit, immune to acid, heat protection up to 90C and depth protection up to 1300m";

        public override string[] StepsToFabricatorTab => new string[] { "Personal", "Equipment" };

        public override TechType RequiredForUnlock => Main.GetNitrogenTechtype("rivereelscale");

        protected override RecipeData GetBlueprintRecipe()
        {
            if (!Main.HasNitrogenMod())
                return new RecipeData() { };

            TechType ttEelScale = Main.GetNitrogenTechtype("rivereelscale");
            if (ttEelScale == TechType.None)
                return new RecipeData() { };

            RecipeData recipe = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.FiberMesh, 1),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.AramidFibers, 3),
                    new Ingredient(TechType.Diamond, 2),
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.WiringKit, 1),
                    new Ingredient(TechType.AluminumOxide, 2),
                    new Ingredient(ttEelScale, 2)
                }),
                LinkedItems = new List<TechType>()
                {
                    TechTypeUtils.GetModTechType("AcidGloves"),
                    TechTypeUtils.GetModTechType("AcidHelmet")
                }
            };

            return recipe;
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
        }

        public NitrogenBrineSuit2() : base("NitrogenBrineSuit2", title, description)
        {
            OnFinishedPatching += () =>
            {
                TechTypeUtils.AddModTechType(this.TechType);
                Main.AddDiveSuit(this.TechType, 1300f, 0.75f, 20f);
            };
        }
    }

    internal class NitrogenBrineSuit3 : AcidSuit
    {
        public static string title = "Brine Suit Mk3";
        public static string description = "Upgraded dive suit, immune to acid, heat protection up to 105C and effectively-unlimited depth protection";

        public override string[] StepsToFabricatorTab => new string[] { "Personal", "Equipment" };

        public override TechType RequiredForUnlock => Main.GetNitrogenTechtype("lavalizardscale");


        protected override RecipeData GetBlueprintRecipe()
        {
            if (!Main.HasNitrogenMod())
                return new RecipeData() { };

            TechType ttEelScale = Main.GetNitrogenTechtype("rivereelscale");
            TechType ttLizardScale = Main.GetNitrogenTechtype("lavalizardscale");

            if (ttEelScale == TechType.None || ttLizardScale == TechType.None)
                return new RecipeData() { };

            RecipeData recipe = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.FiberMesh, 1),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.AramidFibers, 3),
                    new Ingredient(TechType.Diamond, 2),
                    new Ingredient(TechType.Titanium, 2),
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.WiringKit, 1),
                    new Ingredient(TechType.AluminumOxide, 2),
                    new Ingredient(ttEelScale, 2),
                    new Ingredient(TechType.Kyanite, 2),
                    new Ingredient(ttLizardScale, 2)
                }),
                LinkedItems = new List<TechType>()
                {
                    TechTypeUtils.GetModTechType("AcidGloves"),
                    TechTypeUtils.GetModTechType("AcidHelmet")
                }
            };

            

            return recipe;
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
        }

        public NitrogenBrineSuit3() : base("NitrogenBrineSuit3", title, description)
        {
            OnFinishedPatching += () =>
            {
                TechTypeUtils.AddModTechType(this.TechType);
                Main.AddDiveSuit(this.TechType, 8000f, 0.55f, 35f);
            };
        }
    }

    internal class Blueprint_BrineMk1toMk2 : Blueprint
    {
        // This is the recipe that turns a Brine Suit into a Brine Suit Mk2
        public override string[] StepsToFabricatorTab => new string[] { "ReinforcedSuits" };

        protected override RecipeData GetBlueprintRecipe()
        {
            TechType ttAnimalScale = Main.GetNitrogenTechtype("rivereelscale");
            if (ttAnimalScale == TechType.None)
                return new RecipeData() { };
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechTypeUtils.GetModTechType("AcidGloves"), 1),
                    new Ingredient(TechType.AluminumOxide, 2),
                    new Ingredient(ttAnimalScale, 2)
                }),
                LinkedItems = new List<TechType>()
                {
                    TechTypeUtils.GetModTechType("NitrogenBrineSuit2")
                }
            };

            return recipe;
        }

        public Blueprint_BrineMk1toMk2() : base("Blueprint_BrineMk1toMk2", NitrogenBrineSuit2.title, NitrogenBrineSuit2.description)
        {
        }
    }

    internal class Blueprint_BrineMk2toMk3 : Blueprint
    {
        // This is the recipe that turns a Brine Suit Mk2 into a Brine Suit Mk3
        public override string[] StepsToFabricatorTab => new string[] { "ReinforcedSuits" };

        protected override RecipeData GetBlueprintRecipe()
        {
            TechType ttEelScale = Main.GetNitrogenTechtype("rivereelscale");
            TechType ttLizardScale = Main.GetNitrogenTechtype("lavalizardscale");

            if (ttEelScale == TechType.None || ttLizardScale == TechType.None)
                return new RecipeData() { };

            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechTypeUtils.GetModTechType("NitrogenBrineSuit2"), 1),
                    new Ingredient(TechType.Kyanite, 2),
                    new Ingredient(ttLizardScale, 2)
                })
            };

            recipe.LinkedItems.Add(TechTypeUtils.GetModTechType("NitrogenBrineSuit3"));

            return recipe;
        }

        public Blueprint_BrineMk2toMk3() : base("Blueprint_BrineMk2toMk3", NitrogenBrineSuit3.title, NitrogenBrineSuit3.description)
        {
        }
    }

    /*internal class Blueprint_BrineMk1toMk3 : Blueprint
    {
        // This is the recipe that turns a Brine Suit into a Brine Suit Mk3
        public override string[] StepsToFabricatorTab => new string[] { "ReinforcedSuits" };

        protected override RecipeData GetBlueprintRecipe()
        {
            TechType ttEelScale = Main.GetNitrogenTechtype("rivereelscale");
            TechType ttLizardScale = Main.GetNitrogenTechtype("lavalizardscale");

            if (ttEelScale == TechType.None || ttLizardScale == TechType.None)
                return new RecipeData() { };

            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechTypeUtils.GetModTechType("AcidGloves"), 1),
                    new Ingredient(TechType.AluminumOxide, 2),
                    new Ingredient(ttEelScale, 2),
                    new Ingredient(TechType.Kyanite, 2),
                    new Ingredient(ttLizardScale, 2)
                })
            };

            recipe.LinkedItems.Add(TechTypeUtils.GetModTechType("NitrogenBrineSuit3"));

            return recipe;
        }

        public Blueprint_BrineMk1toMk3() : base("Blueprint_BrineMk1toMk3", NitrogenBrineSuit3.title, NitrogenBrineSuit3.description)
        {
        }
    }*/

    internal class Blueprint_ReinforcedMk2toBrineMk2 : Blueprint
    {
        // This is the recipe that turns a Reinforced Dive Suit Mk2 into a Brine Suit Mk2
        public override string[] StepsToFabricatorTab => new string[] { "ReinforcedSuits" };

        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(Main.GetNitrogenTechtype("ReinforcedSuit2"), 1),
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.FiberMesh, 1),
                    new Ingredient(TechType.Aerogel, 1)
                }),
                LinkedItems = new List<TechType>()
                {
                    TechTypeUtils.GetModTechType("NitrogenBrineSuit2")
                }
            };

            return recipe;
        }

        public Blueprint_ReinforcedMk2toBrineMk2() : base("Blueprint_ReinforcedMk2toBrineMk2", NitrogenBrineSuit2.title, NitrogenBrineSuit2.description)
        {
        }
    }

    internal class Blueprint_ReinforcedMk3toBrineMk3 : Blueprint
    {
        // This is the recipe that turns a Reinforced Dive Suit Mk3 into a Brine Suit Mk3
        public override string[] StepsToFabricatorTab => new string[] { "ReinforcedSuits" };

        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(Main.GetNitrogenTechtype("ReinforcedSuit3"), 1),
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.FiberMesh, 1),
                    new Ingredient(TechType.Aerogel, 1)
                }),
                LinkedItems = new List<TechType>()
                {
                    TechTypeUtils.GetModTechType("NitrogenBrineSuit3")
                }
            };

            return recipe;
        }

        public Blueprint_ReinforcedMk3toBrineMk3() : base("Blueprint_ReinforcedMk3toBrineMk3", NitrogenBrineSuit3.title, NitrogenBrineSuit3.description)
        {
        }
    }

    /*internal class Blueprint_ReinforcedMk2toBrineMk3 : Blueprint
    {
        // This is the recipe that turns a Reinforced Dive Suit Mk2 into a Brine Suit Mk3
        public override string[] StepsToFabricatorTab => new string[] { "ReinforcedSuits" };

        protected override RecipeData GetBlueprintRecipe()
        {
            TechType ttLizardScale = Main.GetNitrogenTechtype("lavalizardscale");

            if (ttLizardScale == TechType.None)
                return new RecipeData() { };


            RecipeData recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(Main.GetNitrogenTechtype("ReinforcedSuit2"), 1),
                    new Ingredient(TechType.HydrochloricAcid, 1),
                    new Ingredient(TechType.FiberMesh, 1),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.Kyanite, 2),
                    new Ingredient(ttLizardScale, 2)
                }),
                LinkedItems = new List<TechType>()
                {
                    TechTypeUtils.GetModTechType("NitrogenBrineSuit3")
                }
            };

            return recipe;
        }

        public Blueprint_ReinforcedMk2toBrineMk3() : base("Blueprint_ReinforcedMk2toBrineMk3", NitrogenBrineSuit3.title, NitrogenBrineSuit3.description)
        {
        }
    }*/
#endif
}
