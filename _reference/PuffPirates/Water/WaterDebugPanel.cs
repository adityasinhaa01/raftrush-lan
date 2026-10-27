using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using GamesMrkt.PuffPirates;
using PuffPirates.Audio;
using PuffPirates.Boats;
using PuffPirates.CameraRig;
using PuffPirates.Networking;
using PuffPirates.Obstacles.Physics;
using PuffPirates.Players;
using PuffPirates.Settings;
using PuffPirates.Systems;
using PuffPirates.Track;
using UnityEngine;

namespace PuffPirates.Water
{
	[DisallowMultipleComponent]
	public sealed class WaterDebugPanel : MonoBehaviour
	{
		[SerializeField]
		private float _levelOffset;

		[SerializeField]
		private float _waveAmp = 0.3f;

		[SerializeField]
		private float _waveLen = 8f;

		[SerializeField]
		private float _waveSpd = 1.1f;

		[SerializeField]
		private float _waterDensity = 1000f;

		[SerializeField]
		private float _boatDensity = 450f;

		[SerializeField]
		private float _propDensity = 450f;

		[Tooltip("F2'de canlı geçilen su shader presetleri (Uber / StylizedWater / FlatKit materyalleri).")]
		[SerializeField]
		private Material[] _waterPresets;

		private int _presetIndex;

		private int _lastNetWaterId = int.MinValue;

		private float _lastNetGasXf1 = float.MinValue;

		private Material _liveMat;

		private bool _showMatProps;

		private Vector2 _scroll;

		private bool _open = true;

		private bool _buoyancyOn;

		private float _waterFriction = 1.2f;

		private float _glideFriction;

		private float _boatMass = 40f;

		private float _boatRide = 0.28f;

		private bool _noGasAir;

		private float _gasRampSeconds = 3f;

		private float _minGasForce = 100f;

		private float _gasReleaseSeconds = 0.5f;

		private float _waterBobAmplitude;

		private float _waterBobFrequency = 0.5f;

		private float _waterBobWaveLength = 0.6f;

		private float _waterBobSpeedThreshold = 3f;

		private float _waterBobFadeRange = 2f;

		private float _refreshTimer;

		private readonly List<WaterChannel> _channels = new List<WaterChannel>();

		private readonly List<BuoyantBody> _boats = new List<BuoyantBody>();

		private readonly List<BuoyantBody> _props = new List<BuoyantBody>();

		private readonly List<BoatPhysicsController> _boatPhysics = new List<BoatPhysicsController>();

		private readonly List<GasVisuals> _gasVisuals = new List<GasVisuals>();

		private float _flameMinForce = 0.6f;

		private float _flameHold;

		private bool _flameOnAll = true;

		private bool _flameHardCutAll = true;

		private float _gasXf1 = 70f;

		private float _gasXf2 = 40f;

		private float _gasCrossfadeDur = 0.5f;

		private float _gasReleaseDur = 0.3f;

		private float _gasReleasePitch = 1.7f;

		private float _contentHeight = 2062f;

		private readonly List<SeesawDisk> _seesaws = new List<SeesawDisk>();

		private float _seesawTilt = 18f;

		private float _seesawLoose = 0.5f;

		private InputPlayer _inputPlayer;

		private BoatCameraRig _camRig;

		private float _padSens = GameSettings.PadSensitivity;

		private static bool PanelAvailable => Application.isEditor;

		private void OnEnable()
		{
			if (!PanelAvailable)
			{
				_open = false;
			}
			Rescan();
			if (_channels.Count > 0 && _channels[0] != null)
			{
				_waveAmp = _channels[0].WaveAmplitude;
				_waveLen = _channels[0].WaveLength;
				_waveSpd = _channels[0].WaveSpeed;
			}
			if (_boats.Count > 0 && _boats[0] != null)
			{
				_waterFriction = _boats[0].LinearDrag;
			}
			if (_boatPhysics.Count > 0 && _boatPhysics[0] != null)
			{
				_glideFriction = _boatPhysics[0].GlideFriction;
				_boatMass = _boatPhysics[0].Mass;
				_boatRide = _boatPhysics[0].WaterRideHeight;
				_noGasAir = _boatPhysics[0].NoGasWhenAirborne;
				_gasRampSeconds = _boatPhysics[0].GasRampSeconds;
				_minGasForce = _boatPhysics[0].MinGasForce;
				_gasReleaseSeconds = _boatPhysics[0].GasReleaseSeconds;
				_waterBobAmplitude = _boatPhysics[0].WaterBobAmplitude;
				_waterBobFrequency = _boatPhysics[0].WaterBobFrequency;
				_waterBobWaveLength = _boatPhysics[0].WaterBobWaveLength;
				_waterBobSpeedThreshold = _boatPhysics[0].WaterBobSpeedThreshold;
				_waterBobFadeRange = _boatPhysics[0].WaterBobFadeRange;
			}
			if (_gasVisuals.Count > 0 && _gasVisuals[0] != null)
			{
				_flameOnAll = _gasVisuals[0].FlameEnabled;
				_flameMinForce = _gasVisuals[0].FlameMinForce01;
				_flameHold = _gasVisuals[0].FlameHoldSeconds;
				_flameHardCutAll = _gasVisuals[0].FlameHardCut;
			}
			SyncGasPresetFields();
			SeesawDisk seesawDisk = ((_seesaws.Count > 0) ? _seesaws[0] : null);
			if (seesawDisk != null && seesawDisk.IsSpawned)
			{
				_seesawTilt = ((seesawDisk.MaxTiltAngleDeg > 0f) ? seesawDisk.MaxTiltAngleDeg : 18f);
				float value = ((seesawDisk.TiltSpringForce > 0f) ? seesawDisk.TiltSpringForce : 900f);
				_seesawLoose = Mathf.InverseLerp(2000f, 100f, value);
			}
			ApplyLevel(_levelOffset);
			ApplyWater(_waterDensity);
			ApplyBoat(_boatDensity);
			ApplyProp(_propDensity);
			LevelBuilder.BuoyancyOverride = _buoyancyOn;
		}

		private void OnDisable()
		{
			LevelBuilder.BuoyancyOverride = null;
			if (_liveMat != null)
			{
				UnityEngine.Object.Destroy(_liveMat);
				_liveMat = null;
			}
		}

		private bool IsTogglePressed()
		{
			if (_inputPlayer == null && this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out var system))
			{
				_inputPlayer = system.Player;
			}
			if (_inputPlayer != null)
			{
				return _inputPlayer.GetButtonDown("Debug/ToggleWaterPanel");
			}
			return false;
		}

		private void Update()
		{
			if (PanelAvailable && IsTogglePressed())
			{
				_open = !_open;
			}
			RaceState instance = RaceState.Instance;
			if (instance != null && instance.WaterId != _lastNetWaterId)
			{
				if (instance.WaterId < 0)
				{
					_lastNetWaterId = instance.WaterId;
				}
				else
				{
					if (_channels.Count == 0)
					{
						Rescan();
					}
					if (_channels.Count > 0)
					{
						_lastNetWaterId = instance.WaterId;
						ApplyPresetByIndex(instance.WaterId);
					}
				}
			}
			if (instance != null && instance.GasXf1Start != _lastNetGasXf1)
			{
				if (instance.GasXf1Start < 0f)
				{
					_lastNetGasXf1 = instance.GasXf1Start;
				}
				else
				{
					_lastNetGasXf1 = instance.GasXf1Start;
					ApplyNetworkedGasAudio(instance);
				}
			}
			_refreshTimer -= Time.deltaTime;
			if (_refreshTimer <= 0f)
			{
				_refreshTimer = 1.5f;
				if (_open)
				{
					Rescan();
				}
			}
		}

		private void ApplyNetworkedGasAudio(RaceState rs)
		{
			GasAudioSettings.Xf1Start = rs.GasXf1Start;
			GasAudioSettings.Xf2Start = rs.GasXf2Start;
			GasAudioSettings.CrossfadeDuration = rs.GasCrossfadeDuration;
			GasAudioSettings.ReleaseFadeDuration = rs.GasReleaseFadeDuration;
			GasAudioSettings.ReleasePitchTarget = rs.GasReleasePitchTarget;
			SyncGasPresetFields();
		}

		private void PushGasAudioToNetwork()
		{
			RaceState instance = RaceState.Instance;
			if (!(instance == null))
			{
				instance.HostSetGasAudio(GasAudioSettings.Xf1Start, GasAudioSettings.Xf2Start, GasAudioSettings.CrossfadeDuration, GasAudioSettings.ReleaseFadeDuration, GasAudioSettings.ReleasePitchTarget);
			}
		}

		private void Rescan()
		{
			_channels.Clear();
			_boats.Clear();
			_props.Clear();
			_boatPhysics.Clear();
			_gasVisuals.Clear();
			_seesaws.Clear();
			WaterChannel[] array = UnityEngine.Object.FindObjectsByType<WaterChannel>();
			foreach (WaterChannel item in array)
			{
				_channels.Add(item);
			}
			BuoyantBody[] array2 = UnityEngine.Object.FindObjectsByType<BuoyantBody>();
			foreach (BuoyantBody buoyantBody in array2)
			{
				if (buoyantBody.GetComponentInParent<NetworkBoat>() != null)
				{
					_boats.Add(buoyantBody);
				}
				else
				{
					_props.Add(buoyantBody);
				}
			}
			BoatPhysicsController[] array3 = UnityEngine.Object.FindObjectsByType<BoatPhysicsController>();
			foreach (BoatPhysicsController item2 in array3)
			{
				_boatPhysics.Add(item2);
			}
			GasVisuals[] array4 = UnityEngine.Object.FindObjectsByType<GasVisuals>();
			foreach (GasVisuals item3 in array4)
			{
				_gasVisuals.Add(item3);
			}
			SeesawDisk[] array5 = UnityEngine.Object.FindObjectsByType<SeesawDisk>();
			foreach (SeesawDisk item4 in array5)
			{
				_seesaws.Add(item4);
			}
			_camRig = UnityEngine.Object.FindFirstObjectByType<BoatCameraRig>();
		}

		private void OnGUI()
		{
			if (!PanelAvailable)
			{
				return;
			}
			if (!_open)
			{
				GUI.Label(new Rect(12f, 10f, 340f, 22f), "[F2] Su ayar panelini aç");
				return;
			}
			float num = Mathf.Min((float)Screen.height - 20f, 760f);
			GUI.Box(new Rect(10f, 10f, 346f, num), "SU AYARLARI v2 (test)   [F2] gizle");
			_scroll = GUI.BeginScrollView(new Rect(14f, 34f, 346f, num - 30f), viewRect: new Rect(0f, 0f, 322f, _contentHeight), scrollPosition: _scroll);
			float num2 = 4f;
			if (GUI.Button(new Rect(20f, num2, 290f, 26f), "\ud83d\udcbe Tüm değerleri dosyaya kaydet"))
			{
				SaveValuesToFile();
			}
			num2 += 34f;
			if (_waterPresets != null && _waterPresets.Length != 0)
			{
				Material material = _waterPresets[_presetIndex];
				GUI.Label(new Rect(20f, num2, 300f, 20f), string.Format("Su shader'ı ({0}/{1}): {2}", _presetIndex + 1, _waterPresets.Length, (material != null) ? material.name : "-"));
				if (GUI.Button(new Rect(20f, num2 + 20f, 60f, 24f), "◀"))
				{
					CyclePreset(-1);
				}
				if (GUI.Button(new Rect(250f, num2 + 20f, 60f, 24f), "▶"))
				{
					CyclePreset(1);
				}
				num2 += 52f;
			}
			num2 = Row(num2, "Su yüksekliği", ref _levelOffset, -12f, 12f, ApplyLevel);
			num2 = Row(num2, "Dalga yüksekliği", ref _waveAmp, 0f, 2f, ApplyWaveAmp);
			num2 = Row(num2, "Dalga boyu", ref _waveLen, 1f, 20f, ApplyWaveLen);
			num2 = Row(num2, "Dalga hızı", ref _waveSpd, 0f, 4f, ApplyWaveSpd);
			num2 = Row(num2, "Su yoğunluğu", ref _waterDensity, 100f, 8000f, ApplyWater);
			num2 = Row(num2, "Bot yoğunluğu", ref _boatDensity, 50f, 2000f, ApplyBoat);
			num2 = Row(num2, "Obje yoğunluğu", ref _propDensity, 50f, 2000f, ApplyProp);
			num2 = RowU(num2, "Su sürtünmesi (YENİ)", ref _waterFriction, 0f, 5f, ApplyWaterFriction);
			num2 = RowU(num2, "Yatay sürtünme (ESKİ)", ref _glideFriction, 0f, 5f, ApplyGlideFriction);
			num2 = RowU(num2, "Bot kütlesi (kg)", ref _boatMass, 5f, 300f, ApplyMass);
			num2 = RowU(num2, "Bot su yük. (ESKİ ↓batık)", ref _boatRide, 0.05f, 0.6f, ApplyBoatRide);
			num2 = RowU(num2, "İtiş ramp süresi (sn)", ref _gasRampSeconds, 0.2f, 8f, ApplyGasRampSeconds);
			num2 = RowU(num2, "İtiş taban gücü", ref _minGasForce, 0f, 2000f, ApplyMinGasForce);
			num2 = RowU(num2, "Bırakma azalma süresi (sn)", ref _gasReleaseSeconds, 0.05f, 5f, ApplyGasReleaseSeconds);
			num2 = RowU(num2, "Sahte dalga bobbing genliği", ref _waterBobAmplitude, 0f, 0.3f, ApplyWaterBobAmplitude);
			num2 = RowU(num2, "Sahte dalga bobbing hızı", ref _waterBobFrequency, 0f, 2f, ApplyWaterBobFrequency);
			num2 = RowU(num2, "Sahte dalga boyu (küçük=zıt taraf)", ref _waterBobWaveLength, 0.1f, 2f, ApplyWaterBobWaveLength);
			num2 = RowU(num2, "Bobbing hız eşiği (üstünde söner)", ref _waterBobSpeedThreshold, 0f, 15f, ApplyWaterBobSpeedThreshold);
			num2 = RowU(num2, "Bobbing sönme mesafesi", ref _waterBobFadeRange, 0.1f, 10f, ApplyWaterBobFadeRange);
			GUI.Label(new Rect(20f, num2, 300f, 20f), "Mekanik (bu harita):");
			string text = (_buoyancyOn ? "YENİ — buoyancy (batar/yüzer)" : "ESKİ — kuru sürüş (raycast)");
			if (GUI.Button(new Rect(20f, num2 + 20f, 290f, 24f), text))
			{
				_buoyancyOn = !_buoyancyOn;
				LevelBuilder.BuoyancyOverride = _buoyancyOn;
			}
			num2 += 52f;
			GUI.Label(new Rect(20f, num2, 300f, 20f), "Havada gaz (bot yerde değilken):");
			string text2 = (_noGasAir ? "KESİK — havada gaz YOK" : "AÇIK — havada da gaz verir");
			if (GUI.Button(new Rect(20f, num2 + 20f, 290f, 24f), text2))
			{
				_noGasAir = !_noGasAir;
				ApplyNoGasAir(_noGasAir);
			}
			num2 += 52f;
			GUI.Label(new Rect(20f, num2, 300f, 20f), "Duvar öldürme (bariyer respawn):");
			bool globallyEnabled = BarrierKill.GloballyEnabled;
			if (GUI.Button(new Rect(20f, num2 + 20f, 290f, 24f), globallyEnabled ? "AÇIK — duvara değen başa döner" : "KAPALI — duvar katı ama öldürmez"))
			{
				BarrierKill.GloballyEnabled = !globallyEnabled;
			}
			num2 += 52f;
			GUI.Label(new Rect(20f, num2, 300f, 20f), "Odada tek başına başlat:");
			bool editorSoloStart = LobbySystem.EditorSoloStart;
			if (GUI.Button(new Rect(20f, num2 + 20f, 290f, 24f), editorSoloStart ? "AÇIK — 1 kişiyle başlar" : "KAPALI — min 2 oyuncu"))
			{
				LobbySystem.EditorSoloStart = !editorSoloStart;
			}
			num2 += 52f;
			GUI.Label(new Rect(20f, num2, 320f, 20f), "Gaz sesi ayarları:");
			num2 += 22f;
			num2 = RowU(num2, "Yüksek biter / Düşük başlar (%)", ref _gasXf1, 0f, 100f, (float v) =>
			{
				GasAudioSettings.Xf1Start = v;
				PushGasAudioToNetwork();
			});
			num2 = RowU(num2, "Düşük biter / Damping başlar (%)", ref _gasXf2, 0f, 100f, (float v) =>
			{
				GasAudioSettings.Xf2Start = v;
				PushGasAudioToNetwork();
			});
			num2 = RowU(num2, "Crossfade süresi (sn)", ref _gasCrossfadeDur, 0.05f, 2f, (float v) =>
			{
				GasAudioSettings.CrossfadeDuration = v;
				PushGasAudioToNetwork();
			});
			num2 = RowU(num2, "Bırakma fade süresi (sn)", ref _gasReleaseDur, 0.05f, 2f, (float v) =>
			{
				GasAudioSettings.ReleaseFadeDuration = v;
				PushGasAudioToNetwork();
			});
			num2 = RowU(num2, "Bırakma pitch hedefi", ref _gasReleasePitch, -3f, 3f, (float v) =>
			{
				GasAudioSettings.ReleasePitchTarget = v;
				PushGasAudioToNetwork();
			});
			GUI.Label(new Rect(20f, num2, 320f, 20f), $"Seesaw ({_seesaws.Count} disk) — dönme + gevşeklik:");
			num2 += 22f;
			num2 = RowU(num2, "Seesaw eğim açısı (°)", ref _seesawTilt, 1f, 45f, ApplySeesaw);
			num2 = RowU(num2, "Seesaw gevşekliği", ref _seesawLoose, 0f, 1f, ApplySeesaw);
			GUI.Label(new Rect(20f, num2, 300f, 20f), "Malzeme (su görünümü):");
			if (GUI.Button(new Rect(20f, num2 + 20f, 290f, 24f), _showMatProps ? "Gizle ▲" : "Göster ▼ (renk / köpük / dalga)"))
			{
				_showMatProps = !_showMatProps;
			}
			num2 += 50f;
			if (_showMatProps)
			{
				EnsureLiveMat();
				if (_liveMat == null)
				{
					GUI.Label(new Rect(20f, num2, 320f, 20f), "Su yüzeyi bulunamadı.");
					num2 += 24f;
				}
				else
				{
					num2 = MatColorRow(num2, "Sığ su rengi (R/G/B)", "_Color_Shallow", 2f);
					num2 = MatColorRow(num2, "Derin su rengi (R/G/B)", "_Color_Deep", 2f);
					num2 = MatColorRow(num2, "Köpük rengi (R/G/B)", "_SurfFoam_Color", 1f);
					num2 = MatFloatRow(num2, "Köpük miktarı", "_SurfFoam_AlphaBlend", 0f, 1f);
					num2 = MatFloatRow(num2, "Köpük sıklığı", "_SurfFoam_Scale", 0.5f, 12f);
					num2 = MatFloatRow(num2, "Su derinliği (renk)", "_Water_Depth", 0f, 3f);
					num2 = WaveToggleRow(num2);
					num2 = MatFloat2Row(num2, "Dalga yük. (shader)", "_1st_Wave_Height", "_2nd_Wave_Height", 0f, 0.6f);
					num2 = MatFloat2Row(num2, "Dalga hızı (shader)", "_1st_Wave_Speed", "_2nd_Wave_Speed", 0f, 3f);
					num2 = MatFloat2Row(num2, "Dalga boyu (shader)", "_1st_Wave_Length", "_2nd_Wave_Length", 1f, 12f);
				}
			}
			if (_camRig != null)
			{
				float gasShakeAmplitude = _camRig.GasShakeAmplitude;
				GUI.Label(new Rect(20f, num2, 300f, 20f), $"Kamera sallanması (gaz): {gasShakeAmplitude:0.00}");
				float num3 = GUI.HorizontalSlider(new Rect(20f, num2 + 20f, 290f, 18f), gasShakeAmplitude, 0f, 0.5f);
				if (!Mathf.Approximately(num3, gasShakeAmplitude))
				{
					_camRig.GasShakeAmplitude = num3;
				}
				num2 += 46f;
			}
			GUI.Label(new Rect(20f, num2, 320f, 20f), "Joystick ayarları:");
			num2 += 22f;
			num2 = RowU(num2, "Direksiyon hassasiyeti", ref _padSens, 0.2f, 2f, (float v) =>
			{
				GameSettings.PadSensitivity = v;
			});
			if (_camRig != null)
			{
				float val = _camRig.PadYawSpeed;
				num2 = RowU(num2, "Kamera yatay hızı (°/sn)", ref val, 10f, 400f, (float v) =>
				{
					_camRig.PadYawSpeed = v;
				});
				val = _camRig.PadPitchSpeed;
				num2 = RowU(num2, "Kamera dikey hızı (°/sn)", ref val, 10f, 400f, (float v) =>
				{
					_camRig.PadPitchSpeed = v;
				});
				val = _camRig.PadZoomSpeed;
				num2 = RowU(num2, "Zoom hızı (m/sn)", ref val, 1f, 40f, (float v) =>
				{
					_camRig.PadZoomSpeed = v;
				});
			}
			num2 = PadRebindRow(num2, "Sola", GameAction.MoveLeft);
			num2 = PadRebindRow(num2, "Sağa", GameAction.MoveRight);
			num2 = PadRebindRow(num2, "Gaz", GameAction.NormalGas);
			foreach (GasVisuals gasVisual in _gasVisuals)
			{
				if (!(gasVisual == null) && gasVisual.IsSpawned && gasVisual.IsOwner)
				{
					PirateGas pirateGas = gasVisual.GetComponent<PiratePlayer>()?.Gas;
					if (!(pirateGas == null))
					{
						float num4 = ((pirateGas.MaxGas > 0.001f) ? (pirateGas.GasEnergy / pirateGas.MaxGas) : 0f);
						GUI.Label(new Rect(20f, num2, 340f, 20f), $"Depo (yerel): {pirateGas.GasEnergy:0.0}/{pirateGas.MaxGas:0.0}  dolu%={num4 * 100f:0}  boşalma01={1f - num4:0.00}");
						num2 += 22f;
						GUI.Label(new Rect(20f, num2, 340f, 20f), $"Gaz gücü (yerel): basılı={pirateGas.IsGassing}  charge={pirateGas.GasCharge:0.00}  " + $"powerFraction={pirateGas.GasPowerFraction:0.00}  fxForce={pirateGas.GasFxForce:0}");
						num2 += 22f;
					}
					break;
				}
			}
			GUI.Label(new Rect(20f, num2, 300f, 40f), "Yoğunluk SUdan KÜÇÜK → yüzer\nBÜYÜK → batar.");
			num2 += 44f;
			if (Event.current.type == EventType.Repaint)
			{
				_contentHeight = num2;
			}
			GUI.EndScrollView();
		}

		private float Row(float y, string label, ref float val, float min, float max, Action<float> apply)
		{
			GUI.Label(new Rect(20f, y, 300f, 20f), $"{label}: {val:0} kg/m³");
			float num = GUI.HorizontalSlider(new Rect(20f, y + 20f, 290f, 18f), val, min, max);
			if (!Mathf.Approximately(num, val))
			{
				val = num;
				apply(val);
			}
			return y + 46f;
		}

		private float RowU(float y, string label, ref float val, float min, float max, Action<float> apply)
		{
			GUI.Label(new Rect(20f, y, 300f, 20f), $"{label}: {val:0.0}");
			float num = GUI.HorizontalSlider(new Rect(20f, y + 20f, 290f, 18f), val, min, max);
			if (!Mathf.Approximately(num, val))
			{
				val = num;
				apply(val);
			}
			return y + 46f;
		}

		private float PadRebindRow(float y, string label, GameAction action)
		{
			GUI.Label(new Rect(20f, y, 150f, 22f), label);
			string text = (KeyBindings.IsRebinding ? "..." : KeyBindings.DisplayName(action, "Gamepad"));
			if (GUI.Button(new Rect(170f, y, 140f, 22f), text) && !KeyBindings.IsRebinding)
			{
				KeyBindings.RebindAsync(action, "Gamepad", base.destroyCancellationToken).Forget();
			}
			return y + 28f;
		}

		private void ApplyLevel(float v)
		{
			foreach (WaterChannel channel in _channels)
			{
				if (channel != null)
				{
					channel.LevelOffset = v;
				}
			}
		}

		private void ApplyWaveAmp(float v)
		{
			foreach (WaterChannel channel in _channels)
			{
				if (channel != null)
				{
					channel.WaveAmplitude = v;
				}
			}
		}

		private void ApplyWaveLen(float v)
		{
			foreach (WaterChannel channel in _channels)
			{
				if (channel != null)
				{
					channel.WaveLength = v;
				}
			}
		}

		private void ApplyWaveSpd(float v)
		{
			foreach (WaterChannel channel in _channels)
			{
				if (channel != null)
				{
					channel.WaveSpeed = v;
				}
			}
		}

		private void ApplyWater(float v)
		{
			foreach (WaterChannel channel in _channels)
			{
				if (channel != null)
				{
					channel.WaterDensity = v;
				}
			}
		}

		private void ApplyBoat(float v)
		{
			foreach (BuoyantBody boat in _boats)
			{
				if (boat != null)
				{
					boat.BodyDensity = v;
				}
			}
		}

		private void ApplyProp(float v)
		{
			foreach (BuoyantBody prop in _props)
			{
				if (prop != null)
				{
					prop.BodyDensity = v;
				}
			}
		}

		private void ApplyWaterFriction(float v)
		{
			foreach (BuoyantBody boat in _boats)
			{
				if (boat != null)
				{
					boat.LinearDrag = v;
				}
			}
		}

		private void ApplyGlideFriction(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.GlideFriction = v;
				}
			}
		}

		private void ApplyMass(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.Mass = v;
				}
			}
		}

		private void ApplyBoatRide(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.WaterRideHeight = v;
				}
			}
		}

		private void ApplyNoGasAir(bool v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.NoGasWhenAirborne = v;
				}
			}
		}

		private void ApplyGasRampSeconds(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.GasRampSeconds = v;
				}
			}
		}

		private void ApplyMinGasForce(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.MinGasForce = v;
				}
			}
		}

		private void ApplyGasReleaseSeconds(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.GasReleaseSeconds = v;
				}
			}
		}

		private void ApplyWaterBobAmplitude(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.WaterBobAmplitude = v;
				}
			}
		}

		private void ApplyWaterBobFrequency(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.WaterBobFrequency = v;
				}
			}
		}

		private void ApplyWaterBobWaveLength(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.WaterBobWaveLength = v;
				}
			}
		}

		private void ApplyWaterBobSpeedThreshold(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.WaterBobSpeedThreshold = v;
				}
			}
		}

		private void ApplyWaterBobFadeRange(float v)
		{
			foreach (BoatPhysicsController boatPhysic in _boatPhysics)
			{
				if (boatPhysic != null)
				{
					boatPhysic.WaterBobFadeRange = v;
				}
			}
		}

		private void SyncGasPresetFields()
		{
			_gasXf1 = GasAudioSettings.Xf1Start;
			_gasXf2 = GasAudioSettings.Xf2Start;
			_gasCrossfadeDur = GasAudioSettings.CrossfadeDuration;
			_gasReleaseDur = GasAudioSettings.ReleaseFadeDuration;
			_gasReleasePitch = GasAudioSettings.ReleasePitchTarget;
		}

		private void ApplySeesaw(float _)
		{
			float spring = Mathf.Lerp(2000f, 100f, _seesawLoose);
			float damping = Mathf.Lerp(600f, 40f, _seesawLoose);
			float returnForce = Mathf.Lerp(4000f, 1200f, _seesawLoose);
			foreach (SeesawDisk seesaw in _seesaws)
			{
				if (seesaw != null)
				{
					seesaw.SetTuning(_seesawTilt, spring, damping, returnForce);
				}
			}
		}

		private void CyclePreset(int dir)
		{
			if (_waterPresets != null && _waterPresets.Length != 0)
			{
				int num = (_presetIndex + dir + _waterPresets.Length) % _waterPresets.Length;
				RaceState instance = RaceState.Instance;
				if (instance != null)
				{
					instance.HostSetWaterId(num);
					return;
				}
				_presetIndex = num;
				ApplyWaterPreset();
			}
		}

		private void ApplyPresetByIndex(int i)
		{
			if (_waterPresets != null && _waterPresets.Length != 0)
			{
				_presetIndex = (i % _waterPresets.Length + _waterPresets.Length) % _waterPresets.Length;
				ApplyWaterPreset();
			}
		}

		private void ApplyWaterPreset()
		{
			if (_waterPresets == null || _waterPresets.Length == 0)
			{
				return;
			}
			Material material = _waterPresets[_presetIndex];
			if (!(material == null))
			{
				if (_liveMat != null)
				{
					UnityEngine.Object.Destroy(_liveMat);
				}
				_liveMat = new Material(material);
				AssignLiveMat();
			}
		}

		private void AssignLiveMat()
		{
			if (_liveMat == null)
			{
				return;
			}
			foreach (WaterChannel channel in _channels)
			{
				if (channel != null && channel.WaterSurface != null)
				{
					channel.WaterSurface.sharedMaterial = _liveMat;
				}
			}
		}

		private void EnsureLiveMat()
		{
			if (_liveMat != null)
			{
				return;
			}
			foreach (WaterChannel channel in _channels)
			{
				if (channel != null && channel.WaterSurface != null && channel.WaterSurface.sharedMaterial != null)
				{
					_liveMat = new Material(channel.WaterSurface.sharedMaterial);
					AssignLiveMat();
					break;
				}
			}
		}

		private float MatFloatRow(float y, string label, string prop, float min, float max)
		{
			if (_liveMat == null || !_liveMat.HasProperty(prop))
			{
				return y;
			}
			float num = _liveMat.GetFloat(prop);
			GUI.Label(new Rect(20f, y, 300f, 20f), $"{label}: {num:0.00}");
			float num2 = GUI.HorizontalSlider(new Rect(20f, y + 20f, 290f, 18f), num, min, max);
			if (!Mathf.Approximately(num2, num))
			{
				_liveMat.SetFloat(prop, num2);
			}
			return y + 42f;
		}

		private float MatFloat2Row(float y, string label, string propA, string propB, float min, float max)
		{
			if (_liveMat == null || !_liveMat.HasProperty(propA))
			{
				return y;
			}
			float num = _liveMat.GetFloat(propA);
			GUI.Label(new Rect(20f, y, 300f, 20f), $"{label}: {num:0.00}");
			float num2 = GUI.HorizontalSlider(new Rect(20f, y + 20f, 290f, 18f), num, min, max);
			if (!Mathf.Approximately(num2, num))
			{
				_liveMat.SetFloat(propA, num2);
				if (_liveMat.HasProperty(propB))
				{
					_liveMat.SetFloat(propB, num2);
				}
			}
			return y + 42f;
		}

		private float MatColorRow(float y, string label, string prop, float maxc)
		{
			if (_liveMat == null || !_liveMat.HasProperty(prop))
			{
				return y;
			}
			Color color = _liveMat.GetColor(prop);
			GUI.Label(new Rect(20f, y, 300f, 20f), label);
			float num = GUI.HorizontalSlider(new Rect(20f, y + 20f, 88f, 18f), color.r, 0f, maxc);
			float num2 = GUI.HorizontalSlider(new Rect(118f, y + 20f, 88f, 18f), color.g, 0f, maxc);
			float num3 = GUI.HorizontalSlider(new Rect(216f, y + 20f, 88f, 18f), color.b, 0f, maxc);
			if (!Mathf.Approximately(num, color.r) || !Mathf.Approximately(num2, color.g) || !Mathf.Approximately(num3, color.b))
			{
				_liveMat.SetColor(prop, new Color(num, num2, num3, color.a));
			}
			return y + 42f;
		}

		private float WaveToggleRow(float y)
		{
			if (_liveMat == null || !_liveMat.HasProperty("_ENABLEWAVE"))
			{
				return y;
			}
			bool flag = _liveMat.GetFloat("_ENABLEWAVE") > 0.5f;
			if (GUI.Button(new Rect(20f, y, 290f, 24f), flag ? "Shader dalgaları: AÇIK" : "Shader dalgaları: KAPALI"))
			{
				flag = !flag;
				_liveMat.SetFloat("_ENABLEWAVE", flag ? 1f : 0f);
				if (flag)
				{
					_liveMat.EnableKeyword("_ENABLEWAVE");
				}
				else
				{
					_liveMat.DisableKeyword("_ENABLEWAVE");
				}
			}
			return y + 30f;
		}

		private void SaveValuesToFile()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# Puff Pirates — F2 panel değerleri  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("[Su]");
			stringBuilder.AppendLine($"Su yüksekliği    = {_levelOffset}");
			stringBuilder.AppendLine($"Dalga yüksekliği = {_waveAmp}");
			stringBuilder.AppendLine($"Dalga boyu       = {_waveLen}");
			stringBuilder.AppendLine($"Dalga hızı       = {_waveSpd}");
			stringBuilder.AppendLine($"Su yoğunluğu     = {_waterDensity}");
			stringBuilder.AppendLine($"Bot yoğunluğu    = {_boatDensity}");
			stringBuilder.AppendLine($"Obje yoğunluğu   = {_propDensity}");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("[Fizik]");
			stringBuilder.AppendLine($"Su sürtünmesi (YENİ)  = {_waterFriction}");
			stringBuilder.AppendLine($"Yatay sürtünme (ESKİ) = {_glideFriction}");
			stringBuilder.AppendLine($"Bot kütlesi (kg)      = {_boatMass}");
			stringBuilder.AppendLine($"Bot su yük. (ESKİ)    = {_boatRide}");
			stringBuilder.AppendLine($"Sahte dalga bobbing genliği = {_waterBobAmplitude}");
			stringBuilder.AppendLine($"Sahte dalga bobbing hızı    = {_waterBobFrequency}");
			stringBuilder.AppendLine($"Sahte dalga boyu            = {_waterBobWaveLength}");
			stringBuilder.AppendLine($"Bobbing hız eşiği           = {_waterBobSpeedThreshold}");
			stringBuilder.AppendLine($"Bobbing sönme mesafesi      = {_waterBobFadeRange}");
			stringBuilder.AppendLine($"Mekanik buoyancy      = {_buoyancyOn}");
			stringBuilder.AppendLine($"Havada gaz kesik      = {_noGasAir}");
			stringBuilder.AppendLine($"Duvar öldürme         = {BarrierKill.GloballyEnabled}");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("[Osuruk alevi]");
			stringBuilder.AppendLine($"Alev açık       = {_flameOnAll}");
			stringBuilder.AppendLine($"Alev güç sınırı = {_flameMinForce}");
			stringBuilder.AppendLine($"Alev gecikmesi  = {_flameHold}");
			stringBuilder.AppendLine($"Alev sert kesik = {_flameHardCutAll}");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("[Gaz sesi]");
			stringBuilder.AppendLine($"Yüksek/Düşük eşiği (%)    = {_gasXf1}");
			stringBuilder.AppendLine($"Düşük/Damping eşiği (%)   = {_gasXf2}");
			stringBuilder.AppendLine($"Crossfade süresi (sn)     = {_gasCrossfadeDur}");
			stringBuilder.AppendLine($"Bırakma fade süresi (sn)  = {_gasReleaseDur}");
			stringBuilder.AppendLine($"Bırakma pitch hedefi      = {_gasReleasePitch}");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("[Seesaw]");
			stringBuilder.AppendLine($"Eğim açısı (°) = {_seesawTilt}");
			stringBuilder.AppendLine($"Gevşeklik      = {_seesawLoose}");
			if (_camRig != null)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("[Kamera]");
				stringBuilder.AppendLine($"Zoom MIN mesafe    = {_camRig.DistanceMin}");
				stringBuilder.AppendLine($"Zoom MAX mesafe    = {_camRig.DistanceMax}");
				stringBuilder.AppendLine($"Zoom VARSAYILAN    = {_camRig.DefaultDistance}");
				stringBuilder.AppendLine($"Oklüzyon açık      = {_camRig.OcclusionEnabled}");
				stringBuilder.AppendLine($"Kamera sallanması  = {_camRig.GasShakeAmplitude}");
			}
			string text = Path.Combine(Application.persistentDataPath, "puffpirates_f2_values.txt");
			try
			{
				File.WriteAllText(text, stringBuilder.ToString());
				Debug.Log($"[F2] Değerler kaydedildi → {text}\n{stringBuilder}");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[F2] Kaydetme hatası: {ex.Message}\n{stringBuilder}");
			}
		}
	}
}
