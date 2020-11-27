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

        /*internal static AcidSuit suitPrefab = new AcidSuit();
        internal static AcidGloves glovesPrefab = new AcidGloves();
        internal static AcidHelmet helmetPrefab = new AcidHelmet();
        // We could fit these into the prefabs list, but given that we want to access them frequently, we want them as publicly-accessible types anyway.
        internal static NitrogenBrineSuit2 nitroBrine2;
        internal static NitrogenBrineSuit3 nitroBrine3;*/
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

        public static TechType GetTechTypeInSlot_Patch(TechType __result, string slot)
        {
            Logger.Log(Logger.Level.Debug, $"GetTechTypeInSlot_Patch called with values for __result of {__result.ToString()} and slot {slot}");
            if (bNoPatchTechtypeInSlot || slot != "Body")
                return __result;

            if (__result == Module.AcidSuit.TechTypeID)
                return TechType.ReinforcedDiveSuit;
            else if (__result == Module.AcidGloves.TechTypeID)
                return TechType.ReinforcedGloves;
            else if (HasNitrogenMod())
            {
                TechType suitMk2 = GetNitrogenTechtype("reinforcedsuit2");
                TechType suitMk3 = GetNitrogenTechtype("reinforcedsuit3");
                if (suitMk2 == TechType.None)
                {
                    Logger.Log(Logger.Level.Debug, $"Could not find reinforcedsuit2 TechType");
                    return __result;
                }

                if (suitMk3 == TechType.None)
                {
                    Logger.Log(Logger.Level.Debug, $"Could not find reinforcedsuit3 TechType");
                    return __result;
                }

                if (__result == Module.NitrogenBrineSuit2.TechTypeID)
                {
                    return suitMk2;
                }
                else if (__result == Module.NitrogenBrineSuit2.TechTypeID)
                {
                    return suitMk3;
                }
            }

            return __result;
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
            List<Craftable> Prefabs = new List<Craftable>()
            {
                new AcidSuit(),
                new AcidGloves(),
                new AcidHelmet(),
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
                }
                else
                {
                    Logger.Log(Logger.Level.Debug, $"Load(): Could not find TechType for Nitrogen class ID {sTechType}");
                }
            }
            if (HasNitrogenMod())
            {
                Logger.Log(Logger.Level.Debug, $"Main.Load(): Found NitrogenMod, adding Nitrogen prefabs");
                //nitroBrine2 = new NitrogenBrineSuit2();
                //nitroBrine3 = new NitrogenBrineSuit3();
                //Prefabs.Add(nitroBrine2);
                //Prefabs.Add(nitroBrine3);
                Prefabs.Add(new NitrogenBrineSuit2());
                Prefabs.Add(new NitrogenBrineSuit3());
                Prefabs.Add(new Blueprint_BrineMk1toMk2());
                Prefabs.Add(new Blueprint_BrineMk2toMk3());
                Prefabs.Add(new Blueprint_BrineMk1toMk3());
                Prefabs.Add(new Blueprint_ReinforcedMk2toBrineMk2());
                Prefabs.Add(new Blueprint_ReinforcedMk3toBrineMk3());
                //Patches.Equipment_GetCount_Patch.Substitutions.Add(TechType.RadiationSuit, Main.nitroBrine2.TechType);
                //Patches.Equipment_GetCount_Patch.Substitutions.Add(TechType.RadiationSuit, Main.nitroBrine3.TechType);
            }

            foreach (Craftable c in Prefabs)
            {
                Logger.Log(Logger.Level.Debug, $"running Patch() for prefab {c.ToString()}");
                c.Patch();
            }

            Main.DamageResistances = new List<DamageResistance> {
            // Gloves
                new DamageResistance(
                    Module.AcidGloves.TechTypeID,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.15f)/*,
                        new DamageInfo(DamageType.Radiation, -0.10f)*/
                    }),


            // Helmet
                new DamageResistance(
                    Module.AcidHelmet.TechTypeID,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.25f)/*,
                        new DamageInfo(DamageType.Radiation, -0.20f)*/
                    }),


            // Suit
                new DamageResistance(
                    Module.AcidSuit.TechTypeID,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.6f)/*,
                        new DamageInfo(DamageType.Radiation, -0.70f)*/
                    })
            };

            if (HasNitrogenMod())
            {
                Main.DamageResistances.Add(new DamageResistance(
                    Module.NitrogenBrineSuit2.TechTypeID,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.6f)/*,
                        new DamageInfo(DamageType.Radiation, -0.70f)*/
                    }));
                Main.DamageResistances.Add(new DamageResistance(
                    Module.NitrogenBrineSuit3.TechTypeID,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.6f)/*,
                        new DamageInfo(DamageType.Radiation, -0.70f)*/
                    }));

                Patches.Equipment_GetCount_Patch.Substitutions.Add(new Patches.Equipment_GetCount_Patch.TechTypeSub(Module.NitrogenBrineSuit2.TechTypeID, TechType.RadiationSuit));
                Patches.Equipment_GetCount_Patch.Substitutions.Add(new Patches.Equipment_GetCount_Patch.TechTypeSub(Module.NitrogenBrineSuit3.TechTypeID, TechType.RadiationSuit));
            }
            Harmony.CreateAndPatchAll(myAssembly, $"DaWrecka_{myAssembly.GetName().Name}");
        }
    }
}
