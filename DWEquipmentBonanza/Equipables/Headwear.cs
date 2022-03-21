using Common;
using Common.Utility;
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
using UWE;
#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
#endif

namespace DWEquipmentBonanza.Equipables
{
    public abstract class HeadwearBase<T> : Equipable
    {
        //protected static GameObject prefab;
        protected static Sprite sprite;
        protected virtual float tempBonus => 0f;
        protected virtual TechType spriteTemplate => TechType.None;
        protected abstract TechType prefabTemplate { get; }
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
            if (sprite == null)
            {
                try
                {
                    sprite = ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/{ClassID}.png");
                }
                catch
                {
                    if (spriteTemplate != TechType.None)
                        sprite = SpriteUtils.GetWithNoDefault(spriteTemplate);
                }
            }

            return sprite;
        }

        protected virtual GameObject ModifyGameObject(GameObject prefab)
        {
            return prefab;
        }

#if !ASYNC
        public override GameObject GetGameObject()
        {
            return ModifyGameObject(CraftData.GetPrefabForTechType(prefabTemplate));
        }
#endif

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject modPrefab;
            if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate);
                yield return task;

                modPrefab = ModifyGameObject(GameObject.Instantiate(task.GetResult(), default(Vector3), default(Quaternion), false));
                TechTypeUtils.AddModTechType(this.TechType, modPrefab);
            }

            gameObject.Set(modPrefab);
        }

        protected virtual void OnFinishedPatch()
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
            if(tempBonus > 0f)
                Main.AddTempBonusOnly(this.TechType, tempBonus);
        }

        public HeadwearBase(string classId, string friendlyName, string description) : base(classId, friendlyName, description)
        {
            OnFinishedPatching += () => OnFinishedPatch();
        }
    }

#if SUBNAUTICA
    public class FlashlightHelmet : HeadwearBase<FlashlightHelmet>
    {
        public static GameObject flashlightPrefab { get; internal set; }
        public static GameObject flashlightHelmetPrefab { get; internal set; }
        protected override TechType prefabTemplate => TechType.Rebreather;

        protected override List<TechType> compoundTech => null;
        protected override List<TechType> substitutions => null;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Fabricator;
        public override string[] StepsToFabricatorTab => new string[] { "Personal", "Equipment" };

        public static bool bPrefabsPrepared { get; private set; }
        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.PrecursorIonCrystal, 1),
                    new Ingredient(TechType.Titanium, 1),
                    new Ingredient(TechType.EnameledGlass, 1)
                }
            };
        }

#if !ASYNC
        public override GameObject GetGameObject()
        {
            GameObject modPrefab;

            if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
                FlashlightHelmet.flashlightPrefab ??= CraftData.GetPrefabForTechType(TechType.Flashlight);
                flashlightHelmetPrefab ??= CraftData.GetPrefabForTechType(TechType.Rebreather, false);

                modPrefab = ModifyGameObject(flashlightHelmetPrefab);
                TechTypeUtils.AddModTechType(this.TechType, modPrefab);
            }

            return modPrefab;
        }
#endif

        protected override GameObject ModifyGameObject(GameObject go)
        {
            return FlashlightHelmet.PreparePrefab(GameObjectUtils.InstantiateInactive(go), this.ClassID);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject modPrefab;
            if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
#if SUBNAUTICA
                yield return FlashlightHelmet.SetUpPrefabsCoroutine();
#endif
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate);
                yield return task;

                modPrefab = ModifyGameObject(task.GetResult());
                TechTypeUtils.AddModTechType(this.TechType, modPrefab);

            }
            gameObject.Set(modPrefab);
        }

        public static void SetUpPrefabs()
        {
            if (bPrefabsPrepared)
                return;
#if ASYNC
            CoroutineHost.StartCoroutine(SetUpPrefabsCoroutine());
#else
            FlashlightHelmet.flashlightPrefab ??= CraftData.GetPrefabForTechType(TechType.Flashlight);
            bPrefabsPrepared = true;
#endif
        }

        public static IEnumerator SetUpPrefabsCoroutine()
        {
            bPrefabsPrepared = true;
            if (FlashlightHelmet.flashlightPrefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.Flashlight);
                yield return task;

                FlashlightHelmet.flashlightPrefab = task.GetResult();
            }

            if (FlashlightHelmet.flashlightHelmetPrefab == null)
            {
                CoroutineTask<GameObject> prefabTask = CraftData.GetPrefabForTechTypeAsync(TechType.Rebreather);
                yield return prefabTask;

                flashlightHelmetPrefab = GameObjectUtils.InstantiateInactive(prefabTask.GetResult());
            }

            yield break;
        }

        public static GameObject PreparePrefab(GameObject prefabToModify, string classID)
        {
            if (prefabToModify == null)
            {
                Log.LogError($"FlashlightHelmet.PreparePrefab called with null prefab!");
                return null;
            }

            FlashlightHelmetComponentSN1 headlightComponent = prefabToModify.GetComponent<FlashlightHelmetComponentSN1>();
            if(headlightComponent == null)
            {
                Log.LogDebug($"Setting up prefab {classID}");
                prefabToModify.name = classID;
                headlightComponent = prefabToModify.AddComponent<FlashlightHelmetComponentSN1>();

                Log.LogDebug($"prefab {classID}: Adding StorageRoot child");
                headlightComponent.storageRoot = prefabToModify.FindChild("StorageRoot") ?? new GameObject("StorageRoot", new Type[] { typeof(ChildObjectIdentifier) });

                Log.LogDebug($"prefab {classID}: Adding HeadLampParent"); 
                var lightsParent = FlashlightHelmetComponentSN1.lightsParent ??= prefabToModify.FindChild("HeadLampParent") ?? new GameObject("HeadLampParent");
                lightsParent.transform.SetParent(Player.main.gameObject.transform);
                lightsParent.name = "HeadLampParent";
                Log.LogDebug($"prefab {classID}: Adding spotlight object");
                var spotLightObject = lightsParent.FindChild("HeadFlashLight_spot") ?? new GameObject("HeadFlashLight_spot", new Type[] { typeof(Light) });
                spotLightObject.transform.SetParent(lightsParent.transform);
                var spotLight = spotLightObject.EnsureComponent<Light>();
                if (spotLight == null)
                {
                    Log.LogError($"Error creating spotLight component on FlashlightHelmet object");
                }
                else
                {
                    spotLightObject.name = "HeadFlashLight_spot";
                    spotLight.type = LightType.Spot;
                    spotLight.spotAngle = 90f;
                    spotLight.innerSpotAngle = 71.41338f;
                    spotLight.color = new Color(0.992f, 0.992f, 0.996f, 1);
                    spotLight.range = 50;
                    spotLight.shadows = LightShadows.Hard;
                }

                Log.LogDebug($"prefab {classID}: Adding point light object");
                var pointLightObject = lightsParent.FindChild("HeadFlashLight_point") ?? new GameObject("HeadFlashLight_point", new Type[] { typeof(Light) });
                var pointLight = pointLightObject.EnsureComponent<Light>();
                pointLightObject.transform.SetParent(lightsParent.transform);
                if (pointLight == null)
                {
                    Log.LogError($"Error creating pointLight component on FlashlightHelmet object");
                }
                else
                {
                    pointLightObject.name = "HeadFlashLight_point";
                    pointLight.type = LightType.Point;
                    pointLight.intensity = 0.9f;
                    pointLight.range = 12;
                }

                Log.LogDebug($"prefab {classID}: Setting up ToggleLights");
                headlightComponent.toggleLights = prefabToModify.EnsureComponent<ToggleLights>();
                headlightComponent.toggleLights.energyMixin = prefabToModify.EnsureComponent<FreeEnergyMixin>();
                headlightComponent.toggleLights.energyMixin.storageRoot = headlightComponent.storageRoot.GetComponent<ChildObjectIdentifier>();
                headlightComponent.toggleLights.lightsParent = lightsParent;
                Log.LogDebug($"prefab {classID}: Configuring lights on/off sounds");
                var toggleLightsPrefab = FlashlightHelmet.flashlightPrefab.GetComponent<ToggleLights>();
                headlightComponent.toggleLights.lightsOnSound = toggleLightsPrefab.lightsOnSound;
                headlightComponent.toggleLights.lightsOffSound = toggleLightsPrefab.lightsOffSound;
                Log.LogDebug($"prefab {classID}: Setup complete");
            }

            return prefabToModify;
        }

        protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();
            SetUpPrefabs();
        }

        public FlashlightHelmet(string classID = "FlashlightHelmet",
            string friendlyName = "Headlamp",
            string Description = "Head-mounted light for hands-free illumination") : base(classID, friendlyName, Description)
        { }
    }
#endif

    internal class IlluminatedRebreather : HeadwearBase<IlluminatedRebreather>
    {
        protected override TechType prefabTemplate => TechType.Rebreather;
        protected override List<TechType> compoundTech => new List<TechType>
        {
            TechType.Rebreather,
            Main.GetModTechType("FlashlightHelmet") // And this is why I named my SN1 version the same as the BZ version
        };

        protected override List<TechType> substitutions => new List<TechType>()
        {
            TechType.Rebreather,
#if BELOWZERO
            Main.GetModTechType("FlashlightHelmet")
#endif
        };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Main.GetModTechType("FlashlightHelmet"), 1),
                    new Ingredient(TechType.Rebreather, 1),
                    new Ingredient(TechType.WiringKit, 1)
                }
            };
        }

#if !ASYNC
        public override GameObject GetGameObject()
        {
            GameObject modPrefab;
            if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
                modPrefab = ModifyGameObject(CraftData.GetPrefabForTechType(prefabTemplate));
                TechTypeUtils.AddModTechType(this.TechType, modPrefab);
            }

            return modPrefab;
        }
#endif

        protected override GameObject ModifyGameObject(GameObject go)
        {
#if SUBNAUTICA
            return FlashlightHelmet.PreparePrefab(GameObjectUtils.InstantiateInactive(go), this.ClassID);
#elif BELOWZERO
            return GameObjectUtils.InstantiateInactive(go);
#endif
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject modPrefab;
            if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
#if SUBNAUTICA
                yield return FlashlightHelmet.SetUpPrefabsCoroutine();
#endif
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate);
                yield return task;

                modPrefab = ModifyGameObject(task.GetResult());
                TechTypeUtils.AddModTechType(this.TechType, modPrefab);

            }
            gameObject.Set(modPrefab);
        }

        protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();
#if BELOWZERO
            TooltipFactoryPatches.AddNoBarTechType(this.TechType);
#endif
        }

        public IlluminatedRebreather() : base("IlluminatedRebreather", "Light Rebreather", "Rebreather equipped with a hands-free lamp.")
        {
        }
    }

#if SUBNAUTICA_STABLE
    internal class LightRadHelmet : HeadwearBase<LightRadHelmet>
    {
        protected override TechType prefabTemplate => TechType.RadiationHelmet;
        protected override List<TechType> compoundTech => new List<TechType>
        {
            TechType.RadiationHelmet,
            Main.GetModTechType("FlashlightHelmet") // And this is why I named my SN1 version the same as the BZ version
        };

        protected override List<TechType> substitutions => new List<TechType>()
        {
            TechType.RadiationHelmet
        };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Main.GetModTechType("FlashlightHelmet"), 1),
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.WiringKit, 1)
                }
            };
        }

        protected override GameObject ModifyGameObject(GameObject go)
        {
            return FlashlightHelmet.PreparePrefab(GameObjectUtils.InstantiateInactive(go), this.ClassID);
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject modPrefab;
            if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
#if SUBNAUTICA
                yield return FlashlightHelmet.SetUpPrefabsCoroutine();
#endif
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate);
                yield return task;

                modPrefab = ModifyGameObject(task.GetResult());
                TechTypeUtils.AddModTechType(this.TechType, modPrefab);

            }
            gameObject.Set(modPrefab);
        }

        protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();
            Main.AddDamageResist(this.TechType, DamageType.Radiation, 0.15f);
            EquipmentPatch.AddSubstitution(this.TechType, TechType.RadiationHelmet);
        }

        public LightRadHelmet() : base("LightRadHelmet", "Light Rad Helmet", "Radiation-protective helmet with attached light")
        {
        }
    }

    internal class AcidHelmet : HeadwearBase<AcidHelmet>
    {
        public static Texture2D texture { get; internal set; }
        public static Texture2D illumTexture { get; internal set; }
        protected override float tempBonus => 8f;

        protected override TechType prefabTemplate => TechType.Rebreather;
        protected override List<TechType> substitutions => new()
        {
            TechType.Rebreather,
            TechType.RadiationHelmet
        };
        protected override List<TechType> compoundTech => null;
        protected override TechType spriteTemplate => TechType.None;
        public override CraftTree.Type FabricatorType => CraftTree.Type.None;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        public AcidHelmet() : base("AcidHelmet", "Brine Helmet", "Rebreather treated with an acid-resistant layer")
        {
            OnFinishedPatching += () =>
            {
                TechTypeUtils.AddModTechType(this.TechType);
                Main.AddDamageResist(this.TechType, DamageType.Acid, 0.25f);
                texture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidHelmetskin.png"));
                illumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidHelmetillum.png"));
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Head;

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override Vector2int SizeInInventory => new(2, 2);

#if !ASYNC
        public override GameObject GetGameObject()
        {
            System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: begin");

            GameObject modPrefab;
            if(!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
                modPrefab = ModifyGameObject(CraftData.GetPrefabForTechType(TechType.Rebreather));
                TechTypeUtils.AddModTechType(this.TechType, modPrefab);
            }

            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}: end");
            return modPrefab;
        }
#endif

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject modPrefab;
            if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
#if SUBNAUTICA
                yield return FlashlightHelmet.SetUpPrefabsCoroutine();
#endif
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate);
                yield return task;

                modPrefab = ModifyGameObject(task.GetResult());
                TechTypeUtils.AddModTechType(this.TechType, modPrefab);

            }
            gameObject.Set(modPrefab);
        }

        protected override GameObject ModifyGameObject(GameObject prefab)
        {
            var obj = GameObjectUtils.InstantiateInactive(prefab);
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
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.FiberMesh, 1),
                    new Ingredient(TechType.Aerogel, 1),
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.Rebreather, 1)
                }
            };
        }
    }

#elif BELOWZERO
    internal class InsulatedRebreather : HeadwearBase<InsulatedRebreather>
    {
        protected override TechType prefabTemplate => TechType.ColdSuitHelmet;
        protected override TechType spriteTemplate => TechType.ColdSuitHelmet;
        protected override float tempBonus => 8f;

        public InsulatedRebreather(string classId = "InsulatedRebreather",
            string friendlyName = "Insulated Rebreather",
            string description = "Rebreather equipped with insulation helps slow the onset of hypothermia") : base(classId, friendlyName, description)
        {
        }

        protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();
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
            GameObject modPrefab;
            if(!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate, verbose: true);
                yield return task;

                modPrefab = GameObjectUtils.InstantiateInactive(task.GetResult());
                //ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
            }

            gameObject.Set(modPrefab);
        }
    }

    internal class LightColdHelmet : HeadwearBase<LightColdHelmet>
    {
        protected override TechType prefabTemplate => TechType.FlashlightHelmet;
        protected override Sprite GetItemSprite()
        {
            sprite ??= ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/ultimatehelmet.png");

            return sprite;
        }

        protected override List<TechType> compoundTech => new List<TechType>
        {
            TechType.Rebreather,
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
                    new Ingredient(TechType.ColdSuitHelmet, 1),
                    new Ingredient(TechType.WiringKit, 1)
                }
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject modPrefab;
            if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate, verbose: true);
                yield return task;

                modPrefab = GameObjectUtils.InstantiateInactive(task.GetResult());
                //ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
            }

            gameObject.Set(modPrefab);
        }

    protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();
            TooltipFactoryPatches.AddNoBarTechType(this.TechType);
        }

        public LightColdHelmet() : base("LightColdHelmet", "Light Cold Helmet", "Insulated, heat-retaining helmet equipped with a hands-free lamp.")
        {
        }
    }

    internal class Blueprint_LightColdToUltimateHelmet : Craftable
    {
        protected static Sprite sprite;
        protected static TechType fallbackSprite => Main.GetModTechType("UltimateHelmet");
        public override Vector2int SizeInInventory => new(2, 2);
        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        protected static Sprite StaticGetItemSprite()
        {
            sprite ??= SpriteManager.Get(fallbackSprite, null);

            return sprite;
        }

        protected override Sprite GetItemSprite()
        {
            return StaticGetItemSprite();
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            var recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Main.GetModTechType("LightColdHelmet"), 1),
                    new Ingredient(TechType.ColdSuitHelmet, 1),
                    new Ingredient(TechType.RadioTowerPPU, 1)
                }
            };
            recipe.LinkedItems = new List<TechType>
            {
                Main.GetModTechType("UltimateHelmet")
            };

            return recipe;
        }

        public Blueprint_LightColdToUltimateHelmet() : base("Blueprint_LightColdToUltimateHelmet", UltimateHelmet.friendlyName, UltimateHelmet.description)
        {
            OnFinishedPatching += () =>
            {
                Reflection.AddCompoundTech(this.TechType, new List<TechType>
                {
                    TechType.FlashlightHelmet,
                    TechType.ColdSuit,
                    TechType.Rebreather
                });
            };
        }
    }
#endif

#if SUBNAUTICA
    internal class Blueprint_LightRebreatherPlusRad : Craftable
    {
        protected static Sprite sprite;
        protected TechType fallbackSprite => Main.GetModTechType("UltimateHelmet");
        public override Vector2int SizeInInventory => new(2, 2);
        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        protected override Sprite GetItemSprite()
        {
            try
            {
                sprite ??= ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/ultimatehelmet.png");
            }
            catch
            {
                if (fallbackSprite != TechType.None)
                    sprite = SpriteUtils.GetWithNoDefault(fallbackSprite);
            }

            return sprite;
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            var recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Main.GetModTechType("IlluminatedRebreather"), 1),
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.Benzene, 1)
                }
            };
            recipe.LinkedItems = new List<TechType>
            {
                Main.GetModTechType("UltimateHelmet")
            };

            return recipe;
        }

        public Blueprint_LightRebreatherPlusRad() : base("Blueprint_LightRebreatherPlusRad", UltimateHelmet.friendlyName, UltimateHelmet.description)
        {
            OnFinishedPatching += () =>
            {
                Reflection.AddCompoundTech(this.TechType, new List<TechType>
                {
                    Main.GetModTechType("FlashlightHelmet"),
                    TechType.RadiationHelmet,
                    TechType.Rebreather
                });
            };
        }
    }

    internal class Blueprint_LightRadHelmetPlusRebreather : Craftable
    {
        protected static Sprite sprite;
        protected TechType fallbackSprite => Main.GetModTechType("UltimateHelmet");
        public override Vector2int SizeInInventory => new(2, 2);
        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        protected override Sprite GetItemSprite()
        {
            try
            {
                sprite ??= ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/ultimatehelmet.png");
            }
            catch
            {
                if (fallbackSprite != TechType.None)
                    sprite = SpriteUtils.GetWithNoDefault(fallbackSprite);
            }

            return sprite;
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            var recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Main.GetModTechType("LightRadHelmet"), 1),
                    new Ingredient(TechType.Rebreather, 1),
                    new Ingredient(TechType.Benzene, 1)
                }
            };
            recipe.LinkedItems = new List<TechType>
            {
                Main.GetModTechType("UltimateHelmet")
            };

            return recipe;
        }

        public Blueprint_LightRadHelmetPlusRebreather() : base("Blueprint_LightRadHelmetPlusRebreather", UltimateHelmet.friendlyName, UltimateHelmet.description)
        {
            OnFinishedPatching += () =>
            {
                Reflection.AddCompoundTech(this.TechType, new List<TechType>
                {
                    Main.GetModTechType("FlashlightHelmet"),
                    TechType.RadiationHelmet,
                    TechType.Rebreather
                });
            };
        }
    }

    internal class Blueprint_FlashlightPlusBrineHelmet : Craftable
    {
        protected static Sprite sprite;
        protected TechType fallbackSprite => Main.GetModTechType("UltimateHelmet");
        public override Vector2int SizeInInventory => new(2, 2);
        public override TechType RequiredForUnlock => TechType.Unobtanium;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

        protected override Sprite GetItemSprite()
        {
            try
            {
                sprite ??= ImageUtils.LoadSpriteFromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Assets/ultimatehelmet.png");
            }
            catch
            {
                if (fallbackSprite != TechType.None)
                    sprite = SpriteUtils.GetWithNoDefault(fallbackSprite);
            }

            return sprite;
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            var recipe = new RecipeData()
            {
                craftAmount = 0,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Main.GetModTechType("AcidHelmet"), 1),
                    new Ingredient(Main.GetModTechType("FlashlightHelmet"), 1),
                }
            };
            recipe.LinkedItems = new List<TechType>
            {
                Main.GetModTechType("UltimateHelmet")
            };

            return recipe;
        }

        public Blueprint_FlashlightPlusBrineHelmet() : base("Blueprint_LightRadHelmetToUltimateHelmet", UltimateHelmet.friendlyName, UltimateHelmet.description)
        {
            OnFinishedPatching += () =>
            {
                Reflection.AddCompoundTech(this.TechType, new List<TechType>
                {
                    Main.GetModTechType("FlashlightHelmet"),
                    TechType.RadiationHelmet,
                    TechType.Rebreather
                });
            };
        }
    }
#endif

    internal class UltimateHelmet : HeadwearBase<UltimateHelmet>
    {
        internal const string classId = "UltimateHelmet";
        internal const string friendlyName = "Ultimate Helmet";
#if SUBNAUTICA
        internal const string description = "The ultimate in survival headwear. An acid-resistant, radiation-proof helmet with integrated rebreather and lamp.";
#elif BELOWZERO
        internal const string description = "The ultimate in survival headwear. An insulated helmet with integrated rebreather and lamp.";
#endif

        protected override float tempBonus => 10f;
#if SUBNAUTICA
        protected override TechType prefabTemplate => TechType.Rebreather;
#elif BELOWZERO
        protected override TechType prefabTemplate => TechType.ColdSuitHelmet;
#endif
        protected override List<TechType> compoundTech => new List<TechType>
        {
            TechType.Rebreather,
#if SUBNAUTICA
            TechType.RadiationHelmet,
#elif BELOWZERO
            TechType.ColdSuit,
#endif
            Main.GetModTechType("FlashlightHelmet")
        };

        protected override List<TechType> substitutions => new List<TechType>()
        {
#if SUBNAUTICA
            TechType.RadiationHelmet,
#elif BELOWZERO
            TechType.ColdSuitHelmet,
#endif
            TechType.Rebreather
        };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Main.GetModTechType("FlashlightHelmet"), 1),
                    new Ingredient(TechType.Rebreather, 1),
#if SUBNAUTICA
                    new Ingredient(TechType.RadiationHelmet, 1),
                    new Ingredient(TechType.AdvancedWiringKit, 1),
#elif BELOWZERO
                    new Ingredient(TechType.ColdSuitHelmet, 1),
                    new Ingredient(TechType.RadioTowerPPU, 1)
#endif
                }
            };
        }

        protected override GameObject ModifyGameObject(GameObject prefab)
        {
#if SUBNAUTICA
            var obj = FlashlightHelmet.PreparePrefab(GameObjectUtils.InstantiateInactive(prefab), this.ClassID);
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Shader shader = Shader.Find("MarmosetUBER");
            foreach (var renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    material.shader = shader; // apply the shader
                    material.mainTexture = AcidHelmet.texture; // apply the main texture
                    material.SetTexture(ShaderPropertyID._Illum, AcidHelmet.illumTexture); // apply the illum texture
                    material.SetTexture("_SpecTex", AcidHelmet.texture); // apply the spec texture
                }
            }
#elif BELOWZERO
            var obj = GameObjectUtils.InstantiateInactive(prefab);
#endif

            return obj;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject modPrefab;
            if (!TechTypeUtils.TryGetModPrefab(this.TechType, out modPrefab))
            {
#if SUBNAUTICA
                yield return FlashlightHelmet.SetUpPrefabsCoroutine();
#endif
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(prefabTemplate);
                yield return task;

                modPrefab = ModifyGameObject(task.GetResult());
                TechTypeUtils.AddModTechType(this.TechType, modPrefab);

            }
            gameObject.Set(modPrefab);
        }

        protected override void OnFinishedPatch()
        {
            base.OnFinishedPatch();
#if SUBNAUTICA
            TechTypeUtils.AddModTechType(this.TechType);
            Main.AddDamageResist(this.TechType, DamageType.Acid, 0.25f);
            AcidHelmet.texture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidHelmetskin.png"));
            AcidHelmet.illumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidHelmetillum.png"));
#elif BELOWZERO
            TooltipFactoryPatches.AddNoBarTechType(this.TechType);
#endif
        }

        public UltimateHelmet() : base(classId, friendlyName, description)
        {
        }
    }
}
