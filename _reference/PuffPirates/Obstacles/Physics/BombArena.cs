using System;
using System.Collections.Generic;
using PuffPirates.Boats;
using PuffPirates.CameraRig;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	public sealed class BombArena : PhysicsObstacleBase
	{
		private struct ActiveBomb
		{
			public BombProjectile Visual;

			public Vector3 TargetPos;

			public float LandTime;
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

		[Header("Toplar")]
		[Tooltip("Elle yerleştirilmiş top transform'ları (opsiyonel). Boşsa aşağıdaki sayı+yarıçapla otomatik, eşit aralıklı yerleştirilir.")]
		[SerializeField]
		private Transform[] _cannonTransforms;

		[Tooltip("_cannonTransforms boşsa kaç top otomatik yerleştirilsin.")]
		[SerializeField]
		[Min(1f)]
		private int _cannonCount = 4;

		[Tooltip("Otomatik yerleşimde topların arena merkezinden uzaklığı. <=0 ise _arenaRadius kullanılır.")]
		[SerializeField]
		private float _cannonRingRadius = -1f;

		[Tooltip("Otomatik yerleşimde topların yerden yüksekliği.")]
		[SerializeField]
		[Min(0f)]
		private float _cannonHeight = 2.5f;

		[Header("Ateşleme Zamanlaması")]
		[Tooltip("Her topun ortalama ateşleme aralığı (sn). Toplar bu aralık içinde birbirinden faz-kaydırılmış başlar.")]
		[SerializeField]
		[Min(1f)]
		private float _fireInterval = 6f;

		[Tooltip("Ateşleme anına eklenen deterministik rastgele sapma (±sn) — takvimi öngörülemez kılar.")]
		[SerializeField]
		[Min(0f)]
		private float _fireJitter = 1.5f;

		[Header("Bomba Uçuşu")]
		[Tooltip("Bombanın iniş süresi (sn) — hem parabolün hem zemin-uyarısının ortak 'iniş anı'nı belirleyen ASIL zamanlayıcı.")]
		[SerializeField]
		[Min(0.2f)]
		private float _flightTime = 1.6f;

		[Tooltip("Parabol tepe yüksekliği (dünya birimi).")]
		[SerializeField]
		[Min(0.1f)]
		private float _bombArcHeight = 6f;

		[Tooltip("Bomba görselinin kendi ekseni etrafında dönüş hızı (derece/sn) — 'bomb speed' okunabilirlik ipucu, uçuş SÜRESİNİ etkilemez (bkz. BombProjectile.BeginFlight).")]
		[SerializeField]
		[Min(0f)]
		private float _bombSpinSpeed = 260f;

		[Header("Zemin Uyarısı")]
		[Tooltip("Bomba inmeden önce zemin uyarısının görünür/koyulaşır olduğu süre (sn) — iniş anında tam koyu/büyük olur.")]
		[SerializeField]
		[Min(0.2f)]
		private float _warningDuration = 2.2f;

		[Tooltip("Patlamadan sonra zeminin eski (görünmez) haline dönmesi için geçen süre (sn).")]
		[SerializeField]
		[Min(0.1f)]
		private float _groundRestoreDuration = 1f;

		[Tooltip("Uyarı dekalının yarıçapı (dünya birimi).")]
		[SerializeField]
		[Min(0.2f)]
		private float _warningRadius = 3f;

		[Header("Patlama")]
		[Tooltip("Fiziksel itkinin uygulandığı yarıçap — bu mesafenin dışındaki botlar etkilenmez.")]
		[SerializeField]
		[Min(0.5f)]
		private float _explosionRadius = 6f;

		[Tooltip("Merkezdeki yatay itki büyüklüğü (mesafeyle azalır — distanceFalloff=true).")]
		[SerializeField]
		[Min(0f)]
		private float _explosionHorizontalForce = 14f;

		[Tooltip("Dikey (yukarı) itki büyüklüğü (mesafeyle azalır).")]
		[SerializeField]
		[Min(0f)]
		private float _explosionUpwardForce = 6f;

		[Tooltip("Yatay itkinin yukarı doğru açısı (derece).")]
		[SerializeField]
		[Range(0f, 90f)]
		private float _explosionUpwardAngle = 35f;

		[Tooltip("Patlama VFX/ses'i BİR ÖRNEK için kamera sarsıntısı (0..1 trauma) — bkz. BoatCameraRig.Shake.")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _explosionShakeTrauma = 0.45f;

		[Tooltip("Patlama sesinin oynatma seviyesi (0..1).")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _explosionSfxVolume = 0.9f;

		[Tooltip("Patlama partikülünün rengi — MaterialPropertyBlock ile her patlamada VFX prefabının ParticleSystemRenderer'ına uygulanır (prefabın kendi materyal rengini BU alan geçersiz kılar).")]
		[SerializeField]
		private Color _explosionTint = new Color(1f, 0.55f, 0.15f, 1f);

		[Header("Havuzlanan Prefab'lar")]
		[SerializeField]
		private BombProjectile _bombPrefab;

		[SerializeField]
		private BombWarningDecal _warningPrefab;

		[SerializeField]
		private PooledVfx _explosionVfxPrefab;

		[Header("Havuz Boyutları")]
		[SerializeField]
		[Min(0f)]
		private int _bombPrewarm = 4;

		[SerializeField]
		[Min(1f)]
		private int _bombCap = 12;

		[SerializeField]
		[Min(0f)]
		private int _warningPrewarm = 4;

		[SerializeField]
		[Min(1f)]
		private int _warningCap = 12;

		[SerializeField]
		[Min(0f)]
		private int _explosionPrewarm = 4;

		[SerializeField]
		[Min(1f)]
		private int _explosionCap = 12;

		private Vector3[] _cannonPositions;

		private int[] _lastWarnCycle;

		private int[] _lastBombCycle;

		private ObstaclePool<BombProjectile> _bombPool;

		private ObstaclePool<BombWarningDecal> _warningPool;

		private ObstaclePool<PooledVfx> _explosionPool;

		private readonly List<ActiveBomb> _activeBombs = new List<ActiveBomb>(8);

		private readonly List<BombWarningDecal> _activeWarnings = new List<BombWarningDecal>(8);

		private readonly List<NetworkBoat> _boatBuffer = new List<NetworkBoat>(16);

		private MaterialPropertyBlock _explosionMpb;

		private static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");

		protected override void Awake()
		{
			base.Awake();
			BuildCannonLayout();
			_explosionMpb = new MaterialPropertyBlock();
			Transform parent = base.transform;
			_bombPool = new ObstaclePool<BombProjectile>(_bombPrefab, parent, _bombPrewarm, _bombCap);
			_warningPool = new ObstaclePool<BombWarningDecal>(_warningPrefab, parent, _warningPrewarm, _warningCap);
			_explosionPool = new ObstaclePool<PooledVfx>(_explosionVfxPrefab, parent, _explosionPrewarm, _explosionCap);
		}

		private void BuildCannonLayout()
		{
			int num;
			if (_cannonTransforms != null && _cannonTransforms.Length != 0)
			{
				num = _cannonTransforms.Length;
				_cannonPositions = new Vector3[num];
				for (int i = 0; i < num; i++)
				{
					Transform transform = _cannonTransforms[i];
					_cannonPositions[i] = ((transform != null) ? transform.position : base.transform.position);
				}
			}
			else
			{
				num = Mathf.Max(1, _cannonCount);
				_cannonPositions = new Vector3[num];
				float num2 = ((_cannonRingRadius > 0f) ? _cannonRingRadius : _arenaRadius);
				Vector3 vector = base.transform.position + Vector3.up * _cannonHeight;
				for (int j = 0; j < num; j++)
				{
					float f = (float)j / (float)num * MathF.PI * 2f;
					_cannonPositions[j] = vector + new Vector3(Mathf.Cos(f) * num2, 0f, Mathf.Sin(f) * num2);
				}
			}
			_lastWarnCycle = new int[num];
			_lastBombCycle = new int[num];
			for (int k = 0; k < num; k++)
			{
				_lastWarnCycle[k] = -1;
				_lastBombCycle[k] = -1;
			}
		}

		protected override void OnTick(int tick, float dt, float time)
		{
			if (_cannonPositions != null && _cannonPositions.Length != 0)
			{
				for (int i = 0; i < _cannonPositions.Length; i++)
				{
					ProcessCannon(i, time);
				}
				UpdateActiveBombs(time);
				UpdateActiveWarnings(time);
			}
		}

		private void ProcessCannon(int cannonIndex, float time)
		{
			float num = Mathf.Max(0.5f, _fireInterval);
			int num2 = _cannonPositions.Length;
			float num3 = ((num2 > 0) ? ((float)cannonIndex * (num / (float)num2)) : 0f);
			float num4 = time - num3;
			if (num4 < 0f)
			{
				return;
			}
			int num5 = Mathf.FloorToInt(num4 / num);
			float num6 = num3 + (float)num5 * num;
			float num7 = RandRange(num5, JitterSalt(cannonIndex), 0f - _fireJitter, _fireJitter);
			float num8 = num * 0.5f;
			float num9 = num6 + Mathf.Clamp(num8 + num7, 0.05f, num - 0.05f);
			if (!(time < num9))
			{
				float num10 = Mathf.Max(_flightTime, _warningDuration);
				float num11 = num9 + num10;
				float num12 = num11 - _flightTime;
				float num13 = num11 - _warningDuration;
				if (_lastWarnCycle[cannonIndex] != num5 && time >= num13)
				{
					_lastWarnCycle[cannonIndex] = num5;
					Vector3 target = RandomArenaPoint(num5, cannonIndex);
					SpawnWarning(target, num13, num11);
				}
				if (_lastBombCycle[cannonIndex] != num5 && time >= num12)
				{
					_lastBombCycle[cannonIndex] = num5;
					Vector3 target2 = RandomArenaPoint(num5, cannonIndex);
					SpawnBomb(_cannonPositions[cannonIndex], target2, num12, num11);
				}
			}
		}

		private Vector3 RandomArenaPoint(int cycleIndex, int cannonIndex)
		{
			float f = RandRange(cycleIndex, TargetRadiusSalt(cannonIndex), 0f, 1f);
			float num = RandRange(cycleIndex, TargetAngleSalt(cannonIndex), 0f, 1f);
			float num2 = _arenaRadius * Mathf.Sqrt(f);
			float f2 = num * MathF.PI * 2f;
			return base.transform.position + new Vector3(Mathf.Cos(f2) * num2, 0f, Mathf.Sin(f2) * num2);
		}

		private static int JitterSalt(int cannonIndex)
		{
			return cannonIndex * 100 + 1;
		}

		private static int TargetAngleSalt(int cannonIndex)
		{
			return cannonIndex * 100 + 2;
		}

		private static int TargetRadiusSalt(int cannonIndex)
		{
			return cannonIndex * 100 + 3;
		}

		private void SpawnBomb(Vector3 launchPos, Vector3 target, float launchTime, float landTime)
		{
			BombProjectile bombProjectile = _bombPool.Get(launchPos, Quaternion.identity);
			if (bombProjectile != null)
			{
				bombProjectile.OwnerPool = _bombPool;
				bombProjectile.BeginFlight(launchPos, target, launchTime, landTime, _bombArcHeight, _bombSpinSpeed);
			}
			_activeBombs.Add(new ActiveBomb
			{
				Visual = bombProjectile,
				TargetPos = target,
				LandTime = landTime
			});
		}

		private void SpawnWarning(Vector3 target, float warnStart, float landingTime)
		{
			BombWarningDecal bombWarningDecal = _warningPool.Get(target, Quaternion.identity);
			if (!(bombWarningDecal == null))
			{
				bombWarningDecal.OwnerPool = _warningPool;
				bombWarningDecal.Begin(target, warnStart, landingTime, landingTime + _groundRestoreDuration, _warningRadius);
				_activeWarnings.Add(bombWarningDecal);
			}
		}

		private void UpdateActiveBombs(float time)
		{
			for (int num = _activeBombs.Count - 1; num >= 0; num--)
			{
				ActiveBomb activeBomb = _activeBombs[num];
				if (time >= activeBomb.LandTime)
				{
					TriggerExplosion(activeBomb.TargetPos);
					if (activeBomb.Visual != null)
					{
						_bombPool.Return(activeBomb.Visual);
					}
					_activeBombs.RemoveAt(num);
				}
				else
				{
					activeBomb.Visual?.Tick(time);
				}
			}
		}

		private void UpdateActiveWarnings(float time)
		{
			for (int num = _activeWarnings.Count - 1; num >= 0; num--)
			{
				BombWarningDecal bombWarningDecal = _activeWarnings[num];
				if (bombWarningDecal == null || bombWarningDecal.Tick(time))
				{
					if (bombWarningDecal != null)
					{
						_warningPool.Return(bombWarningDecal);
					}
					_activeWarnings.RemoveAt(num);
				}
			}
		}

		private void TriggerExplosion(Vector3 pos)
		{
			PooledVfx pooledVfx = _explosionPool.Get(pos, Quaternion.identity);
			if (pooledVfx != null)
			{
				pooledVfx.OwnerPool = _explosionPool;
				ApplyExplosionTint(pooledVfx);
				pooledVfx.Play();
			}
			BoatCameraRig.Shake(_explosionShakeTrauma);
			int num = QueryHostBoats(pos, _explosionRadius, _boatBuffer);
			for (int i = 0; i < num; i++)
			{
				PhysicsObstacleBase.ApplyRadialImpulse(_boatBuffer[i], pos, _explosionHorizontalForce, _explosionUpwardForce, _explosionUpwardAngle, distanceFalloff: true, _explosionRadius);
			}
		}

		private void ApplyExplosionTint(PooledVfx vfx)
		{
			ParticleSystemRenderer componentInChildren = vfx.GetComponentInChildren<ParticleSystemRenderer>(includeInactive: true);
			if (!(componentInChildren == null))
			{
				componentInChildren.GetPropertyBlock(_explosionMpb);
				_explosionMpb.SetColor(BaseColorPropId, _explosionTint);
				componentInChildren.SetPropertyBlock(_explosionMpb);
			}
		}

		private void OnDrawGizmosSelected()
		{
			Vector3 position = base.transform.position;
			DrawArenaOutline(position);
			PhysicsObstacleBase.GizmoSphere(position, _explosionRadius, new Color(1f, 0.2f, 0.1f, 0.8f));
			Vector3[] array = (Application.isPlaying ? _cannonPositions : PreviewCannonPositions());
			if (array != null)
			{
				foreach (Vector3 vector in array)
				{
					PhysicsObstacleBase.GizmoSphere(vector, 0.4f, Color.yellow);
					PhysicsObstacleBase.GizmoArrow(vector, (position - vector).normalized * 2f, Color.red);
				}
			}
		}

		private void DrawArenaOutline(Vector3 center)
		{
			int num = Mathf.Max(3, _arenaGizmoSides);
			Color color = Gizmos.color;
			Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
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

		private Vector3[] PreviewCannonPositions()
		{
			if (_cannonTransforms != null && _cannonTransforms.Length != 0)
			{
				Vector3[] array = new Vector3[_cannonTransforms.Length];
				for (int i = 0; i < array.Length; i++)
				{
					Transform transform = _cannonTransforms[i];
					array[i] = ((transform != null) ? transform.position : base.transform.position);
				}
				return array;
			}
			int num = Mathf.Max(1, _cannonCount);
			Vector3[] array2 = new Vector3[num];
			float num2 = ((_cannonRingRadius > 0f) ? _cannonRingRadius : _arenaRadius);
			Vector3 vector = base.transform.position + Vector3.up * _cannonHeight;
			for (int j = 0; j < num; j++)
			{
				float f = (float)j / (float)num * MathF.PI * 2f;
				array2[j] = vector + new Vector3(Mathf.Cos(f) * num2, 0f, Mathf.Sin(f) * num2);
			}
			return array2;
		}
	}
}
