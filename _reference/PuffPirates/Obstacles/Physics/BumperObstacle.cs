using System;
using System.Collections.Generic;
using PuffPirates.Boats;
using PuffPirates.CameraRig;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	public sealed class BumperObstacle : PhysicsObstacleBase
	{
		public enum BumperMode
		{
			OneShot = 0,
			Rebuildable = 1
		}

		private enum BumperPhase
		{
			Armed = 0,
			Expanding = 1,
			Open = 2,
			Rebuilding = 3,
			Spent = 4
		}

		[Header("Algılama (HER makinede, NetworkBoat.All üzerinde)")]
		[Tooltip("Bu yarıçap içinde HERHANGİ bir bot varsa tampon aniden açılır. NetworkBoat.All (replike liste) taranır — host filtresi YOK, tüm makineler aynı replike konumdan aynı kararı bağımsız üretir. 0 = algılama etkisiz.")]
		[SerializeField]
		[Min(0f)]
		private float _detectionRadius = 8f;

		[Header("Etki Alanı (Host-only kuvvet)")]
		[Tooltip("Ateşleme anında QueryHostBoats bu yarıçap içindeki TÜM host-otorite botları toplar — yalnız ilki değil, hepsi işlenir. Algılama yarıçapından bağımsız bir değerdir (daha küçük/büyük olabilir).")]
		[SerializeField]
		[Min(0.5f)]
		private float _impactRadius = 5f;

		[Header("Kuvvet (Host-only, ApplyRadialImpulse)")]
		[Tooltip("Merkezden dışarı yatay itki büyüklüğü — mesafeyle azalır (spec gereği her zaman açık).")]
		[SerializeField]
		[Min(0f)]
		private float _horizontalForce = 18f;

		[Tooltip("Ek dikey (yukarı) itki büyüklüğü — bu da mesafeyle azalır.")]
		[SerializeField]
		[Min(0f)]
		private float _upwardForce = 8f;

		[Tooltip("Yatay itkinin yukarı doğru açısı (derece) — spec: ~30°.")]
		[SerializeField]
		[Range(0f, 90f)]
		private float _upwardAngleDeg = 30f;

		[Header("Mod ve Zamanlama")]
		[Tooltip("OneShot: bir kez açılır, kalıcı olarak açık kalır (Spent), bir daha algılama yapmaz. Rebuildable: açılır, cooldown+rebuildTime sonunda kendini kapatıp yeniden kurar.")]
		[SerializeField]
		private BumperMode _mode = BumperMode.Rebuildable;

		[Tooltip("Direklerin kapalıdan açığa 'pop' etme süresi (sn) — patlama-benzeri anlık bir açılış için küçük tutulur.")]
		[SerializeField]
		[Min(0.02f)]
		private float _expandDuration = 0.18f;

		[Tooltip("SADECE Rebuildable modda: tam açık kaldıktan sonra kapanmaya (Rebuilding) başlamadan önce geçen süre (sn) — ani yeniden-tetiklemeyi önleyen asıl kilit budur.")]
		[SerializeField]
		[Min(0f)]
		private float _cooldown = 1f;

		[Tooltip("SADECE Rebuildable modda: direklerin açıktan kapalıya geri 'pop' etme (kapanma) süresi (sn) — bittiğinde tekrar Armed olur.")]
		[SerializeField]
		[Min(0.05f)]
		private float _rebuildTime = 1.5f;

		[Header("Direkler (Postlar)")]
		[Tooltip("Prefab'ta AÇIK (tetiklenmiş) yerleşimiyle konumlandırılmış lastik-direk transform'ları. Kapalı hâl, Awake'te bu açık konumlardan merkeze doğru (Closed Radius Ratio ile) ve ölçekçe (Closed Scale Ratio ile) OTOMATİK türetilir — tasarımcı yalnız 'açık' hâli sahnede/prefab'ta elle düzenler, 'kapalı' hâli script hesaplar.")]
		[SerializeField]
		private Transform[] _posts;

		[Tooltip("Kapalı hâlde her direğin, kendi açık yerel XZ mesafesinin ne kadarında duracağı (0=merkeze yapışık, 1=hiç fark yok). Merkezdeki (XZ'de sıfır konumlu) bir direk yalnız ölçekle küçülür.")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _closedRadiusRatio = 0.35f;

		[Tooltip("Kapalı hâlde her direğin, kendi açık yerel ölçeğinin ne kadarına küçüleceği.")]
		[SerializeField]
		[Range(0.05f, 1f)]
		private float _closedScaleRatio = 0.4f;

		[Header("VFX/Ses (opsiyonel, HER makinede — zamanlaması zaten deterministik)")]
		[Tooltip("Her açılışta (Armed→Expanding geçişinde) HER makinede oynatılan havuzlanmış patlama-tarzı VFX. Boşsa atlanır.")]
		[SerializeField]
		private PooledVfx _expandVfxPrefab;

		[SerializeField]
		[Min(0f)]
		private int _vfxPrewarm = 2;

		[SerializeField]
		[Min(1f)]
		private int _vfxCap = 6;

		[Tooltip("Her açılışta BoatCameraRig.Shake'e verilen trauma (0..1). 0 = sarsıntı yok.")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _expandShakeTrauma = 0.5f;

		private BumperPhase _phase;

		private float _cycleAnchorTime;

		private int _cycleIndex;

		private float _currentPose;

		private Vector3[] _postOpenLocalPos;

		private Vector3[] _postClosedLocalPos;

		private Vector3[] _postOpenLocalScale;

		private Vector3[] _postClosedLocalScale;

		private readonly List<NetworkBoat> _boatBuffer = new List<NetworkBoat>(16);

		private ObstaclePool<PooledVfx> _vfxPool;

		protected override void Awake()
		{
			base.Awake();
			_phase = BumperPhase.Armed;
			_cycleAnchorTime = 0f;
			_cycleIndex = 0;
			_currentPose = 0f;
			CachePostPoses();
			ApplyPostPose(0f);
			_vfxPool = new ObstaclePool<PooledVfx>(_expandVfxPrefab, base.transform, _vfxPrewarm, _vfxCap);
		}

		private void CachePostPoses()
		{
			int num = ((_posts != null) ? _posts.Length : 0);
			_postOpenLocalPos = new Vector3[num];
			_postClosedLocalPos = new Vector3[num];
			_postOpenLocalScale = new Vector3[num];
			_postClosedLocalScale = new Vector3[num];
			for (int i = 0; i < num; i++)
			{
				Transform transform = _posts[i];
				if (!(transform == null))
				{
					Vector3 localPosition = transform.localPosition;
					_postOpenLocalPos[i] = localPosition;
					Vector3 vector = new Vector3(localPosition.x, 0f, localPosition.z);
					Vector3 vector2 = ((vector.sqrMagnitude > 0.0001f) ? (vector * _closedRadiusRatio) : vector);
					_postClosedLocalPos[i] = new Vector3(vector2.x, localPosition.y, vector2.z);
					_postOpenLocalScale[i] = transform.localScale;
					_postClosedLocalScale[i] = transform.localScale * _closedScaleRatio;
				}
			}
		}

		protected override void OnTick(int tick, float dt, float time)
		{
			if (_phase == BumperPhase.Armed)
			{
				if (!IsBoatWithinDetectionRange())
				{
					ApplyPostPose(0f);
					return;
				}
				_cycleAnchorTime = time;
				_cycleIndex++;
				PlayTriggerCues();
				FireImpulse();
			}
			AdvanceFromAnchor(time);
		}

		private bool IsBoatWithinDetectionRange()
		{
			if (_detectionRadius <= 0f)
			{
				return false;
			}
			List<NetworkBoat> all = NetworkBoat.All;
			float num = _detectionRadius * _detectionRadius;
			Vector3 position = base.transform.position;
			for (int i = 0; i < all.Count; i++)
			{
				NetworkBoat networkBoat = all[i];
				if (!(networkBoat == null) && (networkBoat.transform.position - position).sqrMagnitude <= num)
				{
					return true;
				}
			}
			return false;
		}

		private void FireImpulse()
		{
			Vector3 position = base.transform.position;
			int num = QueryHostBoats(position, _impactRadius, _boatBuffer);
			for (int i = 0; i < num; i++)
			{
				PhysicsObstacleBase.ApplyRadialImpulse(_boatBuffer[i], position, _horizontalForce, _upwardForce, _upwardAngleDeg, distanceFalloff: true, _impactRadius);
			}
		}

		private void PlayTriggerCues()
		{
			Vector3 position = base.transform.position;
			if (_expandVfxPrefab != null)
			{
				PooledVfx pooledVfx = _vfxPool.Get(position, base.transform.rotation);
				if (pooledVfx != null)
				{
					pooledVfx.OwnerPool = _vfxPool;
					pooledVfx.Play();
				}
			}
			if (_expandShakeTrauma > 0f)
			{
				BoatCameraRig.Shake(_expandShakeTrauma);
			}
		}

		private void AdvanceFromAnchor(float time)
		{
			float num = time - _cycleAnchorTime;
			float expandDuration = _expandDuration;
			float num2;
			if (num < expandDuration)
			{
				_phase = BumperPhase.Expanding;
				num2 = Mathf.Clamp01(num / Mathf.Max(0.0001f, _expandDuration));
			}
			else if (_mode == BumperMode.OneShot)
			{
				_phase = BumperPhase.Spent;
				num2 = 1f;
			}
			else
			{
				float num3 = expandDuration + _cooldown;
				float num4 = num3 + _rebuildTime;
				if (num < num3)
				{
					_phase = BumperPhase.Open;
					num2 = 1f;
				}
				else if (num < num4)
				{
					_phase = BumperPhase.Rebuilding;
					num2 = 1f - Mathf.Clamp01((num - num3) / Mathf.Max(0.0001f, _rebuildTime));
				}
				else
				{
					_phase = BumperPhase.Armed;
					num2 = 0f;
				}
			}
			_currentPose = num2;
			ApplyPostPose(num2);
		}

		private void ApplyPostPose(float t01)
		{
			if (_posts == null)
			{
				return;
			}
			for (int i = 0; i < _posts.Length; i++)
			{
				Transform transform = _posts[i];
				if (!(transform == null))
				{
					transform.localPosition = Vector3.Lerp(_postClosedLocalPos[i], _postOpenLocalPos[i], t01);
					transform.localScale = Vector3.Lerp(_postClosedLocalScale[i], _postOpenLocalScale[i], t01);
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			Vector3 position = base.transform.position;
			PhysicsObstacleBase.GizmoSphere(position, _detectionRadius, new Color(0.2f, 0.9f, 1f, 0.65f));
			PhysicsObstacleBase.GizmoSphere(position, _impactRadius, new Color(1f, 0.35f, 0.15f, 0.85f));
			PhysicsObstacleBase.GizmoSphere(position, 0.15f, Color.white);
			float num = Mathf.Max(0.5f, _impactRadius * 0.4f);
			float num2 = Mathf.Min(_impactRadius, 1f);
			for (int i = 0; i < 8; i++)
			{
				float f = (float)i * 45f * (MathF.PI / 180f);
				Vector3 vector = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
				Vector3 vector2 = Vector3.Cross(Vector3.up, vector);
				if (vector2.sqrMagnitude < 0.0001f)
				{
					vector2 = Vector3.right;
				}
				Vector3 vector3 = Quaternion.AngleAxis(0f - _upwardAngleDeg, vector2.normalized) * vector;
				PhysicsObstacleBase.GizmoArrow(position + vector * num2, vector3 * num, new Color(1f, 0.85f, 0.1f, 0.9f));
			}
		}
	}
}
