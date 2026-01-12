using Main = DWEquipmentBonanza.DWEBPlugin;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Crafting;
using Nautilus.Utility;
using Nautilus.Handlers;
	#if SN1
	//using Ingredient = CraftData\.Ingredient;
	#endif
using Common.NautilusHelper;
using RecipeData = Nautilus.Crafting.RecipeData;
#else
using RecipeData = SMLHelper.V2.Crafting.TechData;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Utility;
using SMLHelper.V2.Handlers;
#endif
using UnityEngine;
using UWE;
#if BEPINEX
	using BepInEx;
	using BepInEx.Logging;
#elif QMM
	using Logger = QModManager.Utility.Logger;
#endif


using Common;

namespace DWEquipmentBonanza.Equipables
{
#if BELOWZERO
	internal class ReinforcedColdGloves : Equipable
	{
#if NAUTILUS
		protected override string templateClassId => String.Empty;
		protected override TechType templateType => TechType.ColdSuitGloves;
#endif
		private const float tempBonus = 8f;
		public ReinforcedColdGloves() : base("ReinforcedColdGloves", "Reinforced Cold Gloves", "Reinforced insulating gloves provide physical protection and insulation from extreme temperatures.")
		{
			//Console.WriteLine($"{this.ClassID} constructing"); 
			OnFinishedPatching += () =>
			{
				int coldResist = TechData.GetColdResistance(TechType.ColdSuitGloves);
				DWEquipmentBonanza.Reflection.AddColdResistance(this.TechType, System.Math.Max(10, coldResist));
				DWEquipmentBonanza.Reflection.SetItemSize(this.TechType, 2, 2);
				Log.LogDebug($"Finished patching {this.TechType.AsString()}, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
				Main.AddSubstitution(this.TechType, TechType.ColdSuitGloves);
				Main.AddSubstitution(this.TechType, TechType.ReinforcedGloves);
				Main.AddModTechType(this.TechType);
				Main.AddTempBonusOnly(this.TechType, tempBonus);
			};
		}

		protected static GameObject prefab;

		public override EquipmentType EquipmentType => EquipmentType.Gloves;
		public override Vector2int SizeInInventory => new(2, 2);
		public override QuickSlotType QuickSlotType => QuickSlotType.None;
		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 0,
				Ingredients = new List<Ingredient>()
			};
		}

		protected override Sprite GetItemSprite()
		{
			return SpriteManager.Get(TechType.ColdSuitGloves);
		}

#if NAUTILUS
#else
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuitGloves, verbose: true);
				yield return task;

				prefab = GameObject.Instantiate(task.GetResult());
				ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
			}

			GameObject go = GameObject.Instantiate(prefab);
			gameObject.Set(go);
		}
#endif
	}

	internal class ReinforcedColdSuit : Equipable
	{
#if NAUTILUS
		protected override string templateClassId => String.Empty;
		protected override TechType templateType => TechType.ColdSuit;
#else
		public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
		{
			if (prefab == null)
			{
				CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.ColdSuit, verbose: true);
				yield return task;

				prefab = GameObject.Instantiate(task.GetResult());
				ModPrefabCache.AddPrefab(prefab, false); // This doesn't actually do any caching, but it does disable the prefab without "disabling" it - the prefab doesn't show up in the world [as with SetActive(false)] but it can still be instantiated.
			}

			GameObject go = GameObject.Instantiate(prefab);
			gameObject.Set(go);
		}
#endif
		public ReinforcedColdSuit() : base("ReinforcedColdSuit", "Reinforced Cold Suit", "Reinforced, insulated diving suit providing physical protection and insulation from extreme temperatures.")
		{
			//Console.WriteLine($"{this.ClassID} constructing");
			OnFinishedPatching += () =>
			{
				int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
				//Console.WriteLine($"{this.ClassID} Setting cold resistance");
				Reflection.AddColdResistance(this.TechType, System.Math.Max(50, coldResist));
				//Console.WriteLine($"{this.ClassID} Setting item size");
				Reflection.SetItemSize(this.TechType, 2, 3);
				Log.LogDebug($"Finished patching {this.TechType.AsString()}, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
				//Console.WriteLine($"{this.ClassID} Adding substitutions");
				Main.AddSubstitution(this.TechType, TechType.ColdSuit);
				Main.AddSubstitution(this.TechType, TechType.ReinforcedDiveSuit);
				//Console.WriteLine($"{this.ClassID} Adding Mod TechType");
				Main.AddModTechType(this.TechType);
				//Console.WriteLine($"{this.ClassID} Adding compound techs");
				Reflection.AddCompoundTech(this.TechType, new List<TechType>()
				{
					TechType.ReinforcedDiveSuit,
					TechType.ColdSuit
				});
			};
		}

		protected static GameObject prefab;

		public override EquipmentType EquipmentType => EquipmentType.Body;
		public override bool UnlockedAtStart => false;
		public override Vector2int SizeInInventory => new(2, 2);
		public override QuickSlotType QuickSlotType => QuickSlotType.None;
		public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
		public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(TechType.ColdSuit, 1),
						new Ingredient(TechType.ColdSuitGloves, 1),
						new Ingredient(TechType.ReinforcedDiveSuit, 1),
						new Ingredient(TechType.ReinforcedGloves, 1)
					}
				),
				LinkedItems = new List<TechType>()
				{
					Main.GetModTechType("ReinforcedColdGloves")
				}
			};
		}

		protected override Sprite GetItemSprite()
		{
			return SpriteManager.Get(TechType.ColdSuit);
		}

	}
#endif
}
