using System;
using System.Collections.Generic;
using System.Text;

#if SUBNAUTICA_STABLE
using Sprite = Atlas.Sprite;
#endif


namespace Common.Utility
{
    public class SpriteUtils
    {
        internal static Sprite GetSpriteWithNoDefault(TechType tt)
        {
#if SUBNAUTICA_STABLE
            return SpriteManager.GetWithNoDefault(tt);
#elif BELOWZERO
            return SpriteManager.Get(tt, null);
#endif
        }
    }
}
