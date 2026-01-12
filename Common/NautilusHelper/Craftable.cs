#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using CraftTreeHandler = Nautilus.Handlers.CraftTreeHandler;

namespace Common.NautilusHelper
{
	public abstract class Craftable : PdaItem
	{

		/// <summary>
		/// Override with the vanilla fabricator that crafts this item.<para/>
		/// Leave this as <see cref="CraftTree.Type.None"/> if you are manually adding this item to a custom fabricator.
		/// </summary>
		public virtual CraftTree.Type FabricatorType => CraftTree.Type.None;

		/// <summary>
		/// Override with the tab node steps to take to get to the tab you want the item's blueprint to appear in.
		/// If not overriden, the item will appear at the craft tree's root.
		/// </summary>
		public virtual string[] StepsToFabricatorTab => null;

		/// <summary>
		/// Override with a custom crafting time for this item. Normal default crafting time is <c>1f</c>.<para/>
		/// Any value zero or less will be ignored.
		/// </summary>
		public virtual float CraftingTime => 5f;

		public override void FinalisePrefab(CustomPrefab prefab)
		{
			base.FinalisePrefab(prefab);
			prefab.SetRecipe(GetBlueprintRecipe())
				.WithStepsToFabricatorTab(StepsToFabricatorTab)
				.WithFabricatorType(FabricatorType)
				.WithCraftingTime(CraftingTime);
		}

		/// <summary>
		/// Initializes a new <see cref="Craftable"/>, the basic class for any item that can be crafted at a fabricator.
		/// </summary>
		/// <param name="classId">The main internal identifier for this item. Your item's <see cref="TechType" /> will be created using this name.</param>
		/// <param name="friendlyName">The name displayed in-game for this item whether in the open world or in the inventory.</param>
		/// <param name="description">The description for this item; Typically seen in the PDA, inventory, or crafting screens.</param>
		protected Craftable(string classId, string friendlyName, string description)
			: base(classId, friendlyName, description)
		{
		}

		/*private void PatchCraftingData()
		{
			if (this.FabricatorType == CraftTree.Type.None)
			{
				Log.LogDebug($"Craftable '{this.ClassID}' was not automatically patched into a crafting tree.");
			}
			else
			{
				if (this.StepsToFabricatorTab == null || this.StepsToFabricatorTab.Length == 0)
					CraftTreeHandler.AddCraftingNode(this.FabricatorType, this.TechType);
				else
					CraftTreeHandler.AddCraftingNode(this.FabricatorType, this.TechType, this.StepsToFabricatorTab);
			}

			if (this.CraftingTime > 0f)
				CraftDataHandler.SetCraftingTime(this.TechType, this.CraftingTime);
		}*/
	}
}
#endif
