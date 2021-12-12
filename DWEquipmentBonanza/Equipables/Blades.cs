using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using UnityEngine;
using SMLHelper.V2.Handlers;
using Logger = QModManager.Utility.Logger;
using System.Collections;
using DWEquipmentBonanza.Patches;
using FMOD.Studio;
using DWEquipmentBonanza.MonoBehaviours;

#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza
{

    internal class Vibroblade : Equipable
    {
        protected static GameObject prefab;
        protected static GameObject hbPrefab;

        public Vibroblade(string classId = "Vibroblade", string friendlyName = "Vibroblade", string description = "Hardened survival blade with high-frequency oscillator inflicts horrific damage with even glancing blows") : base(classId, friendlyName, description)
        {
            OnFinishedPatching += () =>
            {
#if SUBNAUTICA_STABLE
                var diamondBlade = new TechData()
                {
                    craftAmount = 1,
                    Ingredients = new List<Ingredient>()
                    {
                        new Ingredient(TechType.Knife, 1),
                        new Ingredient(TechType.Diamond, 1)
                    }
                };
                CraftDataHandler.SetTechData(TechType.DiamondBlade, diamondBlade);
                CraftTreeHandler.AddCraftingNode(CraftTree.Type.Workbench, TechType.DiamondBlade, new string[] { DWConstants.KnifeMenuPath });
#endif
                Main.AddModTechType(this.TechType);
                PlayerPatch.AddSubstitution(this.TechType, TechType.Knife);
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Hand;
        public override Vector2int SizeInInventory => new Vector2int(1, 1);
        public override TechType RequiredForUnlock => TechType.Diamond;
        public override TechGroup GroupForPDA => TechGroup.Personal;
        public override TechCategory CategoryForPDA => TechCategory.Tools;
        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
        public override string[] StepsToFabricatorTab => new string[] { DWConstants.KnifeMenuPath };
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public override float CraftingTime => base.CraftingTime*2;

        private static GameObject SetupPrefab(GameObject activePrefab)
        {
            var obj = GameObject.Instantiate(activePrefab);
            if (obj == null)
            {
                return null;
            }

            Knife knife = obj.GetComponent<Knife>();

            VibrobladeBehaviour blade = obj.EnsureComponent<VibrobladeBehaviour>();
            if (blade != null)
            {
                if (hbPrefab != null)
                {
                    HeatBlade hb = hbPrefab.GetComponent<HeatBlade>();
                    blade.fxControl = hb.fxControl;
                    blade.vfxEventType = hb.vfxEventType;
                }
                if (knife != null)
                {
#if SUBNAUTICA_STABLE
                    blade.attackSound = knife.attackSound;
                    blade.underwaterMissSound = knife.underwaterMissSound;
                    blade.surfaceMissSound = knife.surfaceMissSound;
#endif
                    blade.mainCollider = knife.mainCollider;
                    blade.drawSound = knife.drawSound;
                    blade.firstUseSound = knife.firstUseSound;
                    blade.hitBleederSound = knife.hitBleederSound;
                    if (hbPrefab == null)
                        blade.vfxEventType = knife.vfxEventType;
                    GameObject.DestroyImmediate(knife);
                }
                blade.attackDist = 2f;
                blade.damageType = DamageType.Normal;
                blade.socket = PlayerTool.Socket.RightHand;
                blade.ikAimRightArm = true;
#if BELOWZERO
                blade.bleederDamage = 90f;
#endif
            }
            else
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Could not ensure VibrobladeBehaviour component in Vibroblade prefab");
#endif
            }

            ModPrefabCache.AddPrefab(obj, false);
            return obj;
        }

#if SUBNAUTICA_STABLE
        public override GameObject GetGameObject()
        {
            if (prefab == null)
            {
                GameObject dbPrefab = CraftData.GetPrefabForTechType(TechType.DiamondBlade);
                hbPrefab = CraftData.GetPrefabForTechType(TechType.HeatBlade);

                prefab = SetupPrefab(dbPrefab);
            }

            return prefab;
        }
#endif
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HeatBlade);
            yield return task;
            hbPrefab = task.GetResult();

            task = CraftData.GetPrefabForTechTypeAsync(TechType.DiamondBlade);
            yield return task;

            prefab = SetupPrefab(task.GetResult());

            gameObject.Set(prefab);
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
#if SUBNAUTICA_STABLE
                    new Ingredient(TechType.DiamondBlade, 1),
                    new Ingredient(TechType.Diamond, 1),
#elif BELOWZERO
                    new Ingredient(TechType.Knife, 1),
                    new Ingredient(TechType.Diamond, 2),
#endif
                    new Ingredient(TechType.Battery, 1),
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
