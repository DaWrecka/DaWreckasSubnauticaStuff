using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace DWEquipmentBonanza.MonoBehaviours
{
	public abstract class PropulsionArmCollector : MonoBehaviour
	{
		protected abstract PropulsionCannon cannon { get; }
		protected abstract IEnumerable<StorageContainer> containers { get; }
		public static float breakableHitInterval;

		protected bool wasHoldingControl = false;
		protected bool lastDamagedBreakable = false;
		public static GameInput.Button usebutton => GameInput.Button.Deconstruct;

		public void Update()
		{
			float deltaTime = Time.deltaTime;
			bool isHoldingControl = GameInput.GetButtonDown(usebutton);
			if (cannon != null && cannon.IsGrabbingObject())
			{
				GameObject grabbedObject = cannon.grabbedObject;
				Pickupable grabbedPickup = grabbedObject.GetComponent<Pickupable>();
				if (grabbedPickup != null && grabbedPickup.isPickupable)
				{
					bool bHasRoomFor = false;
					foreach (StorageContainer container in containers)
					{
						if (container.container.HasRoomFor(grabbedPickup))
						{
							bHasRoomFor = true;

							if (isHoldingControl && !wasHoldingControl)
							{
								//if (exosuit.storageContainer.container.AddItem(grabbedPickup) != null)
								cannon.grabbedObject = null;
								CoroutineHost.StartCoroutine(AddItemAsync(grabbedPickup, null, container));
							}
							else
							{
								HandReticle.main.SetIcon(HandReticle.IconType.Hand);
								HandReticle.main.SetText(HandReticle.TextType.UseSubscript, LanguageCache.GetPickupText(grabbedPickup.GetTechType()), true, usebutton);
							}
							return;
						}
					}

					HandReticle.main.SetIcon(HandReticle.IconType.HandDeny);
					HandReticle.main.SetText(HandReticle.TextType.UseSubscript, Language.main.GetFormat("InventoryFull"), true, usebutton);
				}
				else if (grabbedObject.GetComponent<BreakableResource>() != null)
				{
					BreakableResource breakable = grabbedObject.GetComponent<BreakableResource>();
					if (breakable != null)
					{
						if (!lastDamagedBreakable)
						{
							breakable.BreakIntoResources();
							lastDamagedBreakable = true;
							return;
						}

						lastDamagedBreakable = false;
					}
				}
			}

			wasHoldingControl = isHoldingControl;
		}

		protected virtual IEnumerator AddItemAsync(Pickupable pickupAble, PickPrefab pickPrefab, StorageContainer storageContainer)
		{
			if (pickupAble != null && pickupAble.isPickupable && storageContainer.container.HasRoomFor(pickupAble))
			{
				pickupAble.Initialize();
				//InventoryItem item = new InventoryItem(pickupAble);

				pickupAble.PlayPickupSound();
				storageContainer.container.AddItem(pickupAble);
			}
			else if (pickPrefab != null)
			{
				TaskResult<bool> result = new TaskResult<bool>();
				yield return pickPrefab.AddToContainerAsync(storageContainer.container, result);
				if (result.Get())
				{
					pickPrefab.SetPickedUp();
				}
				result = null;
			}

			yield break;
		}
	}

	public class ExosuitPropulsionArmCollector : PropulsionArmCollector
	{
		private ExosuitPropulsionArm arm;
		private Exosuit exosuit;
		protected override PropulsionCannon cannon => arm != null ? arm.propulsionCannon : null;
		protected override IEnumerable<StorageContainer> containers => new[] { exosuit?.storageContainer };

		public IEnumerator Start()
		{ 
			yield return new WaitUntil(() => gameObject != null);

			Log.LogDebug($"ExosuitPropulsionArmCollector.Start executing");
			arm = gameObject.GetComponent<ExosuitPropulsionArm>();
			exosuit = gameObject.GetComponentInParent<Exosuit>();
		}

		public void Dispose()
		{
			arm = null;
			exosuit = null;
		}

		//protected override IEnumerator AddItemAsync(Pickupable pickupAble, PickPrefab pickPrefab, StorageContainer storageContainer)
	}

#if BELOWZERO
	public class SeaTruckPropulsionArmCollector : PropulsionArmCollector
	{
		private GameObject SeaTruckParent;
		private GameObject ArmParent;
		private StorageContainer[] seaTruckContainers;

		protected override PropulsionCannon cannon => ArmParent != null ? ArmParent.GetComponent<PropulsionCannon>() : null;
		protected override IEnumerable<StorageContainer> containers
		{
			get
			{
				seaTruckContainers = SeaTruckParent.GetComponentsInChildren<StorageContainer>();
				return seaTruckContainers.AsEnumerable();
			}
		}

		public IEnumerator Start()
		{
			yield return new WaitUntil(() => gameObject != null);

			Log.LogDebug($"SeaTruckPropulsionArmCollector.Start executing");
			ArmParent = gameObject;
			SeaTruckParent = gameObject.GetComponentInParent<SeaTruckUpgrades>().gameObject;
		}

		public void Dispose()
		{
			SeaTruckParent = null;
			ArmParent = null;
		}
	}
#endif
}
