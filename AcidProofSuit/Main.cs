using AcidProofSuit.Module;
using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Logger = QModManager.Utility.Logger;

namespace AcidProofSuit
{
    [QModCore]
    public static class Main
    {
        public const string version = "1.2.0.5";
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

        private static readonly Assembly myAssembly = Assembly.GetExecutingAssembly();
        private static readonly string modPath = Path.GetDirectoryName(myAssembly.Location);
        internal static readonly string AssetsFolder = Path.Combine(modPath, "Assets");

        private static readonly Type NitrogenMain = Type.GetType("NitrogenMod.Main, NitrogenMod", false, false);
        private static readonly MethodInfo NitroAddDiveSuit = NitrogenMain?.GetMethod("AddDiveSuit", BindingFlags.Public | BindingFlags.Static);
        internal static readonly Texture2D glovesTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidGlovesskin.png"));
        internal static readonly Texture2D suitTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidSuitskin.png"));
        internal static readonly Texture2D glovesIllumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidGlovesillum.png"));
        internal static readonly Texture2D suitIllumTexture = ImageUtils.LoadTextureFromFile(Path.Combine(Main.AssetsFolder, "AcidSuitillum.png"));

        public static bool bUseNitrogenAPI; // If true, use the Nitrogen API instead of patching GetTechTypeInSlot. Overrides bNoPatchTechTypeInSlot.

        // Get total amount equipped from a list
        public static int EquipmentGetCount(Equipment e, TechType[] techTypes)
        {
            int count = 0;
            foreach (TechType tt in techTypes)
            {
                if (tt == TechType.None)
                    continue;

                count += e.GetCount(tt);
                //Logger.Log(Logger.Level.Debug, $"EquipmentGetCount incremented return value to {count} for TechType {tt.ToString()}");
            }
            return count;
        }

#pragma warning disable IDE0044 // Add readonly modifier
        private static Dictionary<string, TechType> NitrogenTechtypes = new Dictionary<string, TechType>();
#pragma warning restore IDE0044 // Add readonly modifier

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
            return QModServices.Main.ModPresent("NitrogenMod");
        }

        // This function was stol*cough*take*cough*nicked wholesale from FCStudios
        public static object GetPrivateInstanceField<T>(this T instance, string fieldName, BindingFlags bindingFlags = BindingFlags.Default)
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
        //Interfaces would be the way I think, but I've not yet wrapped my brain around that.
        internal static List<DamageResistance> DamageResistances;
        public static float ModifyDamage(TechType tt, float damage, DamageType type)
        {
            float baseDamage = damage;
            float damageMod = 0f;
            //Logger.Log(Logger.Level.Debug, $"Main.ModifyDamage called: tt = {tt.ToString()}, damage = {damage}; DamageType = {type}");
            foreach (DamageResistance r in DamageResistances)
            {
                //Logger.Log(Logger.Level.Debug, $"Found DamageResistance with TechType: {r.TechType.ToString()}");
                if (r.TechType == tt)
                {
                    foreach (DamageInfo d in r.damageInfoList)
                    {
                        if (d.damageType == type)
                        {
                            damageMod += baseDamage * d.damageMult;
                            //Logger.Log(Logger.Level.Debug, $"Player has equipped armour of TechType {tt.ToString()}, base damage = {baseDamage}, type = {type}, modifying damage by {d.damageMult}x with result of {damageMod}");
                        }
                    }
                }
            }
            return damageMod;
        }

        [QModPatch]
        public static void Load()
        {
#if !RELEASE
            Logger.Log(Logger.Level.Debug, "Checking for Nitrogen mod"); 
#endif
            bool bHasN2 = QModServices.Main.ModPresent("NitrogenMod");
            string sStatus = "Nitrogen mod " + (bHasN2 ? "" : "not ") + "present";
#if !RELEASE
            Logger.Log(Logger.Level.Debug, sStatus); 
#endif

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

            if (bHasN2)
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Main.Load(): Found NitrogenMod, adding Nitrogen prefabs"); 
#endif
                foreach (string sTechType in new List<string> { "reinforcedsuit2", "reinforcedsuit3", "rivereelscale", "lavalizardscale" })
                {
                    if (SMLHelper.V2.Handlers.TechTypeHandler.TryGetModdedTechType(sTechType, out TechType tt))
                    {
                        NitrogenTechtypes.Add(sTechType, tt);
                        bHasN2 = true;
                    }
                    else
                    {
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Load(): Could not find TechType for Nitrogen class ID {sTechType}"); 
#endif
                    }
                }
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
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"running Patch() for prefab {c.ToString()}"); 
#endif
                c.Patch();
            }
#if !RELEASE

            Logger.Log(Logger.Level.Debug, $"Adding basic EquipmentGetCount substitutions"); 
#endif
            Patches.Equipment_GetCount_Patch.AddSubstitution(prefabHelmet.TechType, TechType.Rebreather);
            Patches.Equipment_GetCount_Patch.AddSubstitution(prefabHelmet.TechType, TechType.RadiationHelmet);
            Patches.Equipment_GetCount_Patch.AddSubstitution(prefabSuitMk1.TechType, TechType.RadiationSuit);
            Patches.Equipment_GetCount_Patch.AddSubstitution(prefabGloves.TechType, TechType.RadiationGloves);
            Patches.Equipment_GetCount_Patch.AddSubstitution(prefabGloves.TechType, TechType.ReinforcedGloves);
            Patches.Equipment_GetCount_Patch.AddSubstitution(prefabSuitMk1.TechType, TechType.ReinforcedDiveSuit);
#if !RELEASE

            Logger.Log(Logger.Level.Debug, $"Setting up DamageResistances list"); 
#endif
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
            Harmony.CreateAndPatchAll(myAssembly, $"DaWrecka_{myAssembly.GetName().Name}");
        }

        [QModPostPatch]
        public static void PostPatch()
        {
            if (HasNitrogenMod())
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Setting up Nitrogen suit TechTypes"); 
#endif
                if (prefabSuitMk2.TechType != TechType.None)
                {
                    Main.DamageResistances.Add(new DamageResistance(
                        prefabSuitMk2.TechType,
                        new DamageInfo[] {
                            new DamageInfo(DamageType.Acid, -0.6f)/*,
                            new DamageInfo(DamageType.Radiation, -0.70f)*/
                        }));

                    Patches.Equipment_GetCount_Patch.AddSubstitution(prefabSuitMk2.TechType, TechType.RadiationSuit);
                    Patches.Equipment_GetCount_Patch.AddSubstitution(prefabSuitMk2.TechType, TechType.ReinforcedDiveSuit);
                }
                else
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Error, $"NitrogenBrinesuit2 techtype could not be found"); 
#endif
                }

                if (prefabSuitMk3.TechType != TechType.None)
                {
                    Main.DamageResistances.Add(new DamageResistance(
                        prefabSuitMk3.TechType,
                        new DamageInfo[] {
                            new DamageInfo(DamageType.Acid, -0.6f)/*,
                            new DamageInfo(DamageType.Radiation, -0.70f)*/
                        }));
                    Patches.Equipment_GetCount_Patch.AddSubstitution(prefabSuitMk3.TechType, TechType.RadiationSuit);
                    Patches.Equipment_GetCount_Patch.AddSubstitution(prefabSuitMk3.TechType, TechType.ReinforcedDiveSuit);
                }
                else
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Error, $"NitrogenBrinesuit3 techtype could not be found"); 
#endif
                }
                if (NitroAddDiveSuit != null)
                {
#if !RELEASE
                    Logger.Log(Logger.Level.Debug, $"Found Nitrogen API, adding dive suits."); 
#endif
                    NitroAddDiveSuit.Invoke(null, new object[] { prefabSuitMk1.TechType, 800f, 0.85f, 15f });
                    NitroAddDiveSuit.Invoke(null, new object[] { prefabSuitMk2.TechType, 1300f, 0.75f, 20f });
                    NitroAddDiveSuit.Invoke(null, new object[] { prefabSuitMk3.TechType, 8000f, 0.55f, 35f });
                }
            }
        }
    }
}
