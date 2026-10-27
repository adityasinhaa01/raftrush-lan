using System;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	public sealed class WaterDropArena : PhysicsObstacleBase
	{
		private struct TargetSlot
		{
			public bool Active;

			public bool DropSpawned;

			public Vector3 TargetPos;

			public float DropStartTime;

			public float LandTime;

			public int PrefabIndex;

			public WaterTarget Marker;

			public WaterDropObject Visual;
		}

		[Header("Arena (merkez = bu GameObject'in konumu)")]
		[Tooltip("Hedeflerin seçildiği dairesel alanın yarıçapı (dünya birimi).")]
		[SerializeField]
		[Min(1f)]
		private float _arenaRadius = 15f;

		[Tooltip("Yalnız gizmo çizimi için — arenayı çokgen olarak önizler (3-32). Hedef seçimini ETKİLEMEZ, seçim her zaman tam dairededir.")]
		[SerializeField]
		[Range(3f, 32f)]
		private int _arenaGizmoSides = 24;

		[Header("Hedef Zamanlaması")]
		[Tooltip("Aynı anda en fazla kaç kırmızı hedef/düşen nesne birlikte aktif olabilir (eşzamanlı slot sayısı).")]
		[SerializeField]
		[Min(1f)]
		private int _maxSimultaneousTargets = 3;

		[Tooltip("Yeni bir saldırının (kırmızı hedef) başlama sıklığı (sn) — kapasite doluysa o döngünün saldırısı sessizce atlanır, takvim durmaz.")]
		[SerializeField]
		[Min(0.2f)]
		private float _attackInterval = 2.5f;

		[Header("Uyarı (kırmızı hedef)")]
		[Tooltip("Kırmızı hedefin, nesne düşmeye başlamadan önce göründüğü süre (sn) — spec: 'yaklaşık üç saniye'.")]
		[SerializeField]
		[Min(0.2f)]
		private float _warningDuration = 3f;

		[Tooltip("Kırmızı hedef dekalının yarıçapı (dünya birimi).")]
		[SerializeField]
		[Min(0.2f)]
		private float _targetRadius = 2.5f;

		[Header("Düşüş")]
		[Tooltip("Düşen nesnenin başladığı yükseklik (hedef noktasının üstünde, dünya birimi).")]
		[SerializeField]
		[Min(0.5f)]
		private float _dropHeight = 10f;

		[Tooltip("Düşüşün (dropHeight'tan zemine) süresi (sn).")]
		[SerializeField]
		[Min(0.1f)]
		private float _fallDuration = 1f;

		[Header("Su Sıçraması (VFX + Ses — TEK etki, kuvvet/hasar/kamera sarsıntısı YOK)")]
		[Tooltip("Sıçrama sesinin oynatma seviyesi (0..1).")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _splashSfxVolume = 0.85f;

		[Tooltip("Sıçrama sesinin perdesi — projede özel bir 'splash' klibi YOK (bkz. WaterDropArena.TriggerSplash üst yorumu); mevcut ImpactThudClip'in perdesi hafif düşürülerek 'ıslak' bir his verilir.")]
		[SerializeField]
		[Range(0.5f, 2f)]
		private float _splashSfxPitch = 0.9f;

		[Header("Havuzlanan Prefab'lar")]
		[SerializeField]
		private WaterTarget _targetPrefab;

		[Tooltip("Gökten düşecek nesneler — HER SALDIRIDA aralarından biri deterministik seçilir (RandInt). Her eleman kök seviyesinde bir WaterDropObject bileşeni İÇERMELİDİR; içermeyen/null eleman sessizce atlanır (o slottaki havuz devre dışı kalır, hiçbir zaman fırlatmaz).")]
		[SerializeField]
		private GameObject[] _dropPrefabs;

		[SerializeField]
		private PooledVfx _splashVfxPrefab;

		[Header("Havuz Boyutları")]
		[SerializeField]
		[Min(0f)]
		private int _targetPrewarm = 3;

		[SerializeField]
		[Min(1f)]
		private int _targetCap = 8;

		[SerializeField]
		[Min(0f)]
		private int _dropPrewarmPerPrefab = 2;

		[SerializeField]
		[Min(1f)]
		private int _dropCapPerPrefab = 6;

		[SerializeField]
		[Min(0f)]
		private int _splashPrewarm = 3;

		[SerializeField]
		[Min(1f)]
		private int _splashCap = 8;

		private const int RadiusSalt = 1;

		private const int AngleSalt = 2;

		private const int PrefabSalt = 3;

		private TargetSlot[] _slots;

		private int _lastCycleProcessed;

		private ObstaclePool<WaterTarget> _markerPool;

		private ObstaclePool<WaterDropObject>[] _dropPools;

		private ObstaclePool<PooledVfx> _splashPool;

		protected override void Awake()
		{
			base.Awake();
			_slots = new TargetSlot[Mathf.Max(1, _maxSimultaneousTargets)];
			_lastCycleProcessed = -1;
			Transform parent = base.transform;
			_markerPool = new ObstaclePool<WaterTarget>(_targetPrefab, parent, _targetPrewarm, _targetCap);
			_splashPool = new ObstaclePool<PooledVfx>(_splashVfxPrefab, parent, _splashPrewarm, _splashCap);
			int num = ((_dropPrefabs != null) ? _dropPrefabs.Length : 0);
			_dropPools = new ObstaclePool<WaterDropObject>[num];
			for (int i = 0; i < num; i++)
			{
				GameObject gameObject = _dropPrefabs[i];
				WaterDropObject prefab = ((gameObject != null) ? gameObject.GetComponent<WaterDropObject>() : null);
				_dropPools[i] = new ObstaclePool<WaterDropObject>(prefab, parent, _dropPrewarmPerPrefab, _dropCapPerPrefab);
			}
		}

		protected override void OnTick(int tick, float dt, float time)
		{
			if (_slots != null && _slots.Length != 0)
			{
				ProcessAttackSchedule(time);
				UpdateSlots(time);
			}
		}

		private void ProcessAttackSchedule(float time)
		{
			float num = Mathf.Max(0.2f, _attackInterval);
			int num2 = Mathf.FloorToInt(time / num);
			if (num2 != _lastCycleProcessed)
			{
				_lastCycleProcessed = num2;
				int num3 = FindFreeSlot();
				if (num3 >= 0)
				{
					float num4 = (float)num2 * num;
					float num5 = Mathf.Max(0f, _warningDuration);
					float num6 = Mathf.Max(0.05f, _fallDuration);
					float num7 = num4 + num5;
					float landTime = num7 + num6;
					Vector3 vector = RandomArenaPoint(num2);
					WaterTarget marker = SpawnMarker(vector, num4, num7);
					int num8 = ((_dropPools != null) ? _dropPools.Length : 0);
					int prefabIndex = ((num8 > 0) ? RandInt(num2, 3, 0, num8) : 0);
					_slots[num3] = new TargetSlot
					{
						Active = true,
						DropSpawned = false,
						TargetPos = vector,
						DropStartTime = num7,
						LandTime = landTime,
						PrefabIndex = prefabIndex,
						Marker = marker,
						Visual = null
					};
				}
			}
		}

		private void UpdateSlots(float time)
		{
			for (int i = 0; i < _slots.Length; i++)
			{
				TargetSlot targetSlot = _slots[i];
				if (!targetSlot.Active)
				{
					continue;
				}
				targetSlot.Marker?.Tick(time);
				if (!targetSlot.DropSpawned && time >= targetSlot.DropStartTime)
				{
					targetSlot.Visual = SpawnDropVisual(targetSlot.TargetPos, targetSlot.PrefabIndex, targetSlot.DropStartTime, targetSlot.LandTime);
					targetSlot.DropSpawned = true;
				}
				targetSlot.Visual?.Tick(time);
				if (time >= targetSlot.LandTime)
				{
					TriggerSplash(targetSlot.TargetPos);
					if (targetSlot.Marker != null)
					{
						targetSlot.Marker.OwnerPool?.Return(targetSlot.Marker);
					}
					if (targetSlot.Visual != null)
					{
						targetSlot.Visual.OwnerPool?.Return(targetSlot.Visual);
					}
					_slots[i] = default;
				}
				else
				{
					_slots[i] = targetSlot;
				}
			}
		}

		private Vector3 RandomArenaPoint(int cycleIndex)
		{
			float f = RandRange(cycleIndex, 1, 0f, 1f);
			float num = RandRange(cycleIndex, 2, 0f, 1f);
			float num2 = _arenaRadius * Mathf.Sqrt(f);
			float f2 = num * MathF.PI * 2f;
			return base.transform.position + new Vector3(Mathf.Cos(f2) * num2, 0f, Mathf.Sin(f2) * num2);
		}

		private int FindFreeSlot()
		{
			for (int i = 0; i < _slots.Length; i++)
			{
				if (!_slots[i].Active)
				{
					return i;
				}
			}
			return -1;
		}

		private WaterTarget SpawnMarker(Vector3 pos, float warnStart, float holdEnd)
		{
			WaterTarget waterTarget = _markerPool.Get(pos, Quaternion.identity);
			if (waterTarget == null)
			{
				return null;
			}
			waterTarget.OwnerPool = _markerPool;
			waterTarget.Begin(pos, warnStart, holdEnd, _targetRadius);
			return waterTarget;
		}

		private WaterDropObject SpawnDropVisual(Vector3 targetPos, int prefabIndex, float startTime, float landTime)
		{
			int num = ((_dropPools != null) ? _dropPools.Length : 0);
			if (num == 0)
			{
				return null;
			}
			ObstaclePool<WaterDropObject> obstaclePool = _dropPools[Mathf.Clamp(prefabIndex, 0, num - 1)];
			if (obstaclePool == null)
			{
				return null;
			}
			Vector3 vector = targetPos + Vector3.up * Mathf.Max(0.1f, _dropHeight);
			WaterDropObject waterDropObject = obstaclePool.Get(vector, Quaternion.identity);
			if (waterDropObject == null)
			{
				return null;
			}
			waterDropObject.OwnerPool = obstaclePool;
			waterDropObject.BeginFall(vector, targetPos, startTime, landTime);
			return waterDropObject;
		}

		private void TriggerSplash(Vector3 pos)
		{
			PooledVfx pooledVfx = _splashPool.Get(pos, Quaternion.identity);
			if (pooledVfx != null)
			{
				pooledVfx.OwnerPool = _splashPool;
				pooledVfx.Play();
			}
		}

		private void OnDrawGizmosSelected()
		{
			Vector3 position = base.transform.position;
			DrawArenaOutline(position);
			Vector3 vector = position + Vector3.forward * (_arenaRadius * 0.5f);
			float num = Mathf.Max(0.1f, _dropHeight);
			PhysicsObstacleBase.GizmoSphere(vector, Mathf.Max(0.1f, _targetRadius), new Color(1f, 0.1f, 0.05f, 0.85f));
			PhysicsObstacleBase.GizmoArrow(vector + Vector3.up * num, Vector3.down * num, new Color(0.2f, 0.6f, 1f, 0.85f));
		}

		private void DrawArenaOutline(Vector3 center)
		{
			int num = Mathf.Max(3, _arenaGizmoSides);
			Color color = Gizmos.color;
			Gizmos.color = new Color(0.1f, 0.5f, 1f, 0.6f);
			Vector3 vector = center + new Vector3(_arenaRadius, 0f, 0f);
			for (int i = 1; i <= num; i++)
			{
				float f = (float)i / (float)num * MathF.PI * 2f;
				Vector3 vector2 = center + new Vector3(Mathf.Cos(f) * _arenaRadius, 0f, Mathf.Sin(f) * _arenaRadius);
				Gizmos.DrawLine(vector, vector2);
				vector = vector2;
			}
			Gizmos.color = color;
		}
	}
}
