using System.Collections.Generic;
using PuffPirates.Boats;
using PuffPirates.Obstacles.Physics;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class Whirlpool : PhysicsObstacleBase
	{
		[SerializeField]
		private float _pullForce = 1100f;

		[SerializeField]
		private float _swirlForce = 900f;

		[SerializeField]
		private float _slowSeconds = 1f;

		[Tooltip("SphereCollider yoksa kullanılan alan yarıçapı (m).")]
		[SerializeField]
		[Min(0.5f)]
		private float _fallbackRadius = 8f;

		[Tooltip("Alan içinde yatay hıza uygulanan sönüm (1/sn, kütleden bağımsız). Olmazsa çekim tekneyi merkeze 15 m/s ile fırlatıp geçiriyordu; sönümle tekne spiral çizerek YAKALANIR ve merkez çevresinde döner.")]
		[SerializeField]
		[Min(0f)]
		private float _innerDrag = 2.5f;

		[Tooltip("Merkeze bu mesafeden yakında çekim kuvveti doğrusal azalır (merkezde titreşme/geçme olmasın); dönme sürer.")]
		[SerializeField]
		[Min(0.1f)]
		private float _coreRadius = 2f;

		private readonly List<NetworkBoat> _boats = new List<NetworkBoat>(8);

		private SphereCollider _zone;

		public float Radius
		{
			get
			{
				if (_zone == null)
				{
					_zone = GetComponent<SphereCollider>();
				}
				if (_zone == null)
				{
					return _fallbackRadius;
				}
				Vector3 lossyScale = base.transform.lossyScale;
				return _zone.radius * Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z));
			}
		}

		public void Configure(float pull, float swirl)
		{
			_pullForce = pull;
			_swirlForce = swirl;
		}

		protected override void OnTick(int tick, float dt, float time)
		{
			if (QueryHostBoats(base.transform.position, Radius, _boats) == 0)
			{
				return;
			}
			for (int i = 0; i < _boats.Count; i++)
			{
				NetworkBoat networkBoat = _boats[i];
				Rigidbody rigidbody = networkBoat.Rigidbody;
				if (!(rigidbody == null) && !rigidbody.isKinematic)
				{
					Vector3 vector = base.transform.position - rigidbody.position;
					vector.y = 0f;
					float magnitude = vector.magnitude;
					if (!(magnitude < 0.05f))
					{
						vector /= magnitude;
						Vector3 vector2 = Vector3.Cross(Vector3.up, vector);
						float num = Mathf.Clamp01(magnitude / _coreRadius);
						rigidbody.AddForce(vector * (_pullForce * num) * dt, ForceMode.Impulse);
						rigidbody.AddForce(vector2 * _swirlForce * dt, ForceMode.Impulse);
						Vector3 linearVelocity = rigidbody.linearVelocity;
						rigidbody.AddForce(new Vector3(0f - linearVelocity.x, 0f, 0f - linearVelocity.z) * _innerDrag, ForceMode.Acceleration);
						networkBoat.ApplyStatus(BoatStatusSlot.Slow, BoatStatusFlags.Slow, _slowSeconds);
					}
				}
			}
		}
	}
}
