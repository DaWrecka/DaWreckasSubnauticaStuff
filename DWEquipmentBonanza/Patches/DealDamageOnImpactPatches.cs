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
	[HarmonyPatch(typeof(DealDamageOnImpact))]
	internal class DealDamageOnImpactPatches
	{
		public class CollisionData
		{
			public CollisionData(Component _component, Collision _collision)
			{
				this.component = _component;
				this.collision = _collision;
			}

			Component component;
			Collision collision;
		}
		public static CollisionData lastCollision { get; private set; }
		public static CollisionData lastInterceptedCollision { get; private set; }

		[HarmonyPrefix]
		[HarmonyPatch("OnCollisionEnter")]
		public static bool PreOnCollisionEnter(ref DealDamageOnImpact __instance, Collision collision)
		{
			SubRoot sub = __instance.GetComponent<SubRoot>();

			if (sub != null && collision.collider != null && sub == collision.collider.GetComponentInParent<SubRoot>())
			{
				//Log.LogDebug($"DealDamageOnImpactPatches.PreOnCollisionEnter(): Intercepting collision between Cyclops and collider named " + collision.collider.name, null, true);
				//lastInterceptedCollision = new CollisionData(__instance, collision);
				__instance.AddException(collision.gameObject);
				return false;
			}

			return true;
		}

		//[HarmonyPostfix]
		//[HarmonyPatch("OnCollisionEnter")]
		public static void PostOnCollisionEnter(ref DealDamageOnImpact __instance, bool __runOriginal, Collision collision)
		{
			if (__runOriginal || __instance.gameObject.GetComponent<SubRoot>() == null)
				return;

			//Log.LogDebug($"DealDamageOnImpactPatches.PostOnCollisionEnter(): Collision between Cyclops and collider named " + collision.collider.name + " (not intercepted)", null, true);
			//lastCollision = new CollisionData(__instance, collision);
		}
	}
}
