using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
    public class ResourceCollider : SphereCollider
    {
        // An internal class whose sole purpose is to make it easier to patch VehicleDockingBay.OnTriggerEnter to ignore vehicle-mounted gravtraps.
        // We do this by replacing SphereColliders on such gravtraps with ResourceColliders, and patching VehicleDockingBay.OnTriggerEnter to ignore any collider which is a ResourceCollider
        // This method adds a new ResourceCollider, copies properties across to the new one, and assuming all went well, destroys the old and returns the new collider.
        public static ResourceCollider ReplaceSphereCollider(SphereCollider replaceTarget)
        {
            if (replaceTarget is ResourceCollider R)
            {
                Log.LogDebug($"ReplaceSphereCollider exiting: Target is already a ResourceCollider");
                return R;
            }

            GameObject goParent = replaceTarget.gameObject;
            if (goParent == null)
            {
                Log.LogDebug($"ReplaceSphereCollider exiting: no valid parent GameObject");
                return null;
            }

            ResourceCollider newCollider = goParent.AddComponent(typeof(ResourceCollider)) as ResourceCollider;
            if (newCollider == null)
            {
                Log.LogDebug($"ReplaceSphereCollider exiting: failed adding ResourceCollider component");
                return null;
            }

            try
            {
                newCollider.radius = replaceTarget.radius;
                newCollider.center = replaceTarget.center;
                newCollider.enabled = replaceTarget.enabled;
                newCollider.isTrigger = replaceTarget.isTrigger;
                newCollider.contactOffset = replaceTarget.contactOffset;
                newCollider.sharedMaterial = replaceTarget.sharedMaterial;
                newCollider.material = replaceTarget.material;
                GameObject.Destroy(replaceTarget);
            }
            catch (Exception e)
            {
                Log.LogDebug($"ReplaceSphereCollider exiting: exception caught\n{e.ToString()}");
                GameObject.Destroy(newCollider);
                return null;
            }

            return newCollider;
        }
    }

    public class ResourceMagnet : Gravsphere
    {
        public static GameObject processedPrefab;
        private const float magnetRadius = 20f;
        private HashSet<GameObject> collidingResources = new HashSet<GameObject>();

        public static void Patch()
        {

        }

        public void InitialiseFromExisting(Gravsphere G)
        {
            this.mainCollider = G.mainCollider;
            this.mainCollider.isTrigger = true;

            this.trigger = G.trigger;
            if (this.trigger is SphereCollider s)
                s.radius = magnetRadius;
        }

        new private void UpdatePads()
        {
        }

        new private void OnTriggerEnter(Collider collider)
        {
            if (collider == trigger)
            {
                Rigidbody componentInHierarchy = UWE.Utils.GetComponentInHierarchy<Rigidbody>(collider.gameObject);
                if (this.IsValidTarget(collider.gameObject) && componentInHierarchy != null && !this.attractableList.Contains(componentInHierarchy) && this.attractableList.Count < 12)
                {
                    componentInHierarchy.isKinematic = false;
                    this.AddAttractable(componentInHierarchy);
                }
            }
            else if (collider == mainCollider)
            {

            }
        }
    }
    internal class VehicleGravsphere : Gravsphere
    {
    }
}
