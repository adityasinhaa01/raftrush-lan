using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	[DisallowMultipleComponent]
	public sealed class PooledVfx : MonoBehaviour
	{
		[SerializeField]
		private ParticleSystem _particles;

		public ObstaclePool<PooledVfx> OwnerPool { get; set; }

		private void Awake()
		{
			if (_particles == null)
			{
				_particles = GetComponentInChildren<ParticleSystem>(includeInactive: true);
			}
		}

		public void Play()
		{
			CancelInvoke("ReturnToPool");
			if (!(_particles == null))
			{
				_particles.Clear(withChildren: true);
				_particles.Play(withChildren: true);
				if (OwnerPool != null)
				{
					Invoke("ReturnToPool", ComputeLifetime());
				}
			}
		}

		private float ComputeLifetime()
		{
			ParticleSystem.MainModule main = _particles.main;
			ParticleSystem.MinMaxCurve startLifetime = main.startLifetime;
			float num = ((startLifetime.mode == ParticleSystemCurveMode.Constant || startLifetime.mode == ParticleSystemCurveMode.TwoConstants) ? Mathf.Max(startLifetime.constantMin, startLifetime.constantMax) : 0f);
			return Mathf.Max(0.05f, main.duration + num);
		}

		private void ReturnToPool()
		{
			OwnerPool?.Return(this);
		}

		private void OnDisable()
		{
			CancelInvoke("ReturnToPool");
		}
	}
}
