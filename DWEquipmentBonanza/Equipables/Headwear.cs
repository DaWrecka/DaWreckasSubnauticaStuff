using Common;
using DWEquipmentBonanza.MonoBehaviours;
using DWEquipmentBonanza.Patches;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if SUBNAUTICA_STABLE
    using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
#endif

namespace DWEquipmentBonanza.Equipables
{
    public abstract class HeadwearBase<T> : Equipable
    {
        protected static GameObject prefab;
        protected static Sprite sprite;
        protected virtual float tempBonus => 0f;
        protected virtual TechType spriteTemplate => TechType.None;
        protected abstract List<TechType> compoundTech { get; }
        protected abstract List<TechType> substitutions { get; }

        public override EquipmentType EquipmentType => EquipmentType.Head;
        public override Vector2int SizeInInventory => new(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        protected override Sprite GetItemSprite()
        {
            if (sprite == null || sprite == SpriteManager.defaultSprite)
            {
                try
                {
                    sprite = ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
                }
                catch
                {
                    if (spriteTemplate != TechType.None)
#if SUBNAUTICA_STABLE
                        sprite = SpriteManager.GetWithNoDefault(spriteTemplate);
#elif BELOWZERO
                        sprite = SpriteManager.Get(spriteTemplate, null);
#endif
                }
            }

            return sprite;
        }

        public HeadwearBase(string classId, string friendlyName, string description) : base(classId, friendlyName, description)
        {
            OnFinishedPatching += () =>
            {
                Main.AddModTechType(this.TechType);
                if (compoundTech != null && compoundTech.Count > 0)
                {
                    Reflection.AddCompoundTech(this.TechType, compoundTech);
                }
                if (substitutions != null && substitutions.Count > 0)
                {
                    foreach (TechType tt in substitutions)
                        EquipmentPatch.AddSubstitution(this.TechType, tt);
                }
                Main.AddTempBonusOnly(this.TechType, tempBonus);
            };
        }
    }

#if SUBNAUTICA_STABLE
    internal class AcidHelmet : HeadwearBase<AcidHelmet>
    {
        public static Texture2D texture;
        public static Texture2D illumTexture;
        protected override float tempBonus => 8f;
        
        protected override List<TechType> substitutions => null;
        protected override List<TechType> compoundTech => null;
        protected override TechType spriteTemplate => TechType.None;

        public AcidHelmet() : base("AcidHelmet", "Brine Helmet", "Rebreather treated with an acid-resistant layer")
        {
            OnFinishedPatching += () =>
            {
                TechTypeUtils.AddModTechType(this.TechType);
                Main.AddDamageResist(this.TechType, DamageType.Acid, 0.25f);
                EquipmentPatch.AddSubstitution(this.TechType, TechType.RadiationHelmet);
                texture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidHelmetskin.png"));
                illumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidHelmetillum.png"));
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Head;

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override Vector2int SizeInInventory => new(2, 2);

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

        /*protected override Sprite GetItemSprite()
        {
            if (sprite == null || sprite == SpriteManager.defaultSprite)
            {
                sprite = ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
            }

            return sprite;
        }*/
    }

#elif BELOWZERO
    internal class InsulatedRebreather : HeadwearBase<InsulatedRebreather>
    {
        protected override TechType spriteTemplate => TechType.ColdSuitHelmet;
        protected override float tempBonus => 8f;

        public InsulatedRebreather(string classId = "InsulatedRebreather",
            string friendlyName = "Insulated Rebreather",
            string description = "Rebreather equipped with insulation helps slow the onset of hypothermia") : base(classId, friendlyName, description)
        {
            OnFinishedPatching += () => OnFinishedPatch();
        }

        protected virtual void OnFinishedPatch()
        {
            int coldResist = TechData.GetColdResistance(TechType.ColdSuitHelmet);
            DWEquipmentBonanza.Reflection.AddColdResistance(this.TechType, System.Math.Max(20, coldResist));
            DWEquipmentBonanza.Reflection.SetItemSize(this.TechType, 2, 2);
            Log.LogDebug($"Finished patching {this.TechType.AsString()}, using source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
        }

        protected override List<TechType> compoundTech => new List<TechType>()
        {
            TechType.ColdSuitHelmet,
            TechType.Rebreather
        };

        protected override List<TechType> substitutions => new List<TechType>()
        {
            TechType.ColdSuitHelmet,
            TechType.Rebreather
        };

        public override EquipmentType EquipmentType => EquipmentType.Head;
        public override Vector2int SizeInInventory => new(2, 2);
        public override QuickSlotType QuickSlotType => QuickSlotType.None;
        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.ColdSuitHelmet, 1),
                    new Ingredient(TechType.Rebreather, 1)
                }
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuitHelmet, verbose: true);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
            }

            gameObject.Set(prefab);
        }
    }

    internal class UltimateHelmet : HeadwearBase<UltimateHelmet>
    {
        protected override float tempBonus => 10f;
        protected override List<TechType> compoundTech => new List<TechType>
        {
            TechType.Rebreather,
            TechType.ColdSuit,
            TechType.FlashlightHelmet
        };

        protected override List<TechType> substitutions => new List<TechType>()
        {
            TechType.ColdSuitHelmet,
            TechType.Rebreather
        };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.FlashlightHelmet, 1),
                    new Ingredient(Main.GetModTechType("InsulatedRebreather"), 1)
                }
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.FlashlightHelmet);
                yield return task;

                prefab = GameObject.Instantiate(task.GetResult());
                //ModPrefabCache.AddPrefab(prefab, false);
                prefab.EnsureComponent<FlashlightEnablerBZ>();
            }

            gameObject.Set(prefab);
        }

        public UltimateHelmet() : base("UltimateHelmet", "Ultimate Helmet", "The ultimate in survival headwear. An insulated helmet with integrated rebreather and lamp.")
        {
            OnFinishedPatching += () =>
            {
                TooltipFactoryPatches.AddNoBarTechType(this.TechType);
            };
        }
    }
#endif
}
