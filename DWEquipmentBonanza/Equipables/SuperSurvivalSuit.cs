using Main = DWEquipmentBonanza.DWEBPlugin;
using DWEquipmentBonanza.MonoBehaviours;
using DWEquipmentBonanza.Patches;
using Common;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;
using UWE;
#if SN1
	//using Sprite = Atlas.Sprite;
	using Object = UnityEngine.Object;
#endif

namespace DWEquipmentBonanza.Equipables
{
	internal class SuperSurvivalSuit : SurvivalSuitBase<SuperSurvivalSuit>
	{
		public SuperSurvivalSuit() : base(classId: "SuperSurvivalSuit",
			friendlyName: "Ultimate Survival Suit",
#if SN1
			Description: "The ultimate in survival gear. Provides protection from extreme temperatures, corrosive substances and physical harm, and reduces the need for external sustenance."
#elif BELOWZERO
			Description: "The ultimate in survival gear. Provides protection from extreme temperatures and physical harm, and reduces the need for external sustenance."
#endif
			)
		{
			Log.LogDebug($"{this.ClassID} constructing");
			OnFinishedPatching += OnFinishedPatch;
			OnStartedPatching += () => {
				Log.LogDebug($"{this.ClassID} started patching");
			};
		}



		public override void OnFinishedPatch()
		{
			base.OnFinishedPatch();
			Log.LogDebug($"SuperSurvivalSuit(): OnFinishedPatching begin");
			Reflection.AddCompoundTech(this.TechType, new List<TechType>()
			{
#if SN1
				TechType.RadiationSuit,
#elif BELOWZERO
				TechType.ColdSuit,
#endif
				Main.StillSuitType,
				TechType.ReinforcedDiveSuit
			});

#if SN1
			Main.AddSubstitution(this.TechType, TechType.RadiationSuit);
			/*Main.DamageResistances[this.TechType] = new List<Main.DamageInfo>()
			{
				{
					new Main.DamageInfo(DamageType.Acid, -0.6f)
				}
			};*/
			Main.AddDamageResist(this.TechType, DamageType.Acid, 0.6f);
			Log.LogDebug($"Finished patching {this.TechType.AsString()}");
#elif BELOWZERO
			int coldResist = TechData.GetColdResistance(TechType.ColdSuit);
			Reflection.AddColdResistance(this.TechType, System.Math.Max(55, coldResist));
			Reflection.SetItemSize(this.TechType, 2, 3);
			Log.LogDebug($"Finished patching {this.TechType.AsString()}, found source cold resist of {coldResist}, cold resistance for techtype {this.TechType.AsString()} = {TechData.GetColdResistance(this.TechType)}");
#endif

			// the SurvivalSuit constructor will call AddModTechType already.
			// It has also been set up to add substitutions based on the value of the 'substitutions' property below,
			// as well as set up CompoundTech based on the value of CompoundDependencies
			Log.LogDebug($"SuperSurvivalSuit(): OnFinishedPatching end");
		}
		public override bool UnlockedAtStart => false;
		public override EquipmentType EquipmentType => EquipmentType.Body;

		protected override float SurvivalCapOverride => 70f;
		protected override float maxDepth => 8000f;
		protected override float breathMultiplier => 0.50f;
		protected override float minTempBonus => 40f;
#if SN1
		protected override float DeathRunDepth => -1f;
#elif BELOWZERO
		protected override TechType prefabTechType => TechType.ColdSuit;
#endif

		protected override TechType[] substitutions => new TechType[] {
					TechType.ReinforcedDiveSuit,
					Main.StillSuitType,
#if BELOWZERO
					TechType.ColdSuit,
#else
					TechType.RadiationSuit,
#endif
				};

		protected override List<TechType> CompoundDependencies => new List<TechType>()
				{
					TechType.ReinforcedDiveSuit,
					Main.StillSuitType,
#if SN1
					TechType.RadiationSuit
#elif BELOWZERO
					TechType.ColdSuit,
#endif
				};

		protected override RecipeData GetBlueprintRecipe()
		{
#if SN1
			if (Main.HasNitrogenMod())
			{
				return new RecipeData()
				{
					craftAmount = 1,
					Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(Main.GetModTechType("NitrogenBrineSuit3"), 1),
						new Ingredient(Main.GetModTechType("SurvivalSuit"), 1),
					})
				};
			}
#endif


			return new RecipeData()
			{
				craftAmount = 1,
				Ingredients = new List<Ingredient>(new Ingredient[]
				{
					new Ingredient(Main.GetModTechType("SurvivalSuit"), 1),
#if SN1
					new Ingredient(Main.GetModTechType("AcidSuit"), 1),
#elif BELOWZERO
					new Ingredient(TechType.ReinforcedDiveSuit, 1),
					new Ingredient(TechType.ReinforcedGloves, 1),
					new Ingredient(TechType.ColdSuit, 1),
					new Ingredient(TechType.ColdSuitGloves, 1),
#endif
				}),
#if BELOWZERO
				LinkedItems = new List<TechType>()
				{
					Main.GetModTechType("ReinforcedColdGloves")
				}
#endif
			};
		}

		protected override Sprite GetItemSprite()
		{
#if SN1
			return SpriteManager.Get(Main.StillSuitType);
#elif BELOWZERO
			return SpriteManager.Get(TechType.ColdSuit);
#endif
		}

	}

	internal abstract class SurvivalSuitBlueprint : Craftable
	{
#if NAUTILUS
		protected override TechType templateType => TechType.ReinforcedDiveSuit;
		protected override string templateClassId => string.Empty;
#endif
		public SurvivalSuitBlueprint(string classId) : base(classId,
					"Ultimate Survival Suit",
					"The ultimate in survival gear. Provides protection from extreme temperatures and physical harm, and reduces the need for external sustenance.")
		{
			//Console.WriteLine($"{this.ClassID} constructing");
			OnFinishedPatching += () =>
			{
			};
		}
		public override TechType RequiredForUnlock => Main.GetModTechType("SuperSurvivalSuit");
		public override CraftTree.Type FabricatorType => CraftTree.Type.Workbench;
		public override string[] StepsToFabricatorTab => new string[] { DWConstants.BodyMenuPath };

		protected override Sprite GetItemSprite()
		{
			return SpriteManager.Get(Main.StillSuitType);
		}
	}

	internal class SurvivalSuitBlueprint_FromReinforcedSurvival : SurvivalSuitBlueprint
	{
		public SurvivalSuitBlueprint_FromReinforcedSurvival() : base("SurvivalSuitBlueprint_FromReinforcedSurvival")
		{
			OnFinishedPatching += () =>
			{
				Reflection.AddCompoundTech(this.TechType, new List<TechType>()
				{
					Main.StillSuitType,
#if SN1
					TechType.RadiationSuit,
#elif BELOWZERO
					TechType.ColdSuit,
#endif
					TechType.ReinforcedDiveSuit
				});
			};
		}

		public override bool UnlockedAtStart => false;

		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 0,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(Main.GetModTechType("ReinforcedSurvivalSuit"), 1),
						new Ingredient(TechType.ReinforcedGloves, 1),
#if SN1
						new Ingredient(TechType.HydrochloricAcid, 1),
						new Ingredient(TechType.CreepvinePiece, 2),
						new Ingredient(TechType.Aerogel, 1),
						new Ingredient(TechType.RadiationGloves, 1),
						new Ingredient(TechType.RadiationHelmet, 1),
						new Ingredient(TechType.RadiationSuit, 1),
#elif BELOWZERO
						new Ingredient(TechType.ColdSuit, 1),
						new Ingredient(TechType.ColdSuitGloves, 1),
#endif
					}
				),
				LinkedItems = new List<TechType>()
				{
					Main.GetModTechType("SuperSurvivalSuit"),
#if SN1
					Main.GetModTechType("AcidGloves"),
#elif BELOWZERO
					Main.GetModTechType("ReinforcedColdGloves")
#endif
				}
			};
		}
	}

#if BELOWZERO
		internal class SurvivalSuitBlueprint_FromReinforcedCold : SurvivalSuitBlueprint
	{
		public SurvivalSuitBlueprint_FromReinforcedCold() : base("SurvivalSuitBlueprint_FromReinforcedCold")
		{
			OnFinishedPatching += () =>
			{
				Reflection.AddCompoundTech(this.TechType, new List<TechType>()
				{
					Main.StillSuitType,
					TechType.ColdSuit,
					TechType.ReinforcedDiveSuit
				});
			};
		}

		public override bool UnlockedAtStart => false;
		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 0,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(Main.GetModTechType("ReinforcedColdSuit"), 1),
						new Ingredient(Main.GetModTechType("SurvivalSuit"), 1)
					}
				),
				LinkedItems = new List<TechType>()
				{
					Main.GetModTechType("SuperSurvivalSuit")
				}
			};
		}
	}

	internal class SurvivalSuitBlueprint_FromSurvivalCold : SurvivalSuitBlueprint
	{
		public SurvivalSuitBlueprint_FromSurvivalCold() : base("SurvivalSuitBlueprint_FromSurvivalCold")
		{
			OnFinishedPatching += () =>
			{
				Reflection.AddCompoundTech(this.TechType, new List<TechType>()
				{
					TechType.ReinforcedDiveSuit,
					Main.StillSuitType,
					TechType.ColdSuit
				});
			};
		}

		public override bool UnlockedAtStart => false;
		protected override RecipeData GetBlueprintRecipe()
		{
			return new RecipeData()
			{
				craftAmount = 0,
				Ingredients = new List<Ingredient>(new Ingredient[]
					{
						new Ingredient(Main.GetModTechType("SurvivalColdSuit"), 1),
						new Ingredient(TechType.ReinforcedGloves, 1),
						new Ingredient(TechType.ColdSuitGloves, 1),
						new Ingredient(TechType.ReinforcedDiveSuit, 1)
					}
				),
				LinkedItems = new List<TechType>()
				{
					Main.GetModTechType("SuperSurvivalSuit"),
					Main.GetModTechType("ReinforcedColdGloves")
				}
			};
		}
	}
#endif
}
