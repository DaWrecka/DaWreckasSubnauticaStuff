using UnityEngine;
using System;
#if SN1
//using Sprite = Atlas.Sprite;
#endif

namespace Common.Utility
{
	public static class SpriteUtils
	{
		public static Sprite Get(TechType tt, Sprite defaultSprite)
		{
			try
			{
/*
 * #if SN1
				if (defaultSprite == null)
					return SpriteManager.GetWithNoDefault(tt);
				else
					return SpriteManager.Get(tt);
#elif BELOWZERO*/
				return SpriteManager.Get(tt, defaultSprite);
//#endif
			}

			catch (Exception e)
			{
				Log.LogError($"Exception caught while attempting to retrieve sprite for TechType {tt.AsString()}:\n{e.ToString()}");
				return defaultSprite;
			}
		}

		public static Sprite GetWithNoDefault(TechType tt)
		{
			return SpriteUtils.Get(tt, null);
		}
	}
}
