using Common;
using DWEquipmentBonanza.Patches;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;
using Math = System.Math;
using DWCommon;

namespace DWEquipmentBonanza.MonoBehaviours
{
	public class HazardShieldComponent : MonoBehaviour,

		IBattery,
		ICraftTarget,
		ISerializationCallbackReceiver,
		IProtoEventListener
	{
		[SerializeField]
		protected TechType _techType;
		public TechType techType => _techType;

		[SerializeField]
		protected float _charge = -1f;
		protected virtual float _capacity => 200f;
		public float capacity => _capacity;

#if SN1
		public static readonly Gradient gradient = new Gradient
		{
			colorKeys = new GradientColorKey[]
			{
				new GradientColorKey(new Color(0.8745099f, 0.2509804f, 0.1490196f, 1f), 0f),
				new GradientColorKey(new Color(1f, 0.8196079f, 0f, 1f), 0.5f),
				new GradientColorKey(new Color(0.5803922f, 0.8705883f, 0f, 1f), 1f)
			},
			alphaKeys = new GradientAlphaKey[]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};
#endif

		public static readonly Gradient shieldHealthGradient = new Gradient
		{
			colorKeys = new GradientColorKey[]
			{
				new GradientColorKey(new Color(0f, 0f, 0f, 1f), 0f),
				new GradientColorKey(new Color(1f, 0f, 0f, 1f), 0.2f),
				new GradientColorKey(new Color(1f, 1f, 0f, 1f), 0.5f),
				new GradientColorKey(new Color(0f, 1f, 0f, 1f), 0.8f),
				new GradientColorKey(new Color(0f, 0f, 1f, 1f), 1f),
			},
			alphaKeys = new GradientAlphaKey[]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};
		
		public float charge
		{
			get { return _charge; }
			set { _charge = Mathf.Clamp(value, 0f, _capacity); }
		}

		public string GetChargeValueText()
		{
			float num = this._charge / this._capacity;
#if SN1
			return Language.main.GetFormat<float, int, float>("BatteryCharge", num, Mathf.RoundToInt(this._charge), this._capacity);

#elif BELOWZERO
			return Language.main.GetFormat<string, float, int, float>("BatteryCharge", ColorUtility.ToHtmlStringRGBA(Battery.gradient.Evaluate(num)), num, Mathf.RoundToInt(this._charge), this._capacity);
#endif
        }

        // A dictionary of the damage types which the Hazard Shield protects against, and an energy rate. The rate of energy drain is multiplied by the value in the dictionary.
        // This allows certain damage types to require more battery power while shielding against that type
        private static readonly Dictionary<DamageType, float> shieldedDamageType = new Dictionary<DamageType, float>()
		{
			{ DamageType.Fire, 1f },
			{ DamageType.Pressure, 1.5f },
			{ DamageType.Acid, 1.2f },
            { DamageType.Cold, 1f },
            { DamageType.Heat, 1f },
            { DamageType.Radiation, 2f }
        };

		protected virtual float damageToEnergyRatio => 0.5f;
		public Material shieldActiveMaterial { get; private set; }
		public static readonly Color shieldActiveColour = Color.yellow;
		public VFXOverlayMaterial shieldFX { get; private set; }
		public float activeOverlayTimer { get; private set; } = 0f;
		public const float DamageOverlayTime = 1f; // How long to keep the shader active after the last instance of damage absorption.
												   // The overlay is immediately deactivated if the battery is emptied
		private bool bTimerIsRunning;

		public enum EState
		{
			None = 0,
			Waiting = 1,
			Done = 2,
			Error = 3
		}
		public EState InitState { get; private set; } = EState.None;

		//protected EnergyMixin energyMixin;

		internal void Initialise(TechType newTechType)
		{
			Log.LogDebug($"HazardShieldComponent.Initialise(): existing chip TechType {this.techType.AsString()}, passed TechType {newTechType.AsString()}");
			if (this.techType == TechType.None)
				this.OnCraftEnd(newTechType);

			CoroutineHost.StartCoroutine(Setup());
		}

		public void OnCraftEnd(TechType craftedTechType)
		{
			if(craftedTechType != TechType.None)
				this._techType = craftedTechType;
			if(_charge < 0f)
				_charge = _capacity;

			Log.LogDebug($"HazardShieldComponent.OnCraftEnd({craftedTechType.AsString()}): completed");
		}

		public void Awake()
		{
			/*var storageObject = gameObject.FindChild("StorageRoot") ?? new GameObject("StorageRoot", new Type[] { typeof(ChildObjectIdentifier) });
			storageObject.transform.SetParent(gameObject.transform);
			energyMixin = gameObject.EnsureComponent<EnergyMixin>();
			energyMixin.storageRoot ??= storageObject.EnsureComponent<ChildObjectIdentifier>();
			energyMixin.defaultBattery = TechType.PrecursorIonBattery;
			energyMixin.compatibleBatteries = new List<TechType>() { TechType.PrecursorIonBattery };
			energyMixin.allowBatteryReplacement = false;*/
		}

		public IEnumerator Setup()
		{
			Log.LogDebug("HazardShieldComponent.Setup() begin");
			//energyMixin.defaultBattery = TechType.PrecursorIonBattery;
			//energyMixin.defaultBatteryCharge = 1f;

			//yield return energyMixin.SpawnBatteryAsync(TechType.PrecursorIonBattery, 1.0f, DiscardTaskResult<GameObject>.Instance);
			InitState = EState.Waiting;
			if (shieldActiveMaterial == null)
			{
				Log.LogDebug("HazardShieldComponent.Setup: preparing shieldActiveMaterial", null, true);
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.Scanner);
				yield return task;

				Log.LogDebug("HazardShieldComponent.Setup: Got prefab for TechType.Scanner", null, true);
				GameObject scannerPrefab = task.GetResult();
				ScannerTool scannerToolPrefab = scannerPrefab.GetComponent<ScannerTool>();

				if (scannerToolPrefab != null)
				{
					Log.LogDebug("HazardShieldComponent.Setup: Got ScannerTool component, finalising shieldActiveMaterial", null, true);
					Shader scannerToolScanning = ShaderManager.preloadedShaders.scannerToolScanning;
					shieldActiveMaterial = new Material(scannerToolScanning);
					shieldActiveMaterial.hideFlags = HideFlags.HideAndDontSave;
					shieldActiveMaterial.SetTexture(ShaderPropertyID._MainTex, scannerToolPrefab.scanCircuitTex);
					shieldActiveMaterial.SetColor(ShaderPropertyID._Color, shieldActiveColour);
				}
				else
				{
					Log.LogError("HazardShieldComponent.Setup: Got prefab for Scanner but no ScannerTool component!", null, true);
					InitState = EState.Error;
				}
			}

			InitState = EState.Done;
			yield break;
		}

		[ProtoBeforeSerialization]
		public void OnBeforeSerialize()
		{
			System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): begin");
			try
			{
				if (gameObject == null)
				{
					Log.LogError($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}) gameObject is null!");
					return;
				}
			}
			catch (Exception e)
			{
				Log.LogError($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): Exception in OnBeforeSerialize:");
				Log.LogError(e.ToString());
				return;
			}

			string moduleId = gameObject.GetComponent<PrefabIdentifier>()?.id;
			if (string.IsNullOrEmpty(moduleId))
			{

				Log.LogError($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): Cannot save charge value; Invalid ID for object");
				return;
			}
			Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}({this.GetInstanceID()}): Saving charge value of {this.charge} to disk for module ID of '{moduleId}'");
			DWEBPlugin.saveCache.AddModuleCharge(moduleId, this.charge);
		}

		[ProtoAfterDeserialization]
		public void OnAfterDeserialize()
		{
			CoroutineHost.StartCoroutine(PostDeserializeCoroutine());
			CoroutineHost.StartCoroutine(Setup());
		}

		public IEnumerator PostDeserializeCoroutine()
		{
			if (this.techType == TechType.None)
			{
				yield return new WaitUntil(() =>
				{
					this._techType = CraftData.GetTechType(gameObject);
					return this._techType != TechType.None;
				});
			}

			if (this.techType == TechType.None)
			{
				Log.LogError($"HazardShieldComponent.OnAfterDeserialize(): deserialised with null TechType!");
				yield break;
			}

			PrefabIdentifier pId = null;
			yield return new WaitUntil(() =>
			{
				pId = gameObject?.GetComponent<PrefabIdentifier>();
				return pId != null;
			});

			string moduleId = "";
			yield return new WaitUntil(() =>
			{
				moduleId = pId.id;
				return !string.IsNullOrEmpty(moduleId);

			});

			if (DWEBPlugin.saveCache.TryGetModuleCharge(moduleId, out float charge))
			{
				Log.LogDebug($"HazardShieldComponent.OnAfterDeserialize(): Retrieved charge value of {charge} from disk for module ID of '{moduleId}'");
				this.charge = charge;
			}
			else
				Log.LogDebug($"HazardShieldComponent.OnAfterDeserialize(): no charge value available for module ID of '{moduleId}'");
			DWEBPlugin.saveCache.RegisterReceiver(this);
		}

		[ProtoBeforeDeserialization]
		public void OnBeforeDeserialize()
		{
			
		}

		public void OnProtoDeserialize(ProtobufSerializer serializer)
		{
			OnAfterDeserialize();
		}

		public void OnProtoSerialize(ProtobufSerializer serializer)
		{
			OnBeforeSerialize();
		}

		protected virtual float ModifyCharge(float amount)
		{
			float consumed = amount < 1 ? -Math.Min(Math.Abs(amount), _charge) : Math.Min(amount, _capacity - charge);
			_charge += consumed;

			return Math.Abs(consumed);
		}

		// Attempt to absorb damage; returns amount of damage absorbed.
		public float AbsorbDamage(float damage, DamageType type)
		{
			if (shieldedDamageType.TryGetValue(type, out float energyRate))
			{
				float energyRequired = damage * damageToEnergyRatio * energyRate;
				float energyConsumed = ModifyCharge(-energyRequired);

				if (energyConsumed == energyRequired)
				{
					PlayScanFX();
					return damage;
				}

				ErrorMessage.AddMessage("Shield failure!");
				StopScanFX();
				return energyConsumed / (damageToEnergyRatio * energyRate);
			}

			StopScanFX();
			return 0f;
		}

		private IEnumerator InactiveTimer()
		{
			if (bTimerIsRunning)
				yield break; // If called while the timer is already running, terminate this coroutine

			bTimerIsRunning = true;
			float chargeLevel = 0f;
			Color shieldColour = new Color();
			while (bTimerIsRunning)
			{
				yield return new WaitForEndOfFrame();

				activeOverlayTimer -= Time.deltaTime;
				if (activeOverlayTimer < 0f)
				{
					bTimerIsRunning = false;
					StopScanFX();
					yield break;
				}

				chargeLevel = _charge / _capacity;

				shieldColour = shieldHealthGradient.Evaluate(chargeLevel);
				shieldActiveMaterial.SetColor(ShaderPropertyID._Color, shieldColour); 
			}
		}

		public void Update()
		{
			if (this.activeOverlayTimer > 0f)
			{
				this.activeOverlayTimer = Mathf.Max(0f, this.activeOverlayTimer - Time.deltaTime);
				if (this.activeOverlayTimer == 0f)
					StopScanFX();
			}
		}

		public void PlayScanFX()
		{
			this.activeOverlayTimer = DamageOverlayTime;
			if (this.shieldFX == null)
			{
				this.shieldFX = Player.main.gameObject.AddComponent<VFXOverlayMaterial>();
				this.shieldFX.ApplyOverlay(shieldActiveMaterial, "VFXOverlay: Scanning", false);
				float num = 1f;
				if (!MiscSettings.flashes)
					num = 0.1f;

				//shieldActiveMaterial.SetFloat(ShaderPropertyID._TimeScale, num);
				// For some reason, Visual Studio fails to compile the above line at 'ShaderPropertyID._TimeScale' when compiling for BelowZero, claiming "ShaderPropertyID does not contain a definition for _TimeScale".
				// The below approach works though
				shieldActiveMaterial.SetFloat(Shader.PropertyToID("_TimeScale"), num);
				if(!bTimerIsRunning)
					CoroutineHost.StartCoroutine(InactiveTimer());
			}
		}

		public void StopScanFX()
		{
			if (this.shieldFX == null)
				return;

			this.shieldFX.RemoveOverlay();
			this.StopCoroutine(InactiveTimer());
		}
	}
}
