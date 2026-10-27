using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using PuffPirates.Audio;
using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class BounceObstacle : FieldObstacle
	{
		[Tooltip("Velocity change (m/s) applied to the boat, away from the obstacle center.")]
		[SerializeField]
		private float _knockbackImpulse = 6f;

		[Tooltip("3D sound played on top of the obstacle when a boat bounces off it. Empty = silent.")]
		[SerializeField]
		private SoundData _bounceSound;

		[Tooltip("Minimum seconds between two bounce sounds from this obstacle (collision + trigger or jittery contacts).")]
		[SerializeField]
		[Min(0f)]
		private float _sfxCooldownSeconds = 0.15f;

		private static readonly List<BounceObstacle> Active = new List<BounceObstacle>();

		private float _nextSfxAllowed;

		private void OnEnable()
		{
			Active.Add(this);
			if (_bounceSound != null)
			{
				AudioManager.PreloadAsync(_bounceSound).Forget();
			}
		}

		private void OnDisable()
		{
			Active.Remove(this);
		}

		private void OnTriggerEnter(Collider other)
		{
			Hit(other);
		}

		private void OnCollisionEnter(Collision collision)
		{
			Hit(collision.collider);
		}

		private void Hit(Collider other)
		{
			NetworkBoat networkBoat = FieldObstacle.HostBoat(other);
			if (networkBoat == null || networkBoat.IsInvulnerable)
			{
				return;
			}
			Rigidbody rigidbody = FieldObstacle.BodyOf(networkBoat);
			if (!(rigidbody == null))
			{
				Vector3 vector = rigidbody.position - base.transform.position;
				vector.y = 0f;
				if (vector.sqrMagnitude > 0.001f)
				{
					rigidbody.AddForce(vector.normalized * _knockbackImpulse, ForceMode.VelocityChange);
				}
				if (_bounceSound != null && Time.time >= _nextSfxAllowed)
				{
					_nextSfxAllowed = Time.time + _sfxCooldownSeconds;
					networkBoat.PlayBounceSfx(base.transform.position);
				}
			}
		}

		public static void PlaySfxNear(Vector3 position)
		{
			BounceObstacle bounceObstacle = null;
			float num = float.MaxValue;
			for (int i = 0; i < Active.Count; i++)
			{
				float sqrMagnitude = (Active[i].transform.position - position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					bounceObstacle = Active[i];
				}
			}
			if (bounceObstacle != null && bounceObstacle._bounceSound != null)
			{
				Sfx3D.Play(bounceObstacle._bounceSound, bounceObstacle.transform.position);
			}
		}
	}
}
