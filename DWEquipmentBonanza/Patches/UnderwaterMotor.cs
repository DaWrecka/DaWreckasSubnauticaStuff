using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.Patches
{
    [HarmonyPatch(typeof(UnderwaterMotor))]
    public class UnderwaterMotorPatches
    {
        private static Dictionary<TechType, float> speedModifiers = new Dictionary<TechType, float>();

        public static float GetSpeedModifier(TechType tt)
        {
			return speedModifiers.GetOrDefault(tt, 0f);

			/*if (speedModifiers.TryGetValue(tt, out float modifier))
                return modifier;

            return 0f;*/
        }

        public static void AddSpeedModifier(TechType tt, float modifier)
        {
            speedModifiers[tt] = modifier;
        }

		[HarmonyPatch("AlterMaxSpeed")]
		[HarmonyPrefix]
		internal static bool PreAlterMaxSpeed(UnderwaterMotor __instance, float __result, ref float inMaxSpeed)
		{
			// AlterMaxSpeed is a complete mess and damn-near impossible to expand without transpiling a whole lot of it.
			// So I said "fuck it" and replace the entire thing.

			//Log.LogDebug($"UnderwaterMotorPatches.PreAlterMaxSpeed: inMaxSpeed = {inMaxSpeed}");
			Inventory main = Inventory.main;
			Equipment equipment = main.equipment;
			ItemsContainer container = main.container;
			TechType techTypeInSlot = equipment.GetTechTypeInSlot("Tank");
			inMaxSpeed += GetSpeedModifier(techTypeInSlot);

//			int count = container.GetCount(TechType.HighCapacityTank);
//			inMaxSpeed = Mathf.Max(inMaxSpeed - (float)count * 1.275f, 2f);

			TechType techTypeInSlot2 = equipment.GetTechTypeInSlot("Body");
			inMaxSpeed += GetSpeedModifier(techTypeInSlot2);

#if SN1
//			float num2 = 1f;
//			global::Utils.AdjustSpeedScalarFromWeakness(ref num2);
//			inMaxSpeed *= num2;
#endif
			TechType techTypeInSlot3 = equipment.GetTechTypeInSlot("Foots");
			inMaxSpeed += GetSpeedModifier(techTypeInSlot3);

			//Log.LogDebug($"UnderwaterMotor.PreAlterMaxSpeed: got out inMaxSpeed {inMaxSpeed}");
			return true;
		}
	}
}
