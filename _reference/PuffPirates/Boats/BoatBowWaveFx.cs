using PuffPirates.Water;
using UnityEngine;

namespace PuffPirates.Boats
{
	[RequireComponent(typeof(NetworkBoat))]
	[DisallowMultipleComponent]
	public sealed class BoatBowWaveFx : MonoBehaviour
	{
		[Header("VFX")]
		[Tooltip("Pruvada doğan su/hit efekti (ör. AllIn1 Water Splash). Boşsa pasif.")]
		[SerializeField]
		private GameObject _bowVfxPrefab;

		[SerializeField]
		private float _vfxScale = 0.6f;

		[SerializeField]
		[Min(0.2f)]
		private float _vfxLifetime = 1.2f;

		[Tooltip("VFX havuzu üst sınırı — bu kadar örnek yeniden kullanılır; dolunca Instantiate+Destroy'a düşer.")]
		[SerializeField]
		[Min(1f)]
		private int _poolMax = 16;

		[Header("Pruva Konumu (yerel)")]
		[Tooltip("Gövde alt yüzeyinin yerel Y'si (ride ile aynı: -0.5).")]
		[SerializeField]
		private float _hullBottomLocalY = -0.5f;

		[Tooltip("Pruvanın (ön) yerel Z'si — gövdenin ÖN ucu.")]
		[SerializeField]
		private float _bowForwardLocal = 0.5f;

		[Header("Hız → Sıklık")]
		[Tooltip("Bu ileri hızın (m/sn) altında dalga YOK.")]
		[SerializeField]
		[Min(0f)]
		private float _minSpeed = 3f;

		[Tooltip("Bu hızda en sık spawn.")]
		[SerializeField]
		[Min(0.1f)]
		private float _fullSpeed = 14f;

		[Tooltip("Tam hızda spawn aralığı (sn).")]
		[SerializeField]
		[Min(0.03f)]
		private float _minInterval = 0.12f;

		[Tooltip("Min hızda spawn aralığı (sn).")]
		[SerializeField]
		[Min(0.1f)]
		private float _maxInterval = 0.4f;

		[Header("Su Teması")]
		[SerializeField]
		private LayerMask _surfaceMask = -1;

		[SerializeField]
		[Min(0.5f)]
		private float _probeAbove = 2f;

		[Tooltip("Pruva yüzeyden bu kadar uzaktaysa (m) dalga YOK (havada).")]
		[SerializeField]
		[Min(0.1f)]
		private float _maxContactDist = 1.2f;

		[Tooltip("Bot bu dikey hızdan (m/sn) hızlı YÜKSELİYORSA pruva dalgası YOK (zıplama patlamasını keser).")]
		[SerializeField]
		[Min(0.1f)]
		private float _maxRiseSpeed = 2.5f;

		private Transform _t;

		private BuoyantBody _buoy;

		private Vector3 _lastPos;

		private float _speed;

		private float _timer;

		private readonly RaycastHit[] _hits = new RaycastHit[8];

		private BoatVfxPool _pool;

		private void OnEnable()
		{
			_t = base.transform;
			_buoy = GetComponentInChildren<BuoyantBody>(includeInactive: true);
			_lastPos = _t.position;
			_speed = 0f;
			_timer = 0f;
			if (_bowVfxPrefab != null && _pool == null)
			{
				_pool = new BoatVfxPool(_bowVfxPrefab, _poolMax, "BowWaveFxPool");
			}
		}

		private void OnDestroy()
		{
			_pool?.Dispose();
		}

		private void LateUpdate()
		{
			if (_pool == null)
			{
				return;
			}
			_pool.Tick();
			Vector3 position = _t.position;
			Vector3 vector = position - _lastPos;
			_lastPos = position;
			bool flag = vector.sqrMagnitude > 64f;
			Vector3 forward = _t.forward;
			forward.y = 0f;
			if (forward.sqrMagnitude < 0.0001f)
			{
				forward = Vector3.forward;
			}
			else
			{
				forward.Normalize();
			}
			Vector3 lhs = new Vector3(vector.x, 0f, vector.z);
			float b = (flag ? 0f : (Vector3.Dot(lhs, forward) / Mathf.Max(Time.deltaTime, 0.0001f)));
			_speed = Mathf.Lerp(_speed, Mathf.Max(0f, b), 1f - Mathf.Exp(-8f * Time.deltaTime));
			if (_timer > 0f)
			{
				_timer -= Time.deltaTime;
			}
			if (!(_speed < _minSpeed))
			{
				Vector3 p = _t.TransformPoint(new Vector3(0f, _hullBottomLocalY, _bowForwardLocal));
				if (TrySurfaceY(p, out var surfaceY) && !(p.y - surfaceY > _maxContactDist) && !((flag ? 0f : (vector.y / Mathf.Max(Time.deltaTime, 0.0001f))) > _maxRiseSpeed) && !(_timer > 0f))
				{
					float t = Mathf.Clamp01(Mathf.InverseLerp(_minSpeed, _fullSpeed, _speed));
					_timer = Mathf.Lerp(_maxInterval, _minInterval, t);
					Vector3 position2 = new Vector3(p.x, surfaceY, p.z);
					Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
					_pool.Spawn(position2, rotation, _vfxScale, _vfxLifetime);
				}
			}
		}

		private bool TrySurfaceY(Vector3 p, out float surfaceY)
		{
			surfaceY = 0f;
			if (_buoy != null && _buoy.InWater && _buoy.Channel != null && _buoy.Channel.TryWaterHeight(p, out var surfaceY2))
			{
				surfaceY = surfaceY2;
				return true;
			}
			int num = Physics.RaycastNonAlloc(p + Vector3.up * _probeAbove, Vector3.down, _hits, _probeAbove + _maxContactDist + 0.5f, _surfaceMask, QueryTriggerInteraction.Ignore);
			float num2 = float.PositiveInfinity;
			bool result = false;
			for (int i = 0; i < num; i++)
			{
				RaycastHit raycastHit = _hits[i];
				if (!(raycastHit.collider == null) && !raycastHit.collider.transform.IsChildOf(_t) && !(raycastHit.distance >= num2))
				{
					num2 = raycastHit.distance;
					surfaceY = raycastHit.point.y;
					result = true;
				}
			}
			return result;
		}
	}
}
