using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if BELOWZERO
    internal class HoverpadUpdater : MonoBehaviour
    {
        private void Start()
        {
            if (Main.config != null)
            {
                CoroutineHost.StartCoroutine(ApplyValuesAsync(Main.config));
                Main.config.onOptionChanged += ApplyValues;
            }
        }

        private void ApplyValues(DWConfig config, bool isEvent = false)
        {
            CoroutineHost.StartCoroutine(ApplyValuesAsync(config, isEvent));
        }

        private IEnumerator ApplyValuesAsync(DWConfig config, bool isEvent = false)
        {
            if (config == null)
            {
                Log.LogError("HoverpadUpdater.ApplyValues() invoked with null config!");
                yield break;
            }

            Hoverpad parent = gameObject?.GetComponent<Hoverpad>();
            while (parent == null)
            {
                yield return new WaitForEndOfFrame();
                parent = gameObject?.GetComponent<Hoverpad>();
            }

            parent.healAmountPerTick = config.healAmountPerTick;
            parent.rechargeAmountPerTick = config.rechargeAmountPerTick;

            yield break;
        }
    }
#endif
}
