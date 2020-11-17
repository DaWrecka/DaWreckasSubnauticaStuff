using AcidProofSuit.Module;
using System.Reflection;
using HarmonyLib;
using QModManager.API.ModLoading;

namespace AcidProofSuit
{
    [QModCore]
    public static class Main
    {
        public static bool bInAcid = false; // Whether or not the player is currently immersed in acid

        internal static AcidSuitPrefab suitPrefab = new AcidSuitPrefab();
        internal static AcidGlovesPrefab glovesPrefab = new AcidGlovesPrefab();
        internal static AcidHelmetPrefab helmetPrefab = new AcidHelmetPrefab();
        internal static bpSupplemental_OnlyRadSuit bpOnlyRadSuit = new bpSupplemental_OnlyRadSuit();
        internal static bpSupplemental_OnlyRebreather bpOnlyRebreather = new bpSupplemental_OnlyRebreather();
        internal static bpSupplemental_OnlyReinforcedSuit bpOnlyReinforced = new bpSupplemental_OnlyReinforcedSuit();
        internal static bpSupplemental_Suits bpSuits = new bpSupplemental_Suits();
        internal static bpSupplemental_RebreatherRad bpRebreatherRad = new bpSupplemental_RebreatherRad();
        internal static bpSupplemental_RebreatherReinforced bpRebReinf = new bpSupplemental_RebreatherReinforced();
        internal static bpSupplemental_RadReinforced bpRadReinf = new bpSupplemental_RadReinforced();

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
        internal static DamageResistance[] DamageResistances;
        public static float ModifyDamage(TechType tt, float damage, DamageType type)
        {
            float baseDamage = damage;
            float damageMod = 0;
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
            SMLHelper.V2.Handlers.CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, "BodyMenu", "Suit Upgrades", SpriteManager.Get(TechType.Stillsuit));

            glovesPrefab.Patch();
            helmetPrefab.Patch();
            suitPrefab.Patch();
            bpSuits.Patch();
            bpOnlyRadSuit.Patch();
            bpOnlyRebreather.Patch();
            bpOnlyReinforced.Patch();
            bpRebreatherRad.Patch();
            bpRebReinf.Patch();
            bpRadReinf.Patch();

            Main.DamageResistances = new DamageResistance[3] {
            // Gloves
                new DamageResistance(
                    Main.glovesPrefab.TechType,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.15f)/*,
                        new DamageInfo(DamageType.Radiation, -0.10f)*/
                    }),


            // Helmet
                new DamageResistance(
                    Main.helmetPrefab.TechType,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.25f)/*,
                        new DamageInfo(DamageType.Radiation, -0.20f)*/
                    }),


            // Suit
                new DamageResistance(
                    Main.suitPrefab.TechType,
                    new DamageInfo[] {
                        new DamageInfo(DamageType.Acid, -0.6f)/*,
                        new DamageInfo(DamageType.Radiation, -0.70f)*/
                    })
            }; 
            Assembly assembly = Assembly.GetExecutingAssembly();
            new Harmony($"DaWrecka_{assembly.GetName().Name}").PatchAll(assembly);
        }
    }
}
