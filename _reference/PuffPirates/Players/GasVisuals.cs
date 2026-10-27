using Cysharp.Threading.Tasks;
using FishNet.Utility.Template;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.PuffPirates;
using PuffPirates.Audio;
using PuffPirates.CameraRig;
using PuffPirates.Juice;
using PuffPirates.Systems;
using UnityEngine;
using UnityEngine.VFX;

namespace PuffPirates.Players
{
	[RequireComponent(typeof(PiratePlayer))]
	[DisallowMultipleComponent]
	public class GasVisuals : TickNetworkBehaviour
	{
		[Tooltip("Popodaki gaz jeti (dışarı).")]
		[SerializeField]
		private ParticleSystem _gasPuff;

		[Tooltip("Tuning data (head-pop, gas sound pools). Shared/versioned as an asset instead of living directly on the prefab.")]
		[SerializeField]
		private GasVisualsConfig _config;

		[Header("Osuruk Alevi (parametrik)")]
		[Tooltip("Alev efektinin KÖK objesi (mesh + VisualEffect'ler birlikte — ör. FireBall02). SetActive ile topluca aç/kapanır, altındaki tüm VFX'ler birlikte oynar. Boşsa alev sistemi pasif.")]
		[SerializeField]
		private GameObject _flameRoot;

		[Tooltip("Alev sistemi açık/kapalı ana anahtar.")]
		[SerializeField]
		private bool _flameEnabled = true;

		[Tooltip("Alev bu güç eşiğinin ÜSTÜNDE tutuşur. 0 = en zayıf gaz, 1 = tam güç/boost.")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _flameMinForce01 = 0.6f;

		[Tooltip("Gaz KESİNTİSİZ bu kadar saniye sürerse alev başlar. 0 = basar basmaz, 2 = 2 sn sonra.")]
		[SerializeField]
		[Min(0f)]
		private float _flameHoldSeconds;

		[Tooltip("Gaz kesilince alev SERT kesilsin (parçacıklar anında silinir = 'kesilerek biter'). Kapalıysa doğal söner (spawn durur, kalan parçacıklar solar).")]
		[SerializeField]
		private bool _flameHardCut = true;

		private Transform _headBone;

		private Vector3 _headBaseScale = Vector3.one;

		private float _headPop;

		private bool _headPopWant;

		private float _lastHeadScale = -1f;

		private static readonly bool EnableHeadPop;

		private bool _flameOn;

		private float _gasHoldTime;

		private VisualEffect[] _flameVfxs;

		private PiratePlayer _me;

		private PlayerInputSystem _input;

		private bool _lastGas;

		private GasLoopPlayer _localGasLoop;

		private GasLoopPlayer _remoteGasLoop;

		private AudioSource _remoteGasSrc;

		private float _remoteGasGoneTimer;

		private bool _gasSoundsPreloaded;

		private int _lastBurst;

		private int _lastTapSequence;

		private bool NetworkInitialize___EarlyPuffPirates_002EPlayers_002EGasVisualsAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EPlayers_002EGasVisualsAssembly_002DCSharp_002Edll_Excuted;

		public bool FlameEnabled
		{
			get
			{
				return _flameEnabled;
			}
			set
			{
				_flameEnabled = value;
			}
		}

		public float FlameMinForce01
		{
			get
			{
				return _flameMinForce01;
			}
			set
			{
				_flameMinForce01 = Mathf.Clamp01(value);
			}
		}

		public float FlameHoldSeconds
		{
			get
			{
				return _flameHoldSeconds;
			}
			set
			{
				_flameHoldSeconds = Mathf.Max(0f, value);
			}
		}

		public bool FlameHardCut
		{
			get
			{
				return _flameHardCut;
			}
			set
			{
				_flameHardCut = value;
			}
		}

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EPlayers_002EGasVisuals_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out _input);
			if (_config == null)
			{
				Debug.LogError("[GasVisuals] No GasVisualsConfig assigned on '" + base.name + "' — visuals will not tune correctly.", this);
			}
			Set(_gasPuff, on: false);
			_lastGas = false;
			_lastBurst = ((_me != null) ? _me.Gas.GasBurstSequence : 0);
			_lastTapSequence = ((_me != null) ? _me.Gas.GasTapSequence : 0);
			if (base.IsOwner && _remoteGasSrc == null)
			{
				_remoteGasSrc = CreateRemoteGasSource();
			}
			_localGasLoop = new GasLoopPlayer(_config.GasHighSound, _config.GasLowSound, _config.GasDampingSound, _remoteGasSrc);
			PreloadGasSoundsAsync().Forget();
			_flameOn = false;
			_gasHoldTime = 0f;
			if (_flameRoot != null)
			{
				_flameVfxs = _flameRoot.GetComponentsInChildren<VisualEffect>(includeInactive: true);
				_flameRoot.SetActive(value: false);
			}
			if (EnableHeadPop)
			{
				_headBone = FindDeep(base.transform, "mixamorig1:Head");
				if (_headBone != null)
				{
					_headBaseScale = _headBone.localScale;
				}
			}
		}

		private AudioSource CreateRemoteGasSource()
		{
			GameObject obj = new GameObject("~RemoteGasAudio");
			obj.transform.SetParent(base.transform, worldPositionStays: false);
			AudioSource audioSource = obj.AddComponent<AudioSource>();
			Sfx3D.Setup3D(audioSource);
			audioSource.loop = true;
			audioSource.maxDistance = 40f;
			return audioSource;
		}

		private async UniTaskVoid PreloadGasSoundsAsync()
		{
			await UniTask.WhenAll(AudioManager.PreloadAsync(_config.GasHighSound), AudioManager.PreloadAsync(_config.GasLowSound), AudioManager.PreloadAsync(_config.GasDampingSound), AudioManager.PreloadAsync(_config.GasTapSound), AudioManager.PreloadAsync(_config.SputterClickSound));
			_gasSoundsPreloaded = true;
		}

		private void ReleaseGasSounds()
		{
			if (_gasSoundsPreloaded)
			{
				_gasSoundsPreloaded = false;
				AudioManager.ReleaseClips(_config.GasHighSound);
				AudioManager.ReleaseClips(_config.GasLowSound);
				AudioManager.ReleaseClips(_config.GasDampingSound);
				AudioManager.ReleaseClips(_config.GasTapSound);
				AudioManager.ReleaseClips(_config.SputterClickSound);
			}
		}

		public override void OnStopNetwork()
		{
			base.OnStopNetwork();
			_localGasLoop?.Stop();
			_remoteGasLoop?.Stop();
			ReleaseGasSounds();
			if (_remoteGasSrc != null)
			{
				_remoteGasSrc.Stop();
				_remoteGasGoneTimer = 0f;
			}
			BoatCameraRig.SetGasShake(0f);
			if (_flameOn)
			{
				StopFlame();
			}
		}

		protected override void TimeManager_OnUpdate()
		{
			if (!base.IsClientInitialized || _me == null)
			{
				return;
			}
			bool flag = (_headPopWant = ((!base.IsOwner) ? _me.Gas.IsGassing : (_input != null && _input.LocalGasHeld && _me.Gas.HasGas)));
			float gasPowerFraction = _me.Gas.GasPowerFraction;
			float num = Mathf.Clamp01(_me.Gas.GasEnergy / Mathf.Max(1f, _me.Gas.MaxGas));
			bool flag2 = false;
			if (flag && num < 0.22f)
			{
				float num2 = Mathf.Repeat(Time.unscaledTime * 3f + (float)base.transform.GetEntityId().GetHashCode() * 0.0137f, 1f);
				float num3 = Mathf.Lerp(0.18f, 0.78f, num / 0.22f);
				flag2 = num2 > num3;
			}
			bool flag3 = flag && !flag2;
			bool num4 = flag3 && !_lastGas && num < 0.22f;
			if (flag3 != _lastGas)
			{
				_lastGas = flag3;
				Set(_gasPuff, flag3);
			}
			if (base.IsOwner)
			{
				if (flag && num >= 0.22f)
				{
					if (!_localGasLoop.IsActive)
					{
						_localGasLoop.Start(num);
					}
					else
					{
						_localGasLoop.UpdateFuel(num);
					}
				}
				else if (_localGasLoop.IsActive)
				{
					_localGasLoop.Stop();
				}
				BoatCameraRig.SetGasShake(flag ? Mathf.Lerp(0.35f, 1f, gasPowerFraction) : 0f);
			}
			else if (_me.Gas.IsGassing)
			{
				_remoteGasGoneTimer = 0f;
				if (_remoteGasSrc == null)
				{
					_remoteGasSrc = CreateRemoteGasSource();
				}
				if (_remoteGasLoop == null)
				{
					_remoteGasLoop = new GasLoopPlayer(_config.GasHighSound, _config.GasLowSound, _config.GasDampingSound, _remoteGasSrc);
				}
				if (num >= 0.22f)
				{
					if (!_remoteGasLoop.IsActive)
					{
						_remoteGasLoop.Start(num);
					}
					else
					{
						_remoteGasLoop.UpdateFuel(num);
					}
				}
				else if (_remoteGasLoop.IsActive)
				{
					_remoteGasLoop.Stop();
				}
			}
			else if (_remoteGasLoop != null && _remoteGasLoop.IsActive)
			{
				_remoteGasGoneTimer += Time.deltaTime;
				if (_remoteGasGoneTimer > 0.15f)
				{
					_remoteGasLoop.Stop();
					_remoteGasGoneTimer = 0f;
				}
			}
			if (num4 && _config.SputterClickSound != null)
			{
				if (base.IsOwner)
				{
					Sfx3D.Play(_config.SputterClickSound, base.transform.position);
				}
				else if (_remoteGasSrc != null)
				{
					AudioManager.PlaySound(_remoteGasSrc, _config.SputterClickSound);
				}
			}
			UpdateFlame(_me.Gas.IsGassing, gasPowerFraction);
			if (_me.Gas.GasBurstSequence != _lastBurst)
			{
				_lastBurst = _me.Gas.GasBurstSequence;
				if (_gasPuff != null)
				{
					if (!_gasPuff.isPlaying)
					{
						_gasPuff.Play(withChildren: true);
					}
					_gasPuff.Emit(Mathf.RoundToInt(Mathf.Lerp(15f, 51f, gasPowerFraction)));
				}
				if (_me != null && _me.IsOwner)
				{
					HapticManager.Impact(Mathf.Lerp(0.15f, 0.4f, gasPowerFraction));
				}
			}
			if (_me.Gas.GasTapSequence == _lastTapSequence)
			{
				return;
			}
			_lastTapSequence = _me.Gas.GasTapSequence;
			if (_config.GasTapSound != null)
			{
				Sfx3D.Play(_config.GasTapSound, base.transform.position);
			}
			if (!base.IsOwner && _gasPuff != null)
			{
				if (!_gasPuff.isPlaying)
				{
					_gasPuff.Play(withChildren: true);
				}
				_gasPuff.Emit(30);
			}
		}

		private void LateUpdate()
		{
			if (EnableHeadPop && _headBone != null)
			{
				float num = (_headPopWant ? 1f : 0f);
				_headPop = Mathf.Lerp(_headPop, num, 1f - Mathf.Exp((0f - _config.HeadPopLerp) * Time.deltaTime));
				float num2 = Mathf.Lerp(1f, _config.HeadPopScale, _headPop);
				if (Mathf.Abs(_headPop - num) > 0.001f || Mathf.Abs(num2 - _lastHeadScale) > 0.0001f)
				{
					_headBone.localScale = _headBaseScale * num2;
					_lastHeadScale = num2;
				}
			}
		}

		private static Transform FindDeep(Transform root, string name)
		{
			if (root.name == name)
			{
				return root;
			}
			for (int i = 0; i < root.childCount; i++)
			{
				Transform transform = FindDeep(root.GetChild(i), name);
				if (transform != null)
				{
					return transform;
				}
			}
			return null;
		}

		private static void Set(ParticleSystem ps, bool on)
		{
			if (!(ps == null))
			{
				ParticleSystem.EmissionModule emission = ps.emission;
				emission.enabled = on;
				if (on)
				{
					ps.Play(withChildren: true);
				}
				else
				{
					ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
				}
			}
		}

		private void UpdateFlame(bool gas, float force01)
		{
			bool flag = (_flameEnabled && _flameRoot != null) & gas;
			if (flag && !_flameOn)
			{
				StartFlame();
			}
			else if (!flag && _flameOn)
			{
				StopFlame();
			}
		}

		private void StartFlame()
		{
			_flameOn = true;
			if (_flameRoot == null)
			{
				return;
			}
			if (!_flameRoot.activeSelf)
			{
				_flameRoot.SetActive(value: true);
			}
			if (_flameVfxs == null)
			{
				return;
			}
			VisualEffect[] flameVfxs = _flameVfxs;
			foreach (VisualEffect visualEffect in flameVfxs)
			{
				if (visualEffect != null)
				{
					visualEffect.Reinit();
					visualEffect.Play();
				}
			}
		}

		private void StopFlame()
		{
			_flameOn = false;
			if (_flameRoot == null)
			{
				return;
			}
			if (_flameVfxs != null)
			{
				VisualEffect[] flameVfxs = _flameVfxs;
				foreach (VisualEffect visualEffect in flameVfxs)
				{
					if (visualEffect != null)
					{
						visualEffect.Stop();
					}
				}
			}
			if (!_flameHardCut)
			{
				return;
			}
			if (_flameVfxs != null)
			{
				VisualEffect[] flameVfxs = _flameVfxs;
				foreach (VisualEffect visualEffect2 in flameVfxs)
				{
					if (visualEffect2 != null)
					{
						visualEffect2.Reinit();
					}
				}
			}
			_flameRoot.SetActive(value: false);
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EPlayers_002EGasVisualsAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EPlayers_002EGasVisualsAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EPlayers_002EGasVisualsAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EPlayers_002EGasVisualsAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void Awake_UserLogic_PuffPirates_002EPlayers_002EGasVisuals_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Update);
			_me = GetComponent<PiratePlayer>();
		}
	}
}
