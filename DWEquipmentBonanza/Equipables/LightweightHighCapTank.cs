using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using UnityEngine;
using UWE;
using Logger = QModManager.Utility.Logger;
using FMODUnity;
using Common;
using Common.Utility;
using DWEquipmentBonanza.Patches;

#if SUBNAUTICA_STABLE
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
using Object = UnityEngine.Object;
using Oculus.Newtonsoft;
using Oculus.Newtonsoft.Json;
#elif BELOWZERO
using Newtonsoft;
using Newtonsoft.Json;
#endif

namespace DWEquipmentBonanza.Equipables
{
    public class PlasteelHighCapTank : Equipable
    {

        protected static Sprite icon;
        protected static GameObject prefab;

        public PlasteelHighCapTank() : base("PlasteelHighCapTank", "Plasteel Ultra Capacity Tank", "Lightweight tank with high oxygen capacity")
        {
            OnFinishedPatching += () =>
            {
                Main.AddSubstitution(this.TechType, TechType.PlasteelTank);
                Main.AddSubstitution(this.TechType, TechType.HighCapacityTank);
                Main.AddModTechType(this.TechType);
                Reflection.AddCompoundTech(this.TechType, new List<TechType>()
                {
                    TechType.PlasteelTank,
                    TechType.HighCapacityTank
                });
                CoroutineHost.StartCoroutine(PostPatchSetup());

                UnderwaterMotorPatches.AddSpeedModifier(this.TechType, -0.10625f);
                Main.AddCustomOxyExclusion(this.TechType, true, true);
                Main.AddCustomOxyTank(this.TechType, -1f, icon);
            };
        }

        public override EquipmentType EquipmentType => EquipmentType.Tank;

        public override Vector2int SizeInInventory => new Vector2int(3, 4);

        public override QuickSlotType QuickSlotType => QuickSlotType.None;

        public override TechType RequiredForUnlock => TechType.Unobtanium;

        public override TechCategory CategoryForPDA => TechCategory.Equipment;

        public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;

        public override string[] StepsToFabricatorTab => new string[] { DWConstants.TankMenuPath };

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.HighCapacityTank, 1),
                        new Ingredient(TechType.PlasteelTank, 1),
                        new Ingredient(TechType.Lubricant, 2),
                        new Ingredient(TechType.WiringKit, 1)
                    }
                )
            };
        }

        protected virtual IEnumerator PostPatchSetup()
        {
            bool bWaiting = true;

            while (bWaiting)
            {
                if (icon == null)
                {
                    icon = SpriteUtils.GetSpriteWithNoDefault(TechType.HighCapacityTank);
                }
                else
                    bWaiting = false;

                yield return new WaitForSecondsRealtime(0.5f);
            }

            CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HighCapacityTank);
            yield return task;
        }
        protected override Sprite GetItemSprite()
        {
            return (icon != null && icon != SpriteManager.defaultSprite) ? icon : SpriteManager.Get(TechType.HighCapacityTank);
        }

#if SUBNAUTICA_STABLE
        public override GameObject GetGameObject()
        {
            if (prefab == null)
            {
                prefab = PreparePrefab(CraftData.GetPrefabForTechType(TechType.HighCapacityTank));
            }

            return prefab;
        }
#elif BELOWZERO
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                Log.LogDebug($"LightweightHighCapTank.GetGameObjectAsync: getting HighCapacityTank prefab");
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.HighCapacityTank, verbose: true);
                yield return task;

                prefab = PreparePrefab(task.GetResult());
            }

            float oxyCap = prefab.GetComponent<Oxygen>().oxygenCapacity;
            Log.LogDebug($"GameObject created with oxygenCapacity of {oxyCap}");
            gameObject.Set(prefab);
        }
#endif

        private GameObject PreparePrefab(GameObject prefab)
        {
            GameObject go = GameObject.Instantiate(prefab);
            ModPrefabCache.AddPrefab(go, false);
            return go;
        }
    }
}
