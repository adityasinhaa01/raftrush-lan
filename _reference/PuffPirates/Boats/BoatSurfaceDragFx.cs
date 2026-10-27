using PuffPirates.Water;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Boats
{
	[RequireComponent(typeof(NetworkBoat))]
	[DisallowMultipleComponent]
	public sealed class BoatSurfaceDragFx : MonoBehaviour
	{
		[Header("Hız Tepkisi")]
		[Tooltip("Bu yatay hızın (m/sn) altında iz YOK.")]
		[SerializeField]
		[Min(0f)]
		private float _minSpeed = 1.5f;

		[Tooltip("Bu hızda tam emisyon.")]
		[SerializeField]
		[Min(0.1f)]
		private float _fullSpeed = 12f;

		[Tooltip("En yüksek partikül emisyonu (partikül/sn).")]
		[SerializeField]
		[Min(1f)]
		private float _maxRate = 80f;

		[SerializeField]
		[Min(0.05f)]
		private float _speedLerp = 8f;

		[Header("Yüzey Teması")]
		[Tooltip("Yüzey raycast maskesi — BoatPhysicsController._waterSurfaceMask ile aynı seç.")]
		[SerializeField]
		private LayerMask _surfaceMask = -1;

		[Tooltip("Gövde alt yüzeyinin yerel Y'si (ride ile aynı: -0.5).")]
		[SerializeField]
		private float _hullBottomLocalY = -0.5f;

		[Tooltip("Raycast'in gövde altından yukarı başlama yüksekliği (m).")]
		[SerializeField]
		[Min(0.2f)]
		private float _probeAbove = 1.5f;

		[Tooltip("Gövde alt yüzeyi yüzeyden bu kadar uzaktaysa (m) 'temas yok' → iz durur (havada).")]
		[SerializeField]
		[Min(0.1f)]
		private float _maxContactDist = 1.1f;

		[Header("Görünüm")]
		[SerializeField]
		private Color _color = new Color(0.86f, 0.86f, 0.8f, 0.7f);

		[SerializeField]
		[Min(0.05f)]
		private float _startSize = 0.7f;

		[SerializeField]
		[Min(0.2f)]
		private float _lifetime = 0.9f;

		private Transform _t;

		private NetworkBoat _boat;

		private BuoyantBody _buoy;

		private Vector3 _lastPos;

		private float _speed;

		private ParticleSystem _ps;

		private ParticleSystem.EmissionModule _emission;

		private Material _mat;

		private readonly RaycastHit[] _hits = new RaycastHit[8];

		private static Texture2D _softTex;

		private void OnEnable()
		{
			_t = base.transform;
			_boat = GetComponent<NetworkBoat>();
			_buoy = GetComponentInChildren<BuoyantBody>(includeInactive: true);
			_lastPos = _t.position;
			_speed = 0f;
			if (_ps == null)
			{
				BuildParticles();
			}
			SetRate(0f);
		}

		private void OnDestroy()
		{
			if (_mat != null)
			{
				Object.Destroy(_mat);
			}
		}

		private void LateUpdate()
		{
			if (_ps == null)
			{
				return;
			}
			Vector3 position = _t.position;
			Vector3 vector = position - _lastPos;
			_lastPos = position;
			bool num = vector.sqrMagnitude > 64f;
			vector.y = 0f;
			_speed = Mathf.Lerp(b: num ? 0f : (vector.magnitude / Mathf.Max(Time.deltaTime, 0.0001f)), t: 1f - Mathf.Exp((0f - _speedLerp) * Time.deltaTime), a: _speed);
			float num2 = Mathf.InverseLerp(_minSpeed, _fullSpeed, _speed);
			if (num || (_boat != null && _boat.IsRespawning) || num2 <= 0f)
			{
				SetRate(0f);
				return;
			}
			Vector3 hullBottom = _t.TransformPoint(new Vector3(0f, _hullBottomLocalY, 0f));
			if (!TrySurfaceY(hullBottom, out var surfaceY))
			{
				SetRate(0f);
				return;
			}
			if (hullBottom.y - surfaceY > _maxContactDist)
			{
				SetRate(0f);
				return;
			}
			_ps.transform.position = new Vector3(hullBottom.x, surfaceY + 0.03f, hullBottom.z);
			SetRate(Mathf.Lerp(6f, _maxRate, Mathf.Clamp01(num2)));
		}

		private bool TrySurfaceY(Vector3 hullBottom, out float surfaceY)
		{
			surfaceY = 0f;
			if (_buoy != null && _buoy.InWater && _buoy.Channel != null && _buoy.Channel.TryWaterHeight(hullBottom, out var surfaceY2))
			{
				surfaceY = surfaceY2;
				return true;
			}
			int num = Physics.RaycastNonAlloc(hullBottom + Vector3.up * _probeAbove, Vector3.down, _hits, _probeAbove + _maxContactDist + 0.5f, _surfaceMask, QueryTriggerInteraction.Ignore);
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

		private void SetRate(float rate)
		{
			if (!(_ps == null) && !Mathf.Approximately(_emission.rateOverTime.constant, rate))
			{
				_emission.rateOverTime = rate;
			}
		}

		private void BuildParticles()
		{
			GameObject gameObject = new GameObject("SurfaceDragFX");
			gameObject.transform.SetParent(_t, worldPositionStays: false);
			_ps = gameObject.AddComponent<ParticleSystem>();
			_ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = _ps.main;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.startLifetime = _lifetime;
			main.startSize = _startSize;
			main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.8f);
			main.gravityModifier = -0.05f;
			main.startColor = _color;
			main.maxParticles = 400;
			_emission = _ps.emission;
			_emission.enabled = true;
			_emission.rateOverTime = 0f;
			ParticleSystem.ShapeModule shape = _ps.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Circle;
			shape.radius = 0.5f;
			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _ps.colorOverLifetime;
			colorOverLifetime.enabled = true;
			Gradient gradient = new Gradient();
			gradient.SetKeys(new GradientColorKey[2]
			{
				new GradientColorKey(Color.white, 0f),
				new GradientColorKey(Color.white, 1f)
			}, new GradientAlphaKey[3]
			{
				new GradientAlphaKey(0.7f, 0f),
				new GradientAlphaKey(0.4f, 0.4f),
				new GradientAlphaKey(0f, 1f)
			});
			colorOverLifetime.color = gradient;
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = _ps.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.4f, 1f), new Keyframe(1f, 1.3f)));
			ParticleSystemRenderer component = gameObject.GetComponent<ParticleSystemRenderer>();
			component.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
			component.shadowCastingMode = ShadowCastingMode.Off;
			component.receiveShadows = false;
			component.sortingFudge = -1.5f;
			Shader shader = Shader.Find("Sprites/Default");
			_mat = new Material(shader)
			{
				mainTexture = SoftTex()
			};
			component.material = _mat;
			_ps.Play();
		}

		private static Texture2D SoftTex()
		{
			if (_softTex != null)
			{
				return _softTex;
			}
			Texture2D texture2D = new Texture2D(64, 64, TextureFormat.RGBA32, mipChain: false)
			{
				wrapMode = TextureWrapMode.Clamp
			};
			Color32[] array = new Color32[4096];
			Vector2 b = new Vector2(31.5f, 31.5f);
			float num = 32f;
			for (int i = 0; i < 64; i++)
			{
				for (int j = 0; j < 64; j++)
				{
					float num2 = Vector2.Distance(new Vector2(j, i), b) / num;
					float num3 = Mathf.Clamp01(1f - num2);
					num3 *= num3;
					array[i * 64 + j] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(num3 * 255f));
				}
			}
			texture2D.SetPixels32(array);
			texture2D.Apply();
			_softTex = texture2D;
			return texture2D;
		}
	}
}
