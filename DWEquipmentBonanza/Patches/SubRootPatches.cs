using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using HarmonyLib;
using UnityEngine;

namespace DWEquipmentBonanza.Patches
{
#if SUBNAUTICA
	[HarmonyPatch(typeof(SubRoot))]
	internal class SubRootPatches
	{
		public static DealDamageOnImpactPatches.CollisionData lastCollision { get; private set; }
		public static DealDamageOnImpactPatches.CollisionData lastInterceptedCollision { get; private set; }

		[HarmonyPrefix]
		[HarmonyPatch("OnCollisionEnter")]
		public static bool PreOnCollisionEnter(ref SubRoot __instance, Collision col)
		{
			if (__instance != null && col.collider != null && __instance == col.collider.GetComponentInParent<SubRoot>())
			{
				//Log.LogDebug($"SubRootPatches.PreOnCollisionEnter(): Intercepting collision between Cyclops and collider named " + col.collider.name, null, true);
				//lastInterceptedCollision = new DealDamageOnImpactPatches.CollisionData(__instance, col);
				
				return false;
			}

			return true;
		}

		//[HarmonyPostfix]
		//[HarmonyPatch("OnCollisionEnter")]
		public static void PostOnCollisionEnter(ref SubRoot __instance, bool __runOriginal, Collision col)
		{
			if (__runOriginal || __instance == null)
				return;

			//Log.LogDebug($"SubRootPatches.PostOnCollisionEnter(): Collision between Cyclops and collider named " + col.collider.name + " (not intercepted)", null, true);
			//lastCollision = new DealDamageOnImpactPatches.CollisionData(__instance, col);
		}
	}
#endif
}
