using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class GustZone : FieldObstacle
	{
		[Tooltip("YEREL eksen (prefab'ın kendi rotasyonuna göre) — sahnede objeyi döndürmek itiş yönünü değiştirir.")]
		[SerializeField]
		private Vector3 _localDir = Vector3.right;

		[SerializeField]
		private float _force = 950f;

		public void Configure(Vector3 localDir, float force)
		{
			_localDir = localDir.normalized;
			_force = force;
		}

		private void OnTriggerStay(Collider other)
		{
			Shark componentInParent = other.GetComponentInParent<Shark>();
			if (componentInParent != null)
			{
				componentInParent.AddCurrent(base.transform.TransformDirection(_localDir.normalized) * _force);
				return;
			}
			NetworkBoat networkBoat = FieldObstacle.HostBoat(other);
			if (!(networkBoat == null))
			{
				Rigidbody rigidbody = FieldObstacle.BodyOf(networkBoat);
				if (!(rigidbody == null))
				{
					Vector3 vector = base.transform.TransformDirection(_localDir.normalized);
					rigidbody.AddForce(vector * _force * FieldObstacle.Dt(networkBoat), ForceMode.Impulse);
				}
			}
		}
	}
}
