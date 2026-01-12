using Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if QMM
using Logger = QModManager.Utility.Logger;
#endif

namespace CustomiseOxygen.Patches
{
	public class CustomOxy : MonoBehaviour, ICraftTarget
	{
		// This class exists pretty much entirely as an ICraftTarget, so we can receive an OnCraftEnd event.
		// There's probably other ways to get OnCraftEnd, but this seems the easiest.

		private Oxygen oxygen;
		private TechType techType;

		public void OnCraftEnd(TechType techType)
		{
			this.techType = techType;
			this.oxygen = gameObject?.GetComponent<Oxygen>();

			if (this.oxygen == null)
			{
				GameObject.Destroy(this);
			}
			else if (CustomiseOxygenPlugin.config.bManualRefill)
			{
				this.oxygen.oxygenAvailable = this.oxygen.oxygenCapacity;
			}
		}

		private void Awake()
		{
			if (uGUI_MainMenuPatches.bProcessing)
				return;

			if (this.techType == TechType.None)
				this.techType = CraftData.GetTechType(base.gameObject);

			this.oxygen = base.GetComponent<Oxygen>();
			if (this.oxygen == null)
			{
				Log.LogDebug($"CustomOxy: Failed to find Oxygen component in parent"); 
				return;
			}

			if (this.oxygen.isPlayer)
			{
				return;
			}

			if (CustomiseOxygenPlugin.config.GetCapacityOverride(this.techType, this.oxygen.oxygenCapacity, out float capacityOverride, out float capacityMultiplier))
			{
				bool bIsManual = CustomiseOxygenPlugin.config.bManualRefill;
				Log.LogDebug($"CustomiseOxygen.Main.GetCapacityOverride returned true with values of capacityOverride={capacityOverride}, capacityMultiplier={capacityMultiplier}");
				if (capacityOverride > 0)
					this.oxygen.oxygenCapacity = capacityOverride;
				else
				{
					this.oxygen.oxygenCapacity *= capacityMultiplier;
				}
				Log.LogDebug($"CustomOxy.Awake(): Oxygen capacity set to {this.oxygen.oxygenCapacity}");
			}
			if (!CustomiseOxygenPlugin.IsTankRegistered(this.techType))
			{
				Log.LogDebug("Calling processing routine for TechType " + this.techType.AsString());
				CustomiseOxygenPlugin.AddTank(this.techType, oxygen.oxygenCapacity);
			}
        }
    }

	[HarmonyPatch]
	public class OxygenPatches
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Oxygen), "Awake")]
		public static bool PreAwake(ref Oxygen __instance)
		{
			if (!__instance.isPlayer)
			{
				__instance.gameObject.EnsureComponent<CustomOxy>();
			}

			return true; // Base Oxygen.Awake() method does nothing but refill the oxygen tank if its oxygenAvailable property is less than zero.
			// It does no harm to let it run unless another mod is sticking its oar in, but we can't really terminate it "just in case".
		}
	}
}
