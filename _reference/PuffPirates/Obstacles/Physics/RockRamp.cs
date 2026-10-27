using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	public sealed class RockRamp : PhysicsObstacleBase
	{
		private struct ActiveRock
		{
			public Rock Instance;

			public ObstaclePool<Rock> Pool;

			public float SpawnTime;
		}

		[Header("Havuzlanan Kaya Prefab'ları")]
		[Tooltip("Bu rampaya özel kaya prefab'ları (Rock component'i taşımalı) — her rampa FARKLI prefab'lar atayabilir. Her doğuşta aralarından tick-tohumlu rastgele biri seçilir.")]
		[SerializeField]
		private GameObject[] _rockPrefabs;

		[Header("Spawn Noktaları (öncelikli) veya Alan (boşsa)")]
		[Tooltip("Elle yerleştirilmiş doğuş noktaları (rampanın üst ucu). Doluysa HER doğuşta aralarından tick-tohumlu rastgele biri seçilir; boşsa aşağıdaki spawn ALANI kullanılır.")]
		[SerializeField]
		private Transform[] _spawnPoints;

		[Tooltip("Spawn ALANI'nın merkezi (opsiyonel) — atanmazsa bu GameObject'in kendi konumu kullanılır. Sadece _spawnPoints boşken etkilidir.")]
		[SerializeField]
		private Transform _spawnAreaCenterOverride;

		[Tooltip("Spawn ALANI'nın dünya-birimi boyutu (X/Y/Z yarım-genişlik × 2) — içinde deterministik rastgele bir nokta seçilir. Sadece _spawnPoints boşken etkilidir.")]
		[SerializeField]
		private Vector3 _spawnAreaSize = new Vector3(4f, 0.5f, 2f);

		[Header("Rampa Yönü")]
		[Tooltip("Kayaların başlangıç itkisinin uygulandığı yön (dünya-uzayı, otomatik normalize edilir) — rampanın 'aşağı' eğimini temsil eder.")]
		[SerializeField]
		private Vector3 _rampDirection = new Vector3(0f, -0.35f, 1f);

		[Header("Doğuş Zamanlaması (frequency)")]
		[Tooltip("Ortalama doğuş aralığı (sn).")]
		[SerializeField]
		[Min(0.2f)]
		private float _spawnInterval = 2.5f;

		[Tooltip("Doğuş anına eklenen deterministik rastgele sapma (±sn) — takvimi öngörülemez kılar.")]
		[SerializeField]
		[Min(0f)]
		private float _spawnIntervalJitter = 0.6f;

		[Header("Kaya Rastgeleliği")]
		[Tooltip("Kayanın tekdüze ölçeği (boyut) — min/max arası tick-tohumlu rastgele.")]
		[SerializeField]
		[Min(0.05f)]
		private float _rockScaleMin = 0.6f;

		[SerializeField]
		[Min(0.05f)]
		private float _rockScaleMax = 1.6f;

		[Tooltip("Kayanın kütlesi (kg) — min/max arası tick-tohumlu rastgele.")]
		[SerializeField]
		[Min(0.1f)]
		private float _rockMassMin = 8f;

		[SerializeField]
		[Min(0.1f)]
		private float _rockMassMax = 40f;

		[Tooltip("Doğuşta rampa yönünde uygulanan başlangıç itki büyüklüğü — min/max arası tick-tohumlu rastgele.")]
		[SerializeField]
		[Min(0f)]
		private float _launchForceMin = 4f;

		[SerializeField]
		[Min(0f)]
		private float _launchForceMax = 14f;

		[Header("Sınırlar ve Temizlik")]
		[Tooltip("Ekranda AYNI ANDA bulunabilecek maksimum kaya sayısı — dolu iken yeni doğuş döngüsü sessizce atlanır (bkz. sınıf-üstü not).")]
		[SerializeField]
		[Min(1f)]
		private int _maxConcurrentRocks = 6;

		[Tooltip("Bir kayanın doğuştan itibaren ne kadar sonra (bitiş alanına ulaşmasa bile) havuza döneceği (sn).")]
		[SerializeField]
		[Min(0.5f)]
		private float _rockLifetimeSeconds = 12f;

		[Header("Havuz Boyutları (her _rockPrefabs elemanı için ayrı ayrı uygulanır)")]
		[SerializeField]
		[Min(0f)]
		private int _rockPrewarm = 3;

		[SerializeField]
		[Min(1f)]
		private int _rockCap = 10;

		private Rock[] _rockComponents;

		private ObstaclePool<Rock>[] _rockPools;

		private Vector3 _rampDirectionNormalized;

		private int _lastSpawnCycle = -1;

		private readonly List<ActiveRock> _activeRocks = new List<ActiveRock>(16);

		private const int JitterSalt = 1;

		private const int PrefabSalt = 2;

		private const int SpawnPointSalt = 3;

		private const int ScaleSalt = 4;

		private const int MassSalt = 5;

		private const int ForceSalt = 6;

		private const int AreaXSalt = 7;

		private const int AreaYSalt = 8;

		private const int AreaZSalt = 9;

		protected override void Awake()
		{
			base.Awake();
			_rampDirectionNormalized = ((_rampDirection.sqrMagnitude > 0.0001f) ? _rampDirection.normalized : Vector3.forward);
			GameObject[] rockPrefabs = _rockPrefabs;
			int num = ((rockPrefabs != null) ? rockPrefabs.Length : 0);
			_rockComponents = new Rock[num];
			_rockPools = new ObstaclePool<Rock>[num];
			Transform parent = base.transform;
			for (int i = 0; i < num; i++)
			{
				GameObject gameObject = _rockPrefabs[i];
				Rock rock = ((gameObject != null) ? gameObject.GetComponent<Rock>() : null);
				_rockComponents[i] = rock;
				_rockPools[i] = new ObstaclePool<Rock>(rock, parent, _rockPrewarm, _rockCap);
			}
		}

		protected override void OnTick(int tick, float dt, float time)
		{
			UpdateActiveRocks(time);
			ProcessSpawnSchedule(time);
		}

		private void ProcessSpawnSchedule(float time)
		{
			if (_rockPrefabs != null && _rockPrefabs.Length != 0)
			{
				float num = Mathf.Max(0.1f, _spawnInterval);
				int num2 = Mathf.FloorToInt(time / num);
				float num3 = (float)num2 * num;
				float num4 = RandRange(num2, 1, 0f - _spawnIntervalJitter, _spawnIntervalJitter);
				float num5 = num3 + Mathf.Clamp(num * 0.5f + num4, 0.05f, num - 0.05f);
				if (!(time < num5) && _lastSpawnCycle != num2)
				{
					_lastSpawnCycle = num2;
					TrySpawnRock(num2, time);
				}
			}
		}

		private void TrySpawnRock(int cycleIndex, float time)
		{
			if (_activeRocks.Count >= Mathf.Max(1, _maxConcurrentRocks))
			{
				return;
			}
			int num = RandInt(cycleIndex, 2, 0, _rockPrefabs.Length);
			ObstaclePool<Rock> obstaclePool = _rockPools[num];
			if (obstaclePool != null)
			{
				ResolveSpawnTransform(cycleIndex, out var pos, out var rot);
				Rock rock = obstaclePool.Get(pos, rot);
				if (!(rock == null))
				{
					float uniformScale = RandRange(cycleIndex, 4, _rockScaleMin, _rockScaleMax);
					float mass = RandRange(cycleIndex, 5, _rockMassMin, _rockMassMax);
					float num2 = RandRange(cycleIndex, 6, _launchForceMin, _launchForceMax);
					rock.Configure(uniformScale, mass, _rampDirectionNormalized * num2);
					_activeRocks.Add(new ActiveRock
					{
						Instance = rock,
						Pool = obstaclePool,
						SpawnTime = time
					});
				}
			}
		}

		private void ResolveSpawnTransform(int cycleIndex, out Vector3 pos, out Quaternion rot)
		{
			if (_spawnPoints != null && _spawnPoints.Length != 0)
			{
				int num = RandInt(cycleIndex, 3, 0, _spawnPoints.Length);
				Transform transform = _spawnPoints[num];
				if (transform != null)
				{
					pos = transform.position;
					rot = transform.rotation;
					return;
				}
			}
			pos = RandomAreaPoint(cycleIndex);
			rot = Quaternion.identity;
		}

		private Vector3 RandomAreaPoint(int cycleIndex)
		{
			Vector3 obj = ((_spawnAreaCenterOverride != null) ? _spawnAreaCenterOverride.position : base.transform.position);
			Vector3 vector = _spawnAreaSize * 0.5f;
			float x = RandRange(cycleIndex, 7, 0f - vector.x, vector.x);
			float y = RandRange(cycleIndex, 8, 0f - vector.y, vector.y);
			float z = RandRange(cycleIndex, 9, 0f - vector.z, vector.z);
			return obj + new Vector3(x, y, z);
		}

		private void UpdateActiveRocks(float time)
		{
			for (int num = _activeRocks.Count - 1; num >= 0; num--)
			{
				ActiveRock activeRock = _activeRocks[num];
				if (activeRock.Instance == null)
				{
					_activeRocks.RemoveAt(num);
				}
				else if (time >= activeRock.SpawnTime + _rockLifetimeSeconds)
				{
					activeRock.Pool.Return(activeRock.Instance);
					_activeRocks.RemoveAt(num);
				}
			}
		}

		public void OnRockReachedFinish(Rock rock)
		{
			if (rock == null)
			{
				return;
			}
			for (int i = 0; i < _activeRocks.Count; i++)
			{
				if (!(_activeRocks[i].Instance != rock))
				{
					_activeRocks[i].Pool.Return(rock);
					_activeRocks.RemoveAt(i);
					break;
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (_spawnPoints != null && _spawnPoints.Length != 0)
			{
				Transform[] spawnPoints = _spawnPoints;
				foreach (Transform transform in spawnPoints)
				{
					if (!(transform == null))
					{
						PhysicsObstacleBase.GizmoSphere(transform.position, 0.4f, Color.yellow);
						PhysicsObstacleBase.GizmoArrow(transform.position, RampDirNormalizedForGizmo() * 2f, new Color(0.2f, 0.6f, 1f, 0.9f));
					}
				}
			}
			else
			{
				Vector3 center = ((_spawnAreaCenterOverride != null) ? _spawnAreaCenterOverride.position : base.transform.position);
				Color color = Gizmos.color;
				Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
				Gizmos.DrawWireCube(center, _spawnAreaSize);
				Gizmos.color = color;
			}
			PhysicsObstacleBase.GizmoArrow(base.transform.position, RampDirNormalizedForGizmo() * 3f, new Color(1f, 0.4f, 0.1f, 1f));
		}

		private Vector3 RampDirNormalizedForGizmo()
		{
			if (!(_rampDirection.sqrMagnitude > 0.0001f))
			{
				return Vector3.forward;
			}
			return _rampDirection.normalized;
		}
	}
}
