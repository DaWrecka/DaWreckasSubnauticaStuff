using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if SUBNAUTICA
    // A variant of EnergyMixin that doesn't consume battery power
    public class FreeEnergyMixin : EnergyMixin
    {
        public override void Awake()
        {
            base.Awake();
            var storageObject = gameObject.FindChild("Inventory Storage") ?? new GameObject("Inventory Storage");
            storageObject.transform.SetParent(gameObject.transform);
            this.storageRoot ??= storageObject.GetComponent<ChildObjectIdentifier>();
            this.defaultBattery = TechType.PrecursorIonBattery;
            this.compatibleBatteries = new List<TechType>() { TechType.PrecursorIonBattery };
            this.allowBatteryReplacement = false;
        }

        public override void Start()
        {
            base.Start();
            if(this.battery == null)
                this.SpawnDefault(1f);
        }
    }
#endif
}
