using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UWE;

namespace DWEquipmentBonanza.VehicleModules
{
#if BELOWZERO
	//public class ExosuitLightningClaw : MonoBehaviour, IExosuitArm
	public class ExosuitLightningClaw : ExosuitClawArm
	{
		// Stol<cough>Borrowed from Senna's Seamoth Arms
		private void Awake()
		{
			animator = GetComponent<Animator>();
			fxControl = GetComponent<VFXController>();
			vfxEventType = VFXEventTypes.impact;

			foreach (FMODAsset asset in GetComponents<FMODAsset>())
			{
				if (asset.name == "claw_hit_terrain")
					this.hitTerrainSound = asset;

				if (asset.name == "claw_hit_fish")
					this.hitFishSound = asset;
			}

			this.pickupSounds = GetComponent<TechSoundData>();

			// Getting the wrist doesn't seem to work properly this early; I make it a coroutine here so that it can be set when available.
			CoroutineHost.StartCoroutine(GetDeepChildCoroutine());
			//base.Awake();
		}

		private IEnumerator GetDeepChildCoroutine()
		{
			while (this.front == null)
			{
				GameObject obj = null;
				if(this.gameObject != null)
					obj = TransformUtils.FindDeepChild(this.gameObject, "wrist");
				if (obj != null)
				{
					this.front = obj.transform;
				}
				yield return new WaitForSeconds(0.2f);
			}
			yield break;
		}

		new public void OnHit()
		{
			Exosuit componentInParent = base.GetComponentInParent<Exosuit>();
			if (componentInParent.CanPilot() && componentInParent.GetPilotingMode())
			{
				Vector3 position = default;
				GameObject gameObject = null;
				Vector3 vector;
				UWE.Utils.TraceFPSTargetPosition(componentInParent.gameObject, 6.5f, ref gameObject, ref position, out vector, true);
				if (gameObject == null)
				{
					InteractionVolumeUser component = Player.main.gameObject.GetComponent<InteractionVolumeUser>();
					if (component != null && component.GetMostRecent() != null)
					{
						gameObject = component.GetMostRecent().gameObject;
					}
				}
				if (gameObject)
				{
					LiveMixin liveMixin = gameObject.FindAncestor<LiveMixin>();
					if (liveMixin)
					{
						liveMixin.IsAlive();
						liveMixin.TakeDamage(50f, position, DamageType.Normal, null);
						// Originally the change was from Normal to Electrical, but there should be a Normal component and an Electrical.
						// Without a Normal component to the damage, Eye Jellies (to use one example) give no fucks about being twatted in the eye with a Lightning Claw.
						// That's not right; even if the electrical damage does nothing for them, it's a chunk of heavy metal being propelled at high speed by precision hydraulics.
						Log.LogDebug($"ExosuitLightningClaw: inflicting Electrical damage on target {gameObject.ToString()}");
						liveMixin.TakeDamage(30f, position, DamageType.Electrical, null); // Animals treat this as over 1000 damage when deciding whether to flee or not.
						// Curiously, Snow Stalkers appear to give no fucks about any sort of damage, and will continue pursuing until you either get beyond their
						// "bollocks to this" range, or until they are dead.
						global::Utils.PlayFMODAsset(base.hitFishSound, this.front, 50f);
					}
					else
					{
						global::Utils.PlayFMODAsset(base.hitTerrainSound, this.front, 50f);
					}
					VFXSurface component2 = gameObject.GetComponent<VFXSurface>();
					Vector3 euler = MainCameraControl.main.transform.eulerAngles + new Vector3(300f, 90f, 0f);
					VFXSurfaceTypeManager.main.Play(component2, base.vfxEventType, position, Quaternion.Euler(euler), componentInParent.gameObject.transform);
					gameObject.SendMessage("BashHit", this, SendMessageOptions.DontRequireReceiver);
				}
			}
		}

		/*
		// Token: 0x060019E5 RID: 6629 RVA: 0x0002B86D File Offset: 0x00029A6D
		GameObject IExosuitArm.GetGameObject()
		{
			return base.gameObject;
		}

		// Token: 0x060019E6 RID: 6630 RVA: 0x0008095C File Offset: 0x0007EB5C
		GameObject IExosuitArm.GetInteractableRoot(GameObject target)
		{
			Pickupable componentInParent = target.GetComponentInParent<Pickupable>();
			if (componentInParent != null && componentInParent.isPickupable)
			{
				return componentInParent.gameObject;
			}
			PickPrefab componentProfiled = target.GetComponentProfiled<PickPrefab>();
			if (componentProfiled != null)
			{
				return componentProfiled.gameObject;
			}
			BreakableResource componentInParent2 = target.GetComponentInParent<BreakableResource>();
			if (componentInParent2 != null)
			{
				return componentInParent2.gameObject;
			}
			return null;
		}

		// Token: 0x060019E7 RID: 6631 RVA: 0x000809B8 File Offset: 0x0007EBB8
		void IExosuitArm.SetSide(Exosuit.Arm arm)
		{
			this.exosuit = base.GetComponentInParent<Exosuit>();
			if (arm == Exosuit.Arm.Right)
			{
				base.transform.localScale = new Vector3(-1f, 1f, 1f);
				return;
			}
			base.transform.localScale = new Vector3(1f, 1f, 1f);
		}

		// Token: 0x060019E8 RID: 6632 RVA: 0x00080A14 File Offset: 0x0007EC14
		bool IExosuitArm.OnUseDown(out float cooldownDuration)
		{
			return this.TryUse(out cooldownDuration);
		}

		// Token: 0x060019E9 RID: 6633 RVA: 0x00080A14 File Offset: 0x0007EC14
		bool IExosuitArm.OnUseHeld(out float cooldownDuration)
		{
			return this.TryUse(out cooldownDuration);
		}

		// Token: 0x060019EA RID: 6634 RVA: 0x00080A1D File Offset: 0x0007EC1D
		bool IExosuitArm.OnUseUp(out float cooldownDuration)
		{
			cooldownDuration = 0f;
			return true;
		}

		// Token: 0x060019EB RID: 6635 RVA: 0x000044D5 File Offset: 0x000026D5
		bool IExosuitArm.OnAltDown()
		{
			return false;
		}

		// Token: 0x060019EC RID: 6636 RVA: 0x00002319 File Offset: 0x00000519
		void IExosuitArm.Update(ref Quaternion aimDirection)
		{
		}

		// Token: 0x060019ED RID: 6637 RVA: 0x00002319 File Offset: 0x00000519
		void IExosuitArm.ResetArm()
		{
		}

		// Token: 0x060019EE RID: 6638 RVA: 0x00080A28 File Offset: 0x0007EC28
		private bool TryUse(out float cooldownDuration)
		{
			if (Time.time - this.timeUsed >= this.cooldownTime)
			{
				Pickupable pickupable = null;
				PickPrefab x = null;
				if (this.exosuit.GetActiveTarget())
				{
					pickupable = this.exosuit.GetActiveTarget().GetComponent<Pickupable>();
					x = this.exosuit.GetActiveTarget().GetComponent<PickPrefab>();
				}
				if (pickupable != null && pickupable.isPickupable)
				{
					if (this.exosuit.storageContainer.container.HasRoomFor(pickupable))
					{
						this.animator.SetTrigger("use_tool");
						this.cooldownTime = (cooldownDuration = this.cooldownPickup);
						this.shownNoRoomNotification = false;
						return true;
					}
					if (!this.shownNoRoomNotification)
					{
						ErrorMessage.AddMessage(Language.main.Get("ContainerCantFit"));
						this.shownNoRoomNotification = true;
					}
				}
				else
				{
					if (x != null)
					{
						this.animator.SetTrigger("use_tool");
						this.cooldownTime = (cooldownDuration = this.cooldownPickup);
						return true;
					}
					this.animator.SetTrigger("bash");
					this.cooldownTime = (cooldownDuration = this.cooldownPunch);
					this.fxControl.Play(0);
					return true;
				}
			}
			cooldownDuration = 0f;
			return false;
		}

		// Token: 0x060019EF RID: 6639 RVA: 0x00080B60 File Offset: 0x0007ED60
		public void OnPickup()
		{
			Exosuit componentInParent = base.GetComponentInParent<Exosuit>();
			if (componentInParent.GetActiveTarget())
			{
				Pickupable component = componentInParent.GetActiveTarget().GetComponent<Pickupable>();
				PickPrefab component2 = componentInParent.GetActiveTarget().GetComponent<PickPrefab>();
				base.StartCoroutine(this.OnPickupAsync(component, component2, componentInParent));
			}
		}

		// Token: 0x060019F0 RID: 6640 RVA: 0x00080BA9 File Offset: 0x0007EDA9
		private IEnumerator OnPickupAsync(Pickupable pickupAble, PickPrefab pickPrefab, Exosuit exo)
		{
			if (pickupAble != null && pickupAble.isPickupable && exo.storageContainer.container.HasRoomFor(pickupAble))
			{
				pickupAble.Initialize();
				InventoryItem item = new InventoryItem(pickupAble);
				exo.storageContainer.container.UnsafeAdd(item);
				global::Utils.PlayFMODAsset(this.pickupSound, this.front, 5f);
			}
			else if (pickPrefab != null)
			{
				TaskResult<bool> result = new TaskResult<bool>();
				yield return pickPrefab.AddToContainerAsync(exo.storageContainer.container, result);
				if (result.Get())
				{
					pickPrefab.SetPickedUp();
				}
				result = null;
			}
			yield break;
		}

		// Token: 0x04001D57 RID: 7511
		public const float kGrabDistance = 6f;

		// Token: 0x04001D58 RID: 7512
		public Animator animator;

		// Token: 0x04001D59 RID: 7513
		public FMODAsset hitTerrainSound;

		// Token: 0x04001D5A RID: 7514
		public FMODAsset hitFishSound;

		// Token: 0x04001D5B RID: 7515
		public FMODAsset pickupSound;

		// Token: 0x04001D5C RID: 7516
		public Transform front;

		// Token: 0x04001D5D RID: 7517
		public VFXEventTypes vfxEventType;

		// Token: 0x04001D5E RID: 7518
		public VFXController fxControl;

		// Token: 0x04001D5F RID: 7519
		public float cooldownPunch = 1f;

		// Token: 0x04001D60 RID: 7520
		public float cooldownPickup = 1.533f;

		// Token: 0x04001D61 RID: 7521
		private const float attackDist = 6.5f;

		// Token: 0x04001D62 RID: 7522
		private const float damage = 50f;

		// Token: 0x04001D63 RID: 7523
		private const DamageType damageType = DamageType.Normal;

		// Token: 0x04001D64 RID: 7524
		[AssertLocalization]
		private const string noRoomNotification = "ContainerCantFit";

		// Token: 0x04001D65 RID: 7525
		private float timeUsed = float.NegativeInfinity;

		// Token: 0x04001D66 RID: 7526
		private float cooldownTime;

		// Token: 0x04001D67 RID: 7527
		private bool shownNoRoomNotification;

		// Token: 0x04001D68 RID: 7528
		private Exosuit exosuit;
	*/
	}
#endif
}
