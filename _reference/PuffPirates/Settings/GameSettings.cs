using System;
using GamesMrkt.Audio;
using PuffPirates.Save;
using UnityEngine;

namespace PuffPirates.Settings
{
	public static class GameSettings
	{
		private const string KeyMouse = "pp_mouse_sens";

		private const string KeyInvert = "pp_mouse_invert";

		private const string KeyPad = "pp_pad_sens";

		private const string KeyTouchSens = "pp_touch_sens";

		private const string KeyJoyLeft = "pp_joy_left";

		private const string KeyBtnScale = "pp_btn_scale";

		private const string KeyBtnOpacity = "pp_btn_opacity";

		private const string KeyMasterVol = "pp_master_vol";

		private const string KeyMusicVol = "pp_music_vol";

		private const string KeySfxVol = "pp_sfx_vol";

		private const string KeyAudioMuted = "pp_audio_muted";

		private const string KeyMicGas = "pp_micgas";

		private const string KeyMicSens = "pp_micsens";

		internal const float MicSensMin = 0.25f;

		internal const float MicSensMax = 4f;

		internal const float MicSensDefault = 1f;

		private const string KeyMicDev = "pp_micdev";

		private const string MicDeviceDefault = "";

		private const string KeyCameraShake = "pp_camera_shake";

		private const string KeyCameraMaxDist = "pp_camera_max_dist";

		private const string KeyFartVol = "pp_fart_vol";

		public const float MouseMin = 0.05f;

		public const float MouseMax = 2f;

		public const float MouseDefault = 0.5f;

		public const float PadMin = 0.2f;

		public const float PadMax = 2f;

		public const float PadDefault = 1f;

		public const float KeyMax = 3f;

		public const float TouchMin = 0.2f;

		public const float TouchMax = 2f;

		public const float TouchDefault = 1f;

		public const float ButtonScaleMin = 0.7f;

		public const float ButtonScaleMax = 1.6f;

		public const float ButtonScaleDefault = 1f;

		public const float OpacityMin = 0.25f;

		public const float OpacityMax = 1f;

		public const float OpacityDefault = 0.7f;

		public const float VolumeMin = 0f;

		public const float VolumeMax = 1f;

		public const float VolumeDefault = 0.8f;

		public const float CameraMaxDistMin = 15f;

		public const float CameraMaxDistMax = 45f;

		public const float CameraMaxDistDefault = 30f;

		public static readonly (string Level, string LabelKey)[] GraphicsQualityOptions = new (string, string)[3]
		{
			("Low", "settings.graphic.quality.low"),
			("Medium", "settings.graphic.quality.medium"),
			("High", "settings.graphic.quality.high")
		};

		public const string GraphicsQualityDefault = "High";

		private static float _mouse = 0.5f;

		private static float _pad = 1f;

		private static bool _invert;

		private static float _touch = 1f;

		private static bool _joyLeft = true;

		private static float _btnScale = 1f;

		private static float _btnOpac = 0.7f;

		private static float _masterVol = 0.8f;

		private static float _musicVol = 0.8f;

		private static float _sfxVol = 0.8f;

		private static bool _audioMuted;

		private static bool _micGas;

		private static float _micSens = 1f;

		private static string _micDev = "";

		private static bool _cameraShake = true;

		private static float _cameraMaxDist = 30f;

		private static float _fartVol = 0.8f;

		private static string _graphicsQuality = "High";

		public static bool MicGasEnabled
		{
			get
			{
				return false;
			}
			set
			{
				_micGas = value;
				SaveDevice();
			}
		}

		public static float MicSensitivity
		{
			get
			{
				return _micSens;
			}
			set
			{
				_micSens = Mathf.Clamp(value, 0.25f, 4f);
				SaveDevice();
			}
		}

		public static string MicDeviceName
		{
			get
			{
				return _micDev;
			}
			set
			{
				_micDev = value ?? "";
				SaveDevice();
			}
		}

		public static float MouseSensitivity
		{
			get
			{
				return _mouse;
			}
			set
			{
				_mouse = Mathf.Clamp(value, 0.05f, 2f);
				Save();
			}
		}

		public static float PadSensitivity
		{
			get
			{
				return _pad;
			}
			set
			{
				_pad = Mathf.Clamp(value, 0.2f, 2f);
				Save();
			}
		}

		public static bool InvertMouse
		{
			get
			{
				return _invert;
			}
			set
			{
				_invert = value;
				Save();
			}
		}

		public static float KeyboardSensitivity => 1f;

		public static float KeyboardSign => 1f;

		public static float TouchSensitivity
		{
			get
			{
				return _touch;
			}
			set
			{
				_touch = Mathf.Clamp(value, 0.2f, 2f);
				SaveDevice();
			}
		}

		public static bool JoystickOnLeft
		{
			get
			{
				return _joyLeft;
			}
			set
			{
				_joyLeft = value;
				SaveDevice();
			}
		}

		public static float ButtonScale
		{
			get
			{
				return _btnScale;
			}
			set
			{
				_btnScale = Mathf.Clamp(value, 0.7f, 1.6f);
				SaveDevice();
			}
		}

		public static float ButtonOpacity
		{
			get
			{
				return _btnOpac;
			}
			set
			{
				_btnOpac = Mathf.Clamp(value, 0.25f, 1f);
				SaveDevice();
			}
		}

		public static float MasterVolume
		{
			get
			{
				return _masterVol;
			}
			set
			{
				_masterVol = Mathf.Clamp(value, 0f, 1f);
				Save();
			}
		}

		public static float MusicVolume
		{
			get
			{
				return _musicVol;
			}
			set
			{
				_musicVol = Mathf.Clamp(value, 0f, 1f);
				Save();
			}
		}

		public static float SFXVolume
		{
			get
			{
				return _sfxVol;
			}
			set
			{
				_sfxVol = Mathf.Clamp(value, 0f, 1f);
				Save();
			}
		}

		public static bool Muted
		{
			get
			{
				return _audioMuted;
			}
			set
			{
				_audioMuted = value;
				Save();
			}
		}

		public static float FartVolume
		{
			get
			{
				return _fartVol;
			}
			set
			{
				_fartVol = Mathf.Clamp(value, 0f, 1f);
				Save();
			}
		}

		public static bool CameraShakeEnabled
		{
			get
			{
				return _cameraShake;
			}
			set
			{
				_cameraShake = value;
				Save();
			}
		}

		public static string GraphicsQuality
		{
			get
			{
				return _graphicsQuality;
			}
			set
			{
				_graphicsQuality = SanitizeGraphicsQuality(value);
				ApplyGraphicsQuality();
				SaveDevice();
			}
		}

		public static float CameraMaxDistance
		{
			get
			{
				return _cameraMaxDist;
			}
			set
			{
				_cameraMaxDist = Mathf.Clamp(value, 15f, 45f);
				Save();
			}
		}

		public static string Language
		{
			get
			{
				return LanguagePreference.Current;
			}
			set
			{
				LanguagePreference.Set(value);
			}
		}

		public static void ApplyGraphicsQuality()
		{
			if (!Application.isMobilePlatform)
			{
				int num = Array.IndexOf(QualitySettings.names, _graphicsQuality);
				if (num >= 0 && num != QualitySettings.GetQualityLevel())
				{
					QualitySettings.SetQualityLevel(num, applyExpensiveChanges: true);
				}
			}
		}

		private static string SanitizeGraphicsQuality(string level)
		{
			(string, string)[] graphicsQualityOptions = GraphicsQualityOptions;
			for (int i = 0; i < graphicsQualityOptions.Length; i++)
			{
				if (graphicsQualityOptions[i].Item1 == level)
				{
					return level;
				}
			}
			return "High";
		}

		private static void Save()
		{
			SettingsSaveScheduler.Request("settings");
		}

		private static void SaveDevice()
		{
			SettingsSaveScheduler.Request("device_settings");
		}

		public static void ApplyAudio()
		{
			AudioManager.SetMasterVolume(_masterVol);
			AudioManager.SetVolume(GamesMrkt.Audio.AudioType.Music, _musicVol);
			AudioManager.SetVolume(GamesMrkt.Audio.AudioType.SFX, _sfxVol);
			AudioManager.SetVolume(GamesMrkt.Audio.AudioType.UISfx, _sfxVol);
			AudioManager.SetVolume(GamesMrkt.Audio.AudioType.Fart, _fartVol);
		}

		internal static SettingsData ExportSettings()
		{
			return new SettingsData
			{
				mouseSensitivity = _mouse,
				padSensitivity = _pad,
				invertMouse = _invert,
				masterVolume = _masterVol,
				musicVolume = _musicVol,
				sfxVolume = _sfxVol,
				fartVolume = _fartVol,
				muted = _audioMuted,
				cameraShake = _cameraShake,
				cameraMaxDistance = _cameraMaxDist
			};
		}

		internal static void ImportSettings(SettingsData d)
		{
			if (d != null)
			{
				_mouse = Mathf.Clamp(d.mouseSensitivity, 0.05f, 2f);
				_pad = Mathf.Clamp(d.padSensitivity, 0.2f, 2f);
				_invert = d.invertMouse;
				_masterVol = Mathf.Clamp(d.masterVolume, 0f, 1f);
				_musicVol = Mathf.Clamp(d.musicVolume, 0f, 1f);
				_sfxVol = Mathf.Clamp(d.sfxVolume, 0f, 1f);
				_fartVol = Mathf.Clamp(d.fartVolume, 0f, 1f);
				_audioMuted = d.muted;
				_cameraShake = d.cameraShake;
				_cameraMaxDist = Mathf.Clamp(d.cameraMaxDistance, 15f, 45f);
			}
		}

		internal static DeviceSettingsData ExportDevice()
		{
			return new DeviceSettingsData
			{
				micGas = _micGas,
				micSensitivity = _micSens,
				micDevice = _micDev,
				touchSensitivity = _touch,
				joystickOnLeft = _joyLeft,
				buttonScale = _btnScale,
				buttonOpacity = _btnOpac,
				graphicsQuality = _graphicsQuality
			};
		}

		internal static void ImportDevice(DeviceSettingsData d)
		{
			if (d != null)
			{
				_micGas = d.micGas;
				_micSens = Mathf.Clamp(d.micSensitivity, 0.25f, 4f);
				_micDev = d.micDevice ?? "";
				_touch = Mathf.Clamp(d.touchSensitivity, 0.2f, 2f);
				_joyLeft = d.joystickOnLeft;
				_btnScale = Mathf.Clamp(d.buttonScale, 0.7f, 1.6f);
				_btnOpac = Mathf.Clamp(d.buttonOpacity, 0.25f, 1f);
				_graphicsQuality = SanitizeGraphicsQuality(d.graphicsQuality);
			}
		}

		internal static void ImportLegacyPlayerPrefs()
		{
			_mouse = PlayerPrefs.GetFloat("pp_mouse_sens", 0.5f);
			_pad = PlayerPrefs.GetFloat("pp_pad_sens", 1f);
			_invert = PlayerPrefs.GetInt("pp_mouse_invert", 0) == 1;
			_touch = PlayerPrefs.GetFloat("pp_touch_sens", 1f);
			_joyLeft = PlayerPrefs.GetInt("pp_joy_left", 1) == 1;
			_btnScale = PlayerPrefs.GetFloat("pp_btn_scale", 1f);
			_btnOpac = PlayerPrefs.GetFloat("pp_btn_opacity", 0.7f);
			_masterVol = PlayerPrefs.GetFloat("pp_master_vol", 0.8f);
			_musicVol = PlayerPrefs.GetFloat("pp_music_vol", 0.8f);
			_sfxVol = PlayerPrefs.GetFloat("pp_sfx_vol", 0.8f);
			_audioMuted = PlayerPrefs.GetInt("pp_audio_muted", 0) == 1;
			_micGas = PlayerPrefs.GetInt("pp_micgas", 0) == 1;
			_micSens = PlayerPrefs.GetFloat("pp_micsens", 1f);
			_micDev = PlayerPrefs.GetString("pp_micdev", "");
			_cameraShake = PlayerPrefs.GetInt("pp_camera_shake", 1) == 1;
			_cameraMaxDist = PlayerPrefs.GetFloat("pp_camera_max_dist", 30f);
			_fartVol = PlayerPrefs.GetFloat("pp_fart_vol", 0.8f);
		}

		internal static bool HasLegacyPlayerPrefs()
		{
			if (!PlayerPrefs.HasKey("pp_mouse_sens"))
			{
				return PlayerPrefs.HasKey("pp_master_vol");
			}
			return true;
		}

		public static void ResetDefaults()
		{
			_mouse = 0.5f;
			_pad = 1f;
			_invert = false;
			_touch = 1f;
			_joyLeft = true;
			_btnScale = 1f;
			_btnOpac = 0.7f;
			_masterVol = 0.8f;
			_musicVol = 0.8f;
			_sfxVol = 0.8f;
			_audioMuted = false;
			_micGas = false;
			_micSens = 1f;
			_micDev = "";
			_cameraShake = true;
			_cameraMaxDist = 30f;
			_fartVol = 0.8f;
			_graphicsQuality = "High";
			Save();
			SaveDevice();
		}
	}
}
