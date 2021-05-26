using SMLHelper.V2.Crafting;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Logger = QModManager.Utility.Logger;

namespace IngredientsFromScanning_BZ.Configuration
{
	// The code should fail safe, assuming a weight of 1.0 if the searched techType is not present in the array.
	// This would also mean the array only needs to contain those entries which should have a different weight;
	// In other words, only those which should never be used, or should be used less-often.

	/*[Menu("Ingredients from Fragments", IgnoreUnattributedMembers = true, SaveOn = CreateAssetMenuAttribute.SaveEvents.ChangeValue)]*/
	[Menu("Ingredients from Scanning")]
	public class DWConfig : ConfigFile
	{
		private const int MAX_PRIZE = 10;
		private const int MIN_PRIZE = 0;
		[Slider("Minimum ingredients", MIN_PRIZE, MAX_PRIZE, DefaultValue = 2, Id = "MinComponents",
			Tooltip = "Scanning a known item will always return this number of items, or the total number of items in the recipe, whichever is lower."), OnChange(nameof(OnSliderChange))]
		public int minComponents = 2; // Minimum number of ingredients received from scanning an existing fragment

		[Slider("Maximum ingredients", MIN_PRIZE, MAX_PRIZE, DefaultValue = 2, Id = "MaxComponents",
			Tooltip = "Scanning a known item will return no more than this many items, regardless of the length of the recipe."), OnChange(nameof(OnSliderChange))]
		public int maxComponents = 2; // Maximum number

		[Toggle("Show all fragments in Map Room")]
		private bool bOverrideMapRoom = true;

		private void OnSliderChange(SliderChangedEventArgs e)
		{
			if (e.Id == "MinComponents")
			{
				if (e.IntegerValue > maxComponents)
					maxComponents = e.IntegerValue;
			}
			else if (e.Id == "MaxComponents") // As of the time of writing this comment, there are only two sliders, so a simple 'else' would suffice.
											  // I'm hedging my bets in case of adding other sliders in the future, though.
			{
				if (e.IntegerValue < minComponents)
					minComponents = e.IntegerValue;
			}
		}

		private int Clamp(int i, int min, int max)
		{
			if (min > max)
				min = max;
			return Math.Min(Math.Max(i, min), max);
		}

		// Generate a random number between minComponents and maxComponents, both inclusive.
		public int GenerateGiftValue()
		{
			System.Random rng = new System.Random();
			// Sanity-check the values; the UI won't let them be set outside a 1-10 inclusive range, but they might be set that way in the JSON by a cheeky user.
			maxComponents = Clamp(maxComponents, MIN_PRIZE, MAX_PRIZE);
			minComponents = Clamp(minComponents, MIN_PRIZE, maxComponents);
#if !RELEASE
			Logger.Log(Logger.Level.Debug, $"Generating prize value between {minComponents} and {maxComponents}"); 
#endif
			// maxComponents is intended to be inclusive, but Random.Next(1, X) will always return an integer less than X. So we need to add 1 to maxComponents here to get the results we want.
			return rng.Next(minComponents, maxComponents + 1);
		}

		public Dictionary<string, float> TechWeights;
		//private Dictionary<string, float> defaultTechWeights

		public struct TechWeight
		{
			private readonly string weightedTechType;
			private readonly float weight;

			public TechWeight(string type, float weight)
			{
				this.weightedTechType = type;
				this.weight = weight;
			}

			public float GetWeightForType(TechType targetTechType)
			{
				if (targetTechType.ToString() == this.weightedTechType)
				{
					return this.weight;
				}
				else
				{
					return 1.0f;
				}
			}
		}

		//
		// Summary:
		//     A class for representing a required ingredient in a recipe using a string instead of a TechType.
		public class StringIngredient
		{
			public StringIngredient(string techType, int amount)
			{
				this.amount = amount;
				this.techType = techType;
			}

			public string techType { get; }
			public int amount { get; }
		}

		public struct RecipeOverride
		{
			public string overridden;
			public List<StringIngredient> replacements;

			public RecipeOverride(string overridenRecipe, List<StringIngredient> replacements)
			{
				this.overridden = overridenRecipe;
				this.replacements = replacements;
			}
		}

		public List<RecipeOverride> RecipeOverrides;
		//private List<RecipeOverride> defaultRecipeOverrides

		public bool TryOverrideRecipe(TechType fragment, out RecipeData outRecipe)
		{
			string sFragment = fragment.ToString();
			foreach (RecipeOverride r in RecipeOverrides)
			{
				if (sFragment == r.overridden)
				{
					outRecipe = new RecipeData();
					foreach (StringIngredient s in r.replacements)
					{
						if (Enum.TryParse<TechType>(s.techType, out TechType t))
						{
							outRecipe.Ingredients.Add(new Ingredient(t, s.amount));
						}
						else
						{
#if !RELEASE
							Logger.Log(Logger.Level.Error, $"Could not parse {s.techType} as TechType; check the spelling and/or case"); 
#endif
							outRecipe = null;
							return false;
						}
					}
					return true;
				}
			}
			outRecipe = null;
			return false;
		}

		//This would work fine as a private struct, as it's not referenced outside this class, but VS2019 won't compile it. *shrug*
		public struct SSubstitutionEntry
		{
			public string replacedTech;
			public List<StringIngredient> replacements;

			public SSubstitutionEntry(string replaced, List<StringIngredient> replacements)
			{
				this.replacedTech = replaced;
				this.replacements = replacements;
			}
		};

		public List<SSubstitutionEntry> SubstitutionList;
		//private List<SSubstitutionEntry> defaultSubstitutionList

		public float GetWeightForTechType(TechType tech)
		{
			// float weight;
			if (TechWeights.TryGetValue(tech.ToString(), out float weight))
			{
				return weight;
			}
			else
			{
				return 1.0f;
			}
		}

		public bool TrySubstituteIngredient(TechType tech, out List<Ingredient> Substitutes)
		{
			Substitutes = new List<Ingredient>();
			for (int i = 0; i < SubstitutionList.Count; i++)
			{
				if (tech.ToString() == SubstitutionList[i].replacedTech)
				{
					//for (int j = 0; j < SubstitutionList[i].replacements.Count; j++)
					foreach (StringIngredient si in SubstitutionList[i].replacements)
					{
						if (Enum.TryParse<TechType>(si.techType, out TechType tt))
						{
							Substitutes.Add(new Ingredient(tt, si.amount));
						}
						else
						{
#if !RELEASE
							Logger.Log(Logger.Level.Error, $"Failed to parse string '{si.techType}' as TechType; check to make sure the entry is spelled correctly, and using the correct case"); 
#endif
						}
					}
					//Substitutes = SubstitutionList[i].replacements;
					return (Substitutes.Count > 0); // It's possible every single entry in the list failed to parse; we don't want to return true in that instance. But this condition is an easy fix.
				}
			}

			// We only get this far if the tech to be replaced doesn't match this object's replacedTech.
			Substitutes = null;
			return false;
		}

		public void Init()
		{
			// Using the | operator instead of || because we want all three functions to run regardless
			if (InitWeights() | InitRecipeOverrides() | InitSubstitutions())
			{
#if !RELEASE
				Logger.Log(Logger.Level.Info, "Some configuration settings have been reset to defaults."); 
#endif
				Save();
			}
			else
			{
#if !RELEASE
				Logger.Log(Logger.Level.Debug, "All values present and correct"); 
#endif
			}
		}

		public bool InitWeights()
		{
			if (TechWeights == null)
			{
#if !RELEASE                
				Logger.Log(Logger.Level.Warn, "No TechWeights found, setting default values"); 
#endif
				TechWeights = new Dictionary<string, float>() {
					{ "None", 0f },
					{ "CalciteOld", 0f },
					{ "DolomiteOld", 0f },
					{ "FlintOld", 0f },
					{ "EmeryOld", 0f },
					{ "MercuryOre", 0f },
					{ "Placeholder", 0f },
					{ "CarbonOld", 0f },
					{ "EthanolOld", 0f },
					{ "EthyleneOld", 0f },
					{ "Magnesium", 0f },
					{ "HydrogenOld", 0f },
					{ "Lodestone", 0f },
					{ "SandLoot", 0f },
					{ "Battery", 0.5f },
					{ "BatteryAcidOld", 0f },
					{ "TitaniumIngot", 0.5f },
					{ "AdvancedWiringKit", 0.5f },
					{ "PlasteelIngot", 0.5f },
					{ "EnameledGlass", 0.5f },
					{ "Enamel", 0f },
					{ "AcidOld", 0f },
					{ "VesselOld", 0f },
					{ "CombustibleOld", 0f },
					{ "OpalGem", 0f },
					{ "Uranium", 0f },
					{ "HydrochloricAcid", 0.5f },
					{ "AminoAcids", 0f },
					{ "Polyaniline", 0.5f },
					{ "Graphene", 0f },
					{ "Nanowires", 0f },
					{ "Lubricant", 0.5f },
					{ "ReactorRod", 0f },
					{ "DepletedReactorRod", 0f },
					{ "PrecursorIonCrystalMatrix", 0f },
					{ "DiveSuit", 0f },
					{ "Fins", 0f },
					{ "Tank", 0f },
					{ "Knife", 0f },
					{ "Drill", 0f },
					{ "Flashlight", 0f },
					{ "Beacon", 0f },
					{ "Builder", 0f },
					{ "EscapePod", 0f },
					{ "Compass", 0f },
					{ "AirBladder", 0f },
					{ "Terraformer", 0f },
					{ "Pipe", 0f },
					{ "Thermometer", 0f },
					{ "DiveReel", 0f },
					{ "Rebreather", 0f },
					{ "RadiationSuit", 0f },
					{ "RadiationHelmet", 0f },
					{ "RadiationGloves", 0f },
					{ "ReinforcedDiveSuit", 0f },
					{ "Scanner", 0f },
					{ "FireExtinguisher", 0f },
					{ "MapRoomHUDChip", 0f },
					{ "PipeSurfaceFloater", 0f },
					{ "CyclopsDecoy", 0.5f },
					{ "ReinforcedGloves", 0f },
					{ "Constructor", 0f },
					{ "Transfuser", 0f },
					{ "StasisRifle", 0f },
					{ "BuildBot", 0f },
					{ "PlasteelTank", 0f },
					{ "HighCapacityTank", 0f },
					{ "UltraGlideFins", 0f },
					{ "SwimChargeFins", 0f },
					{ "Stillsuit", 0f },
					{ "CompostCreepvine", 0f },
					{ "ProcessUranium", 0f },
					{ "SafeShallowsEgg", 0f },
					{ "KelpForestEgg", 0f },
					{ "GrassyPlateausEgg", 0f },
					{ "GrandReefsEgg", 0f },
					{ "MushroomForestEgg", 0f },
					{ "KooshZoneEgg", 0f },
					{ "TwistyBridgesEgg", 0f },
					{ "LavaZoneEgg", 0f },
					{ "StalkerEgg", 0f },
					{ "ReefbackEgg", 0f },
					{ "SpadefishEgg", 0f },
					{ "RabbitrayEgg", 0f },
					{ "MesmerEgg", 0f },
					{ "JumperEgg", 0f },
					{ "SandsharkEgg", 0f },
					{ "JellyrayEgg", 0f },
					{ "BonesharkEgg", 0f },
					{ "CrabsnakeEgg", 0f },
					{ "ShockerEgg", 0f },
					{ "GasopodEgg", 0f },
					{ "RabbitrayEggUndiscovered", 0f },
					{ "JellyrayEggUndiscovered", 0f },
					{ "StalkerEggUndiscovered", 0f },
					{ "ReefbackEggUndiscovered", 0f },
					{ "JumperEggUndiscovered", 0f },
					{ "BonesharkEggUndiscovered", 0f },
					{ "GasopodEggUndiscovered", 0f },
					{ "MesmerEggUndiscovered", 0f },
					{ "SandsharkEggUndiscovered", 0f },
					{ "ShockerEggUndiscovered", 0f },
					{ "GenericEgg", 0f },
					{ "CrashEgg", 0f },
					{ "CrashEggUndiscovered", 0f },
					{ "CrabsquidEgg", 0f },
					{ "CrabsquidEggUndiscovered", 0f },
					{ "CutefishEgg", 0f },
					{ "CutefishEggUndiscovered", 0f },
					{ "LavaLizardEgg", 0f },
					{ "LavaLizardEggUndiscovered", 0f },
					{ "CrabsnakeEggUndiscovered", 0f },
					{ "SpadefishEggUndiscovered", 0f },
					{ "HullReinforcementModule", 0f },
					{ "HullReinforcementModule2", 0f },
					{ "HullReinforcementModule3", 0f },
					{ "HatchingEnzymes", 0f },
					{ "CyclopsShieldModule", 0f },
					{ "CyclopsSonarModule", 0f },
					{ "CyclopsSeamothRepairModule", 0f },
					{ "CyclopsDecoyModule", 0f },
					{ "CyclopsFireSuppressionModule", 0f },
					{ "CyclopsFabricator", 0f },
					{ "CyclopsThermalReactorModule", 0f },
					{ "CyclopsHullModule3", 0f },
					{ "SeamothReinforcementModule", 0f },
					{ "VehiclePowerUpgradeModule", 0f },
					{ "SeamothSolarCharge", 0f },
					{ "VehicleStorageModule", 0f },
					{ "SeamothElectricalDefense", 0f },
					{ "VehicleArmorPlating", 0f },
					{ "SeamothTorpedoModule", 0f },
					{ "SeamothSonarModule", 0f },
					{ "WhirlpoolTorpedo", 0f },
					{ "VehicleHullModule1", 0f },
					{ "VehicleHullModule2", 0f },
					{ "VehicleHullModule3", 0f },
					{ "ExosuitJetUpgradeModule", 0f },
					{ "ExosuitDrillArmModule", 0f },
					{ "ExosuitThermalReactorModule", 0f },
					{ "ExosuitClawArmModule", 0f },
					{ "GasTorpedo", 0f },
					{ "ExosuitPropulsionArmModule", 0f },
					{ "ExosuitGrapplingArmModule", 0f },
					{ "ExosuitTorpedoArmModule", 0f },
					{ "ExoHullModule1", 0f },
					{ "ExoHullModule2", 0f },
					{ "MapRoomUpgradeScanRange", 0f },
					{ "MapRoomUpgradeScanSpeed", 0f },
					{ "Creepvine", 0f },
					{ "CreepvineSeedCluster", 0f },
					{ "CreepvinePiece", 0f },
					{ "GasPod", 0f },
					{ "Kyanite", 0.5f },
					{ "PrecursorIonCrystal", 0f }
				};
				return true;
			}
			return false;
		}

		public bool InitRecipeOverrides()
		{
			if (RecipeOverrides == null)
			{
#if !RELEASE
				Logger.Log(Logger.Level.Warn, "No RecipeOverrides found, setting default values"); 
#endif
				RecipeOverrides = new List<RecipeOverride>() {
					new RecipeOverride(
						"CyclopsHullFragment", new List<StringIngredient>()
						{
							new StringIngredient("PlasteelIngot", 3),
							new StringIngredient("Lead", 2)
						}
					),
					new RecipeOverride(
						"CyclopsBridgeFragment", new List<StringIngredient>()
						{
							new StringIngredient("EnameledGlass", 3),
							new StringIngredient("Lead", 1),
							new StringIngredient("PlasteelIngot", 1)
						}
					),
					new RecipeOverride(
						"CyclopsEngineFragment", new List<StringIngredient>()
						{
							new StringIngredient("Lubricant", 1),
							new StringIngredient("AdvancedWiringKit", 1),
							new StringIngredient("Lead", 2),
							new StringIngredient("PlasteelIngot", 1)
						}
					),
					new RecipeOverride(
						"CyclopsDockingBayFragment", new List<StringIngredient>()
						{
							new StringIngredient("PlasteelIngot", 3),
							new StringIngredient("Lead", 1)
						}
					),
					new RecipeOverride(
						"ThermalPlantFragment", new List<StringIngredient>()
						{
							new StringIngredient("Aerogel", 1),
							new StringIngredient("Titanium", 5),
							new StringIngredient("Magnetite", 1)
						}
					),
					new RecipeOverride(
						"BaseMapRoomFragment", new List<StringIngredient>()
						{
							new StringIngredient("Titanium", 5),
							new StringIngredient("Copper", 2),
							new StringIngredient("Gold", 1),
							new StringIngredient("JeweledDiskPiece", 1)
						}
					)
				};
				return true;
			}
			return false;
		}

		public bool InitSubstitutions()
		{
			if (SubstitutionList == null)
			{
#if !RELEASE
				Logger.Log(Logger.Level.Warn, "No SubstitutionList found, setting default values"); 
#endif
				SubstitutionList = new List<SSubstitutionEntry>() {
					new SSubstitutionEntry(
						"AdvancedWiringKit",
						new List<StringIngredient>{
							new StringIngredient("Silver", 2),
							new StringIngredient("ComputerChip", 1)
						}
					),
					new SSubstitutionEntry(
						"PlasteelIngot",
						new List<StringIngredient>{
							new StringIngredient("ScrapMetal", 2),
							new StringIngredient("Lithium", 2)
						}
					),
					new SSubstitutionEntry(
						"TitaniumIngot",
						new List<StringIngredient>{
							new StringIngredient("ScrapMetal", 2)
						}
					),
					new SSubstitutionEntry(
						"Battery",
						new List<StringIngredient>{
							new StringIngredient("Copper", 1)
						}
					),
					new SSubstitutionEntry(
						"PowerCell",
						new List<StringIngredient>{
							new StringIngredient("Copper", 2)
						}
					)
				};
				return true;
			}
			return false;
		}
	}
}
