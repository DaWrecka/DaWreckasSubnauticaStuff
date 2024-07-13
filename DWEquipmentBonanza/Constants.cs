using Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if SN1
using Sprite = Atlas.Sprite;
#endif

namespace DWEquipmentBonanza
{
    public class DWConstants
    {
        public const string ChipsMenuPath = "ChipMenu";
        public const string FinsMenuPath = "FinsMenu";
        public const string KnifeMenuPath = "KnifeMenu";
        public const string ExosuitMenuPath = "ExosuitMenu";
        public const string SeatruckMenuPath = "SeaTruckWBUpgrades";
        public const string TankMenuPath = "TankMenu";
        public const string BodyMenuPath = "BodyMenu";
        public const string ChargerMenuPath = "VehicleChargers";
        public const string BaseSuitsMenuName = "SuitBlueprints";
        public const string BaseHelmetsMenuName = "HelmetBlueprints";
        public static string[] BaseSuitsPath { get; } = { "Personal", BaseSuitsMenuName };
        public static string[] BaseHelmetPath { get; } = { "Personal", BaseHelmetsMenuName };
        public static Sprite BaseSuitsIcon => SpriteUtils.Get(DWEBPlugin.StillSuitType, null);
        public static Sprite BaseHelmetsIcon => SpriteUtils.Get(TechType.Rebreather, null);
        public const float newKyaniteChance = 0.85f;
    }
}

