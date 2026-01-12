using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace Stacking
{
	[Serializable]
	internal class Stackable : MonoBehaviour
	{
		[SerializeField]
		private Stack<Pickupable> stackedItems;
		public int stackAmount => stackedItems.Count;

		[NonSerialized]
		private int _stackLimit = -1;
		private Stackable stackParent;
		private Pickupable item;

		public int stackLimit
		{
			get
			{
				if (gameObject == null)
					return 0;

				if(stackParent != null)
					return stackParent.stackAmount;

				if(_stackLimit < 0)
					_stackLimit = StackablesPlugin.GetStackLimitForGameObject(gameObject);

				return _stackLimit;
			}
		}

		public IEnumerator Start()
		{
			yield return new WaitUntil(() => gameObject != null && gameObject.GetComponent<InventoryItem> != null);
			item = gameObject.GetComponent<Pickupable>();

			if (gameObject.transform.parent != null && gameObject.transform.gameObject != null && gameObject.transform.gameObject.GetComponent<Stackable>() is Stackable checkParent)
			{
				this.SetParent(checkParent);
			}
			else if (stackedItems == null)
			{
				stackedItems = new Stack<Pickupable>();

				stackedItems.Push(item); // Put the current item on the stack as the first entry; after all, a stack of items includes the original one.
										 // Doing this means that stackedItems.Count will always match the number of items in the stack, simplifying a lot of accessing code

				// Run through our GameObject's object hierarchy, in order to restore our stack of pickups if this is running just after a game reload.
				foreach (Pickupable p in this.gameObject.GetComponentsInChildren<Pickupable>(true))
				{
					if (p == this)
						continue;

					stackedItems.Push(p);
				}
			}
			
			_stackLimit = StackablesPlugin.GetStackLimitForGameObject(gameObject);

			yield break;
		}

		internal void SetParent(Stackable newParent)
		{
			if (newParent == null && stackParent != null)
				return;

			stackParent = newParent;
		}

		public bool AddItemToStack(Pickupable item)
		{
			if (stackAmount >= stackLimit || stackedItems.Contains(item))
				return false;

			stackedItems.Push(item);

			// Now we're going to set up our backup method for loading the stacked items.
			item.gameObject.transform.SetParent(gameObject.transform, false);
			item.gameObject.EnsureComponent<Stackable>().SetParent(this);
			item.gameObject.SetActive(false);
			return true;
		}

		public bool AddItemsToStack(Pickupable[] items, out Pickupable[] failedToAdd)
		{
			if (stackParent != null)
				return stackParent.AddItemsToStack(items, out failedToAdd);

			if (stackAmount >= stackLimit)
			{
				failedToAdd = items;
				return false;
			}

			bool bStackFull = false;
			List<Pickupable> failedAdds = new List<Pickupable>();
			
			for (int i = 0; i < items.Length; i++)
			{
				bStackFull = bStackFull || !this.AddItemToStack(items[i]);

				if (bStackFull)
				{
					failedAdds.Add(items[i]);
				}
			}

			failedToAdd = failedAdds.ToArray();
			return !bStackFull;
		}

		public bool TryAddItemToStack(GameObject go)
		{
			if (stackParent != null)
				return stackParent.TryAddItemToStack(go);

			if (go.TryGetComponent<Pickupable>(out Pickupable item))
			{
				return TryAddItemToStack(item);
			}

			return false;
		}

		public bool TryAddItemToStack(InventoryItem item)
		{
			if (item.item == null)
			{
				return false;
			}

			if(stackParent != null)
				return stackParent.TryAddItemToStack(item);

			return TryAddItemToStack(item.item);
		}

		// Attempt to add the specified item to this stack. Returns true if successful, and false if it could not - such as if the stack is full.
		public bool TryAddItemToStack(Pickupable pickupable)
		{
			if (stackParent != null)
				return stackParent.TryAddItemToStack(pickupable);

			if (stackAmount == stackLimit)
				return false;

			stackedItems.Push(pickupable);
			return true;

		}

		// Try to remove a number of items from this stack. Returns the number of items actually removed
		// If the return value is less than the value provided, this means the stack was exhausted before removing all the requested items.
		public int TryRemoveItem(int amount, out bool bStackIsNowEmpty)
		{
			int i;
			var deadItem = stackedItems.Peek();

			for(i = 0; i < amount; i++)
			{
				deadItem = stackedItems.Pop();
				if (deadItem == this)
					break; // We've reached the end of the stack.
				GameObject.Destroy(deadItem.gameObject);
			}

			if (stackAmount == 0)
			{
				bStackIsNowEmpty = true;
				GameObject.Destroy(this.gameObject);
			}

			bStackIsNowEmpty = false;
			return i;
		}
	}
}
