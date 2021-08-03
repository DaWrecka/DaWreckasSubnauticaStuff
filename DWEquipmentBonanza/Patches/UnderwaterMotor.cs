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
        private static Dictionary<TechType, float> speedModifiers = new Dictionary<TechType, float>()
        {
/*            { TechType.Tank, -0.425f },
            { TechType.DoubleTank, -0.5f },
            { TechType.PlasteelTank, -0.10625f },
            { TechType.HighCapacityTank, -0.6375f },
#if BELOWZERO
			{ TechType.SuitBoosterTank, 0f },
#endif
			{ TechType.ReinforcedDiveSuit, -1f },
            { TechType.Fins, 1.5f },
            { TechType.UltraGlideFins, 2.5f }*/
        };

        public static float GetSpeedModifier(TechType tt)
        {
            if (speedModifiers.TryGetValue(tt, out float modifier))
                return modifier;

            return 0f;
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

			Log.LogDebug($"UnderwaterMotorPatches.PreAlterMaxSpeed: inMaxSpeed = {inMaxSpeed}");
			Inventory main = Inventory.main;
			Equipment equipment = main.equipment;
			ItemsContainer container = main.container;
			TechType techTypeInSlot = equipment.GetTechTypeInSlot("Tank");
			inMaxSpeed += GetSpeedModifier(techTypeInSlot);

			int count = container.GetCount(TechType.HighCapacityTank);
			inMaxSpeed = Mathf.Max(inMaxSpeed - (float)count * 1.275f, 2f);

			TechType techTypeInSlot2 = equipment.GetTechTypeInSlot("Body");
			inMaxSpeed += GetSpeedModifier(techTypeInSlot2);

#if SUBNAUTICA_STABLE
			float num2 = 1f;
			global::Utils.AdjustSpeedScalarFromWeakness(ref num2);
			inMaxSpeed *= num2;
#endif
			TechType techTypeInSlot3 = equipment.GetTechTypeInSlot("Foots");
			inMaxSpeed += GetSpeedModifier(techTypeInSlot3);

/*#if SUBNAUTICA_STABLE
			if (main.GetHeldTool() == null)
			{
				num += 1f;
			}
			if (__instance.gameObject.transform.position.y > Ocean.main.GetOceanLevel())
			{
				num *= 1.3f;
			}
#elif BELOWZERO
			if (Player.main.motorMode != Player.MotorMode.Seaglide && main.GetHeldTool() != null)
			{
				num -= 1f;
			}
			if (__instance.gameObject.transform.position.y > Ocean.GetOceanLevel())
			{
				num *= 1.3f;
			}
#endif

#if SUBNAUTICA_STABLE
			float to = 1f;
			if (Player.main.GetBiomeString() == "wreck")
			{
				to = 0.5f;
			}
			float SpeedMultiplier = UWE.Utils.Slerp(__instance.currentWreckSpeedMultiplier, to, 0.3f * Time.deltaTime);
			__instance.currentWreckSpeedMultiplier = SpeedMultiplier;
#elif BELOWZERO
			float SpeedMultiplier = Mathf.MoveTowards(__instance.currentPlayerSpeedMultipler, __instance.playerSpeedModifier.Value, 0.3f * Time.deltaTime);
			__instance.currentPlayerSpeedMultipler = SpeedMultiplier;
#endif
			__result = num * SpeedMultiplier;*/
			Log.LogDebug($"UnderwaterMotor.PreAlterMaxSpeed: got out inMaxSpeed {inMaxSpeed}");


			return true;
		}
	}
}
