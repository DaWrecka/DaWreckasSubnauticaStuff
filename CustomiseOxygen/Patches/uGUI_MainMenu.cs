using Common;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace CustomiseOxygen.Patches
{
    [HarmonyPatch(typeof(uGUI_MainMenu))]
    public class uGUI_MainMenuPatches
    {
        public static bool bProcessing { get; private set; }

        [HarmonyPatch(nameof(uGUI_MainMenu.Start))]
        [HarmonyPostfix]
        public static void PostStart()
        {
            CoroutineHost.StartCoroutine(PostMenuCoroutine());
        }

        private static IEnumerator PostMenuCoroutine()
        {
            if (bProcessing)
                yield break;

            bProcessing = true;

            var tanksCollection = new List<(TechType techType, bool bUnlockAtStart)>()
            {
                (TechType.Tank, true),
                (TechType.DoubleTank, false),
#if BELOWZERO
                (TechType.SuitBoosterTank, false),
#endif
                (TechType.PlasteelTank, false),
                (TechType.HighCapacityTank, false),
            };
            foreach (string testString in new string[]
            {
                "photosynthesistank",
                "photosynthesissmalltank",
                "chemosynthesistank"
            })
            {
                //TechType testType = TechTypeUtils.GetModTechType(testString);
                //if (testType != TechType.None)
                if(TechTypeUtils.TryGetModTechType(testString, out TechType testType))
                    tanksCollection.Add((techType: testType, bUnlockAtStart: false));
            }

            CustomiseOxygenPlugin.config.defaultTankCapacities.Clear();

            foreach (var tt in tanksCollection)
            {
                Log.LogDebug($"uGUI_MainMenuPatches.PostMenuCoroutine(): Initial processing, TechType.{tt.techType.AsString()}, bUnlockAtStart: {tt.bUnlockAtStart}");
                float capacity = -1f;
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(tt.techType);
                yield return task;

                GameObject prefab = task.GetResult();
                Oxygen oxyComponent = null;
                if (prefab != null)
                    oxyComponent = prefab.GetComponent<Oxygen>();
                if (oxyComponent != null)
                    capacity = oxyComponent.oxygenCapacity;

                Log.LogDebug($"uGUI_MainMenuPatches.PostMenuCoroutine(): For TechType.{tt.techType.AsString()}, got base capacity of {capacity}");
                CustomiseOxygenPlugin.AddTank(tt.techType, capacity, tt.bUnlockAtStart, null);
            }

            bProcessing = false;
            CustomiseOxygenPlugin.OnMainMenuStarted();
            yield break;
        }
    }
}
