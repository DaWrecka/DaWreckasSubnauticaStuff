using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombinedItems.MonoBehaviours
{
    class HoverbikeStructuralIntegrityModifier : DamageModifier
    {
        private const float fDamageToEnergyRatio = 0.2f; // Damage negated is multiplied by this value, and then subtracted from energy
        private EnergyMixin parentEnergy;
        private bool bActive;

        public void Awake()
        {
            multiplier = 0.5f; // Reduce all damage by half
            parentEnergy = GetComponentInParent<EnergyMixin>();
            bActive = true;
        }

        public void SetActive(bool value)
        {
            bActive = value;
        }

        public override float ModifyDamage(float damage, DamageType type)
        {
            float modifiedDamage = damage;
            if (bActive)
            {
                if (parentEnergy == null)
                {
                    parentEnergy = GetComponentInParent<EnergyMixin>();
                }

                if (parentEnergy == null)
                {
                    // Error state
                    return damage;
                }

                float damageReduction = damage * (1 - multiplier);
                float energyConsumption = damageReduction * fDamageToEnergyRatio;
                if (parentEnergy.ConsumeEnergy(energyConsumption))
                {
                    Log.LogDebug($"Hoverbike received {damage} damage, reduced by {damageReduction} costing {energyConsumption} energy");
                    modifiedDamage -= damageReduction;
                }
            }

            return modifiedDamage;
        }
    }
}
