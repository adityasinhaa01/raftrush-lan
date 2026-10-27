using System;

namespace PuffPirates.Boats
{
	[Flags]
	public enum BoatStatusFlags
	{
		None = 0,
		Slow = 1,
		ForcedGas = 2,
		Electric = 4,
		Knockback = 0x10,
		Invulnerable = 0x20,
		GasBoost = 0x40,
		GasNerf = 0x80,
		MassHeavy = 0x100,
		MassLight = 0x200,
		Plank = 0x400
	}
}
