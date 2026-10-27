using PuffPirates.Water;
using UnityEngine;

namespace PuffPirates.Boats
{
	[DisallowMultipleComponent]
	public class BoatSplashFx : MonoBehaviour
	{
		[Tooltip("Su yüzeyini geçince doğan tek-atım splash prefab'i (ör. AllIn1 Water Splash). Boşsa pasif.")]
		[SerializeField]
		private GameObject _splashPrefab;

		[Tooltip("Gövde alt yüzeyinin yerel Y'si — BoatPhysicsController ride mantığıyla AYNI (-0.5).")]
		[SerializeField]
		private float _hullBottomLocalY = -0.5f;

		[Tooltip("Su yüzeyi layer maskesi — BoatPhysicsController._waterSurfaceMask ile aynı seç.")]
		[SerializeField]
		private LayerMask _waterSurfaceMask = -1;

		[Tooltip("Splash bu dikey İNİŞ hızının (m/sn) üstünde tetiklenir. Küçük = daha kolay splash.")]
		[SerializeField]
		[Min(0.1f)]
		private float _minImpactSpeed = 2.5f;

		[Tooltip("Ardışık splash'ler arası minimum süre (sn) — spam engeli.")]
		[SerializeField]
		[Min(0.05f)]
		private float _cooldown = 0.3f;

		[Tooltip("Doğan splash bu süre sonra silinir (sn).")]
		[SerializeField]
		[Min(0.2f)]
		private float _splashLifetime = 2f;

		[Tooltip("Yüzey aramasının gövde altından yukarı başlama yüksekliği (m).")]
		[SerializeField]
		[Min(0.5f)]
		private float _probeAbove = 2f;

		[Tooltip("Splash havuzu üst sınırı — bu kadar örnek yeniden kullanılır; dolunca Instantiate+Destroy'a düşer.")]
		[SerializeField]
		[Min(1f)]
		private int _poolMax = 16;

		private readonly RaycastHit[] _hits = new RaycastHit[8];

		private BuoyantBody _buoy;

		private float _lastHullY;

		private bool _hasLast;

		private float _cooldownLeft;

		private bool _wasAbove = true;

		private BoatVfxPool _pool;

		private void OnEnable()
		{
			_buoy = GetComponentInChildren<BuoyantBody>(includeInactive: true);
			if (_splashPrefab != null && _pool == null)
			{
				_pool = new BoatVfxPool(_splashPrefab, _poolMax, "SplashFxPool");
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
			Vector3 vector = base.transform.TransformPoint(new Vector3(0f, _hullBottomLocalY, 0f));
			float num = 0f;
			if (_hasLast && Time.deltaTime > 1E-05f)
			{
				num = (vector.y - _lastHullY) / Time.deltaTime;
			}
			_lastHullY = vector.y;
			_hasLast = true;
			if (_cooldownLeft > 0f)
			{
				_cooldownLeft -= Time.deltaTime;
			}
			float num2 = 0f;
			bool flag = false;
			if (_buoy != null && _buoy.InWater && _buoy.Channel != null && _buoy.Channel.TryWaterHeight(vector, out var surfaceY))
			{
				num2 = surfaceY;
				flag = true;
			}
			else
			{
				int num3 = Physics.RaycastNonAlloc(vector + Vector3.up * _probeAbove, Vector3.down, _hits, _probeAbove + 4f, _waterSurfaceMask, QueryTriggerInteraction.Ignore);
				float num4 = float.PositiveInfinity;
				for (int i = 0; i < num3; i++)
				{
					RaycastHit raycastHit = _hits[i];
					if (!(raycastHit.collider == null) && !raycastHit.collider.transform.IsChildOf(base.transform) && !(raycastHit.distance >= num4))
					{
						num4 = raycastHit.distance;
						num2 = raycastHit.point.y;
						flag = true;
					}
				}
			}
			if (!flag)
			{
				_wasAbove = true;
				return;
			}
			bool flag2 = vector.y > num2;
			if (_wasAbove && !flag2 && num <= 0f - _minImpactSpeed && _cooldownLeft <= 0f)
			{
				Vector3 position = new Vector3(vector.x, num2, vector.z);
				_pool.Spawn(position, Quaternion.identity, 0f, _splashLifetime);
				_cooldownLeft = _cooldown;
			}
			_wasAbove = flag2;
		}
	}
}
