using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if SUBNAUTICA
	public class OverrideTransform : MonoBehaviour
    {
        public IEnumerator Start()
        {
			GameObject go = this.gameObject;
			yield return new WaitUntil(() => (go ??= this.gameObject) != null);
			yield return new WaitForEndOfFrame();

			if (Main.saveCache.HeadlampDataboxPosition == Vector3.zero)
			{
                //Common.Log.LogDebug($"Generating new headlamp databox position");
				var rng = new System.Random();
				// Generate a random position that is within +/- 1.8m of (-403, -232.4, -98.48), and a random rotation around that point
				Main.saveCache.HeadlampDataboxPosition = new Vector3((float)(-404.1f + rng.NextDouble() * 1.8f), -232.35f, (float)(-99.38f + rng.NextDouble() * 1.8f));
				Main.saveCache.HeadlampDataboxRotation = new Vector3(0f, (float)(rng.NextDouble() * 360f), 0f);
				Main.saveCache.Save();
			}

			go.transform.position = Main.saveCache.HeadlampDataboxPosition;
			go.transform.eulerAngles = Main.saveCache.HeadlampDataboxRotation;
		}
	}
#endif
}
