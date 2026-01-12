using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace GravTrapBeacons
{
	public class GravTrapModifier : DamageModifier
	{
		private readonly static Dictionary<DamageType, float> multipliers = new()
		{
			{ DamageType.Heat, 0.2f },
			{ DamageType.Pressure, 0f },
			{ DamageType.Puncture, 0f },
			{ DamageType.Collide, 0.5f },
			{ DamageType.Poison, 0f },
			{ DamageType.Acid, 0f },
			{ DamageType.Electrical, 0f },
			{ DamageType.Cold, 0f },
			{ DamageType.Radiation, 0f },
			{ DamageType.Fire, 0f },
		};

		public override float ModifyDamage(float damage, DamageType type)
		{
			multiplier = multipliers.GetOrDefault(type, 1f);
			Log.LogDebug($"GravSphere {gameObject.name} taken damage: {damage}, of type {type.ToString()}; using multiplier of {multiplier}");

			return damage * multiplier;
		}
	}
}
