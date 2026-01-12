using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
	internal interface IBatteryChip
	{
		internal abstract float charge { get; set; }
		internal abstract TechType techType { get; }
		internal abstract float capacity { get; }

		internal abstract void Initialise(TechType newTechType);
	}
}
