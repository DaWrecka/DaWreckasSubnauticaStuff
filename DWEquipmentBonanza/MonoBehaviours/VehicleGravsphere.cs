using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DWEquipmentBonanza.MonoBehaviours
{
    internal class ResourceCollider : SphereCollider
    {
        public ResourceCollider(SphereCollider source = null)
        {
            if (source != null)
            {

            }
            this.isTrigger = true;
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
