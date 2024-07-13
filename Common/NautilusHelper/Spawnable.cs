#if NAUTILUS
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Handlers;
using Nautilus.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UWE;
using Sprite = Atlas.Sprite;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace Common.NautilusHelper
{
    public abstract class Spawnable
    {
        /// <summary>
        /// Gets a value indicating whether if we should be looking for a Sprite when NOT overriding <see cref="GetItemSprite"/>.
        /// </summary>
        internal readonly Assembly Mod;
        public string ClassID { get; protected set; }
        public TechType TechType => prefabInfo == null ? TechType.None : prefabInfo.TechType;
        public string prefabFileName { get; protected set; }

        private static readonly Vector2int defaultSize = new Vector2int(1, 1);

        /// <summary>
        /// A simple delegate type that takes no parameters and returns void.
        /// </summary>
        public delegate void PatchEvent();

        /// <summary>
        /// Override with the folder where your mod's icons and other assets are stored.
        /// By default, this will point to the same folder where your mod DLL is.
        /// </summary>
        /// <example>"MyModAssembly/Assets"</example>
        public virtual string AssetsFolder => modFolderLocation;

        /// <summary>
        /// Override with the file name for this item's icon.
        /// If not overriden, this defaults to "[this item's ClassID].png".
        /// </summary>
        /// <example>"MyClassID.png"</example>
        public virtual string IconFileName => $"{ClassID}.png";

        /// <summary>
        /// The in-game name of this spawnable item.
        /// </summary>
        public string FriendlyName { get; protected set; }

        /// <summary>
        /// The description text when viewing this spawnable item from the inventory or crafting UI.
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// Returns <c>true</c> if this spawnable item has already been patched; Otherwise <c>false</c>.
        /// This will become <c>true</c> after the <seealso cref="Patch"/> method has finished running.
        /// </summary>
        public bool IsPatched { get; private set; } = false;

        /// <summary>
        /// Returns the size that this entity will occupy inside the player inventory.<br/>
        /// By default this will be 1x1. Override to change the size.
        /// </summary>
        public virtual Vector2int SizeInInventory { get; } = defaultSize;

        /// <summary>
        /// A lightweight class used to specify the position of a Coordinated Spawn and optionally set its rotation.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="eulerAngles"></param>
        //public record SpawnLocation(Vector3 position, Vector3 eulerAngles = default);

        /// <summary>
        /// Returns the list of <see cref="SpawnLocation"/>s that specify the prefab's Coordinated Spawns.<br/>
        /// By default this will be null.
        /// </summary>
        public virtual List<SpawnLocation> CoordinatedSpawns { get; } = null;

        /// <summary>
        /// Returns the List of BiomeData that handles what Biomes this prefab will spawn, how probable it is to spawn there and how many per spawn.
        /// By default this will be null. Override to change this.
        /// </summary>
        public virtual List<LootDistributionData.BiomeData> BiomesToSpawnIn { get; } = null;

        /// <summary>
        /// Returns the <see cref="WorldEntityInfo"/> of this object if it has one.
        /// By default this will be null. Override to change this.
        /// </summary>
        public virtual WorldEntityInfo EntityInfo { get; } = null;

        public virtual bool HasSprite => false;

        protected virtual TechType iconSource => TechType.None;
        protected abstract TechType templateType { get; } // TechType of the template
        protected abstract string templateClassId { get; } // Some objects we might like to use as templates don't have TechTypes, so we need to address them by class ID.
        protected PrefabInfo prefabInfo;
        protected CustomPrefab customPrefab;

        // If the clone needs to have a ModifyPrefab method, add its code here
        public virtual void ModPrefab(GameObject gameObject)
        {
            Log.LogDebug($"{this.ClassID} prefab modding");
        }

        // This method is where anything that needs to happen between the creation of the CloneTemplate and the PrefabInfo's registration
        // This includes setting up a recipe, configuring the fabricator path, assigning what unlocks the blueprint, setting equipment type,
        // and *IMPORTANT* adding any Gadgets needs to happen here.
        public virtual void FinalisePrefab(CustomPrefab prefab)
        {
            Log.LogDebug($"{this.ClassID} finalising prefab");
        }

        /// <summary>
        /// Initializes a new <see cref="Spawnable"/>, the basic class needed for any item that can be spawned into the Subnautica game world.
        /// </summary>
        /// <param name="classId">The main internal identifier for this item. Your item's <see cref="TechType"/> will be created using this name.</param>
        /// <param name="friendlyName">The name displayed in-game for this item whether in the open world or in the inventory.</param>
        /// <param name="description">The description for this item; Typically seen in the PDA, inventory, or crafting screens.</param>
        protected Spawnable(string classId, string friendlyName, string description)
        {
            this.ClassID = classId;
            this.prefabFileName = ($"{friendlyName}Prefab");
            // this.TechType = techType;

            Mod = GetType().Assembly;

            if (string.IsNullOrEmpty(classId))
            {
                Log.LogError($"ClassID for Spawnables must be a non-empty value.");
                throw new ArgumentException($"Error patching Spawnable");
            }

            FriendlyName = friendlyName;
            Description = description;
        }

#if BELOWZERO
        private IEnumerator RegisterSpriteAsync()
        {
            while (!SpriteManager.hasInitialized)
                yield return new WaitForSecondsRealtime(1);

            SpriteHandler.RegisterSprite(TechType, GetItemSprite());
            yield break;
        }
#endif

        /// <summary>
        /// This event triggers <c>before</c> the core patching methods begins.
        /// You can attach simple <seealso cref="PatchEvent"/> methods to this event if you want to run code <c>before</c> the any of the core patching methods begin.
        /// </summary>
        protected PatchEvent OnStartedPatching;

        /// <summary>
        /// The main patching methods are executed here.
        /// This event should only be used by the SMLHelper QuickStart classes.
        /// </summary>
        internal PatchEvent CorePatchEvents;

        /// <summary>
        /// This event triggers <c>after</c> the core patching methods begins.
        /// You can attach simple <seealso cref="PatchEvent"/> methods to this event if you want to run code <c>after</c> the core patching methods have finished.
        /// </summary>
        protected PatchEvent OnFinishedPatching;

        private readonly string modFolderLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Starts all patching code in SMLHelper.
        /// If <seealso cref="IsPatched"/> is <c>true</c> then this method is skipped to avoid duplicate patching.
        /// </summary>
        /// <seealso cref="OnStartedPatching"/>
        /// <seealso cref="OnFinishedPatching"/>
        public void Patch()
        {
            Log.LogDebug($"{this.ClassID} Patch() started");
            if (IsPatched)
            {
                return; // Already patched. Skip.
            }

            Log.LogDebug($"{this.ClassID} OnStartedPatching");
            OnStartedPatching?.Invoke();
            Log.LogDebug($"{this.ClassID} OnStartedPatching done");


            // Because invocation order isn't guaranteed by event handlers,
            // we make sure the TechType is patched first before anything else that might require it.
            //PatchTechType();

            Log.LogDebug($"{this.ClassID} CorePatchEvents");
            CorePatchEvents?.Invoke();
            Log.LogDebug($"{this.ClassID} CorePatchEvents done");

            if (templateType == TechType.None && string.IsNullOrEmpty(templateClassId))
            {
                Log.LogError($"No suitable template specified for Spawnable {this.ClassID}");
                throw new ArgumentException("Error patching Spawnable");
            }

            prefabInfo = PrefabInfo.WithTechType(this.ClassID, this.FriendlyName, this.Description, "English", techTypeOwner: Mod)
                .WithIcon(GetItemSprite());

            customPrefab = new CustomPrefab(prefabInfo);

            CloneTemplate clone;

            if (templateType != TechType.None)
            {
                Log.LogDebug($"{this.ClassID} using clone template with TechType {templateType.AsString()}");
                clone = new CloneTemplate(prefabInfo, templateType);
            }
            else if (!string.IsNullOrEmpty(templateClassId))
            {
                Log.LogDebug($"{this.ClassID} using clone template with class ID {templateClassId}");
                clone = new CloneTemplate(prefabInfo, templateClassId);
            }
            else
            {
                Log.LogError($"Clone template not specified for Spawnable {this.ClassID}");
                throw new ArgumentException($"Error patching Spawnable {this.ClassID}");
            }

            ModifyClone(clone);

            if (BiomesToSpawnIn != null)
            {
                Log.LogDebug($"{this.ClassID} using BiomesToSpawnIn");
                customPrefab.SetSpawns(BiomesToSpawnIn.ToArray());
            }

            if (CoordinatedSpawns != null)
            {
                Log.LogDebug($"{this.ClassID} using CoordinatedSpawns");
                customPrefab.SetSpawns(CoordinatedSpawns.ToArray());
            }

            this.FinalisePrefab(customPrefab);


            IsPatched = true;

            Log.LogDebug($"{this.ClassID} invoking OnFinished patch events");
            OnFinishedPatching?.Invoke();
            Log.LogDebug($"{this.ClassID} setting GameObject");
            customPrefab.SetGameObject(clone);
            Log.LogDebug($"{this.ClassID} registering PrefabInfo");
            customPrefab.Register();
            Log.LogDebug($"{this.ClassID} Patch() finished");
        }

        // Certain prefabs may need to use async methods to modify the clone template
        public virtual void ModifyClone(CloneTemplate clone)
        {
            clone.ModifyPrefab += ModPrefab;
        }

        /*internal virtual void PatchTechType()
        {
            this.TechType = EnumHandler.AddEntry<TechType>(ClassID)
                .WithPdaInfo(FriendlyName, Description)
                .WithIcon(GetItemSprite());

            //TechType = TechTypeHandler.AddTechType(Mod, ClassID, FriendlyName, Description, false);
        }*/

        /// <summary>
        /// Determines thee <see cref="Sprite"/> to be used for this spawnable's icon.<para/>
        /// Default behavior will look for a PNG file named <see cref="IconFileName"/> inside <see cref="AssetsFolder"/>.
        /// </summary>
        /// <returns>Returns the <see cref="Sprite"/> that will be used in the <see cref="SpriteHandler.RegisterSprite(TechType, Sprite)"/> call.</returns>
        protected virtual Sprite GetItemSprite()
        {
            // This is for backwards compatibility with mods that were using the "ModName/Assets" format
            string path = this.AssetsFolder != modFolderLocation
#if QMM
                ? IOUtilities.Combine(".", "QMods", this.AssetsFolder.Trim('/'), this.IconFileName)
#else
                ? IOUtilities.Combine(".", "BepInEx", "plugins", this.AssetsFolder.Trim('/'), this.IconFileName)
#endif
                : Path.Combine(this.AssetsFolder, this.IconFileName);

            if (File.Exists(path))
            {
                return ImageUtils.LoadSpriteFromFile(path);
            }

            if (HasSprite)
                Log.LogError($"Sprite for '{this.prefabFileName}'{Environment.NewLine}Did not find an image file at '{path}'");

            return SpriteManager.defaultSprite;
        }
    }
}
#endif
