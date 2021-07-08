using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
    public class SurvivalsuitBehaviour : MonoBehaviour, IEquippable
    {
        internal static float SurvivalRegenRate = 0.5f; // The proportion of primary needs to replenish every time interval, relative to consumption.
        // Consumption is calculated based on the algorithm used in Survival.UpdateStats() as of the Seaworthy update, and unless those numbers are changed
        // this should remain workable.
        // This value should always be greater than 0 and less than 1.
        // If less than 0, the Survival Suit would make primary needs drain faster.
        // If greater than 1, the Survival Suit would replenish primary needs faster than they drain.
        internal const float foodDivisor = 25.2f; // UWE code divides by 2520 but then multiplies by 100. This might be from 2520 having been one named constant and 100 being a second.
                                                         // Either way, I'm simplifying the result.
        internal const float waterDivisor = 18f;  // see above.
        private Survival primaryNeeds => Player.main.GetComponentInParent<Survival>();

        public void OnEquip(GameObject sender, string slot) { }
        public void OnUnequip(GameObject sender, string slot) { }
        public void UpdateEquipped(GameObject sender, string slot)
        {
        }

        public void UpdateHunger(GameObject sender)
        {
            if (GameModeUtils.RequiresSurvival() && !Player.main.IsFrozenStats())
            {
                float deltaTime = Time.deltaTime;

                // now we can calculate the current calorie/water consumption rates and calibrate based on those.
                // Assuming the buggers at UWE don't change the algorithm.

                float foodRestore = deltaTime / foodDivisor * SurvivalRegenRate;
                float waterRestore = deltaTime / waterDivisor * SurvivalRegenRate;
                primaryNeeds.food  = primaryNeeds.food  + foodRestore; // We don't need to clamp these values - Survival.UpdateHunger will do it for us.
                primaryNeeds.water = primaryNeeds.water + waterRestore;
            }
        }
    }
}
