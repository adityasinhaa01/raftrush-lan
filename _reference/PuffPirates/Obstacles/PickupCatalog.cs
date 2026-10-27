using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public static class PickupCatalog
	{
		public static readonly PickupDef[] All = new PickupDef[7]
		{
			new PickupDef(PickupEffectType.GasBoost, BoatStatusFlags.GasBoost, BoatStatusSlot.GasBoost, 4f, 1.5f, "status.tag.gas_boost", "GB", new Color(0.3f, 0.9f, 0.4f), isBuff: true),
			new PickupDef(PickupEffectType.GasNerf, BoatStatusFlags.GasNerf, BoatStatusSlot.GasNerf, 4f, 0.55f, "status.tag.gas_nerf", "GN", new Color(0.9f, 0.4f, 0.3f), isBuff: false),
			new PickupDef(PickupEffectType.MassHeavy, BoatStatusFlags.MassHeavy, BoatStatusSlot.MassHeavy, 5f, 1.6f, "status.tag.mass_heavy", "MH", new Color(0.6f, 0.5f, 0.4f), isBuff: false),
			new PickupDef(PickupEffectType.MassLight, BoatStatusFlags.MassLight, BoatStatusSlot.MassLight, 5f, 0.7f, "status.tag.mass_light", "ML", new Color(0.5f, 0.8f, 0.95f), isBuff: true),
			new PickupDef(PickupEffectType.Plank, BoatStatusFlags.Plank, BoatStatusSlot.Plank, 5f, 1.2f, "status.tag.plank", "PL", new Color(0.55f, 0.4f, 0.25f), isBuff: false),
			new PickupDef(PickupEffectType.SpeedBoost, BoatStatusFlags.None, BoatStatusSlot.Count, 2.5f, 1f, "status.tag.speed_boost", "SB", new Color(0.95f, 0.85f, 0.3f), isBuff: true),
			new PickupDef(PickupEffectType.Slow, BoatStatusFlags.Slow, BoatStatusSlot.Slow, 3f, 1f, "status.tag.slow", "SL", new Color(0.7f, 0.5f, 0.9f), isBuff: false)
		};

		public static PickupDef Get(PickupEffectType t)
		{
			for (int i = 0; i < All.Length; i++)
			{
				if (All[i].Type == t)
				{
					return All[i];
				}
			}
			return All[0];
		}
	}
}
