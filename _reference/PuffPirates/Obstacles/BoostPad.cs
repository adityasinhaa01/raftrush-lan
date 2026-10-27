using System.Collections.Generic;
using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class BoostPad : FieldObstacle
	{
		[SerializeField]
		private float _force = 2600f;

		[Tooltip("SES: OnTriggerStay HER FİZİK TICK'İNDE çağrılır — debounce olmasa aynı geçiş boyunca sürekli çalardı.")]
		[SerializeField]
		[Min(0.1f)]
		private float _sfxCooldownSeconds = 1.2f;

		private readonly Dictionary<int, float> _nextSfxAllowed = new Dictionary<int, float>();

		public void Configure(float force)
		{
			_force = force;
		}

		private void OnTriggerStay(Collider other)
		{
			NetworkBoat networkBoat = FieldObstacle.HostBoat(other);
			if (networkBoat == null)
			{
				return;
			}
			Rigidbody rigidbody = FieldObstacle.BodyOf(networkBoat);
			if (!(rigidbody == null))
			{
				rigidbody.AddForce(base.transform.forward * _force * FieldObstacle.Dt(networkBoat), ForceMode.Impulse);
				float time = Time.time;
				if (!_nextSfxAllowed.TryGetValue(networkBoat.BoatIndex, out var value) || time >= value)
				{
					_nextSfxAllowed[networkBoat.BoatIndex] = time + _sfxCooldownSeconds;
					networkBoat.PokeBoostPadSfx();
				}
			}
		}
	}
}
