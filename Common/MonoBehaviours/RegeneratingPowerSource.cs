using Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace DWCommon.MonoBehaviours
{
	public class RegeneratingPowerSource : MonoBehaviour
	{
		public static Dictionary<TechType, (float interval, float rate)> regenParameters = new();

		//public float RegenCumulative { get; private set; }
		public IBattery source { get; private set; }
		public TechType sourceType { get; private set; }
		private const float logInterval = 1.0f;
		private float logTimer = 0f; // Used to minimise log spamming
		private float timerPower = 0f; // Amount of juice restored since last log
		//private const float RegenerationRate = 0.0005f; // Fraction of total capacity regenerated per second
		private float RegenerationInterval = 0f;
		private float RegenerationRateAbsolute = 0f; // Energy units regenerated per second.

		public void SetRegenParameters(float rinterval, float rrate, TechType tt = TechType.None)
		{
			RegenerationInterval = rinterval;
			RegenerationRateAbsolute = rrate;

			if (tt == TechType.None)
			{
				tt = CraftData.GetTechType(gameObject);
			}

			regenParameters[tt] = (interval: rinterval, rate: rrate);
		}

		public static void StaticSetRegenParameters(float rinterval, float rrate, TechType tt)
		{
			regenParameters[tt] = (interval: rinterval, rate: rrate);
		}

		private void Awake()
		{
			Log.LogDebug($"RegeneratingPowerSource.Awake() start");
			CoroutineHost.StartCoroutine(Setup());
		}

		private IEnumerator Setup()
		{
			//Log.LogDebug($"RegeneratingPowerSource.Setup() start");
			//int i = 0;
			MonoBehaviour batt = null;
			while (source == null || sourceType == TechType.None)
			{
				if(source == null)
					source = this.gameObject?.GetComponent<IBattery>();
				if (sourceType == TechType.None)
				{
					//TechTag tt = this.gameObject?.GetComponent<TechTag>();
					//sourceType = (tt != null ? tt.type : TechType.None);
					sourceType = CraftData.GetTechType(gameObject);
				}
				batt = (MonoBehaviour)source;
				//Log.LogDebug($"RegeneratingPowerSource: source = {(batt != null ? batt.GetInstanceID().ToString() : "null")}, sourceType = {sourceType.AsString()}, attempt {++i}");
				yield return new WaitForEndOfFrame();
			}

			if (RegenerationInterval <= 0f || RegenerationRateAbsolute <= 0f)
			{
				if (sourceType != TechType.None && regenParameters.TryGetValue(sourceType, out var value))
				{
					RegenerationInterval = value.interval;
					RegenerationRateAbsolute = value.rate;
				}
			}

			batt = (MonoBehaviour)source;
			//Log.LogDebug($"RegeneratingPowerSource: initialisation complete, source = {(batt != null ? batt.GetInstanceID().ToString() : "null")}, sourceType = {sourceType.AsString()}\nRegenerationRate: {RegenerationRateAbsolute}\nRegenerationInterval: {RegenerationInterval}");
			if (RegenerationRateAbsolute > 0f && RegenerationInterval > 0f)
			{
				//Log.LogDebug($"RegeneratingPowerSource: setting up UpdatePower");
				base.InvokeRepeating("UpdatePower", RegenerationInterval, RegenerationInterval);
			}
			yield break;
		}

		private void UpdatePower()
		{
			//Log.LogDebug($"RegeneratingPowerSource.UpdatePower(): begin");
			source ??= this.gameObject?.GetComponent<IBattery>();

			if (source == null)
			{
				//Log.LogDebug($"RegeneratingPowerSource: source is null");
				return;
			}

			if (source.charge >= source.capacity)
			{
				//Log.LogDebug($"RegeneratingPowerSource: Source is fully-charged");
				return;
			}

			//float regen = source.capacity * RegenerationInterval * RegenerationRate;
			float preCharge = source.charge;
			float capacityAvailable = Mathf.Max(source.capacity - preCharge, 0f);
			float regen = Mathf.Min(RegenerationInterval * RegenerationRateAbsolute, capacityAvailable);
			source.charge = preCharge + regen;
			logTimer += RegenerationInterval;
			timerPower += regen;
			//Log.LogDebug($"RegeneratingPowerSource.UpdatePower(): Source ID {((MonoBehaviour)source).GetInstanceID()}, TechType {sourceType.ToString()}; Attempted to restore {regen} units, restored {source.charge - preCharge} units, charge now {source.charge}");

			if (logTimer < 1f)
				return;

			logTimer -= 1f;
			if (timerPower >= 1f)
			{
				//Log.LogDebug($"RegeneratingPowerSource.UpdatePower(): Source ID {((MonoBehaviour)source).GetInstanceID()}, TechType {sourceType.ToString()}; Attempted to restore {regen} units, restored {source.charge - preCharge} units, charge now {source.charge}");
				timerPower = 0f;
			}
		}
	}
}
