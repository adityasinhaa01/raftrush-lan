using FishNet.Object;
using FishNet.Object.Synchronizing;
using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Players
{
	[DisallowMultipleComponent]
	public class PirateGas : NetworkBehaviour
	{
		public readonly struct GasRamp
		{
			public readonly float ChargeRamp;

			public readonly float DepleteMul;

			public GasRamp(float chargeRamp, float depleteMul)
			{
				ChargeRamp = chargeRamp;
				DepleteMul = depleteMul;
			}
		}

		[Tooltip("Base gas tuning data (max fuel, regen rate, recovery threshold). Required.")]
		[SerializeField]
		private PirateGasConfig _config;

		public readonly SyncVar<float> _svGasEnergy = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _svGasDepleted = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svRegenDelayTimer = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svGasCharge = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svGasFxForce = new SyncVar<float>(new SyncTypeSettings(0f));

		private float _hostPowerFraction;

		private PiratePlayer _player;

		private BoatPhysicsController _visualPhysics;

		private const float MaxVisualStep = 0.25f;

		private int _visualFrame = -1;

		private float _visualTime;

		private bool _visualWasGassing;

		private float _visualCharge;

		private float _visualFraction;

		private float _visualReleaseFractionStart;

		private float _visualReleaseChargeStart;

		private float _visualReleaseTimer;

		public readonly SyncVar<int> _svGasBurstSequence = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _svNormalTapWindowOpen = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svNormalTapPressTimer = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _svNormalTapConfirmedHold = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svGasTapSequence = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svReleaseFractionStart = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svReleaseChargeStart = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svReleaseTimer = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _svMousePuffHeld = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _svIsGassing = new SyncVar<bool>(new SyncTypeSettings(0f));

		private bool NetworkInitialize___EarlyPuffPirates_002EPlayers_002EPirateGasAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EPlayers_002EPirateGasAssembly_002DCSharp_002Edll_Excuted;

		public float GasEnergy
		{
			get
			{
				return _svGasEnergy.Value;
			}
			private set
			{
				_svGasEnergy.Value = value;
			}
		}

		public bool GasDepleted
		{
			get
			{
				return _svGasDepleted.Value;
			}
			private set
			{
				_svGasDepleted.Value = value;
			}
		}

		private float RegenDelayTimer
		{
			get
			{
				return _svRegenDelayTimer.Value;
			}
			set
			{
				_svRegenDelayTimer.Value = value;
			}
		}

		public float GasCharge
		{
			get
			{
				return _svGasCharge.Value;
			}
			private set
			{
				_svGasCharge.Value = value;
			}
		}

		public float GasFxForce
		{
			get
			{
				return _svGasFxForce.Value;
			}
			private set
			{
				_svGasFxForce.Value = value;
			}
		}

		public float GasPowerFraction
		{
			get
			{
				if (_visualFrame != Time.frameCount)
				{
					float dt = ((_visualFrame < 0) ? 0f : Mathf.Min(Time.time - _visualTime, 0.25f));
					_visualFrame = Time.frameCount;
					_visualTime = Time.time;
					AdvanceVisualPower(dt);
				}
				return _visualFraction;
			}
		}

		public int GasBurstSequence
		{
			get
			{
				return _svGasBurstSequence.Value;
			}
			private set
			{
				_svGasBurstSequence.Value = value;
			}
		}

		public bool NormalTapWindowOpen
		{
			get
			{
				return _svNormalTapWindowOpen.Value;
			}
			private set
			{
				_svNormalTapWindowOpen.Value = value;
			}
		}

		public float NormalTapPressTimer
		{
			get
			{
				return _svNormalTapPressTimer.Value;
			}
			private set
			{
				_svNormalTapPressTimer.Value = value;
			}
		}

		public bool NormalTapConfirmedHold
		{
			get
			{
				return _svNormalTapConfirmedHold.Value;
			}
			private set
			{
				_svNormalTapConfirmedHold.Value = value;
			}
		}

		public int GasTapSequence
		{
			get
			{
				return _svGasTapSequence.Value;
			}
			private set
			{
				_svGasTapSequence.Value = value;
			}
		}

		private float ReleaseFractionStart
		{
			get
			{
				return _svReleaseFractionStart.Value;
			}
			set
			{
				_svReleaseFractionStart.Value = value;
			}
		}

		private float ReleaseChargeStart
		{
			get
			{
				return _svReleaseChargeStart.Value;
			}
			set
			{
				_svReleaseChargeStart.Value = value;
			}
		}

		private float ReleaseTimer
		{
			get
			{
				return _svReleaseTimer.Value;
			}
			set
			{
				_svReleaseTimer.Value = value;
			}
		}

		private bool MousePuffHeld
		{
			get
			{
				return _svMousePuffHeld.Value;
			}
			set
			{
				_svMousePuffHeld.Value = value;
			}
		}

		public bool IsGassing
		{
			get
			{
				return _svIsGassing.Value;
			}
			private set
			{
				_svIsGassing.Value = value;
			}
		}

		public float MaxGas => _config.MaxGas;

		public bool HasGas
		{
			get
			{
				if (!GasDepleted)
				{
					return GasEnergy > 0.01f;
				}
				return false;
			}
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			if (_config == null)
			{
				Debug.LogError("[PirateGas] No PirateGasConfig assigned on '" + base.name + "' — gas will not function correctly.", this);
			}
			_player = GetComponent<PiratePlayer>();
			ResetVisualPower();
		}

		private void ResetVisualPower()
		{
			_visualWasGassing = false;
			_visualCharge = 0f;
			_visualFraction = 0f;
			_visualReleaseFractionStart = 0f;
			_visualReleaseChargeStart = 0f;
			_visualReleaseTimer = 0f;
		}

		private void AdvanceVisualPower(float dt)
		{
			NetworkBoat networkBoat = ((_player != null) ? _player.Boat : null);
			if (networkBoat == null)
			{
				ResetVisualPower();
				return;
			}
			if (_visualPhysics == null || _visualPhysics.gameObject != networkBoat.gameObject)
			{
				_visualPhysics = networkBoat.GetComponent<BoatPhysicsController>();
			}
			if (_visualPhysics == null)
			{
				return;
			}
			bool isGassing = IsGassing;
			if (isGassing)
			{
				float gasRampSeconds = _visualPhysics.GasRampSeconds;
				_visualCharge = Mathf.Min(_visualCharge + dt, gasRampSeconds);
				float num = ((gasRampSeconds > 0.001f) ? Mathf.Clamp01(_visualCharge / gasRampSeconds) : 1f);
				AnimationCurve gasRampCurve = _visualPhysics.GasRampCurve;
				_visualFraction = ((gasRampCurve != null) ? Mathf.Clamp01(gasRampCurve.Evaluate(num)) : num);
			}
			else
			{
				if (_visualWasGassing)
				{
					_visualReleaseFractionStart = _visualFraction;
					_visualReleaseChargeStart = _visualCharge;
					_visualReleaseTimer = 0f;
				}
				_visualReleaseTimer += dt;
				float gasReleaseSeconds = _visualPhysics.GasReleaseSeconds;
				float num2 = ((gasReleaseSeconds > 0.001f) ? Mathf.Clamp01(_visualReleaseTimer / gasReleaseSeconds) : 1f);
				AnimationCurve gasReleaseCurve = _visualPhysics.GasReleaseCurve;
				float num3 = ((gasReleaseCurve != null) ? Mathf.Clamp01(gasReleaseCurve.Evaluate(num2)) : (1f - num2));
				_visualCharge = _visualReleaseChargeStart * num3;
				_visualFraction = _visualReleaseFractionStart * num3;
			}
			_visualWasGassing = isGassing;
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			GasEnergy = MaxGas;
			IsGassing = false;
			GasFxForce = 0f;
			GasBurstSequence = 0;
			MousePuffHeld = false;
		}

		public void AddGasCharge(float dt, float rampSeconds)
		{
			if (base.IsServerInitialized)
			{
				GasCharge = Mathf.Min(GasCharge + dt, rampSeconds);
			}
		}

		public void ResetGasCharge()
		{
			if (base.IsServerInitialized && GasCharge != 0f)
			{
				GasCharge = 0f;
			}
		}

		public GasRamp GasRampFactor(float rampSeconds, AnimationCurve rampCurve = null)
		{
			float num = ((rampSeconds > 0.001f) ? Mathf.Clamp01(GasCharge / rampSeconds) : 1f);
			float chargeRamp = ((rampCurve != null) ? Mathf.Clamp01(rampCurve.Evaluate(num)) : num);
			float num2 = ((MaxGas > 0.001f) ? (GasEnergy / MaxGas) : 0f);
			float num3 = 0.3f;
			float depleteMul = ((num3 > 0.001f) ? Mathf.Clamp01(num2 / num3) : 1f);
			return new GasRamp(chargeRamp, depleteMul);
		}

		public void BeginNormalTapWindow()
		{
			if (base.IsServerInitialized)
			{
				NormalTapWindowOpen = true;
				NormalTapPressTimer = 0f;
			}
		}

		public bool AdvanceNormalTapWindow(float dt, float thresholdSeconds)
		{
			if (!base.IsServerInitialized)
			{
				return false;
			}
			NormalTapPressTimer += dt;
			return NormalTapPressTimer >= thresholdSeconds;
		}

		public void ConfirmNormalHold()
		{
			if (base.IsServerInitialized)
			{
				NormalTapWindowOpen = false;
				NormalTapConfirmedHold = true;
			}
		}

		public void FireNormalTapBurst(float tapCost)
		{
			if (base.IsServerInitialized)
			{
				ConsumeGas(tapCost);
				GasTapSequence++;
			}
		}

		public void ResetNormalTapState()
		{
			if (base.IsServerInitialized)
			{
				NormalTapWindowOpen = false;
				NormalTapConfirmedHold = false;
				NormalTapPressTimer = 0f;
			}
		}

		public void ConsumeGas(float amount)
		{
			if (base.IsServerInitialized)
			{
				GasEnergy = Mathf.Clamp(GasEnergy - amount, 0f, MaxGas);
				RegenDelayTimer = _config.GasRegenDelay;
				if (GasEnergy <= 0.01f)
				{
					GasDepleted = true;
				}
			}
		}

		public void RegenGas(float dt)
		{
			if (!base.IsServerInitialized)
			{
				return;
			}
			if (RegenDelayTimer > 0f)
			{
				RegenDelayTimer = Mathf.Max(0f, RegenDelayTimer - dt);
				return;
			}
			GasEnergy = Mathf.Clamp(GasEnergy + _config.GasRegenPerSec * dt, 0f, MaxGas);
			if (GasDepleted && GasEnergy >= MaxGas * _config.GasRecoverPercent)
			{
				GasDepleted = false;
			}
		}

		public void SetGasVisual(bool on, float appliedForce, float powerFraction, bool mousePuff)
		{
			if (base.IsServerInitialized)
			{
				if (IsGassing != on)
				{
					IsGassing = on;
				}
				GasFxForce = (on ? Mathf.Max(0f, appliedForce) : 0f);
				_hostPowerFraction = Mathf.Clamp01(powerFraction);
				bool flag = on & mousePuff;
				if (flag && !MousePuffHeld)
				{
					GasBurstSequence++;
				}
				MousePuffHeld = flag;
			}
		}

		public void BeginRelease()
		{
			if (base.IsServerInitialized)
			{
				ReleaseFractionStart = _hostPowerFraction;
				ReleaseChargeStart = GasCharge;
				ReleaseTimer = 0f;
			}
		}

		public float AdvanceReleaseDecay(float dt, float releaseSeconds, AnimationCurve releaseCurve)
		{
			if (!base.IsServerInitialized)
			{
				return 0f;
			}
			if (ReleaseFractionStart <= 0f && ReleaseChargeStart <= 0f)
			{
				if (GasCharge != 0f)
				{
					GasCharge = 0f;
				}
				return 0f;
			}
			ReleaseTimer += dt;
			float num = ((releaseSeconds > 0.001f) ? Mathf.Clamp01(ReleaseTimer / releaseSeconds) : 1f);
			float num2 = ((releaseCurve != null) ? Mathf.Clamp01(releaseCurve.Evaluate(num)) : (1f - num));
			GasCharge = ReleaseChargeStart * num2;
			return ReleaseFractionStart * num2;
		}

		public void ResetForRespawn()
		{
			if (base.IsServerInitialized)
			{
				GasEnergy = MaxGas;
				GasDepleted = false;
				RegenDelayTimer = 0f;
				GasCharge = 0f;
				ReleaseFractionStart = 0f;
				ReleaseChargeStart = 0f;
				ReleaseTimer = 0f;
				ResetNormalTapState();
				SetGasVisual(on: false, 0f, 0f, mousePuff: false);
			}
		}

		public void ForceStopGassing()
		{
			if (base.IsServerInitialized)
			{
				IsGassing = false;
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EPlayers_002EPirateGasAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EPlayers_002EPirateGasAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				_svIsGassing.InitializeEarly(this, 14u, isSyncObject: false);
				_svMousePuffHeld.InitializeEarly(this, 13u, isSyncObject: false);
				_svReleaseTimer.InitializeEarly(this, 12u, isSyncObject: false);
				_svReleaseChargeStart.InitializeEarly(this, 11u, isSyncObject: false);
				_svReleaseFractionStart.InitializeEarly(this, 10u, isSyncObject: false);
				_svGasTapSequence.InitializeEarly(this, 9u, isSyncObject: false);
				_svNormalTapConfirmedHold.InitializeEarly(this, 8u, isSyncObject: false);
				_svNormalTapPressTimer.InitializeEarly(this, 7u, isSyncObject: false);
				_svNormalTapWindowOpen.InitializeEarly(this, 6u, isSyncObject: false);
				_svGasBurstSequence.InitializeEarly(this, 5u, isSyncObject: false);
				_svGasFxForce.InitializeEarly(this, 4u, isSyncObject: false);
				_svGasCharge.InitializeEarly(this, 3u, isSyncObject: false);
				_svRegenDelayTimer.InitializeEarly(this, 2u, isSyncObject: false);
				_svGasDepleted.InitializeEarly(this, 1u, isSyncObject: false);
				_svGasEnergy.InitializeEarly(this, 0u, isSyncObject: false);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EPlayers_002EPirateGasAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EPlayers_002EPirateGasAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
				_svIsGassing.InitializeLate();
				_svMousePuffHeld.InitializeLate();
				_svReleaseTimer.InitializeLate();
				_svReleaseChargeStart.InitializeLate();
				_svReleaseFractionStart.InitializeLate();
				_svGasTapSequence.InitializeLate();
				_svNormalTapConfirmedHold.InitializeLate();
				_svNormalTapPressTimer.InitializeLate();
				_svNormalTapWindowOpen.InitializeLate();
				_svGasBurstSequence.InitializeLate();
				_svGasFxForce.InitializeLate();
				_svGasCharge.InitializeLate();
				_svRegenDelayTimer.InitializeLate();
				_svGasDepleted.InitializeLate();
				_svGasEnergy.InitializeLate();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		public virtual void Awake()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}
	}
}
