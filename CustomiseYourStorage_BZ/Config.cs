//using SMLHelper.V2.Json;
//using SMLHelper.V2.Options;
//using SMLHelper.V2.Options.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;
using Logger = QModManager.Utility.Logger;
using System.IO;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace CustomiseYourStorage_BZ.Configuration
{
    internal class DWStorageConfigNonSML
    {
        private readonly Vector2int nullVector = new Vector2int(0, 0); // Used for quick comparison

        private Dictionary<string, Vector2int> defaultStorageSizes = new Dictionary<string, Vector2int>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "quantumlocker.storagecontainer", new Vector2int(4, 4) },
            { "smalllocker.smalllocker", new Vector2int(5, 6) },
            { "locker.locker", new Vector2int(6, 8) },
            { "labtrashcan.labtrashcan", new Vector2int(3, 4) },
            { "trashcans.trashcans", new Vector2int(4, 5) },
            { "vehiclestoragemodule.seamothstoragemodule", new Vector2int(4, 4) },
#if BELOWZERO
            { "recyclotron.recyclotron", new Vector2int(6, 4) },
            { "coffeevendingmachine.coffeevendingmachine", new Vector2int(2, 1) },
            { "fridge.fridge", new Vector2int(5, 7) },
            { "seatruckfabricatormodule.storagecontainer (1)", new Vector2int(6, 2) },
            { "seatruckstoragemodule.storagecontainer", new Vector2int(3, 5) },
            { "seatruckstoragemodule.storagecontainer (1)", new Vector2int(6, 3) },
            { "seatruckstoragemodule.storagecontainer (2)", new Vector2int(4, 3) },
            { "seatruckstoragemodule.storagecontainer (3)", new Vector2int(4, 3) },
            { "seatruckstoragemodule.storagecontainer (4)", new Vector2int(3, 5) },
            { "spypenguin.inventory", new Vector2int(2, 2) },
            { "exchanger.exchanger", new Vector2int(3, 2) },
            { "supplydrop.supplydrop", new Vector2int(8, 8) },
#endif
            { "smallstorage.storagecontainer", new Vector2int(4, 4) }
        };

        private Vector2int defaultLifepodLockerSize = new Vector2int(6, 8);
        public struct ExoConfigStruct
        {
            public int height;
            public int width;
            public int heightPerModule;

            public ExoConfigStruct(int width, int height, int heightPerModule)
            {
                this.height = height;
                this.width = width;
                this.heightPerModule = heightPerModule;
            }

            public override string ToString() => $"({width}, {height}) + {heightPerModule}";

            public bool IsNull()
            {
                return (this.height == 0 || this.width == 0 || this.heightPerModule == 0);
            }
        }

        public struct FiltrationConfigStruct
        {
            public int maxSalt;
            public int maxWater;
            public Vector2int containerSize;

            public override string ToString() => $"[maxSalt {maxSalt}, maxWater {maxWater}, size ({containerSize.ToString()})]";

            public bool IsNull()
            {
                return maxSalt < 1 || maxWater < 1 || containerSize.x < 1 || containerSize.y < 1;
            }

            public FiltrationConfigStruct(int maxSalt, int maxWater, Vector2int containerSize)
            {
                this.maxSalt = System.Math.Max(maxSalt, 1);
                this.maxWater = System.Math.Max(maxWater, 1);
                this.containerSize = containerSize;
            }
        }

        private ExoConfigStruct defaultExosuitConfig = new ExoConfigStruct(6, 4, 1);
        private FiltrationConfigStruct defaultFiltrationConfig = new FiltrationConfigStruct(2, 2, new Vector2int(2, 2));
        private Vector2int defaultInventorySize = new Vector2int(6, 8);
        private Vector2int defaultBioReactorSize = new Vector2int(4, 4);

        public Dictionary<string, Vector2int> StorageSizes;
        public Vector2int LifepodLockerSize = new Vector2int(0, 0);
        public ExoConfigStruct ExosuitConfig;
        public FiltrationConfigStruct FiltrationConfig;
        public Vector2int InventorySize = new Vector2int(0, 0);
        public Vector2int BioreactorSize = new Vector2int(0, 0);

        public bool TryGetModSize(string Identifier, out Vector2int newSize)
        {
            string lowID = Identifier.ToLower();
            Vector2int defaultSize;
            bool bHasDefault = defaultStorageSizes.TryGetValue(Identifier, out defaultSize);
            if (bHasDefault)
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Found default values for ID {Identifier} using TryGetValue");
#endif
            }
            else
            {
                // Go through the defaults list manually; this *shouldn't* be necessary, as the dictionary has been declared with the IgnoreCase comparer, but
                // experience has not borne this out.

                foreach (KeyValuePair<string, Vector2int> kvp in defaultStorageSizes)
                {
                    string key = kvp.Key.ToLower();
                    if (key == lowID)
                    {
                        bHasDefault = true;
                        defaultSize = kvp.Value;
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Found default values for ID {Identifier} on manual review that were not found with TryGetValue");
#endif
                        break;
                    }
                }
            }

            newSize = new Vector2int(0, 0);
            if (StorageSizes.TryGetValue(Identifier, out newSize))
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Found configured values for ID {Identifier} using TryGetValue");
#endif
                if (bHasDefault)
                {
                    // Return a value of true if the new value is different from default, and false if it's equal
                    return !(newSize.Equals(defaultSize));
                }
                else
                    return true;
            }
            else
            {
                foreach (KeyValuePair<string, Vector2int> kvp in StorageSizes)
                {
                    if (lowID == kvp.Key.ToLower())
                    {
                        newSize = kvp.Value;
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Found configured values for ID {Identifier} on manual review that were not found with TryGetValue");
#endif
                        if (bHasDefault)
                            return !(newSize.Equals(defaultSize));
                        else
                            return true;
                    }
                }
            }

            // Couldn't find a value for this identifier, so:
#if !RELEASE
            Logger.Log(Logger.Level.Debug, "Could not find " + (bHasDefault ? "" : "default or ") + "configured values for the identifier " + Identifier);
#endif
            return false;
        }

        public bool AddContainer(string Id, int width, int height)
        {
            Vector2int size;

            if (TryGetModSize(Id, out size))
            {
                return false; // Already exists
            }

            size.x = width;
            size.y = height;
            StorageSizes.Add(Id, size);
            Save();
            return true;
        }

        public static DWStorageConfigNonSML LoadConfig(string LoadPath)
        {
            DWStorageConfigNonSML config = new DWStorageConfigNonSML();

            if (FileUtils.FileExists(LoadPath))
            {
                using (var reader = new StreamReader(LoadPath))
                {
                    var serializer = new JsonSerializer();
                    config = serializer.Deserialize(reader, typeof(DWStorageConfigNonSML)) as DWStorageConfigNonSML;
                }
            }
            return config;
        }


        public void Init()
        {
            bool bUpdated = false;
            if (StorageSizes == null || StorageSizes.Count < 1)
            {
                StorageSizes = new Dictionary<string, Vector2int>(defaultStorageSizes, System.StringComparer.OrdinalIgnoreCase);
                bUpdated = true;
            }

            if (LifepodLockerSize.x < 1 || LifepodLockerSize.y < 1)
            {
                LifepodLockerSize = defaultLifepodLockerSize;
                bUpdated = true;
            }

            if (ExosuitConfig.IsNull())
            {
                ExosuitConfig = defaultExosuitConfig;
                bUpdated = true;
            }

            if (FiltrationConfig.IsNull())
            {
                FiltrationConfig = defaultFiltrationConfig;
                bUpdated = true;
            }

            if (BioreactorSize.x < 1 || BioreactorSize.y < 1)
            {
                BioreactorSize = defaultBioReactorSize;
                bUpdated = true;
            }

            if (InventorySize.x < 1 || InventorySize.y < 1)
            {
                InventorySize = defaultInventorySize;
                bUpdated = true;
            }


            if (bUpdated)
                Save();
#if !RELEASE
            Logger.Log(Logger.Level.Debug, (bUpdated ? "Some values reset to defaults" : "All values present and correct"));
#endif
        }

        public static bool SaveConfig(DWStorageConfigNonSML configToSave, string SavePath)
        {
            using (var writer = new StreamWriter(SavePath))
            {
                writer.Write(JsonConvert.SerializeObject(configToSave, Formatting.Indented, new StringEnumConverter()
                {
                    NamingStrategy = new CamelCaseNamingStrategy(),
                    AllowIntegerValues = true
                }));
            }
            return false;
        }

        public void Save(string SavePath = "")
        {
            if (string.IsNullOrEmpty(SavePath))
            {
                SavePath = Path.Combine(new string[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json" });
            }

            SaveConfig(this, SavePath);
        }



        public DWStorageConfigNonSML(string LoadPath = "")
        {
        }
    }

    /*public class DWStorageConfig : ConfigFile
    {
        private readonly Vector2int nullVector = new Vector2int(0, 0); // Used for quick comparison

        private Dictionary<string, Vector2int> defaultStorageSizes = new Dictionary<string, Vector2int>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "quantumlocker.storagecontainer", new Vector2int(4, 4) },
            { "smalllocker.smalllocker", new Vector2int(5, 6) },
            { "locker.locker", new Vector2int(6, 8) },
            { "labtrashcan.labtrashcan", new Vector2int(3, 4) },
            { "trashcans.trashcans", new Vector2int(4, 5) },
            { "vehiclestoragemodule.seamothstoragemodule", new Vector2int(4, 4) },
#if BELOWZERO
            { "recyclotron.recyclotron", new Vector2int(6, 4) },
            { "coffeevendingmachine.coffeevendingmachine", new Vector2int(2, 1) },
            { "fridge.fridge", new Vector2int(5, 7) },
            { "seatruckfabricatormodule.storagecontainer (1)", new Vector2int(6, 2) },
            { "seatruckstoragemodule.storagecontainer", new Vector2int(3, 5) },
            { "seatruckstoragemodule.storagecontainer (1)", new Vector2int(6, 3) },
            { "seatruckstoragemodule.storagecontainer (2)", new Vector2int(4, 3) },
            { "seatruckstoragemodule.storagecontainer (3)", new Vector2int(4, 3) },
            { "seatruckstoragemodule.storagecontainer (4)", new Vector2int(3, 5) },
            { "spypenguin.inventory", new Vector2int(2, 2) },
            { "exchanger.exchanger", new Vector2int(3, 2) },
            { "supplydrop.supplydrop", new Vector2int(8, 8) },
#endif
            { "smallstorage.storagecontainer", new Vector2int(4, 4) }
        };

        private Vector2int defaultLifepodLockerSize = new Vector2int(6, 8);
        public struct ExoConfigStruct
        {
            public int height;
            public int width;
            public int heightPerModule;

            public ExoConfigStruct(int width, int height, int heightPerModule)
            {
                this.height = height;
                this.width = width;
                this.heightPerModule = heightPerModule;
            }

            public override string ToString() => $"({width}, {height}) + {heightPerModule}";

            public bool IsNull()
            {
                return (this.height == 0 || this.width == 0 || this.heightPerModule == 0);
            }
        }

        public struct FiltrationConfigStruct
        {
            public int maxSalt;
            public int maxWater;
            public Vector2int containerSize;

            public override string ToString() => $"[maxSalt {maxSalt}, maxWater {maxWater}, size ({containerSize.ToString()})]";

            public bool IsNull()
            {
                return maxSalt < 1 || maxWater < 1 || containerSize.x < 1 || containerSize.y < 1;
            }

            public FiltrationConfigStruct(int maxSalt, int maxWater, Vector2int containerSize)
            {
                this.maxSalt = System.Math.Max(maxSalt, 1);
                this.maxWater = System.Math.Max(maxWater, 1);
                this.containerSize = containerSize;
            }
        }

        private ExoConfigStruct defaultExosuitConfig = new ExoConfigStruct(6, 4, 1);
        private FiltrationConfigStruct defaultFiltrationConfig = new FiltrationConfigStruct(2, 2, new Vector2int(2, 2));
        private Vector2int defaultInventorySize = new Vector2int(6, 8);
        private Vector2int defaultBioReactorSize = new Vector2int(4, 4);

        public Dictionary<string, Vector2int> StorageSizes;
        public Vector2int LifepodLockerSize = new Vector2int(0, 0);
        public ExoConfigStruct ExosuitConfig;
        public FiltrationConfigStruct FiltrationConfig;
        public Vector2int InventorySize = new Vector2int(0, 0);
        public Vector2int BioreactorSize = new Vector2int(0, 0);


        public void Init()
        {
            bool bUpdated = false;
            if (StorageSizes == null)
            {
                StorageSizes = new Dictionary<string, Vector2int>(defaultStorageSizes, System.StringComparer.OrdinalIgnoreCase);
                bUpdated = true;
            }

            if (LifepodLockerSize.x == 0 || LifepodLockerSize.y == 0)
            {
                LifepodLockerSize = defaultLifepodLockerSize;
                bUpdated = true;
            }

            if (ExosuitConfig.IsNull())
            {
                ExosuitConfig = defaultExosuitConfig;
                bUpdated = true;
            }

            if (FiltrationConfig.IsNull())
            {
                FiltrationConfig = defaultFiltrationConfig;
                bUpdated = true;
            }

            if (BioreactorSize.x == 0 || BioreactorSize.y == 0)
            {
                BioreactorSize = defaultBioReactorSize;
                bUpdated = true;
            }

            if (InventorySize.x == 0 || InventorySize.y == 0)
            {
                InventorySize = defaultInventorySize;
                bUpdated = true;
            }


            if (bUpdated)
                Save();
#if !RELEASE
            Logger.Log(Logger.Level.Debug, (bUpdated ? "Some values reset to defaults" : "All values present and correct")); 
#endif
        }

        public bool TryGetModSize(string Identifier, out Vector2int newSize)
        {
            string lowID = Identifier.ToLower();
            Vector2int defaultSize;
            bool bHasDefault = defaultStorageSizes.TryGetValue(Identifier, out defaultSize);
            if (bHasDefault)
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Found default values for ID {Identifier} using TryGetValue"); 
#endif
            }
            else
            {
                // Go through the defaults list manually; this *shouldn't* be necessary, as the dictionary has been declared with the IgnoreCase comparer, but
                // experience has not borne this out.

                foreach (KeyValuePair<string, Vector2int> kvp in defaultStorageSizes)
                {
                    string key = kvp.Key.ToLower();
                    if (key == lowID)
                    {
                        bHasDefault = true;
                        defaultSize = kvp.Value;
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Found default values for ID {Identifier} on manual review that were not found with TryGetValue"); 
#endif
                        break;
                    }
                }
            }

            newSize = new Vector2int(0, 0);
            if (StorageSizes.TryGetValue(Identifier, out newSize))
            {
#if !RELEASE
                Logger.Log(Logger.Level.Debug, $"Found configured values for ID {Identifier} using TryGetValue"); 
#endif
                if (bHasDefault)
                {
                    // Return a value of true if the new value is different from default, and false if it's equal
                    return !(newSize.Equals(defaultSize));
                }
                else
                    return true;
            }
            else
            {
                foreach (KeyValuePair<string, Vector2int> kvp in StorageSizes)
                {
                    if (lowID == kvp.Key.ToLower())
                    {
                        newSize = kvp.Value;
#if !RELEASE
                        Logger.Log(Logger.Level.Debug, $"Found configured values for ID {Identifier} on manual review that were not found with TryGetValue"); 
#endif
                        if (bHasDefault)
                            return !(newSize.Equals(defaultSize));
                        else
                            return true;
                    }
                }
            }

            // Couldn't find a value for this identifier, so:
#if !RELEASE
            Logger.Log(Logger.Level.Debug, "Could not find " + (bHasDefault ? "" : "default or ") + "configured values for the identifier " + Identifier); 
#endif
            return false;
        }

        public bool AddContainer(string Id, int width, int height)
        {
            Vector2int size;

            if (TryGetModSize(Id, out size))
            {
                return false; // Already exists
            }

            size.x = width;
            size.y = height;
            StorageSizes.Add(Id, size);
            Save();
            return true;
        }
    }*/
}
