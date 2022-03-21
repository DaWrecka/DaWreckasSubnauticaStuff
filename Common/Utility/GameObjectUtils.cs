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
            return GameObject.Instantiate(gameObject, default(Vector3), default(Quaternion), false);
        }
    }
}
