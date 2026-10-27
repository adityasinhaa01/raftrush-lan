using PuffPirates.Water;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Boats
{
	[RequireComponent(typeof(NetworkBoat))]
	[DisallowMultipleComponent]
	public sealed class BoatWakeVisual : MonoBehaviour
	{
		public enum TintMode
		{
			WaterFoamColor = 0,
			FixedColor = 1
		}

		private const float MinDeltaTime = 0.0001f;

		private const float TeleportDistance = 8f;

		private const int MaxWakeSamples = 48;

		private const int MaxSamplesPerFrame = 4;

		private static readonly int WakePointsId = Shader.PropertyToID("_BoatWakePoints");

		private static readonly int WakeDataId = Shader.PropertyToID("_BoatWakeData");

		private static readonly int WakeCountId = Shader.PropertyToID("_BoatWakeCount");

		private static readonly Vector4[] WakePoints = new Vector4[48];

		private static readonly Vector4[] WakeData = new Vector4[48];

		private static int _wakeWriteIndex;

		private static int _wakeSampleCount;

		[Header("Outputs")]
		[Tooltip("Spawn the visible foam particles behind the boat.")]
		[SerializeField]
		private bool _emitParticles = true;

		[Tooltip("Write wake segments to the _BoatWakePoints shader globals (only StylizedWater.shader reads them).")]
		[SerializeField]
		private bool _writeShaderWake = true;

		[Header("Water Contact")]
		[Tooltip("Local Y of the hull bottom (same convention as BoatSurfaceDragFx).")]
		[SerializeField]
		private float _hullBottomLocalY = -0.5f;

		[Tooltip("Hull bottom higher than this above the water surface (m) counts as airborne: no wake.")]
		[SerializeField]
		[Min(0f)]
		private float _maxContactHeight = 0.6f;

		[Tooltip("Hull bottom deeper than this below the water surface (m) counts as submerged: no wake.")]
		[SerializeField]
		[Min(0f)]
		private float _maxSubmergedDepth = 3f;

		[Tooltip("Keep emitting this long (s) after contact is lost, to bridge tiny bounces. 0 = stop immediately.")]
		[SerializeField]
		[Min(0f)]
		private float _contactGraceTime;

		[Header("Speed Response")]
		[SerializeField]
		[Min(0f)]
		private float _minWakeSpeed = 0.65f;

		[SerializeField]
		[Min(0.1f)]
		private float _fullWakeSpeed = 11f;

		[SerializeField]
		[Min(0.1f)]
		private float _speedLerp = 8f;

		[Header("Shape")]
		[Tooltip("Wake width = boat diameter x this.")]
		[SerializeField]
		[Range(0.8f, 1.4f)]
		private float _widthMultiplier = 1f;

		[Tooltip("How far behind the boat center the wake starts, as a fraction of the diameter.")]
		[SerializeField]
		[Range(0.1f, 0.7f)]
		private float _sternOffsetRatio = 0.4f;

		[Header("Shader Foam")]
		[SerializeField]
		[Range(0.2f, 0.7f)]
		private float _sampleSpacingRatio = 0.38f;

		[SerializeField]
		[Min(0.5f)]
		private float _wakeLifetime = 2.6f;

		[SerializeField]
		[Range(0f, 1.5f)]
		private float _foamIntensity = 1f;

		[SerializeField]
		[Min(0.05f)]
		private float _minimumSampleDistance = 0.2f;

		[Header("Particles - Density")]
		[Tooltip("Particles per meter travelled at low speed.")]
		[SerializeField]
		[Min(0f)]
		private float _minParticlesPerMeter = 14f;

		[Tooltip("Particles per meter travelled at full speed.")]
		[SerializeField]
		[Min(0.1f)]
		private float _maxParticlesPerMeter = 28f;

		[Header("Particles - Emission")]
		[Tooltip("Particle lifetime in seconds. Independent from the shader wake lifetime.")]
		[SerializeField]
		[Min(0.1f)]
		private float _particleLifetime = 2.6f;

		[Tooltip("Minimum particle pool. Raised automatically if the density needs more to avoid gaps.")]
		[SerializeField]
		[Min(16f)]
		private int _maxParticles = 300;

		[Tooltip("Highest speed (m/s) the boat can reach, boosts included. Sizes the pool: if it is too small the pool fills up and the trail right behind the boat disappears.")]
		[SerializeField]
		[Min(1f)]
		private float _maxExpectedSpeed = 30f;

		[Header("Particles - Look")]
		[Tooltip("Optional material. Empty = Sprites/Default with the foam texture below.")]
		[SerializeField]
		private Material _particleMaterial;

		[Tooltip("Optional foam texture. Empty = generated soft round dot. Ignored when a material is set.")]
		[SerializeField]
		private Texture2D _foamTexture;

		[Tooltip("Particle size = wake width x this.")]
		[SerializeField]
		[Range(0.05f, 1.5f)]
		private float _particleSizeRatio = 0.4f;

		[Tooltip("Size variation between particles. 0 = all the same size.")]
		[SerializeField]
		[Range(0f, 0.9f)]
		private float _particleSizeRandomness = 0.15f;

		[Tooltip("Outward speed of particles at spawn.")]
		[SerializeField]
		[Range(0f, 2f)]
		private float _particleStartSpeed = 0.18f;

		[Tooltip("Emitter width = wake width x this.")]
		[SerializeField]
		[Range(0.1f, 2.5f)]
		private float _emitterWidthMultiplier = 1f;

		[Tooltip("Emitter front-to-back depth. Small values give a thin stern line.")]
		[SerializeField]
		[Range(0.005f, 1f)]
		private float _emitterDepth = 0.05f;

		[Tooltip("Height of the foam above the water surface.")]
		[SerializeField]
		[Range(-0.05f, 0.2f)]
		private float _surfaceOffset = 0.03f;

		[Tooltip("Renderer sorting fudge. Negative draws the foam on top of the water.")]
		[SerializeField]
		private float _sortingFudge = -2f;

		[Header("Particles - Scale Over Lifetime")]
		[SerializeField]
		[Range(0.05f, 2f)]
		private float _startScale = 0.5f;

		[SerializeField]
		[Range(0.05f, 3f)]
		private float _midScale = 1f;

		[SerializeField]
		[Range(0.05f, 4f)]
		private float _endScale = 1.4f;

		[SerializeField]
		[Range(0.01f, 0.99f)]
		private float _scaleMidTime = 0.35f;

		[Header("Particles - Fade")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _startAlpha = 0.85f;

		[SerializeField]
		[Range(0f, 1f)]
		private float _midAlpha = 0.55f;

		[SerializeField]
		[Range(0.01f, 0.99f)]
		private float _fadeMidTime = 0.3f;

		[Header("Particles - Color")]
		[SerializeField]
		private TintMode _tintMode;

		[Tooltip("Used when Tint Mode is FixedColor.")]
		[SerializeField]
		private Color _fixedColor = Color.white;

		[Tooltip("WaterFoamColor only: 0 = water foam color, 1 = pure white.")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _waterTintToWhite = 0.5f;

		[Tooltip("WaterFoamColor only: how often (s) the water material color is re-read.")]
		[SerializeField]
		[Min(0.1f)]
		private float _tintRefreshInterval = 1f;

		[Header("Debug")]
		[Tooltip("Log why the wake stops/starts and warn when the particle pool is full.")]
		[SerializeField]
		private bool _debugLog;

		private NetworkBoat _boat;

		private BuoyantBody _buoy;

		private Vector3 _lastPosition;

		private Vector3 _lastSamplePosition;

		private float _filteredSpeed;

		private float _currentWakeWidth;

		private bool _hasSamplePosition;

		private float _lastContactTime = float.NegativeInfinity;

		private float _lastSurfaceY;

		private WaterChannel _lastChannel;

		private ParticleSystem _ps;

		private ParticleSystem.EmissionModule _emission;

		private ParticleSystemRenderer _psr;

		private Material _particleMat;

		private bool _emitterArmed;

		private WaterChannel _tintChannel;

		private float _tintTimer;

		private float _lastParticleWidth = -1f;

		private static Texture2D _softTex;

		private string _lastStopReason = "";

		private bool _warnedPoolFull;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetWakeBuffer()
		{
			_wakeWriteIndex = 0;
			_wakeSampleCount = 0;
			Shader.SetGlobalInt(WakeCountId, 0);
		}

		private void OnEnable()
		{
			_boat = GetComponent<NetworkBoat>();
			_buoy = GetComponentInChildren<BuoyantBody>();
			_lastPosition = base.transform.position;
			_lastContactTime = float.NegativeInfinity;
			_hasSamplePosition = false;
			if (_ps == null)
			{
				BuildParticleSystem();
			}
			StopParticles();
		}

		private void OnDisable()
		{
			_filteredSpeed = 0f;
			_currentWakeWidth = 0f;
			_hasSamplePosition = false;
			StopParticles();
		}

		private void OnDestroy()
		{
			if (_ps != null)
			{
				Object.Destroy(_ps.gameObject);
			}
			if (_particleMat != null)
			{
				Object.Destroy(_particleMat);
			}
		}

		private void OnValidate()
		{
			if (!(_ps == null))
			{
				float width = ((_currentWakeWidth > 0f) ? _currentWakeWidth : 1f);
				ApplyParticleSettings(width);
				_tintChannel = null;
				_tintTimer = 0f;
			}
		}

		private void LateUpdate()
		{
			if (!(_boat == null))
			{
				Vector3 position = base.transform.position;
				Vector3 movement = position - _lastPosition;
				bool teleported = movement.sqrMagnitude > 64f;
				_lastPosition = position;
				UpdateFilteredSpeed(movement, teleported);
				UpdateWake(position, teleported);
			}
		}

		private void UpdateFilteredSpeed(Vector3 movement, bool teleported)
		{
			if (teleported || _boat.IsRespawning)
			{
				_filteredSpeed = 0f;
				return;
			}
			movement.y = 0f;
			float b = movement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
			float t = 1f - Mathf.Exp((0f - _speedLerp) * Time.deltaTime);
			_filteredSpeed = Mathf.Lerp(_filteredSpeed, b, t);
		}

		private void UpdateWake(Vector3 boatPosition, bool teleported)
		{
			float num = Mathf.InverseLerp(_minWakeSpeed, _fullWakeSpeed, _filteredSpeed);
			if (teleported || _boat.IsRespawning)
			{
				StopWake("teleport/respawn");
				return;
			}
			if (num <= 0f)
			{
				StopWake("too slow");
				return;
			}
			BoatSizeDef boatSizeDef = BoatSizes.Get(_boat.SizeId);
			float num2 = (_currentWakeWidth = Mathf.Max(0.4f, boatSizeDef.Diameter * _widthMultiplier));
			Vector3 forward = base.transform.forward;
			forward.y = 0f;
			if (forward.sqrMagnitude < 0.0001f)
			{
				forward = Vector3.forward;
			}
			forward.Normalize();
			Vector3 sternPosition = boatPosition - forward * (boatSizeDef.Diameter * _sternOffsetRatio);
			if (!TryGetWaterContact(sternPosition, out var surfaceY, out var channel))
			{
				StopWake((_buoy != null && _buoy.InWater) ? "hull off the surface" : "not in a water channel");
				return;
			}
			LogState(null);
			if (_emitParticles)
			{
				DriveWakeParticles(sternPosition, surfaceY, channel, forward, num2, num);
			}
			else
			{
				StopParticles();
			}
			if (_writeShaderWake)
			{
				WriteShaderSamples(sternPosition, num2, num);
			}
			else
			{
				_hasSamplePosition = false;
			}
		}

		private bool TryGetWaterContact(Vector3 sternPosition, out float surfaceY, out WaterChannel channel)
		{
			surfaceY = 0f;
			channel = ((_buoy != null && _buoy.InWater) ? _buoy.Channel : null);
			if (channel != null && channel.TryWaterHeight(sternPosition, out surfaceY))
			{
				float num = base.transform.TransformPoint(new Vector3(0f, _hullBottomLocalY, 0f)).y - surfaceY;
				if (num <= _maxContactHeight && num >= 0f - _maxSubmergedDepth)
				{
					_lastContactTime = Time.time;
					_lastSurfaceY = surfaceY;
					_lastChannel = channel;
					return true;
				}
			}
			if (_contactGraceTime > 0f && Time.time - _lastContactTime <= _contactGraceTime)
			{
				surfaceY = _lastSurfaceY;
				channel = _lastChannel;
				return true;
			}
			return false;
		}

		private void StopWake(string reason)
		{
			LogState(reason);
			_hasSamplePosition = false;
			StopParticles();
		}

		private void LogState(string stopReason)
		{
			if (_debugLog && !(stopReason == _lastStopReason))
			{
				_lastStopReason = stopReason;
				string text = ((_boat != null && _boat.IsSpawned && !_boat.IsServerInitialized) ? "proxy" : "authority");
				Debug.Log((stopReason == null) ? ("[BoatWake] " + base.name + " (" + text + ") started") : ("[BoatWake] " + base.name + " (" + text + ") stopped: " + stopReason), this);
			}
		}

		private void WriteShaderSamples(Vector3 sternPosition, float wakeWidth, float strength)
		{
			if (!_hasSamplePosition)
			{
				_lastSamplePosition = sternPosition;
				_hasSamplePosition = true;
				return;
			}
			float num = Mathf.Max(_minimumSampleDistance, wakeWidth * _sampleSpacingRatio);
			int num2 = 0;
			Vector3 vector = sternPosition - _lastSamplePosition;
			vector.y = 0f;
			float magnitude = vector.magnitude;
			while (magnitude >= num && num2 < 4)
			{
				Vector3 lastSamplePosition = _lastSamplePosition;
				Vector3 vector2 = lastSamplePosition + vector / magnitude * num;
				RecordWakeSample(lastSamplePosition, vector2, wakeWidth, strength);
				_lastSamplePosition = vector2;
				num2++;
				vector = sternPosition - _lastSamplePosition;
				vector.y = 0f;
				magnitude = vector.magnitude;
			}
			if (num2 > 0)
			{
				UploadWakeBuffer();
			}
		}

		private void RecordWakeSample(Vector3 segmentStart, Vector3 segmentEnd, float width, float strength)
		{
			WakePoints[_wakeWriteIndex] = new Vector4(segmentStart.x, segmentStart.z, segmentEnd.x, segmentEnd.z);
			float z = _wakeLifetime * Mathf.Lerp(0.72f, 1f, strength);
			float w = _foamIntensity * Mathf.Lerp(0.5f, 1f, strength);
			WakeData[_wakeWriteIndex] = new Vector4(width * 0.5f, Time.time, z, w);
			_wakeWriteIndex = (_wakeWriteIndex + 1) % 48;
			_wakeSampleCount = Mathf.Min(_wakeSampleCount + 1, 48);
		}

		private static void UploadWakeBuffer()
		{
			Shader.SetGlobalVectorArray(WakePointsId, WakePoints);
			Shader.SetGlobalVectorArray(WakeDataId, WakeData);
			Shader.SetGlobalInt(WakeCountId, _wakeSampleCount);
		}

		private void BuildParticleSystem()
		{
			GameObject gameObject = new GameObject("WakeFX_" + base.name);
			_ps = gameObject.AddComponent<ParticleSystem>();
			_ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = _ps.main;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.gravityModifier = 0f;
			main.startColor = Color.white;
			_emission = _ps.emission;
			_emission.enabled = true;
			_emission.rateOverTime = 0f;
			_emission.rateOverDistance = 0f;
			ParticleSystem.ShapeModule shape = _ps.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Box;
			_psr = gameObject.GetComponent<ParticleSystemRenderer>();
			_psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
			_psr.shadowCastingMode = ShadowCastingMode.Off;
			_psr.receiveShadows = false;
			ApplyParticleSettings(1f);
			_ps.Play();
		}

		private void ApplyParticleSettings(float width)
		{
			if (!(_ps == null))
			{
				float num = Mathf.Max(0.1f, width);
				float num2 = Mathf.Max(0.02f, num * _particleSizeRatio);
				float num3 = Mathf.Clamp01(_particleSizeRandomness);
				ParticleSystem.MainModule main = _ps.main;
				main.startLifetime = Mathf.Max(0.1f, _particleLifetime);
				main.startSpeed = Mathf.Max(0f, _particleStartSpeed);
				int b = Mathf.CeilToInt(Mathf.Max(1f, _maxParticlesPerMeter) * Mathf.Max(_fullWakeSpeed, _maxExpectedSpeed) * Mathf.Max(0.1f, _particleLifetime) * 1.35f);
				main.maxParticles = Mathf.Max(16, Mathf.Max(_maxParticles, b));
				if (num3 <= 0.001f)
				{
					main.startSize = num2;
				}
				else
				{
					main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Max(0.01f, num2 * (1f - num3)), Mathf.Max(0.01f, num2 * (1f + num3)));
				}
				ParticleSystem.ShapeModule shape = _ps.shape;
				shape.scale = new Vector3(Mathf.Max(0.1f, num * _emitterWidthMultiplier), 0.02f, Mathf.Max(0.005f, _emitterDepth));
				ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _ps.colorOverLifetime;
				colorOverLifetime.enabled = true;
				Gradient gradient = new Gradient();
				gradient.SetKeys(new GradientColorKey[2]
				{
					new GradientColorKey(Color.white, 0f),
					new GradientColorKey(Color.white, 1f)
				}, new GradientAlphaKey[3]
				{
					new GradientAlphaKey(_startAlpha, 0f),
					new GradientAlphaKey(_midAlpha, Mathf.Clamp(_fadeMidTime, 0.01f, 0.99f)),
					new GradientAlphaKey(0f, 1f)
				});
				colorOverLifetime.color = gradient;
				ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = _ps.sizeOverLifetime;
				sizeOverLifetime.enabled = true;
				sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, _startScale), new Keyframe(Mathf.Clamp(_scaleMidTime, 0.01f, 0.99f), _midScale), new Keyframe(1f, _endScale)));
				ApplyParticleMaterial();
			}
		}

		private void ApplyParticleMaterial()
		{
			if (_psr == null)
			{
				return;
			}
			_psr.sortingFudge = _sortingFudge;
			if (_particleMaterial != null)
			{
				_psr.sharedMaterial = _particleMaterial;
				return;
			}
			if (_particleMat == null)
			{
				_particleMat = new Material(Shader.Find("Sprites/Default"))
				{
					name = "WakeParticleMat"
				};
			}
			_particleMat.mainTexture = ((_foamTexture != null) ? _foamTexture : SoftTex());
			_psr.sharedMaterial = _particleMat;
		}

		private void DriveWakeParticles(Vector3 sternPosition, float surfaceY, WaterChannel channel, Vector3 forward, float width, float strength)
		{
			if (_ps == null)
			{
				return;
			}
			_ps.transform.SetPositionAndRotation(new Vector3(sternPosition.x, surfaceY + _surfaceOffset, sternPosition.z), Quaternion.LookRotation(forward, Vector3.up));
			if (!Mathf.Approximately(width, _lastParticleWidth))
			{
				_lastParticleWidth = width;
				ApplyParticleSettings(width);
			}
			RefreshTint(channel);
			if (!_emitterArmed)
			{
				_emitterArmed = true;
				SetParticleDensity(0f);
				return;
			}
			SetParticleDensity(Mathf.Lerp(_minParticlesPerMeter, _maxParticlesPerMeter, Mathf.Clamp01(strength)));
			if (_debugLog && !_warnedPoolFull && _ps.particleCount >= _ps.main.maxParticles)
			{
				_warnedPoolFull = true;
				Debug.LogWarning($"[BoatWake] {base.name}: particle pool full ({_ps.main.maxParticles}) at " + $"{_filteredSpeed:F1} m/s — raise Max Expected Speed.", this);
			}
		}

		private void StopParticles()
		{
			_emitterArmed = false;
			SetParticleDensity(0f);
		}

		private void SetParticleDensity(float particlesPerMeter)
		{
			if (!(_ps == null))
			{
				_emission.rateOverTime = 0f;
				float num = Mathf.Max(0f, particlesPerMeter);
				if (!Mathf.Approximately(_emission.rateOverDistance.constant, num))
				{
					_emission.rateOverDistance = num;
				}
			}
		}

		private void RefreshTint(WaterChannel channel)
		{
			if (_tintMode == TintMode.FixedColor)
			{
				SetStartColor(_fixedColor);
				return;
			}
			_tintTimer -= Time.deltaTime;
			if (channel == _tintChannel && _tintTimer > 0f)
			{
				return;
			}
			_tintChannel = channel;
			_tintTimer = _tintRefreshInterval;
			Renderer renderer = ((channel != null) ? channel.WaterSurface : null);
			Material material = ((renderer != null) ? renderer.sharedMaterial : null);
			Color a = Color.white;
			if (material != null)
			{
				if (material.HasProperty("_SurfFoam_Color"))
				{
					a = material.GetColor("_SurfFoam_Color");
				}
				else if (material.HasProperty("_FoamColor"))
				{
					a = material.GetColor("_FoamColor");
				}
				else if (material.HasProperty("_Color_Shallow"))
				{
					a = material.GetColor("_Color_Shallow");
				}
			}
			a = Color.Lerp(a, Color.white, _waterTintToWhite);
			a.a = 1f;
			SetStartColor(a);
		}

		private void SetStartColor(Color c)
		{
			ParticleSystem.MainModule main = _ps.main;
			main.startColor = c;
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
