using System.Collections.Generic;
using PuffPirates.Boats;
using PuffPirates.Obstacles.Physics;
using PuffPirates.Players;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class Lighter : MonoBehaviour
	{
		[SerializeField]
		private PooledVfx _explosionPrefab;

		[Tooltip("Yatay tutuşma yarıçapı (m).")]
		[SerializeField]
		private float _radius = 2.5f;

		[Tooltip("Bu yüksekliğin üstündeki/altındaki botlar sayılmaz (m).")]
		[SerializeField]
		private float _height = 3f;

		[Tooltip("VFX ile ölüm arası gecikme (sn).")]
		[SerializeField]
		private float _fuseSeconds = 0.2f;

		[Tooltip("Patlamadan sonra tekrar tutuşabilmesi için bekleme (sn).")]
		[SerializeField]
		private float _cooldown = 3f;

		[SerializeField]
		private float _respawnInvuln = 2f;

		private NetworkBoat _victim;

		private float _fuseEnd;

		private float _readyAt;

		private static bool IsGassing(NetworkBoat boat)
		{
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (piratePlayer != null && piratePlayer.BoatIndex == boat.BoatIndex && piratePlayer.Gas != null && piratePlayer.Gas.IsGassing)
				{
					return true;
				}
			}
			return false;
		}

		private void Update()
		{
			if (_victim != null)
			{
				if (!(Time.time < _fuseEnd))
				{
					NetworkBoat victim = _victim;
					_victim = null;
					_readyAt = Time.time + _cooldown;
					if (victim != null && victim.IsServerInitialized && !victim.IsInvulnerable)
					{
						Shark.Kill(victim, DeathCause.Fire, _respawnInvuln);
					}
				}
			}
			else
			{
				if (Time.time < _readyAt)
				{
					return;
				}
				Vector3 position = base.transform.position;
				foreach (NetworkBoat item in NetworkBoat.All)
				{
					if (item == null || item.IsInvulnerable)
					{
						continue;
					}
					Vector3 vector = item.transform.position - position;
					if (Mathf.Abs(vector.y) > _height)
					{
						continue;
					}
					vector.y = 0f;
					if (!(vector.sqrMagnitude > _radius * _radius) && IsGassing(item))
					{
						_victim = item;
						_fuseEnd = Time.time + _fuseSeconds;
						if (_explosionPrefab != null)
						{
							PooledVfx pooledVfx = Object.Instantiate(_explosionPrefab, position, Quaternion.identity);
							pooledVfx.Play();
							Object.Destroy(pooledVfx.gameObject, 4f);
						}
						break;
					}
				}
			}
		}
	}
}
