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

#if SUBNAUTICA
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
#endif

namespace UpgradedBlades
{
    public class VibrobladeBehaviour : Knife
    {
        public VFXController fxControl;
        public override string animToolName => TechType.Knife.AsString(true);

        protected override int GetUsesPerHit()
        {
            return 3;
        }

        public override void Awake()
        {
#if !RELEASE
            Logger.Log(Logger.Level.Debug, "VibrobladeBehaviour.Awake() executing"); 
#endif

            this.attackDist = 2f;
            this.bleederDamage = 90f;
            this.damage = 90f;
#if BELOWZERO
            this.spikeyTrapDamage = 9f;
#endif
            this.damageType = DamageType.Normal;
            this.socket = PlayerTool.Socket.RightHand;
            this.ikAimRightArm = true;
        }
    }

    internal class Vibroblade : Equipable
    {
        protected static GameObject prefab;
        protected static GameObject hbPrefab;

        public Vibroblade(string classId = "Vibroblade", string friendlyName = "Vibroblade", string description = "Hardened survival blade with high-frequency oscillator inflicts horrific damage with even glancing blows") : base(classId, friendlyName, description)
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

        private static GameObject ModifyPrefab(GameObject activePrefab)
        {
            var obj = Object.Instantiate(activePrefab);
            if (obj == null)
            {
#if !RELEASE
                Logger.Log(Logger.Level.Error, "Failed to instantiate GameObject for prefab DiamondBlade");
#endif
                return null;
            }


            var component = obj.GetComponent<Knife>();
            if (component != null)
                GameObject.DestroyImmediate(component);

            VibrobladeBehaviour blade = obj.EnsureComponent<VibrobladeBehaviour>();
            if (blade != null)
            {
                /*
                    * #if SN1
                                HeatBlade hb = Resources.Load<GameObject>("WorldEntities/Tools/Heatblade").GetComponent<HeatBlade>();
#elif BELOWZERO
                                task = CraftData.GetPrefabForTechTypeAsync(TechType.HeatBlade);
                                yield return task;

                                HeatBlade hb = task.GetResult().GetComponent<HeatBlade>();
#endif
                */
                if (hbPrefab != null)
                {
                    HeatBlade hb = hbPrefab.GetComponent<HeatBlade>();
                    blade.fxControl = Object.Instantiate(hb.fxControl, obj.transform);
                }
                blade.attackDist = 2f;
                blade.bleederDamage = 90f;
                blade.damage = 90f;
                blade.damageType = DamageType.Normal;
                blade.socket = PlayerTool.Socket.RightHand;
                blade.ikAimRightArm = true;
            }
            else
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Could not ensure VibrobladeBehaviour component in Vibroblade prefab");
#endif
            }

            return obj;
        }

#if SN1
        public override GameObject GetGameObject()
        {
            if (prefab == null)
            {
                GameObject dbPrefab = CraftData.GetPrefabForTechType(TechType.DiamondBlade);
                hbPrefab = CraftData.GetPrefabForTechType(TechType.HeatBlade);

                prefab = ModifyPrefab(dbPrefab);
            }

            return GameObject.Instantiate(prefab);
        }

#elif BELOWZERO
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            var task = CraftData.GetPrefabForTechTypeAsync(TechType.HeatBlade);
            yield return task;
            hbPrefab = task.GetResult();

            task = CraftData.GetPrefabForTechTypeAsync(TechType.DiamondBlade);
            yield return task;

            prefab = ModifyPrefab(task.GetResult());

            gameObject.Set(GameObject.Instantiate(prefab));
        }
#endif

        protected override RecipeData GetBlueprintRecipe()
        {
            RecipeData recipe = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                {
                    new Ingredient(TechType.DiamondBlade, 1),
                    new Ingredient(TechType.Battery, 1),
                    new Ingredient(TechType.Diamond, 1),
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
