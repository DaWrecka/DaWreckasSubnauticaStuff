using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if BELOWZERO
    internal class HoverbikeStructuralIntegrityModifier : DamageModifier//, IInventoryDescription, IBattery
    {
        private const float fDamageToEnergyRatio = 0.2f; // Damage negated is multiplied by this value, and then subtracted from energy
        private EnergyMixin parentEnergy;
        private bool bActive;

        /*private float _charge = 0f;
        private static float _capacity = 100f;
        public float charge
        {
            get
            {
                return _charge;
            }
            set { } // A public setter is required, but in this case we don't want the value to be editable by outside sources. So... empty method body.
        }
        public float capacity
        {
            get
            {
                return _capacity;
            }
            set { }
        }

        public string GetInventoryDescription()
        {
            string arg0 = "";
            string arg1 = "";
            TechType tt = Main.GetModTechType("HoverbikeStructuralIntegrityModule");
            if (tt != TechType.None)
            {
                arg0 = Language.main.Get(tt);
                arg1 = Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(tt));
            }
            //Log.LogDebug($"For techType of {this.techType.AsString()} got arg0 or '{arg0}' and arg1 of '{arg1}'");
            return string.Format("{0}\n", arg1);
        }

        public string GetChargeValueText()
        {
            float num = this._charge / _capacity;
            return Language.main.GetFormat<string, float, int, float>("BatteryCharge", ColorUtility.ToHtmlStringRGBA(Battery.gradient.Evaluate(num)), num, Mathf.RoundToInt(this._charge), _capacity);
        }*/

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
            HoverbikeUpdater updater = gameObject.GetComponent<HoverbikeUpdater>();
            if (updater == null)
                return damage;

            //ErrorMessage.AddMessage($"HoverbikeStructuralIntegrityModifier modifying damage; damage amount {damage}, type {type.ToString()}");
            float modifiedDamage = damage * multiplier;
            if (bActive)
            {
                modifiedDamage = updater.ShieldAbsorb(damage);
                if (updater.bHasShield)
                    return modifiedDamage;

                // So at this point we know the Hoverbike has a Structural Integrity Field and not a Durability Upgrade.
                EnergyMixin parentEnergy = GetComponentInParent<EnergyMixin>();

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
#endif
}
