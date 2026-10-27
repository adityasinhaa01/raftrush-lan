using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	[DisallowMultipleComponent]
	public sealed class BombProjectile : MonoBehaviour
	{
		private Vector3 _launchPos;

		private Vector3 _targetPos;

		private float _launchTime;

		private float _landTime;

		private float _arcHeight;

		private float _spinDegPerSec;

		public ObstaclePool<BombProjectile> OwnerPool { get; set; }

		public void BeginFlight(Vector3 launchPos, Vector3 targetPos, float launchTime, float landTime, float arcHeight, float spinDegPerSec)
		{
			_launchPos = launchPos;
			_targetPos = targetPos;
			_launchTime = launchTime;
			_landTime = Mathf.Max(landTime, launchTime + 0.01f);
			_arcHeight = arcHeight;
			_spinDegPerSec = spinDegPerSec;
			Tick(launchTime);
		}

		public void Tick(float time)
		{
			float num = _landTime - _launchTime;
			float num2 = ((num > 0.0001f) ? Mathf.Clamp01((time - _launchTime) / num) : 1f);
			Vector3 vector = Vector3.Lerp(_launchPos, _targetPos, num2);
			float num3 = 4f * _arcHeight * num2 * (1f - num2);
			base.transform.position = vector + Vector3.up * num3;
			Vector3 vector2 = _targetPos - _launchPos;
			Quaternion quaternion = ((vector2.sqrMagnitude > 0.0001f) ? Quaternion.LookRotation(vector2.normalized, Vector3.up) : Quaternion.identity);
			float x = _spinDegPerSec * (time - _launchTime);
			base.transform.rotation = quaternion * Quaternion.Euler(x, 0f, 0f);
		}
	}
}
