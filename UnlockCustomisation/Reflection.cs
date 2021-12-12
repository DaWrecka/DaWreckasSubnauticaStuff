using Common;
using HarmonyLib;
using SMLHelper.V2.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace UnlockCustomisation
{
	[HarmonyPatch]
	public class Reflection
	{
		private static readonly MethodInfo playerUpdateReinforcedSuitInfo = typeof(Player).GetMethod("UpdateReinforcedSuit", BindingFlags.NonPublic | BindingFlags.Instance);
#if BELOWZERO
		private static readonly MethodInfo addJsonPropertyInfo = typeof(CraftDataHandler).GetMethod("AddJsonProperty", BindingFlags.NonPublic | BindingFlags.Static);
		private static readonly MethodInfo playerCheckColdsuitGoalInfo = typeof(Player).GetMethod("CheckColdsuitGoal", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
		private static readonly FieldInfo knownTechCompoundTech = typeof(KnownTech).GetField("compoundTech", BindingFlags.NonPublic | BindingFlags.Static);
		private static Dictionary<TechType, List<TechType>> pendingCompoundTech = new Dictionary<TechType, List<TechType>>();
		private static bool bProcessingCompounds;

#if BELOWZERO
		public static void AddJsonProperty(TechType techType, string key, JsonValue newValue)
		{
			addJsonPropertyInfo.Invoke(null, new object[] { techType, key, newValue });
		}

		public static void AddColdResistance(TechType techType, int newValue)
		{
			//AddJsonProperty(techType, "coldResistance", new JsonValue(newValue));
			CraftDataHandler.SetColdResistance(techType, newValue);
		}

		public static void SetItemSize(TechType techType, int width, int height)
		{
			AddJsonProperty(techType, "itemSize", new JsonValue
				{
					{
						TechData.propertyX,
						new JsonValue(width)
					},
					{
						TechData.propertyY,
						new JsonValue(height)
					}
				}
			);
		}

		public static void PlayerCheckColdsuitGoal(Player player)
		{
			playerCheckColdsuitGoalInfo.Invoke(player, new object[] { });
		}
#endif

		public static void PlayerUpdateReinforcedSuit(Player player)
		{
			playerUpdateReinforcedSuitInfo.Invoke(player, new object[] { });
		}

		public static void AddCompoundTech(TechType techType, List<TechType> dependencies, bool bForce = false)
		{
			if (techType == TechType.None)
			{
				Log.LogError($"AddCompoundTech called with TechType.None");
				return;
			}

			if (dependencies == null || dependencies.Count < 1)
			{
				Log.LogError($"AddCompoundTech called with TechType {techType.AsString()} but null or zero-length dependencies list.");
				return;
			}

			if (pendingCompoundTech.ContainsKey(techType))
			{
				Log.LogError($"AddCompoundTech called with duplicate TechType {techType.AsString()}");
				return;
			}

			pendingCompoundTech.Add(techType, dependencies);
			//CoroutineHost.StartCoroutine(ProcessPendingCompounds(bForce));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(KnownTech), nameof(KnownTech.Initialize))]
		public static void PostKnownTechInit()
		{
			Log.LogDebug("Reflection.PostKnownTechInit() executing");
			CoroutineHost.StartCoroutine(ProcessPendingCompounds(false));
		}

		private bool KnownTechInitialised()
		{
			return (knownTechCompoundTech.GetValue(null) != null);
		}

		private static IEnumerator ProcessPendingCompounds(bool bForce = false)
		{
			if (bProcessingCompounds)
			{
				if (bForce)
					Log.LogDebug("ProcessPendingCompounds executing: forced");
				else
					yield break;
			}

			if (pendingCompoundTech.Count < 1)
			{
				bProcessingCompounds = false;
				yield break;
			}

			Log.LogDebug("ProcessPendingCompounds executing");
			bProcessingCompounds = true;

			int tries = 0;
			while (pendingCompoundTech.Count > 0)
			{
				List<KnownTech.CompoundTech> compounds = (List<KnownTech.CompoundTech>)knownTechCompoundTech.GetValue(null);
				HashSet<TechType> removals = new HashSet<TechType>();
				Log.LogDebug($"Attempting to process pending compound tech: pendingCompoundTech.Count == {pendingCompoundTech.Count}, attempt {++tries}");
				if (compounds != null)
				{
					Log.LogDebug("Successfully retrieved KnownTech.compoundTech: Now processing pendingCompoundTech");
					foreach (KeyValuePair<TechType, List<TechType>> kvp in pendingCompoundTech)
					{
						Log.LogDebug($"Adding compoundTech: techType = {kvp.Key.AsString()}");
						KnownTech.CompoundTech compound = new KnownTech.CompoundTech();
						compound.techType = kvp.Key;
						compound.dependencies = kvp.Value;
						compounds.Add(compound);
						removals.Add(kvp.Key);
					}
				}
				else
				{
					Log.LogDebug($"KnownTech.compoundTech could not be retrieved");
				}

				foreach (TechType tt in removals)
					pendingCompoundTech.Remove(tt);
				removals.Clear();

				yield return new WaitForSecondsRealtime(2f);
			}

			bProcessingCompounds = false;
		}
	}

}
