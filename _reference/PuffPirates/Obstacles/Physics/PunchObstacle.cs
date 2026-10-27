using System.Collections.Generic;
using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	public sealed class PunchObstacle : PhysicsObstacleBase
	{
		public enum TriggerMode
		{
			Timed = 0,
			Detection = 1
		}

		private enum PunchPhase
		{
			Armed = 0,
			Extending = 1,
			Holding = 2,
			Retracting = 3,
			Recharging = 4
		}

		[Header("Tetikleme Modu")]
		[Tooltip("Timed: sabit(+jitter) aralıkla otomatik ateşler. Detection: NetworkBoat.All üzerinde mesafe taraması yapar, bir bot algılama yarıçapına girince ateşler.")]
		[SerializeField]
		private TriggerMode _triggerMode;

		[Header("Algılama (Detection modu)")]
		[Tooltip("Detection modunda: bu yarıçap içindeki HERHANGİ bir bot yumruğu tetikler. NetworkBoat.All (replike liste) taranır — host filtresi YOK, tüm makineler aynı replike konumdan aynı kararı bağımsız üretir. 0 = Detection modu etkisiz.")]
		[SerializeField]
		[Min(0f)]
		private float _detectionDistance = 6f;

		[Header("Zamanlı Tetikleme (Timed modu)")]
		[Tooltip("Armed (boşta/hazır) durumuna geçtikten sonra otomatik ateşlemeye kadar geçen süre (sn).")]
		[SerializeField]
		[Min(0.1f)]
		private float _waitTime = 3f;

		[Tooltip("waitTime'a eklenen deterministik rastgele sapma (±sn) — takvimi öngörülemez kılar. 0 = jitter yok.")]
		[SerializeField]
		[Min(0f)]
		private float _waitJitter = 0.5f;

		[Header("Yumruk Hareketi")]
		[Tooltip("Yumruğun ileri/geri hareket hızı (birim/sn) — hem çıkış hem geri çekiliş AYNI hızla.")]
		[SerializeField]
		[Min(0.1f)]
		private float _punchSpeed = 14f;

		[Tooltip("Tam çıkışta kutudan ne kadar dışarı uzanacağı (dünya birimi, transform.forward yönünde).")]
		[SerializeField]
		[Min(0.1f)]
		private float _range = 3f;

		[Tooltip("Tam uzanmış konumda geri çekilmeye başlamadan önce ne kadar beklendiği (sn).")]
		[SerializeField]
		[Min(0f)]
		private float _holdSeconds = 0.12f;

		[Header("Kuvvet (Host-only, bkz. PunchHead.cs)")]
		[Tooltip("Extending fazında temas eden bota uygulanan itki büyüklüğü.")]
		[SerializeField]
		[Min(0f)]
		private float _force = 22f;

		[Tooltip("Açık: itki mesafeyle azalır (kutudan uzaklaştıkça zayıflar). Kapalı (varsayılan): mesafeden bağımsız sabit itki.")]
		[SerializeField]
		private bool _forceDistanceFalloff;

		[Tooltip("Mesafe-azalması yarıçapı. <=0 ise _range kullanılır.")]
		[SerializeField]
		private float _falloffRange = -1f;

		[Header("Yeniden Kullanım")]
		[Tooltip("Retract tamamlandıktan sonra tekrar ateşlenebilir (Armed) olana kadar geçen süre (sn) — hem Timed hem Detection modunda geçerli TEK ortak kilit (spam-tetiklemeyi önler).")]
		[SerializeField]
		[Min(0f)]
		private float _rechargeTime = 1.5f;

		[Header("Referanslar")]
		[Tooltip("Çocuk yumruk-başı GameObject'inin KİNEMATİK Rigidbody'si. MovePosition ile buradan taşınır.")]
		[SerializeField]
		private Rigidbody _headRigidbody;

		[Tooltip("Yalnız gizmo önizlemesi için — kutu gövdesinin BoxCollider'ı (opsiyonel, boşsa kutu sınırı çizilmez).")]
		[SerializeField]
		private BoxCollider _boxBoundsGizmo;

		[Header("VFX/Ses (opsiyonel)")]
		[Tooltip("Her ateşlemede (Armed→Extending geçişinde) HER makinede oynatılan havuzlanmış VFX. Boşsa atlanır.")]
		[SerializeField]
		private PooledVfx _punchVfxPrefab;

		[SerializeField]
		[Min(0f)]
		private int _vfxPrewarm = 2;

		[SerializeField]
		[Min(1f)]
		private int _vfxCap = 6;

		private PunchPhase _phase;

		private float _cycleAnchorTime;

		private float _armedSinceTime;

		private int _cycleIndex;

		private float _currentExtension;

		private Vector3 _headOrigin;

		private ObstaclePool<PooledVfx> _vfxPool;

		private const int WaitJitterSalt = 1;

		public bool IsExtending => _phase == PunchPhase.Extending;

		public float Force => _force;

		public Vector3 PushDirection => base.transform.forward;

		protected override void Awake()
		{
			base.Awake();
			if (_headRigidbody != null)
			{
				_headRigidbody.isKinematic = true;
				_headRigidbody.useGravity = false;
				_headRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
				_headRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
				_headOrigin = _headRigidbody.position;
			}
			_phase = PunchPhase.Armed;
			_armedSinceTime = 0f;
			_cycleAnchorTime = 0f;
			_cycleIndex = 0;
			_currentExtension = 0f;
			_vfxPool = new ObstaclePool<PooledVfx>(_punchVfxPrefab, base.transform, _vfxPrewarm, _vfxCap);
		}

		public float ComputeForceScale(Vector3 boatWorldPosition)
		{
			if (!_forceDistanceFalloff)
			{
				return 1f;
			}
			float b = ((_falloffRange > 0.01f) ? _falloffRange : _range);
			float num = Vector3.Distance(base.transform.position, boatWorldPosition);
			return Mathf.Clamp01(1f - num / Mathf.Max(0.01f, b));
		}

		protected override void OnTick(int tick, float dt, float time)
		{
			if (_phase == PunchPhase.Armed)
			{
				if (!TryEvaluateTrigger(tick, time, out var fireTime))
				{
					SetHeadExtension(0f);
					return;
				}
				_cycleAnchorTime = fireTime;
				_cycleIndex++;
				PlayFireCues();
			}
			AdvanceFromAnchor(tick, time);
		}

		private bool TryEvaluateTrigger(int tick, float time, out float fireTime)
		{
			TriggerMode triggerMode = _triggerMode;
			if (triggerMode != TriggerMode.Timed && triggerMode == TriggerMode.Detection)
			{
				fireTime = time;
				return IsBoatWithinDetectionRange();
			}
			float num = ((_waitJitter > 0f) ? RandRange(Mathf.FloorToInt(_armedSinceTime * 1000f), 1, 0f - _waitJitter, _waitJitter) : 0f);
			return time >= (fireTime = _armedSinceTime + _waitTime + num);
		}

		private bool IsBoatWithinDetectionRange()
		{
			if (_detectionDistance <= 0f)
			{
				return false;
			}
			List<NetworkBoat> all = NetworkBoat.All;
			float num = _detectionDistance * _detectionDistance;
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

		private void AdvanceFromAnchor(int tick, float time)
		{
			float num = time - _cycleAnchorTime;
			float num2 = _range / Mathf.Max(0.01f, _punchSpeed);
			float num3 = num2;
			float num4 = num3 + _holdSeconds;
			float num5 = num4 + num2;
			float num6 = num5 + _rechargeTime;
			float value;
			if (num < num3)
			{
				_phase = PunchPhase.Extending;
				value = num * _punchSpeed;
			}
			else if (num < num4)
			{
				_phase = PunchPhase.Holding;
				value = _range;
			}
			else if (num < num5)
			{
				_phase = PunchPhase.Retracting;
				value = _range - (num - num4) * _punchSpeed;
			}
			else if (num < num6)
			{
				_phase = PunchPhase.Recharging;
				value = 0f;
			}
			else
			{
				_phase = PunchPhase.Armed;
				_armedSinceTime = _cycleAnchorTime + num6;
				value = 0f;
			}
			SetHeadExtension(Mathf.Clamp(value, 0f, _range));
		}

		private void SetHeadExtension(float distance)
		{
			_currentExtension = distance;
			if (!(_headRigidbody == null))
			{
				Vector3 position = _headOrigin + base.transform.forward * distance;
				_headRigidbody.MovePosition(position);
			}
		}

		private void PlayFireCues()
		{
			Vector3 pos = ((_headRigidbody != null) ? _headRigidbody.position : base.transform.position);
			if (_punchVfxPrefab != null)
			{
				PooledVfx pooledVfx = _vfxPool.Get(pos, base.transform.rotation);
				if (pooledVfx != null)
				{
					pooledVfx.OwnerPool = _vfxPool;
					pooledVfx.Play();
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			Vector3 position = base.transform.position;
			PhysicsObstacleBase.GizmoSphere(position, _detectionDistance, new Color(0.2f, 0.85f, 1f, 0.65f));
			PhysicsObstacleBase.GizmoArrow((_headRigidbody != null) ? _headRigidbody.transform.position : position, base.transform.forward * _range, new Color(1f, 0.2f, 0.2f, 0.9f));
			if (_boxBoundsGizmo != null)
			{
				Color color = Gizmos.color;
				Matrix4x4 matrix = Gizmos.matrix;
				Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.4f);
				Gizmos.matrix = _boxBoundsGizmo.transform.localToWorldMatrix;
				Gizmos.DrawWireCube(_boxBoundsGizmo.center, _boxBoundsGizmo.size);
				Gizmos.matrix = matrix;
				Gizmos.color = color;
			}
		}
	}
}
