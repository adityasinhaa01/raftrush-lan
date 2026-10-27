using UnityEngine;

namespace GamesMrkt.PuffPirates.Ground
{
	public static class GroundUtility
	{
		public static bool TryGetGround(Collider collider, out IGround ground)
		{
			ground = ((collider != null) ? collider.GetComponentInParent<IGround>() : null);
			return ground != null;
		}

		public static GroundType GetGroundType(Collider collider)
		{
			if (!TryGetGround(collider, out var ground))
			{
				return GroundType.Default;
			}
			return ground.GroundType;
		}
	}
}
