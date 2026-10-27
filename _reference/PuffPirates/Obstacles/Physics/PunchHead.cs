using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	public sealed class PunchHead : FieldObstacle
	{
		private PunchObstacle _owner;

		private void Awake()
		{
			_owner = GetComponentInParent<PunchObstacle>();
			if (_owner == null)
			{
				Debug.LogWarning("[PunchHead] Üst nesnede PunchObstacle bulunamadı — itki devre dışı.", this);
			}
		}

		private void OnTriggerStay(Collider other)
		{
			if (_owner == null || !_owner.IsExtending)
			{
				return;
			}
			NetworkBoat networkBoat = FieldObstacle.HostBoat(other);
			if (!(networkBoat == null))
			{
				Rigidbody rigidbody = FieldObstacle.BodyOf(networkBoat);
				if (!(rigidbody == null))
				{
					float num = _owner.ComputeForceScale(rigidbody.position);
					Vector3 force = _owner.PushDirection * (_owner.Force * num) * FieldObstacle.Dt(networkBoat);
					rigidbody.AddForce(force, ForceMode.Impulse);
				}
			}
		}
	}
}
