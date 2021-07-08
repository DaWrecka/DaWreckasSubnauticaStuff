using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombinedItems.MonoBehaviors
{
    class EnergyMixinNoRecharge : EnergyMixin
    {
        new public float ModifyCharge(float amount)
        {
            return 0f;
        }

        new public bool AddEnergy(float amount)
        {
            return false;
        }
    }
}
