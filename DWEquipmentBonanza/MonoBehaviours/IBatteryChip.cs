using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
	internal class BatteryChip : MonoBehaviour,
		IBattery
	{
		public float charge {
			get
			{
				return 1f;
			}
			set { }
		}
		internal TechType techType { get; }
		public float capacity => 1f;

		internal virtual void Initialise(TechType newTechType) { }

		public string GetChargeValueText()
		{
			return String.Empty;
		}
	}
}
