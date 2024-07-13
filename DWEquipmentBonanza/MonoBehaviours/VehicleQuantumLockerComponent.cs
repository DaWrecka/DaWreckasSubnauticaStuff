using Main = DWEquipmentBonanza.DWEBPlugin;
using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UWE;

namespace DWEquipmentBonanza.MonoBehaviours
{
#if BELOWZERO
    public class VehicleQuantumLockerComponent : QuantumLocker, ISerializationCallbackReceiver
    {
        public void Awake()
        {
        }

        /*new private void Start()
        {
            System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
            bool createNew = !this.loadedFromSaveGame || this.protoVersion < 2;
            CoroutineHost.StartCoroutine(DeferredInit(createNew));
        }*/

        public void ToggleQuantumStorage(bool enable)
        {
            CoroutineHost.StartCoroutine(ToggleCoroutine(!this.loadedFromSaveGame || this.protoVersion < 2, enable));
        }

        private IEnumerator ToggleCoroutine(bool createNew, bool enable)
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
            StorageContainer storageContainer = QuantumLockerStorage.GetStorageContainer(createNew);
            if (storageContainer == null)
            {
                //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}(): QuantumLockerStorage not ready yet");
                while (storageContainer == null)
                {
                    storageContainer = QuantumLockerStorage.GetStorageContainer(false);
                    yield return new WaitForEndOfFrame();
                }
            }

            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}(): StorageContainer received");

            this.storageContainer ??= gameObject.AddComponent<StorageContainer>();

            if (storageContainer != null)
                this.storageContainer.SetContainer(storageContainer.container);
            if (this.protoVersion < 2)
            {
                StorageHelper.TransferItems(this.storageContainer.storageRoot.gameObject, storageContainer.container);
                this.protoVersion = 2;
            }
            yield return new WaitForSecondsRealtime(0.1f);
            if (enable)
                QuantumLockerStorage.Register(this);
            else
                QuantumLockerStorage.Unregister(this);
        }

        /*new public void OnMainLockerLoaded(QuantumLockerStorage mainLocker)
        {
            System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");

            if (mainLocker == null)
            {
                Log.LogError($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() mainLocker is null!");
                return;
            }
            if(this.storageContainer == null)
            {
                Log.LogError($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() storageContainer is null!");
                return;
            }
            this.storageContainer.SetContainer(mainLocker.storageContainer.container);
        }*/

        new public void Update() { }

        public void OnBeforeSerialize()
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
            if (uGUI.isLoading)
                Main.saveCache.RegisterReceiver(this);
        }

        public void OnAfterDeserialize()
        {
        }

        new public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        new public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            //System.Reflection.MethodBase thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Log.LogDebug($"{thisMethod.ReflectedType.Name}.{thisMethod.Name}() executing");
            QuantumLockerStorage.Register(this);
            Main.saveCache.RegisterReceiver(this);
            this.loadedFromSaveGame = true;
        }
    }
#endif
}
