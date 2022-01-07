using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace FuelCells.MonoBehaviours
{
    public class RegeneratingPowerSource : MonoBehaviour
    {
        //public float RegenCumulative { get; private set; }
        public Battery source { get; private set; }
        public TechType sourceType { get; private set; }
        //private const float RegenerationRate = 0.0005f; // Fraction of total capacity regenerated per second
        private const float RegenerationInterval = 0.5f;
        private const float RegenerationRateAbsolute = 0.5f; // Energy units regenerated per second.

        private void Awake()
        {
            Log.LogDebug($"RegeneratingPowerSource.Awake() start");
            CoroutineHost.StartCoroutine(Setup());
        }

        private IEnumerator Setup()
        {
            Log.LogDebug($"RegeneratingPowerSource.Setup() start");
            int i = 0;
            while (source == null || sourceType == TechType.None)
            {
                if(source == null)
                    source = this.gameObject?.GetComponent<Battery>();
                if (sourceType == TechType.None)
                {
                    TechTag tt = this.gameObject?.GetComponent<TechTag>();
                    sourceType = (tt != null ? tt.type : TechType.None);
                }
                Log.LogDebug($"RegeneratingPowerSource: source = {(source != null ? source.GetInstanceID() : "null")}, sourceType = {sourceType.AsString()}, attempt {++i}");
                yield return new WaitForEndOfFrame();
            }

            base.InvokeRepeating("UpdatePower", RegenerationInterval, RegenerationInterval);
            yield break;
        }

        private void UpdatePower()
        {
            //Log.LogDebug($"RegeneratingPowerSource.UpdatePower(): begin");
            if (source == null && this.gameObject != null)
            {
                source = this.gameObject?.GetComponent<Battery>();
            }
            
            if(source == null)
                return;

            //float regen = source.capacity * RegenerationInterval * RegenerationRate;
            float regen = RegenerationInterval * RegenerationRateAbsolute;
            float preCharge = source.charge;
            source.charge = Mathf.Min(source.charge + regen, source.capacity);
            //Log.LogDebug($"RegeneratingPowerSource.UpdatePower(): Source ID {source.GetInstanceID()}, TechType ; Attempted to restore {regen} units, restored {source.charge - preCharge} units, charge now {source.charge}");
        }
    }
}
