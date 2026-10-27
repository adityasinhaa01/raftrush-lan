using System;
using UnityEngine;

namespace PuffPirates.Water
{
	[DisallowMultipleComponent]
	public sealed class WaterChannel : MonoBehaviour
	{
		[Tooltip("Su yüzeyinin kanal-yerel Y yüksekliği (dünya Y'si transform ile hesaplanır).")]
		[SerializeField]
		private float _waterLevelLocalY;

		[Tooltip("Su yoğunluğu (kg/m³). Obje yoğunluğundan BÜYÜKSE obje yüzer. Canlı ayarlanabilir.")]
		[SerializeField]
		[Min(1f)]
		private float _waterDensity = 4000f;

		[Tooltip("EĞİMLİ SU: WaterCol mesh'inin collider'ı. Verilirse su yüzeyi Y'si bu mesh'e RAYCAST ile bulunur → su SADECE bu mesh'in olduğu yerde olur + yüzey eğimini takip eder. Boşsa düz düzlem (WaterY).")]
		[SerializeField]
		private Collider _surfaceCollider;

		[Tooltip("Su yüksekliği ofseti (m) — yüzeye eklenir. Panel'den CANLI ayarlanır.")]
		[SerializeField]
		private float _levelOffset;

		[Tooltip("Su hacmini tanımlayan TRIGGER collider (kanala giriş tespiti). Boşsa bu objenin Collider'ı.")]
		[SerializeField]
		private Collider _volume;

		[Tooltip("Opsiyonel: kanalın kıvrımını takip eden görsel su-yüzeyi mesh'i. Verilirse CPU dalga ile deforme edilir.")]
		[SerializeField]
		private Renderer _waterSurface;

		[Header("Dalga (görsel mesh + buoyancy SENKRON — 'suda sallanma' hissi)")]
		[Tooltip("Dalga tepe yüksekliği (m). 0 = düz su.")]
		[SerializeField]
		[Min(0f)]
		private float _waveAmplitude;

		[Tooltip("Dalga boyu (m) — küçük = sık dalga.")]
		[SerializeField]
		[Min(0.5f)]
		private float _waveLength = 5f;

		[Tooltip("Dalga ilerleme hızı.")]
		[SerializeField]
		private float _waveSpeed = 1f;

		private static readonly Vector2[] _waveDirs = new Vector2[3]
		{
			new Vector2(1f, 0.35f),
			new Vector2(-0.5f, 1f),
			new Vector2(0.7f, -0.6f)
		};

		private static readonly Vector2[] _waveDirsNorm = new Vector2[3]
		{
			_waveDirs[0].normalized,
			_waveDirs[1].normalized,
			_waveDirs[2].normalized
		};

		private Mesh _surfMesh;

		private MeshFilter _surfMf;

		private Vector3[] _surfBase;

		private Vector3[] _surfDeformed;

		private int _ignoredSurfaceLayer = -1;

		private float _lastLevelOffset = float.NaN;

		private float _lastWaveAmplitude = float.NaN;

		private const float SurfaceProbeUp = 60f;

		public float WaterY => base.transform.TransformPoint(new Vector3(0f, _waterLevelLocalY, 0f)).y;

		public Renderer WaterSurface => _waterSurface;

		public float WaterDensity
		{
			get
			{
				return _waterDensity;
			}
			set
			{
				_waterDensity = Mathf.Max(1f, value);
			}
		}

		public float LevelOffset
		{
			get
			{
				return _levelOffset;
			}
			set
			{
				_levelOffset = value;
			}
		}

		public float WaveAmplitude
		{
			get
			{
				return _waveAmplitude;
			}
			set
			{
				_waveAmplitude = Mathf.Max(0f, value);
			}
		}

		public float WaveLength
		{
			get
			{
				return _waveLength;
			}
			set
			{
				_waveLength = Mathf.Max(0.5f, value);
			}
		}

		public float WaveSpeed
		{
			get
			{
				return _waveSpeed;
			}
			set
			{
				_waveSpeed = value;
			}
		}

		public bool IsFlatSurface
		{
			get
			{
				if (_surfaceCollider == null)
				{
					return _waveAmplitude <= 0f;
				}
				return false;
			}
		}

		private float Wave(Vector3 worldPos)
		{
			if (_waveAmplitude <= 0f)
			{
				return 0f;
			}
			float num = MathF.PI * 2f / Mathf.Max(0.5f, _waveLength);
			float num2 = Time.time * _waveSpeed;
			float num3 = 0f;
			for (int i = 0; i < _waveDirs.Length; i++)
			{
				Vector2 vector = _waveDirsNorm[i];
				num3 += Mathf.Sin((vector.x * worldPos.x + vector.y * worldPos.z) * num + num2 * (1f + (float)i * 0.4f));
			}
			return num3 / (float)_waveDirs.Length * _waveAmplitude;
		}

		public bool TryWaterHeight(Vector3 worldPos, out float surfaceY)
		{
			if (_surfaceCollider != null)
			{
				Vector3 origin = new Vector3(worldPos.x, worldPos.y + 60f, worldPos.z);
				if (_surfaceCollider.Raycast(new Ray(origin, Vector3.down), out var hitInfo, 1060f))
				{
					surfaceY = hitInfo.point.y + _levelOffset + Wave(worldPos);
					return true;
				}
				surfaceY = 0f;
				return false;
			}
			surfaceY = WaterY + _levelOffset + Wave(worldPos);
			return true;
		}

		private void Reset()
		{
			_volume = GetComponent<Collider>();
		}

		private void OnEnable()
		{
			if (_volume == null)
			{
				_volume = GetComponent<Collider>();
			}
			if (_volume != null)
			{
				_volume.isTrigger = true;
			}
			_ignoredSurfaceLayer = -1;
			if (_surfaceCollider != null)
			{
				int layer = _surfaceCollider.gameObject.layer;
				if (layer != 0)
				{
					WaterSystem.AcquireDefaultLayerIgnore(layer);
					_ignoredSurfaceLayer = layer;
				}
			}
			if (_waterSurface != null)
			{
				_surfMf = _waterSurface.GetComponent<MeshFilter>();
				if (_surfMf != null && _surfMf.sharedMesh != null && _surfMf.sharedMesh.isReadable)
				{
					_surfMesh = _surfMf.mesh;
					_surfMesh.MarkDynamic();
					if (_surfBase == null)
					{
						_surfBase = _surfMesh.vertices;
						_surfDeformed = new Vector3[_surfBase.Length];
						float num = 1f / Mathf.Max(0.0001f, _waterSurface.transform.lossyScale.y);
						float num2 = (_waveAmplitude + 12f) * num;
						Bounds bounds = _surfMesh.bounds;
						bounds.Expand(new Vector3(0f, 2f * num2, 0f));
						_surfMesh.bounds = bounds;
					}
				}
			}
			WaterSystem.Register(this);
		}

		private void OnDisable()
		{
			if (_ignoredSurfaceLayer > 0)
			{
				WaterSystem.ReleaseDefaultLayerIgnore(_ignoredSurfaceLayer);
				_ignoredSurfaceLayer = -1;
			}
			WaterSystem.Unregister(this);
		}

		private void OnDestroy()
		{
			if (_surfMesh != null)
			{
				UnityEngine.Object.Destroy(_surfMesh);
			}
		}

		private void Update()
		{
			if (_surfMesh == null || _surfBase == null || _surfDeformed == null)
			{
				return;
			}
			bool flag = !Mathf.Approximately(_levelOffset, _lastLevelOffset) || !Mathf.Approximately(_waveAmplitude, _lastWaveAmplitude);
			if ((_waterSurface.isVisible || flag) && (!(_waveAmplitude <= 0f) || flag))
			{
				_lastLevelOffset = _levelOffset;
				_lastWaveAmplitude = _waveAmplitude;
				Transform transform = _waterSurface.transform;
				float num = 1f / Mathf.Max(0.0001f, transform.lossyScale.y);
				Vector3[] surfDeformed = _surfDeformed;
				for (int i = 0; i < surfDeformed.Length; i++)
				{
					Vector3 position = _surfBase[i];
					Vector3 worldPos = transform.TransformPoint(position);
					float num2 = _levelOffset + Wave(worldPos);
					surfDeformed[i] = new Vector3(position.x, position.y + num2 * num, position.z);
				}
				_surfMesh.vertices = surfDeformed;
				_surfMesh.RecalculateNormals();
			}
		}

		public bool Contains(Vector3 worldPos, out float surfaceY)
		{
			surfaceY = WaterY;
			if (_volume == null || !_volume.bounds.Contains(worldPos))
			{
				return false;
			}
			return TryWaterHeight(worldPos, out surfaceY);
		}
	}
}
