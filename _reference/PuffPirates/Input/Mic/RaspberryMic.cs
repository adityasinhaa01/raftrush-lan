using PuffPirates.Settings;
using UnityEngine;

namespace PuffPirates.Input.Mic
{
	public sealed class RaspberryMic : MonoBehaviour
	{
		private const int N = 1024;

		private IMicSource _src;

		private readonly float[] _win = new float[1024];

		private float _noiseFloor = 0.005f;

		private bool _blowState;

		private bool _wantRunning;

		private string _lastDevice = "";

		public static RaspberryMic Instance { get; private set; }

		public static bool HasInstance => Instance != null;

		public bool IsBlowing { get; private set; }

		public float Level { get; private set; }

		public bool TestActive { get; set; }

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			Instance = this;
			Object.DontDestroyOnLoad(base.gameObject);
			_src = new StandaloneMicSource();
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				_src?.Stop();
				Instance = null;
			}
		}

		private void Update()
		{
			bool flag = GameSettings.MicGasEnabled || TestActive;
			bool flag2 = (_wantRunning ? flag : (flag && _src != null && _src.Available));
			if (flag2 && !_wantRunning)
			{
				_src.Start();
				_wantRunning = true;
				_lastDevice = GameSettings.MicDeviceName;
			}
			else if (!flag2 && _wantRunning)
			{
				_src.Stop();
				_wantRunning = false;
				Clear();
				return;
			}
			if (!_wantRunning)
			{
				Clear();
				return;
			}
			if (GameSettings.MicDeviceName != _lastDevice)
			{
				_src.Stop();
				_src.Start();
				_lastDevice = GameSettings.MicDeviceName;
			}
			if (_src.TryReadWindow(_win, out var sampleRate))
			{
				float num = RaspberryDetector.Rms(_win, 1024);
				float lowRatio = RaspberryDetector.LowBandRatio(_win, sampleRate);
				if (num < _noiseFloor)
				{
					_noiseFloor = Mathf.Lerp(_noiseFloor, num, 0.2f);
				}
				else
				{
					_noiseFloor = Mathf.Lerp(_noiseFloor, num, 0.002f);
				}
				_noiseFloor = Mathf.Max(_noiseFloor, 0.0001f);
				float micSensitivity = GameSettings.MicSensitivity;
				Level = RaspberryDetector.LevelOf(num, _noiseFloor, micSensitivity);
				bool flag3 = RaspberryDetector.IsBlowing(num, lowRatio, _noiseFloor, micSensitivity);
				_blowState = RaspberryDetector.HysteresisStep(_blowState, flag3 ? Level : 0f, 0.6f, 0.4f);
				IsBlowing = _blowState;
			}
		}

		private void Clear()
		{
			IsBlowing = false;
			Level = 0f;
			_blowState = false;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Bootstrap()
		{
			if (!(Instance != null))
			{
				new GameObject("~RaspberryMic").AddComponent<RaspberryMic>();
			}
		}
	}
}
