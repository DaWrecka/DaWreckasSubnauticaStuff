using System.Collections.Generic;

namespace PrawnSuitArmSwitcher.ExosuitArms
{
    public static class ExosuitArmsStatics
    {
        public static readonly string LeftArm = "ExosuitArmLeft";
        public static readonly string RightArm = "ExosuitArmRight";

        public static readonly List<string> ExosuitArmFieldNames = new List<string> {
                "leftArm",
                "rightArm"
            };
    }
}
