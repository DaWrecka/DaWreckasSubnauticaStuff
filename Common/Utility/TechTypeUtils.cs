using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    class TechTypeUtils
    {
        // Useful function provided by PrimeSonic. Ta!
        public static TechType GetTechType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return TechType.None;

            // Look for a known TechType
            if (TechTypeExtensions.FromString(value, out TechType tType, true))
                return tType;

            //  Not one of the known TechTypes - is it registered with SMLHelper?
            if (TechTypeHandler.TryGetModdedTechType(value, out TechType custom))
                return custom;

            return TechType.None;
        }
    }
}
