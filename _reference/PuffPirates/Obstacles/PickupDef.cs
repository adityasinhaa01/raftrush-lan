using GamesMrkt.Localization;
using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public readonly struct PickupDef
	{
		public readonly PickupEffectType Type;

		public readonly BoatStatusFlags Flag;

		public readonly BoatStatusSlot Slot;

		public readonly float Seconds;

		public readonly float Magnitude;

		public readonly string LabelKey;

		public readonly string Glyph;

		public readonly Color Color;

		public readonly bool IsBuff;

		public string Label => Loc.Get(LabelKey);

		public PickupDef(PickupEffectType t, BoatStatusFlags f, BoatStatusSlot s, float sec, float mag, string labelKey, string glyph, Color color, bool isBuff)
		{
			Type = t;
			Flag = f;
			Slot = s;
			Seconds = sec;
			Magnitude = mag;
			LabelKey = labelKey;
			Glyph = glyph;
			Color = color;
			IsBuff = isBuff;
		}
	}
}
