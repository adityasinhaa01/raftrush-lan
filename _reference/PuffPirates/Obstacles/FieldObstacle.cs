using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(Collider))]
	public abstract class FieldObstacle : MonoBehaviour
	{
		protected static NetworkBoat HostBoat(Collider other)
		{
			NetworkBoat componentInParent = other.GetComponentInParent<NetworkBoat>();
			if (componentInParent == null || !componentInParent.IsServerInitialized)
			{
				return null;
			}
			return componentInParent;
		}

		protected static Rigidbody BodyOf(NetworkBoat boat)
		{
			return boat.Rigidbody;
		}

		protected static float Dt(NetworkBoat boat)
		{
			if (!boat.IsSpawned)
			{
				return Time.fixedDeltaTime;
			}
			return (float)boat.TimeManager.TickDelta;
		}
	}
}
