﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.Patches
{
#if SUBNAUTICA_STABLE
	internal class SeaMothPatches
    {
		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		public static void PostStart(SeaMoth __instance)
		{
			if (__instance.gameObject != null && __instance.gameObject.TryGetComponent<LiveMixin>(out LiveMixin mixin))
			{
				float defaultHealth = mixin.initialHealth;
				float instanceHealthPct = Mathf.Min(mixin.GetHealthFraction(), 1f);
				float maxHealth = defaultHealth;
				maxHealth *= Main.config.SeaMothHealthMult;

				mixin.data.maxHealth = maxHealth;
				mixin.health = maxHealth * instanceHealthPct;
			}
		}
	}
#endif
}