using CombinedItems.Equipables;
using CombinedItems.MonoBehaviours;
using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CombinedItems.Patches
{
    [HarmonyPatch(typeof(MeleeAttack))]
    internal class MeleeAttackPatches
    {
        // We need a patch here because certain Leviathan attacks bypass inflicting damage entirely.
        [HarmonyPatch("CanBite")]
        [HarmonyPostfix]
        internal static void PostCanBite(MeleeAttack __instance, ref bool __result, GameObject target)
        {
            if (__result) // No point doing anything if the game has already concluded that it can't bite!
            {
                //GameObject target = __instance.GetTarget(collider);
                Player component = target.GetComponent<Player>();
                if (component != null)
                {
                    //TechType chipType = Main.GetModTechType("DiverPerimeterDefenceChipItem");
                    Log.LogDebug($"MeleeAttackPatches.PostCanBite: Target is player, checking for discharge chip");
                    Equipment e = Inventory.main.equipment;
                    if (Main.chipSlots.Count < 1)
                        e.GetSlots(EquipmentType.Chip, Main.chipSlots);
                    foreach(string slot in Main.chipSlots)
                    {
                        TechType tt = e.GetTechTypeInSlot(slot);
                        Log.LogDebug($"MeleeAttackPatches.PostCanBite: found TechType {tt.AsString()} in slot {slot}");
                        if(InventoryPatches.IsChip(tt))
                        {
                            InventoryItem item = e.GetItemInSlot(slot);
                            Log.LogDebug($"MeleeAttackPatches.PostCanBite: Found discharge chip {tt.AsString()} in slot {slot}, checking for associated MonoBehaviour");
                            DiverPerimeterDefenceBehaviour behaviour = item.item.gameObject.GetComponent<DiverPerimeterDefenceBehaviour>();
							if (behaviour != null)
							{
								Log.LogDebug($"MeleeAttackPatches.PostCanBite: MonoBehaviour found, calling Discharge()");
								if (behaviour.Discharge(__instance.gameObject))
								{
	                                Log.LogDebug($"MeleeAttackPatches.PostCanBite: Discharge() returned true");
                                    __result = false;
                                    return;
                                }
                                else
                                {
                                    Log.LogDebug($"MeleeAttackPatches.PostCanBite: Discharge() returned false");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
