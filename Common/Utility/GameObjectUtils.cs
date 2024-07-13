using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Common.Utility
{
    internal static class GameObjectUtils
    {
        public static GameObject InstantiateInactive(GameObject gameObject)
        {
#if LEGACY
            return GameObject.Instantiate(gameObject, default(Vector3), default(Quaternion), false);
#else
            GameObject obj = GameObject.Instantiate(gameObject, default(Vector3), default(Quaternion), null);
            obj.SetActive(false);
            return obj;
#endif
        }
    }
}
