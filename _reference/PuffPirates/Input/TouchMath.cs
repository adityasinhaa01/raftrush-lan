using UnityEngine;

namespace PuffPirates.Input
{
	public static class TouchMath
	{
		public static Vector2 ResolveStick(Vector2 offset, float radius, float deadZone01)
		{
			if (radius <= Mathf.Epsilon)
			{
				return Vector2.zero;
			}
			float num = Mathf.Clamp01(deadZone01);
			float magnitude = offset.magnitude;
			float num2 = num * radius;
			if (magnitude <= num2)
			{
				return Vector2.zero;
			}
			Vector2 vector = offset / magnitude;
			float num3 = Mathf.Clamp01((magnitude - num2) / (radius - num2));
			return vector * num3;
		}
	}
}
