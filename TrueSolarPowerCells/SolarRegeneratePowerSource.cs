using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace TrueSolarPowerCells
{
    internal class SolarRegeneratePowerSource : MonoBehaviour
    {
        private bool bInitialised;
        protected float regenerationRate => TSPCPlugin.config.regenerationRate;
        public PowerSource powerSource;

        public float regenerationThreshhold = 25f;

        public void Start()
        {
            this.regenerationThreshhold = TSPCPlugin.config.regenerationThreshold;
            CoroutineHost.StartCoroutine(SetupPowerSource());
        }

        private IEnumerator SetupPowerSource()
        {
            RegeneratePowerSource component = null;
            while (!gameObject.TryGetComponent<RegeneratePowerSource>(out component))
            {
                yield return new WaitForSecondsRealtime(0.5f);
            }
            component.regenerationThreshhold = this.regenerationThreshhold;
            component.regenerationAmount = 0f;

            while (!gameObject.TryGetComponent<PowerSource>(out powerSource))
            {
                yield return new WaitForSecondsRealtime(0.5f);
            }
            powerSource.maxPower = this.regenerationThreshhold;

            bInitialised = true;
            base.InvokeRepeating("CheckThreshold", 1f, 1f);
        }

        public void OnHover(HandTargetEventData eventData)
        {
            string format = Language.main.GetFormat<int, int>("PowerCellStatus", Mathf.FloorToInt(this.powerSource.GetPower()), Mathf.FloorToInt(this.powerSource.GetMaxPower()));
#if LEGACY
            HandReticle.main.SetInteractText("RegenPowerCell", format, true, false, HandReticle.Hand.None);
#else
            //HandReticle.main.SetInteractText("RegenPowerCell", format, true, false, HandReticle.Hand.None);
            HandReticle.main.SetText(HandReticle.TextType.Hand, "RegenPowerCell", true);
#endif
        }

        private void CheckThreshold()
        {
            bool flag = true;
            bool flag2 = true;

            if (powerSource.maxPower < this.regenerationThreshhold)
            {
                powerSource.maxPower = this.regenerationThreshhold;
                flag = false;
            }
            if (gameObject.TryGetComponent<RegeneratePowerSource>(out RegeneratePowerSource component))
            {
                if (component.regenerationThreshhold < this.regenerationThreshhold)
                {
                    component.regenerationThreshhold = this.regenerationThreshhold;
                    flag2 = false;
                }
            }

            if (flag && flag2)
            {
                base.CancelInvoke("CheckThreshold");
            }
        }

        private void Update()
        {
            if (!bInitialised)
                return;

            float delta = Time.deltaTime;
            if (delta <= 0f)
                return;

            float power = this.powerSource.GetPower();
            if (power >= this.regenerationThreshhold)
                return;

            DayNightCycle main = DayNightCycle.main;
            if (main == null)
            {
                return;
            }
            //int count = base.modules.GetCount(TechType.SeamothSolarCharge);
            float depthScalar = Mathf.Clamp01((Constants.kMaxSolarChargeDepth + gameObject.transform.position.y) / Constants.kMaxSolarChargeDepth);
            float localLightScalar = main.GetLocalLightScalar();
            float rechargeAmount = this.regenerationRate * delta * localLightScalar * depthScalar;
            //Log.LogDebug($"Adding rechargeAmount {rechargeAmount} to solar cell; delta = {delta}, regenerationRate = {this.regenerationRate}, localLightScalar = {localLightScalar}, depthScalar = {depthScalar}");
            this.powerSource.SetPower(Mathf.Min(power + rechargeAmount, this.regenerationThreshhold));
        }
    }
}
