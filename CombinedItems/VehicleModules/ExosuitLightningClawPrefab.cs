using Common;
using System.Collections;
using System.Collections.Generic;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace CombinedItems.VehicleModules
{
    internal class ExosuitLightningClawPrefab : Equipable
    {
        public override EquipmentType EquipmentType => EquipmentType.ExosuitArm;
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public override TechGroup GroupForPDA => TechGroup.VehicleUpgrades;
        public override TechCategory CategoryForPDA => TechCategory.VehicleUpgrades;
        public override TechType RequiredForUnlock => TechType.BaseUpgradeConsole;
        public override CraftTree.Type FabricatorType => CraftTree.Type.SeamothUpgrades;
        //public override string[] StepsToFabricatorTab => new string[] { "ExosuitModules" };
        public override float CraftingTime => 10f;
        public override Vector2int SizeInInventory => new Vector2int(1, 2);

        private GameObject prefab;

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                /*craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[]
                    {
                        new Ingredient(TechType.Polyaniline, 1),
                        new Ingredient(TechType.WiringKit, 1),
                        new Ingredient(TechType.Battery, 1),
                        new Ingredient(TechType.AluminumOxide, 1)
                    }
                )*/
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ExosuitDrillArmModule);
                yield return task;
                prefab = GameObject.Instantiate<GameObject>(task.GetResult());

                // Adapted from Senna's SeaTruckArms mod
                task = CraftData.GetPrefabForTechTypeAsync(TechType.Exosuit, true);
                yield return task;
                Exosuit exosuit = task.GetResult().GetComponent<Exosuit>();
                if (exosuit != null)
                {
                    GameObject armPrefab = null;
                    if (exosuit != null)
                    {
                        for (int i = 0; i < exosuit.armPrefabs.Length; i++)
                        {
                            if (exosuit.armPrefabs[i].techType == TechType.ExosuitClawArmModule)
                            {
                                //Log.LogDebug($"Found claw arm prefab in Exosuit armPrefabs at index {i}");
                                armPrefab = exosuit.armPrefabs[i].prefab;
                                break;
                            }
                        }
                    }

                    if (armPrefab != null)
                    {
                        SkinnedMeshRenderer smr = armPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
                        Mesh clawMesh = smr.sharedMesh;

                        MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
                        mf.sharedMesh = Object.Instantiate(clawMesh);
                        mf.sharedMesh.name = "exosuit_lightningclaw_hand_geo";

                        MeshRenderer mr = prefab.GetComponentInChildren<MeshRenderer>();
                        mr.materials = (Material[])smr.materials.Clone();

                        prefab.transform.Find("model/exosuit_rig_armLeft:exosuit_drill_geo").gameObject.name = "exosuit_lightningclaw_arm_geo";

                        Object.Destroy(prefab.GetComponentInChildren<CapsuleCollider>());

                        BoxCollider bc_1 = prefab.FindChild("collider").AddComponent<BoxCollider>();

                        bc_1.size = new Vector3(1.29f, 0.33f, 0.42f);
                        bc_1.center = new Vector3(-0.53f, 0f, 0.04f);

                        GameObject collider2 = new GameObject("collider2");
                        collider2.transform.SetParent(prefab.transform, false);
                        collider2.transform.localPosition = new Vector3(-1.88f, 0.07f, 0.50f);
                        collider2.transform.localRotation = Quaternion.Euler(0, 34, 0);

                        BoxCollider bc_2 = collider2.AddComponent<BoxCollider>();
                        bc_2.size = new Vector3(1.06f, 0.23f, 0.31f);
                        bc_2.center = new Vector3(0, -0.08f, 0);

                        prefab.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

                        GameObject.DestroyImmediate(prefab.GetComponent<ExosuitClawArm>());
                        GameObject.DestroyImmediate(prefab.GetComponent<ExosuitDrillArm>());
                        prefab.AddComponent<ExosuitLightningClaw>();
                    }
                    else
                        Log.LogDebug($"Failed to find arm prefab in Exosuit prefab");
                }
                else
                    Log.LogDebug($"Failed to find Exosuit prefab");
            }

            gameObject.Set(GameObject.Instantiate(prefab));
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TechType.ExosuitClawArmModule);
        }

        public ExosuitLightningClawPrefab() : base("ExosuitLightningClawArm", "Exosuit Lightning Claw", "Lightning Claw upgrade adds an electrical generator to the claw, delivering an electrical jolt to anything struck by it.")
        {
            OnFinishedPatching += () =>
            {
            };
        }
    }
}
