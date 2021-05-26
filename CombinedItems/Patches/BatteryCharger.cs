/*
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
		protected static HashSet<TechType> _addedBatteries = new HashSet<TechType>();
		protected static HashSet<TechType> _addedPowerCells = new HashSet<TechType>();
		private static FieldInfo compatibleBatteryTechInfo = typeof(BatteryCharger).GetField("compatibleTech", BindingFlags.Static | BindingFlags.NonPublic);
		private static FieldInfo compatiblePowerCellTechInfo = typeof(PowerCellCharger).GetField("compatibleTech", BindingFlags.Static | BindingFlags.NonPublic);
		private static HashSet<TechType> compatibleBatteryTech => (HashSet<TechType>)compatibleBatteryTechInfo.GetValue(null);
		private static HashSet<TechType> compatiblePowerCellTech => (HashSet<TechType>)compatiblePowerCellTechInfo.GetValue(null);
		public static HashSet<TechType> addedBatteries { get { return _addedBatteries; } }
		public static HashSet<TechType> addedPowerCells { get { return _addedPowerCells; } }

		internal static void AddBattery(TechType newBattery)
		{
			if (!_addedBatteries.Contains(newBattery))
			{
				_addedBatteries.Add(newBattery);
				compatibleBatteryTech.UnionWith(_addedBatteries);
			}
		}

		internal static void AddPowerCell(TechType newCell)
		{
			if (!_addedPowerCells.Contains(newCell))
			{
				_addedPowerCells.Add(newCell);
				compatiblePowerCellTech.UnionWith(_addedPowerCells);
			}
			return;
		}

		internal static bool PatchEnergyMixin(ref EnergyMixin energy)
		{
			Log.LogDebug($"Patching EnergyMixin {energy.name}");
			if (energy.compatibleBatteries.Contains(TechType.Battery))
			{
				HashSet<TechType> compatibleTech = new HashSet<TechType>(energy.compatibleBatteries);
				compatibleTech.UnionWith(_addedBatteries);
				energy.compatibleBatteries = compatibleTech.ToList<TechType>();
				return true;
			}
			else if (energy.compatibleBatteries.Contains(TechType.PowerCell))
			{
				HashSet<TechType> compatibleTech = new HashSet<TechType>(energy.compatibleBatteries);
				compatibleTech.UnionWith(_addedPowerCells);
				energy.compatibleBatteries = compatibleTech.ToList<TechType>();
				return true;
			}

			return false;
		}

		internal static bool PatchCharger(ref Charger charger)
		{
			FieldInfo allowedTechField = typeof(Charger).GetField("allowedTech", BindingFlags.Instance | BindingFlags.NonPublic);
			HashSet<TechType> allowedTech = (HashSet<TechType>)(allowedTechField?.GetValue(charger));
			if (allowedTech != null)
			{
				if (allowedTech.Contains(TechType.Battery))
				{
					allowedTech.UnionWith(addedBatteries);
					allowedTechField.SetValue(charger, allowedTech);
					return true;
				}
				else if (allowedTech.Contains(TechType.PowerCell))
				{
					allowedTech.UnionWith(addedPowerCells);
					allowedTechField.SetValue(charger, allowedTech);
					return true;
				}
			}

			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Charger), "Start")]
		internal static void PostChargerStart(ref Charger __instance)
		{
			PatchCharger(ref __instance);
		}


		[HarmonyPostfix]
		[HarmonyPatch(typeof(EnergyMixin), "Start")]
		internal static void PostEnergymixinStart(ref EnergyMixin __instance)
		{
			PatchEnergyMixin(ref __instance);
		}
	}
}*/
