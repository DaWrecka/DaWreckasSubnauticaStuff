using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UWE;

namespace Common
{
	static class TransformUtils
    {
		public static GameObject FindDeepChild(GameObject parent, string childName)
		{
			Queue<Transform> queue = new Queue<Transform>();

			queue.Enqueue(parent.transform);

			while (queue.Count > 0)
			{
				var c = queue.Dequeue();

				if (c.name == childName)
				{
					return c.gameObject;
				}

				foreach (Transform t in c)
				{
					queue.Enqueue(t);
				}
			}
			return null;
		}

	}
}
