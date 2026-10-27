using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class PickupSpawnPoint : MonoBehaviour
	{
		[Tooltip("Bu noktada ÇIKABİLECEK efektler (boş = varsayılan tüm buff/nerf).")]
		[SerializeField]
		private PickupEffectType[] _allowed;

		[Tooltip("Toplandıktan kaç saniye sonra tekrar (yeni efektle) belirir.")]
		[SerializeField]
		[Min(1f)]
		private float _respawnSeconds = 8f;

		[Tooltip("LevelBuilder deterministik sırayla atar. -1 = atanmamış (fallback: hep görünür).")]
		[SerializeField]
		private int _spawnId = -1;

		private static readonly PickupEffectType[] DefaultAll = new PickupEffectType[5]
		{
			PickupEffectType.GasBoost,
			PickupEffectType.GasNerf,
			PickupEffectType.MassHeavy,
			PickupEffectType.MassLight,
			PickupEffectType.Plank
		};

		public PickupEffectType[] Allowed
		{
			get
			{
				if (_allowed == null || _allowed.Length == 0)
				{
					return DefaultAll;
				}
				return _allowed;
			}
		}

		public float RespawnSeconds => _respawnSeconds;

		public int SpawnId => _spawnId;

		public void Configure(int id)
		{
			_spawnId = id;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
			Gizmos.DrawWireSphere(base.transform.position, 0.6f);
			Gizmos.DrawLine(base.transform.position, base.transform.position + base.transform.up * 1.2f);
		}
	}
}
