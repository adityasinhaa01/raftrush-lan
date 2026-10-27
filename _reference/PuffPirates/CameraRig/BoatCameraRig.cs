using System;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using GamesMrkt.PuffPirates;
using PuffPirates.Boats;
using PuffPirates.Input;
using PuffPirates.Networking;
using PuffPirates.Players;
using PuffPirates.Settings;
using PuffPirates.Systems;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace PuffPirates.CameraRig
{
	[RequireComponent(typeof(CinemachineCamera))]
	[DisallowMultipleComponent]
	public class BoatCameraRig : MonoBehaviour
	{
		public enum CamMode
		{
			Boat = 0,
			Follow = 1,
			Free = 2,
			Locked = 3
		}

		[Header("Takip")]
		[SerializeField]
		private float _headingLerp = 3.5f;

		[SerializeField]
		private float _minSpeedForHeading = 0.6f;

		[Header("Serbest Bakış")]
		[Tooltip("Ok tuşlarıyla yatay çevirme (derece/sn).")]
		[SerializeField]
		private float _keyYawSpeed = 110f;

		[Tooltip("Ok tuşlarıyla eğim (derece/sn).")]
		[SerializeField]
		private float _keyPitchSpeed = 60f;

		[SerializeField]
		private float _mouseOrbitSens = 0.25f;

		[SerializeField]
		private float _pitchMin = 5f;

		[SerializeField]
		private float _pitchMax = 75f;

		[Header("Zoom")]
		[SerializeField]
		private float _distanceMin = 6f;

		[Header("Zoom")]
		[SerializeField]
		private float _distanceMax = 45f;

		[SerializeField]
		private float _zoomStep = 2.5f;

		[Tooltip("Zoom + pitch + reset GEÇİŞ easing hızı. Yüksek = anlık, düşük = yumuşak. Sürekli girdi target'a yazar → yüksek değerde 1:1 hissi kalır ama zoom-step / V-reset zıplaması yumuşar.")]
		[SerializeField]
		private float _camEaseLerp = 12f;

		[Header("Gamepad Kamera")]
		[SerializeField]
		private float _padYawSpeed = 110f;

		[SerializeField]
		private float _padPitchSpeed = 60f;

		[SerializeField]
		private float _padZoomSpeed = 10f;

		[Header("Yanlamasına Kaydırma (Pan)")]
		[Tooltip("Z / X ile kamerayı sağa-sola kaydırma hızı (m/sn).")]
		[SerializeField]
		private float _panSpeed = 9f;

		[Tooltip("Maksimum kamera konumu kaydırması (m). Bakış pivotu botta sabit kalır.")]
		[SerializeField]
		private float _panMax = 16f;

		[Tooltip("Tuş bırakılınca kaydırma kendiliğinden ortalanma hızı (0 = kalıcı).")]
		[SerializeField]
		private float _panRecenter;

		[Header("Varsayılan")]
		[SerializeField]
		private float _defaultPitch = 28f;

		[SerializeField]
		private float _defaultDistance = 6f;

		[Header("Hız Hissi (FOV)")]
		[SerializeField]
		private float _baseFov = 55f;

		[SerializeField]
		private float _maxFovBoost = 14f;

		[SerializeField]
		private float _fovAtSpeed = 14f;

		[SerializeField]
		private float _fovLerp = 4f;

		[Header("Hedef Arama")]
		[SerializeField]
		private float _retrySec = 0.4f;

		private CinemachineCamera _cam;

		public bool OcclusionEnabled;

		[Header("Mesafe-Culling (occlusion)")]
		[Tooltip("Kamera far-clip düzlemi (m). Sisin ötesindeki geometri hiç çizilmez → performans. Fog exp2 d=0.013 ~150m'de tamamen kapanır; 220 güvenli margin (pop olmaz). Sis yoğunluğunu değiştirirsen buna da bak.")]
		[SerializeField]
		private float _farClip = 220f;

		private CinemachineRotationComposer _composer;

		private CinemachineFollow _follow;

		private Transform _proxy;

		private NetworkBoat _boat;

		private PiratePlayer _localPirate;

		private Rigidbody _boatRb;

		private ConnectionSystem _connection;

		private PlayerInputSystem _input;

		private Vector3 _heading = Vector3.forward;

		private float _nextSearch;

		[Header("Bot Ekseni (Ticket #1)")]
		[Tooltip("Kamera botun kendi ekseninde dönerken yumuşatma. Yüksek = anında, düşük = yumuşak gecikmeli.")]
		[SerializeField]
		private float _boatYawLerp = 8f;

		private CamMode _mode;

		private float _yaw;

		private float _touchCamIdle;

		private const float TouchCamYawSens = 0.18f;

		private const float TouchCamRecenterDelay = 1.5f;

		private const float TouchCamRecenterLerp = 2f;

		private const float TouchCamZoomSens = 0.03f;

		private const float TouchCamPitchSens = 0.12f;

		private float _pitch;

		private float _dist;

		private float _pitchView;

		private float _distView;

		private Vector3 _panOffset;

		private int _camCollisionMask;

		private readonly RaycastHit[] _camHits = new RaycastHit[128];

		private float _occ = 1f;

		private int _lastOccFrame = -1;

		private Vector3 _lastPivot;

		private bool _hasLastPivot;

		private bool _snapForwardOnRespawn;

		private const float CamCollisionRadius = 0.35f;

		private const float CamCollisionHardMin = 0.5f;

		private const float CamCollisionReturnLerp = 5f;

		private const float CamTeleportSqr = 400f;

		public static BoatCameraRig Active;

		private float _shakeTrauma;

		private float _shakeSeed;

		private const float ShakeMaxPos = 0.35f;

		private const float ShakeFreq = 22f;

		private const float ShakeDecay = 1.8f;

		private const float GasShakeFreq = 8f;

		private const float GasShakeRamp = 6f;

		private const float GasTremoloFreq = 5f;

		[Header("Gaz Titremesi (sürekli)")]
		[Tooltip("Gaza basılıyken sürekli kamera titremesi genliği (metre). 0 = kapalı. Yerel oyuncu gaz gücüne göre sürer.")]
		[SerializeField]
		private float _gasShakeAmplitude = 0.12f;

		private float _gasShake01;

		private float _gasShakeView;

		private float _launcherRetryAt;

		private int _cursorLocked = -1;

		private Vector3 _focusPoint;

		private float _focusUntil;

		private float _focusDist = 12f;

		private float _focusBlend;

		private const float FocusIn = 0.6f;

		private const float FocusOut = 0.8f;

		public float PadYawSpeed
		{
			get
			{
				return _padYawSpeed;
			}
			set
			{
				_padYawSpeed = Mathf.Max(0f, value);
			}
		}

		public float PadPitchSpeed
		{
			get
			{
				return _padPitchSpeed;
			}
			set
			{
				_padPitchSpeed = Mathf.Max(0f, value);
			}
		}

		public float PadZoomSpeed
		{
			get
			{
				return _padZoomSpeed;
			}
			set
			{
				_padZoomSpeed = Mathf.Max(0f, value);
			}
		}

		public float DistanceMin
		{
			get
			{
				return _distanceMin;
			}
			set
			{
				_distanceMin = Mathf.Max(1f, value);
				_dist = Mathf.Clamp(_dist, _distanceMin, _distanceMax);
			}
		}

		public float DistanceMax
		{
			get
			{
				return _distanceMax;
			}
			set
			{
				_distanceMax = Mathf.Max(_distanceMin + 0.5f, value);
				_dist = Mathf.Clamp(_dist, _distanceMin, _distanceMax);
			}
		}

		public float DefaultDistance
		{
			get
			{
				return _defaultDistance;
			}
			set
			{
				_defaultDistance = Mathf.Clamp(value, _distanceMin, _distanceMax);
			}
		}

		public float FarClipPlane
		{
			get
			{
				if (!(_cam != null))
				{
					return _farClip;
				}
				return _cam.Lens.FarClipPlane;
			}
			set
			{
				if (!(_cam == null))
				{
					LensSettings lens = _cam.Lens;
					lens.FarClipPlane = value;
					_cam.Lens = lens;
				}
			}
		}

		public float GasShakeAmplitude
		{
			get
			{
				return _gasShakeAmplitude;
			}
			set
			{
				_gasShakeAmplitude = Mathf.Max(0f, value);
			}
		}

		public static void SetGasShake(float level01)
		{
			if (!(Active == null))
			{
				Active._gasShake01 = (GameSettings.CameraShakeEnabled ? Mathf.Clamp01(level01) : 0f);
			}
		}

		private void Awake()
		{
			_cam = GetComponent<CinemachineCamera>();
			_follow = GetComponent<CinemachineFollow>();
			_composer = GetComponent<CinemachineRotationComposer>();
			GameObject gameObject = new GameObject("~BoatCamTarget");
			gameObject.hideFlags = HideFlags.DontSave;
			_proxy = gameObject.transform;
			_pitch = _defaultPitch;
			_dist = _defaultDistance;
			_pitchView = _pitch;
			_distView = _dist;
			_camCollisionMask = BuildCamMask();
			Active = this;
			DistanceMax = GameSettings.CameraMaxDistance;
			OcclusionEnabled = false;
			_shakeSeed = UnityEngine.Random.value * 100f;
			TrackerSettings trackerSettings = _follow.TrackerSettings;
			trackerSettings.BindingMode = BindingMode.WorldSpace;
			trackerSettings.PositionDamping = Vector3.zero;
			trackerSettings.RotationDamping = Vector3.zero;
			_follow.TrackerSettings = trackerSettings;
			if (_composer != null)
			{
				ScreenComposerSettings composition = _composer.Composition;
				composition.ScreenPosition = Vector2.zero;
				composition.DeadZone.Enabled = false;
				composition.DeadZone.Size = Vector2.zero;
				composition.HardLimits.Enabled = false;
				_composer.Composition = composition;
				_composer.Damping = Vector2.zero;
				_composer.TargetOffset = Vector3.zero;
				_composer.CenterOnActivate = true;
			}
			LensSettings lens = _cam.Lens;
			lens.FieldOfView = _baseFov;
			lens.FarClipPlane = _farClip;
			_cam.Lens = lens;
			ApplyOffset();
		}

		private void OnDestroy()
		{
			if (_boat != null)
			{
				_boat.RespawnedVisual -= OnBoatRespawned;
			}
			if (Active == this)
			{
				Active = null;
			}
			if (_proxy != null)
			{
				UnityEngine.Object.Destroy(_proxy.gameObject);
			}
		}

		private void OnBoatRespawned()
		{
			_snapForwardOnRespawn = true;
		}

		private void LateUpdate()
		{
			ReadCameraInput();
			if (_boat == null)
			{
				if (!(Time.time < _nextSearch))
				{
					_nextSearch = Time.time + _retrySec;
					_boat = FindLocalTeamBoat();
					if (!(_boat == null))
					{
						_boat.RespawnedVisual += OnBoatRespawned;
						_boatRb = _boat.GetComponent<Rigidbody>();
						_heading = _boat.transform.forward;
						_yaw = Quaternion.LookRotation(_heading, Vector3.up).eulerAngles.y;
						_proxy.SetPositionAndRotation(ProxyPos(), Quaternion.Euler(0f, _yaw, 0f));
						_cam.Follow = _proxy;
						_cam.LookAt = _proxy;
					}
				}
				return;
			}
			PiratePlayer piratePlayer = LocalPirate();
			if (piratePlayer != null && _boat.BoatIndex != piratePlayer.BoatIndex && NetworkBoat.FindByIndex(piratePlayer.BoatIndex) != null)
			{
				_boat.RespawnedVisual -= OnBoatRespawned;
				_boat = null;
				return;
			}
			if (_snapForwardOnRespawn)
			{
				_snapForwardOnRespawn = false;
				_mode = CamMode.Boat;
				_yaw = _boat.transform.eulerAngles.y + SeatYawOffset();
				_heading = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
			}
			Vector3 vector = ((_boatRb != null) ? _boatRb.linearVelocity : Vector3.zero);
			vector.y = 0f;
			float magnitude = vector.magnitude;
			switch (_mode)
			{
			case CamMode.Boat:
			{
				float b2 = _boat.transform.eulerAngles.y + SeatYawOffset();
				_yaw = Mathf.LerpAngle(_yaw, b2, 1f - Mathf.Exp((0f - _boatYawLerp) * Time.deltaTime));
				_heading = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
				break;
			}
			case CamMode.Follow:
				if (magnitude > _minSpeedForHeading)
				{
					Vector3 b = vector / magnitude;
					_heading = Vector3.Slerp(_heading, b, 1f - Mathf.Exp((0f - _headingLerp) * Time.deltaTime));
				}
				_yaw = Quaternion.LookRotation(_heading, Vector3.up).eulerAngles.y;
				break;
			case CamMode.Free:
			case CamMode.Locked:
				_heading = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
				break;
			}
			float t = 1f - Mathf.Exp((0f - _camEaseLerp) * Time.deltaTime);
			_pitchView = Mathf.Lerp(_pitchView, _pitch, t);
			_distView = Mathf.Lerp(_distView, _dist, t);
			bool flag = Time.time < _focusUntil;
			_focusBlend = Mathf.MoveTowards(_focusBlend, flag ? 1f : 0f, Time.deltaTime / (flag ? 0.6f : 0.8f));
			float yaw = _yaw;
			float distView = _distView;
			Vector3 vector2 = ProxyPos();
			if (_focusBlend > 0.0001f)
			{
				float t2 = _focusBlend * _focusBlend * (3f - 2f * _focusBlend);
				Vector3 vector3 = _focusPoint - _boat.transform.position;
				vector3.y = 0f;
				if (vector3.sqrMagnitude > 0.01f)
				{
					_yaw = Mathf.LerpAngle(_yaw, Mathf.Atan2(vector3.x, vector3.z) * 57.29578f, t2);
				}
				_distView = Mathf.Lerp(_distView, _focusDist, t2);
				vector2 = Vector3.Lerp(vector2, _focusPoint, t2);
			}
			_proxy.SetPositionAndRotation(vector2, Quaternion.Euler(0f, _yaw, 0f));
			ApplyOffset();
			_yaw = yaw;
			_distView = distView;
			if (_shakeTrauma > 0.0001f && _follow != null)
			{
				float num = _shakeTrauma * _shakeTrauma;
				float y = Time.time * 22f;
				Vector3 vector4 = new Vector3((Mathf.PerlinNoise(_shakeSeed, y) - 0.5f) * 2f, (Mathf.PerlinNoise(_shakeSeed + 11f, y) - 0.5f) * 2f, (Mathf.PerlinNoise(_shakeSeed + 23f, y) - 0.5f) * 2f) * (0.35f * num);
				_follow.FollowOffset += vector4;
				_shakeTrauma = Mathf.Max(0f, _shakeTrauma - 1.8f * Time.deltaTime);
			}
			_gasShakeView = Mathf.Lerp(_gasShakeView, _gasShake01, 1f - Mathf.Exp(-6f * Time.deltaTime));
			if (_gasShakeView > 0.0001f && _follow != null)
			{
				float y2 = Time.time * 8f;
				float num2 = 0.65f + 0.35f * Mathf.PerlinNoise(_shakeSeed + 91f, Time.time * 5f);
				float num3 = _gasShakeAmplitude * _gasShakeView * num2;
				Vector3 vector5 = new Vector3((Mathf.PerlinNoise(_shakeSeed + 37f, y2) - 0.5f) * 2f, (Mathf.PerlinNoise(_shakeSeed + 53f, y2) - 0.5f) * 2f * 1.3f, (Mathf.PerlinNoise(_shakeSeed + 71f, y2) - 0.5f) * 2f) * num3;
				_follow.FollowOffset += vector5;
			}
			float num4 = Mathf.Clamp01(magnitude / Mathf.Max(0.01f, _fovAtSpeed));
			float b3 = _baseFov + _maxFovBoost * num4;
			LensSettings lens = _cam.Lens;
			lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, b3, 1f - Mathf.Exp((0f - _fovLerp) * Time.deltaTime));
			_cam.Lens = lens;
		}

		private void ReadCameraInput()
		{
			if ((_connection == null || _input == null) && Time.unscaledTime >= _launcherRetryAt)
			{
				_launcherRetryAt = Time.unscaledTime + 0.5f;
				if (_connection == null)
				{
					this.TryGetSystem<ConnectionSystem, CoreSystemHandler>(out _connection);
				}
				if (_input == null)
				{
					this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out _input);
				}
			}
			InputPlayer inputPlayer = _input?.Player;
			string text = inputPlayer?.ActiveControlScheme ?? "Keyboard&Mouse";
			bool flag = text == "Keyboard&Mouse";
			bool flag2 = _input != null && _input.InputBlocked;
			bool flag3 = _connection != null && _connection.State == LauncherState.Racing;
			ApplyCursorLock(((text != "Touch") & flag3) && !flag2);
			if (text == "Touch")
			{
				_mode = CamMode.Free;
				float num = TouchControls.ConsumePinchDelta();
				if (Mathf.Abs(num) > 0.01f)
				{
					_dist = Mathf.Clamp(_dist - num * 0.03f, _distanceMin, _distanceMax);
				}
				Vector2 vector = TouchControls.ConsumeCameraDrag();
				if (Mathf.Abs(vector.x) > 0.01f || Mathf.Abs(vector.y) > 0.01f)
				{
					_touchCamIdle = 0f;
					_yaw += vector.x * 0.18f;
					_pitch = Mathf.Clamp(_pitch - vector.y * 0.12f, _pitchMin, _pitchMax);
				}
				else if (Mathf.Abs(num) <= 0.01f && _boat != null)
				{
					_touchCamIdle += Time.deltaTime;
					if (_touchCamIdle > 1.5f)
					{
						_yaw = Mathf.LerpAngle(_yaw, _boat.transform.eulerAngles.y, 1f - Mathf.Exp(-2f * Time.deltaTime));
					}
				}
			}
			else
			{
				if (flag2 || inputPlayer == null)
				{
					return;
				}
				bool flag4 = false;
				bool flag5 = text == "Gamepad";
				if (inputPlayer.GetButtonDown("Camera/CycleMode") && !flag)
				{
					_mode = _mode switch
					{
						CamMode.Boat => CamMode.Follow, 
						CamMode.Follow => CamMode.Free, 
						CamMode.Free => CamMode.Locked, 
						_ => CamMode.Boat, 
					};
					Toast("Kamera: " + ModeName(_mode));
				}
				if (inputPlayer.GetButtonDown("Camera/Reset"))
				{
					_mode = CamMode.Boat;
					_pitch = _defaultPitch;
					_dist = _defaultDistance;
					_panOffset = Vector3.zero;
					ApplyOffset();
					Toast("Kamera sıfırlandı (BOT EKSENİ)");
				}
				float num2 = inputPlayer.GetFloat("Camera/Pan");
				if (Mathf.Abs(num2) > 0.01f)
				{
					Vector3 vector2 = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
					_panOffset += vector2 * (num2 * _panSpeed * Time.deltaTime);
					if (_panOffset.magnitude > _panMax)
					{
						_panOffset = _panOffset.normalized * _panMax;
					}
				}
				else if (_panRecenter > 0f && _panOffset.sqrMagnitude > 0.0001f)
				{
					_panOffset = Vector3.MoveTowards(_panOffset, Vector3.zero, _panRecenter * Time.deltaTime);
				}
				Vector2 vector3 = inputPlayer.GetVector2("Camera/Orbit");
				float num3 = (flag5 ? _padYawSpeed : _keyYawSpeed);
				float num4 = (flag5 ? (0f - _padPitchSpeed) : _keyPitchSpeed);
				if (Mathf.Abs(vector3.x) > 0.01f)
				{
					if (_mode != CamMode.Free)
					{
						SwitchToFree();
					}
					_yaw += vector3.x * num3 * Time.deltaTime;
					if (flag5)
					{
						flag4 = true;
					}
				}
				if (Mathf.Abs(vector3.y) > 0.01f)
				{
					_pitch = Mathf.Clamp(_pitch + vector3.y * num4 * Time.deltaTime, _pitchMin, _pitchMax);
					flag4 = true;
				}
				if (flag)
				{
					Vector2 vector4 = inputPlayer.GetVector2("Camera/MouseOrbit");
					if (vector4.sqrMagnitude > 0.01f)
					{
						if (_mode != CamMode.Free)
						{
							SwitchToFree();
						}
						float num5 = _mouseOrbitSens * (GameSettings.MouseSensitivity / 0.5f);
						float num6 = (GameSettings.InvertMouse ? 1f : (-1f));
						_yaw += vector4.x * num5;
						_pitch = Mathf.Clamp(_pitch + num6 * vector4.y * num5, _pitchMin, _pitchMax);
						flag4 = true;
					}
				}
				float f = inputPlayer.GetFloat("Camera/ZoomStep");
				if (Mathf.Abs(f) > 0.01f)
				{
					_dist = Mathf.Clamp(_dist - Mathf.Sign(f) * _zoomStep, _distanceMin, _distanceMax);
					flag4 = true;
				}
				float num7 = inputPlayer.GetFloat("Camera/Zoom");
				if (Mathf.Abs(num7) > 0.01f)
				{
					_dist = Mathf.Clamp(_dist - num7 * _padZoomSpeed * Time.deltaTime, _distanceMin, _distanceMax);
					flag4 = true;
				}
				if (flag4)
				{
					ApplyOffset();
				}
			}
		}

		private void ApplyCursorLock(bool locked)
		{
			int num = (locked ? 1 : 0);
			CursorLockMode cursorLockMode = (locked ? CursorLockMode.Locked : CursorLockMode.None);
			if (_cursorLocked != num || Cursor.lockState != cursorLockMode)
			{
				_cursorLocked = num;
				Cursor.lockState = cursorLockMode;
				RefreshCursorVisibility();
			}
		}

		private void RefreshCursorVisibility()
		{
			if (_input != null)
			{
				_input.RefreshCursorVisibility();
			}
			else
			{
				Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
			}
		}

		private void OnDisable()
		{
			_cursorLocked = -1;
			Cursor.lockState = CursorLockMode.None;
			RefreshCursorVisibility();
		}

		public static void FocusOn(Vector3 point, float holdSeconds, float distance = 12f)
		{
			if (!(Active == null))
			{
				Active._focusPoint = point;
				Active._focusDist = distance;
				Active._focusUntil = Time.time + 0.6f + holdSeconds;
			}
		}

		public static void Shake(float trauma)
		{
			if (!(Active == null) && GameSettings.CameraShakeEnabled)
			{
				Active._shakeTrauma = Mathf.Clamp01(Active._shakeTrauma + Mathf.Clamp01(trauma));
			}
		}

		private Vector3 ProxyPos()
		{
			return _boat.transform.position;
		}

		private float SeatYawOffset()
		{
			PiratePlayer piratePlayer = LocalPirate();
			if (piratePlayer == null || piratePlayer.SectorHalfWidth >= 179.9f)
			{
				return 0f;
			}
			return piratePlayer.SectorCenter + 180f;
		}

		private PiratePlayer LocalPirate()
		{
			if (_localPirate != null)
			{
				return _localPirate;
			}
			_localPirate = PiratePlayer.Local;
			return _localPirate;
		}

		private void SwitchToFree()
		{
			if (_mode != CamMode.Free)
			{
				_mode = CamMode.Free;
				Toast("Kamera: SERBEST (V = sıfırla)");
			}
		}

		private void ApplyOffset()
		{
			if (!(_follow == null))
			{
				float f = _pitchView * (MathF.PI / 180f);
				Vector3 vector = new Vector3(0f, _distView * Mathf.Sin(f), (0f - _distView) * Mathf.Cos(f));
				Vector3 vector2 = Quaternion.Euler(0f, _yaw, 0f) * vector;
				_follow.FollowOffset = ResolveCollision(vector2 + _panOffset);
			}
		}

		private Vector3 ResolveCollision(Vector3 desired)
		{
			float magnitude = desired.magnitude;
			if (_boat == null || magnitude < 0.001f)
			{
				_occ = 1f;
				_lastOccFrame = Time.frameCount;
				_hasLastPivot = false;
				return desired;
			}
			Vector3 vector = desired / magnitude;
			if (Time.frameCount != _lastOccFrame)
			{
				_lastOccFrame = Time.frameCount;
				AdvanceOcclusion(magnitude, vector);
			}
			if (_occ >= 0.999f)
			{
				return desired;
			}
			return vector * Mathf.Max(0.5f, magnitude * _occ);
		}

		private void AdvanceOcclusion(float desiredLen, Vector3 dir)
		{
			if (!OcclusionEnabled)
			{
				_occ = 1f;
				_hasLastPivot = false;
				return;
			}
			Vector3 position = _boat.transform.position;
			if (_hasLastPivot && (position - _lastPivot).sqrMagnitude > 400f)
			{
				_occ = 1f;
			}
			_lastPivot = position;
			_hasLastPivot = true;
			float num = desiredLen;
			int num2 = Physics.SphereCastNonAlloc(position, 0.35f, dir, _camHits, desiredLen, _camCollisionMask, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < num2; i++)
			{
				RaycastHit raycastHit = _camHits[i];
				if (!(raycastHit.collider == null) && !(raycastHit.collider.GetComponentInParent<NetworkBoat>() != null) && !(raycastHit.collider.GetComponentInParent<PiratePlayer>() != null) && !(raycastHit.distance <= 0f) && raycastHit.distance < num)
				{
					num = raycastHit.distance;
				}
			}
			float num3 = Mathf.Clamp01(num / desiredLen);
			if (num3 < _occ)
			{
				_occ = num3;
			}
			else
			{
				_occ = Mathf.Lerp(_occ, num3, 1f - Mathf.Exp(-5f * Time.deltaTime));
			}
			_occ = Mathf.Clamp01(_occ);
		}

		private static int BuildCamMask()
		{
			int num = 6;
			int num2 = LayerMask.NameToLayer("Water");
			if (num2 >= 0)
			{
				num |= 1 << num2;
			}
			int num3 = LayerMask.NameToLayer("UI");
			if (num3 >= 0)
			{
				num |= 1 << num3;
			}
			return ~num;
		}

		private static string ModeName(CamMode m)
		{
			return m switch
			{
				CamMode.Boat => "BOT EKSENİ (feribotla döner)", 
				CamMode.Free => "SERBEST (ok tuşları / orta fare)", 
				CamMode.Locked => "SABİT (dünyaya kilitli)", 
				_ => "TAKİP (hız yönü)", 
			};
		}

		private void Toast(string msg)
		{
			if (!(_connection == null))
			{
				_connection.RaiseToast(msg);
			}
		}

		private NetworkBoat FindLocalTeamBoat()
		{
			_localPirate = null;
			if (NetworkBoat.All.Count == 0)
			{
				return null;
			}
			PiratePlayer local = PiratePlayer.Local;
			if (local != null)
			{
				NetworkBoat networkBoat = NetworkBoat.FindByIndex(local.BoatIndex);
				if (networkBoat != null)
				{
					_localPirate = local;
					return networkBoat;
				}
			}
			return NetworkBoat.All[0];
		}
	}
}
