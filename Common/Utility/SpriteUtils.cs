#if SUBNAUTICA_STABLE
using Sprite = Atlas.Sprite;
#elif BELOWZERO
using UnityEngine;
#endif

namespace Common.Utility
{
    public class SpriteUtils
    {
        public static Sprite GetSpriteWithNoDefault(TechType tt)
        {
#if SUBNAUTICA_STABLE
            return SpriteManager.GetWithNoDefault(tt);
#elif BELOWZERO
            return SpriteManager.Get(tt, null);
#endif
        }
    }
}
