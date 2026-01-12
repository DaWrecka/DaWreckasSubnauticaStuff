#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.NautilusHelper
{
	public abstract class Equipable : Craftable
	{
		/// <summary>
		/// Gets the type of equipment slot this item can fit into.
		/// </summary>
		/// <value>
		/// The type of the equipment slot compatible with this item.
		/// </value>
		public abstract EquipmentType EquipmentType { get; }

		/// <summary>
		/// Gets the type of equipment slot this item can fit into.
		/// </summary>
		/// <value>
		/// The type of the equipment slot compatible with this item.
		/// </value>
		public abstract QuickSlotType QuickSlotType { get; }

		public override void FinalisePrefab(CustomPrefab prefab)
		{
			base.FinalisePrefab(prefab);
			prefab.SetEquipment(EquipmentType)
				.WithQuickSlotType(QuickSlotType);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Equipable"/> class.
		/// </summary>
		/// <param name="classId">The main internal identifier for this item. Your item's <see cref="TechType" /> will be created using this name.</param>
		/// <param name="friendlyName">The name displayed in-game for this item whether in the open world or in the inventory.</param>
		/// <param name="description">The description for this item; Typically seen in the PDA, inventory, or crafting screens.</param>
		protected Equipable(string classId, string friendlyName, string description)
			: base(classId, friendlyName, description)
		{
		}
	}
}
#endif
