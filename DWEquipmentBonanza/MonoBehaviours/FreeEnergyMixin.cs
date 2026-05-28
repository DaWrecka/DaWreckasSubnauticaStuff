using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;
using Main = DWEquipmentBonanza.DWEBPlugin;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if SN1
	// A variant of EnergyMixin that doesn't consume battery power
	// Only required for SN1 because it's used for the DWEB FlashlightHelmet, which is already in BZ
	public class FreeEnergyMixin : EnergyMixin
	{
		public override void Awake()
		{
			base.Awake();
			var storageObject = gameObject.FindChild("Inventory Storage") ?? new GameObject("Inventory Storage");
			storageObject.transform.SetParent(gameObject.transform);
			this.storageRoot ??= storageObject.GetComponent<ChildObjectIdentifier>();
			this.defaultBattery = Main.GetModTechType("FlashlightBattery");
			this.compatibleBatteries = new List<TechType>() { Main.GetModTechType("FlashlightBattery") };
			this.allowBatteryReplacement = false;
		}

		public override void Start()
		{
			base.Start();
			if (this.battery == null)
			{
				this.defaultBattery = Main.GetModTechType("FlashlightBattery");
#if ASYNC
				CoroutineHost.StartCoroutine(this.SpawnDefaultAsync(1f, DiscardTaskResult<bool>.Instance));
#else
				this.SpawnDefault(1f);
#endif
			}
		}
	}
#endif
}
