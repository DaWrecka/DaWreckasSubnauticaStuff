using AcidProofSuit.Module;
using System.Reflection;
using System.Collections;
using HarmonyLib;
using QModManager.API.ModLoading;
using System.IO;
using System.Collections.Generic;
using RecipeData = SMLHelper.V2.Crafting.TechData;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Assets;
using Logger = QModManager.Utility.Logger;

namespace AcidProofSuit
{
    [QModCore]
    public static class Main
    {
        public static bool bInAcid = false; // Whether or not the player is currently immersed in acid
        public static List<string> playerSlots = new List<string>()
        {
            "Head",
            "Body",
            "Gloves",
            "Foots", // Seriously? 'Foots'?
            "Chip1",
            "Chip2",
            "Tank"
        };

        internal static AcidSuit prefabSuitMk1 = new AcidSuit();
        internal static AcidGloves prefabGloves = new AcidGloves();
        internal static AcidHelmet prefabHelmet = new AcidHelmet();
        // We could fit these into the prefabs list, but given that we want to access them frequently, we want them as publicly-accessible types anyway.
        internal static NitrogenBrineSuit2 prefabSuitMk2;
        internal static NitrogenBrineSuit3 prefabSuitMk3;

        private static Assembly myAssembly = Assembly.GetExecutingAssembly();
        private static string modPath = Path.GetDirectoryName(myAssembly.Location);
        internal static string AssetsFolder = Path.Combine(modPath, "Assets");

        public static bool bNoPatchTechtypeInSlot = false; // If true, skips any custom processing of GetTechTypeInSlot

        // Get total amount equipped from a list
        public static int EquipmentGetCount(Equipment e, TechType[] techTypes)
        {
            int count = 0;
            foreach (TechType tt in techTypes)
            {
                if (tt == TechType.None)
                    continue;

                count += e.GetCount(tt);
                Logger.Log(Logger.Level.Debug, $"EquipmentGetCount incremented return value to {count} for TechType {tt.ToString()}");
            }
            return count;
        }

        private static Dictionary<string, TechType> NitrogenTechtypes = new Dictionary<string, TechType>();

        public static TechType GetNitrogenTechtype(string name)
        {
            TechType tt;
            if (NitrogenTechtypes.TryGetValue(name, out tt))
                return tt;

            if (SMLHelper.V2.Handlers.TechTypeHandler.TryGetModdedTechType(name, out tt))
                return tt;

            return TechType.None;
        }

        public static bool HasNitrogenMod()
        {
            return (NitrogenTechtypes.Count > 0);
        }

        public static TechType GetTechTypeInSlot_Patch(TechType input, string slot)
        {
            if (bNoPatchTechtypeInSlot)
            {
                Logger.Log(Logger.Level.Debug, $"GetTechTypeInSlot_Patch skipped because bNoPatchTechtypeInSlot, returning result {input.ToString()}");
                return input;
            }
            else if (slot != "Body")
            {
                Logger.Log(Logger.Level.Debug, $"GetTechTypeInSlot_Patch skipped because slot != Body");
                return input;
            }
            Logger.Log(Logger.Level.Debug, $"GetTechTypeInSlot_Patch called with values for input of {input.ToString()} and slot {slot}");

            if (input == prefabSuitMk1.TechType)
                return TechType.ReinforcedDiveSuit;
            else if (input == prefabGloves.TechType)
                return TechType.ReinforcedGloves;
            else if (HasNitrogenMod())
            {
                TechType suitMk2 = GetNitrogenTechtype("reinforcedsuit2");
                TechType suitMk3 = GetNitrogenTechtype("reinforcedsuit3");
                Logger.Log(Logger.Level.Debug, $"Got values for TechTypes reinforcedsuit2 and reinforcedsuit3 of {suitMk2.ToString()} and {suitMk3.ToString()}");
                if (suitMk2 == TechType.None)
                {
                    Logger.Log(Logger.Level.Debug, $"Could not find reinforcedsuit2 TechType");
                    return input;
                }

                if (suitMk3 == TechType.None)
                {
                    Logger.Log(Logger.Level.Debug, $"Could not find reinforcedsuit3 TechType");
                    return input;
                }

                if (Main.prefabSuitMk2 != null && input == Main.prefabSuitMk2.TechType)
                {
                    return suitMk2;
                }
                else if (Main.prefabSuitMk3 != null && input == Main.prefabSuitMk2.TechType)
                {
                    return suitMk3;
                }
            }

            return input;
        }


        // This function was stol*cough*take*cough*nicked wholesale from FCStudios
        public static object GetPrivateField<T>(this T instance, string fieldName, BindingFlags bindingFlags = BindingFlags.Default)
        {
            return typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | bindingFlags).GetValue(instance);
        }

        internal struct DamageInfo
        {
            public DamageType damageType;
            public float damageMult;

            public DamageInfo(DamageType t, float m)
            {
                this.damageType = t;
                this.damageMult = m;
            }
        }

        internal struct DamageResistance
        {
            public TechType TechType;
            public DamageInfo[] damageInfoList;

            public DamageResistance(TechType tt, DamageInfo[] dil)
            {
                this.TechType = tt;
                this.damageInfoList = dil;
            }
        }

        // This particular system is not that useful, but it could be expanded to allow any sort of equipment type to reduce damage.
        // For example, you could add a chip that projects a sort of shield that protects from environmental damage, such as Acid, Radiation, Heat, Poison, or others.
        // Although the system would need to be extended to allow, say, a shield that drains a battery when resisting damage.
        internal static List<DamageResistance> DamageResistances;
        public static float ModifyDamage(TechType tt, float damage, DamageType type)
        {
            float baseDamage = damage;
            float damageMod = 0;
            Logger.Log(Logger.Level.Debug, $"Main.ModifyDamage called: tt = {tt.ToString()}, damage = {damage}; DamageType = {type}");
            foreach (DamageResistance r in DamageResistances)
            {
                Logger.Log(Logger.Level.Debug, $"Found DamageResistance with TechType: {r.TechType.ToString()}");
                if (r.TechType == tt)
                {
                    foreach (DamageInfo d in r.damageInfoList)
                    {
                        if (d.damageType == type)
                        {
                            damageMod += baseDamage * d.damageMult;
                            Logger.Log(Logger.Level.Debug, $"Player has equipped armour of TechType {tt.ToString()}, base damage = {baseDamage}, type = {type}, modifying damage by {d.damageMult}x with result of {damageMod}");
                        }
                    }
                }
            }
            return damageMod;
        }

        [QModPatch]
        public static void Load()
        {
            bool bHasN2 = HasNitrogenMod();


            List<Craftable> Prefabs = new List<Craftable>()
            {
                prefabGloves,
                prefabHelmet,
                prefabSuitMk1,
                new Blueprint_OnlyRadSuit(),
                new Blueprint_OnlyRebreather(),
                new Blueprint_OnlyReinforcedSuit(),
                new Blueprint_Suits(),
                new Blueprint_RebreatherRad(),
                new Blueprint_RebreatherReinforced(),
                new Blueprint_RadReinforced()
            };

            // There doesn't appear to be any handler function for verifying whether a certain tab node already exists. This is relevant since I'm deliberately using a node with the same
            // name as another mod, More Modified Items, so that the non-Nitrogen suit upgrades appear in the same menu as the Reinforced Stillsuit.
            SMLHelper.V2.Handlers.CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "BodyMenu", "Suit Upgrades", SpriteManager.Get(TechType.Stillsuit));

            /*glovesPrefab.Patch();
            helmetPrefab.Patch();
            suitPrefab.Patch();*/
            /*bpSuits.Patch();
            bpOnlyRadSuit.Patch();
            bpOnlyRebreather.Patch();
            bpOnlyReinforced.Patch();
            bpRebreatherRad.Patch();
            bpRebReinf.Patch();
            bpRadReinf.Patch();*/
            foreach (string sTechType in new List<string> { "reinforcedsuit2", "reinforcedsuit3", "rivereelscale", "lavalizardscale", "thermophilesample" } )
            {
                if (SMLHelper.V2.Handlers.TechTypeHandler.TryGetModdedTechType(sTechType, out TechType tt))
                {
                    NitrogenTechtypes.Add(sTechType, tt);
                    bHasN2 = true;
                }
                else
                {
                    Logger.Log(Logger.Level.Debug, $"Load(): Could not find TechType for Nitrogen class ID {sTechType}");
                }
            }
            if (bHasN2)
            {
                Logger.Log(Logger.Level.Debug, $"Main.Load(): Found NitrogenMod, adding Nitrogen prefabs");
                prefabSuitMk2 = new NitrogenBrineSuit2();
                prefabSuitMk3 = new NitrogenBrineSuit3();
                Prefabs.Add(prefabSuitMk2);
                Prefabs.Add(prefabSuitMk3);
                Prefabs.Add(new Blueprint_BrineMk1toMk2());
                Prefabs.Add(new Blueprint_BrineMk2toMk3());
                Prefabs.Add(new Blueprint_BrineMk1toMk3());
                Prefabs.Add(new Blueprint_ReinforcedMk2toBrineMk2());
                Prefabs.Add(new Blueprint_ReinforcedMk3toBrineMk3());
            }

            foreach (Craftable c in Prefabs)
            {
                Logger.Log(Logger.Level.Debug, $"running Patch() for prefab {c.ToString()}");
                c.Patch();
            }

            /*prefabGloves.TechType = cAcidGloves.TechType;
            prefabHelmet.TechType = cAcidHelmet.TechType;
            prefabSuitMk1.TechType = cSuitMk1.TechType;*/
            Patches.Equipment_GetCount_Patch.Substitutions = new List<Patches.Equipment_GetCount_Patch.TechTypeSub>
            {
                new Patches.Equipment_GetCount_Patch.TechTypeSub( prefabHelmet.TechType,TechType.Rebreather),
                new Patches.Equipment_GetCount_Patch.TechTypeSub( prefabHelmet.TechType, TechType.RadiationHelmet),
                new Patches.Equipment_GetCount_Patch.TechTypeSub( prefabSuitMk1.TechType, TechType.RadiationSuit ),
                new Patches.Equipment_GetCount_Patch.TechTypeSub( prefabGloves.TechType, TechType.ReinforcedGloves )
            };

            Main.DamageResistances = new List<DamageResistance> {
            // Gloves
                new DamageResistance(
                    prefabGloves.TechType,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.15f)/*,
                        new DamageInfo(DamageType.Radiation, -0.10f)*/
                    }),


            // Helmet
                new DamageResistance(
                    prefabHelmet.TechType,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.25f)/*,
                        new DamageInfo(DamageType.Radiation, -0.20f)*/
                    }),


            // Suit
                new DamageResistance(
                    prefabSuitMk1.TechType,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.6f)/*,
                        new DamageInfo(DamageType.Radiation, -0.70f)*/
                    })
            };

            if (bHasN2)
            {
                if (prefabSuitMk2.TechType != TechType.None)
                {
                    //prefabSuitMk2.TechType = cSuitMk2.TechType;
                    Main.DamageResistances.Add(new DamageResistance(
                        //cSuitMk2.TechType,
                        prefabSuitMk2.TechType,
                        new DamageInfo[] {
                            new DamageInfo(DamageType.Acid, -0.6f)/*,
                            new DamageInfo(DamageType.Radiation, -0.70f)*/
                        }));

                    Patches.Equipment_GetCount_Patch.Substitutions.Add(new Patches.Equipment_GetCount_Patch.TechTypeSub(prefabSuitMk2.TechType, TechType.RadiationSuit));
                }
                else
                    Logger.Log(Logger.Level.Error, $"NitrogenBrinesuit2 techtype could not be found");

                if (prefabSuitMk3.TechType != TechType.None)
                {
                    //prefabSuitMk3.TechType = cSuitMk3.TechType;
                    Patches.Equipment_GetCount_Patch.Substitutions.Add(new Patches.Equipment_GetCount_Patch.TechTypeSub(prefabSuitMk3.TechType, TechType.RadiationSuit));
                    Main.DamageResistances.Add(new DamageResistance(
                        prefabSuitMk3.TechType,
                        new DamageInfo[] {
                            new DamageInfo(DamageType.Acid, -0.6f)/*,
                            new DamageInfo(DamageType.Radiation, -0.70f)*/
                        }));
                }
                else
                    Logger.Log(Logger.Level.Error, $"NitrogenBrinesuit3 techtype could not be found");
            }
            Harmony.CreateAndPatchAll(myAssembly, $"DaWrecka_{myAssembly.GetName().Name}");
        }
    }
}
