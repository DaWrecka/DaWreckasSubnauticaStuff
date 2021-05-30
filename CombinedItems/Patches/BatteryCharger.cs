using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CombinedItems.Patches
{
	[HarmonyPatch]
	internal class BatteryChargerPatches
	{
		private static FieldInfo compatibleBatteryTechInfo = typeof(BatteryCharger).GetField("compatibleTech", BindingFlags.Static | BindingFlags.NonPublic);
		private static FieldInfo compatiblePowerCellTechInfo = typeof(PowerCellCharger).GetField("compatibleTech", BindingFlags.Static | BindingFlags.NonPublic);
		private static HashSet<TechType> compatibleBatteryTech => (HashSet<TechType>)compatibleBatteryTechInfo.GetValue(null);
		private static HashSet<TechType> compatiblePowerCellTech => (HashSet<TechType>)compatiblePowerCellTechInfo.GetValue(null);
	}
}
