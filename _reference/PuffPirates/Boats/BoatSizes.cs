using UnityEngine;

namespace PuffPirates.Boats
{
	public static class BoatSizes
	{
		public const int Default = 2;

		public static readonly BoatSizeDef[] All = new BoatSizeDef[5]
		{
			new BoatSizeDef("XS · 1.5m", 0.65f, 0.65f, 28f, 0.88f, 17f),
			new BoatSizeDef("S · 1.8m", 0.82f, 0.82f, 38f, 1.07f, 16f),
			new BoatSizeDef("M · 2.2m", 1f, 1f, 60.2f, 1.27f, 15f),
			new BoatSizeDef("L · 2.8m", 1.25f, 1.25f, 68f, 1.55f, 13.5f),
			new BoatSizeDef("XL · 3.5m", 1.55f, 1.55f, 90f, 1.89f, 12f)
		};

		public static int Count => All.Length;

		public static BoatSizeDef Get(int id)
		{
			return All[Mathf.Clamp(id, 0, All.Length - 1)];
		}

		public static string Label(int id)
		{
			return Get(id).Label;
		}
	}
}
