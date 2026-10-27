using System.Collections.Generic;
using FishNet.Utility.Template;
using PuffPirates.Biomes;
using PuffPirates.Networking;
using PuffPirates.Players;
using PuffPirates.Track;
using PuffPirates.Water;
using UnityEngine;

namespace PuffPirates.Boats
{
	[RequireComponent(typeof(NetworkBoat))]
	[DisallowMultipleComponent]
	public class BoatPhysicsController : TickNetworkBehaviour
	{
		[Tooltip("Base physics tuning data (gas force, water ride, ground alignment, flip recovery, tilt clamp). Required.")]
		[SerializeField]
		private BoatPhysicsConfig _config;

		private float _glideFriction;

		private bool _noGasWhenAirborne;

		private float _waterRideHeight;

		private float _gasRampSeconds;

		private float _minGasForce;

		private float _gasReleaseSeconds;

		private float _waterBobAmplitude;

		private float _waterBobFrequency;

		private float _waterBobWaveLength;

		private float _waterBobSpeedThreshold;

		private float _waterBobFadeRange;

		private NetworkBoat _boat;

		private Rigidbody _rb;

		private BuoyantBody _buoyancy;

		private BoatWaterRide _waterRide;

		private BoatGroundStabilizer _groundStabilizer;

		private readonly RaycastHit[] _groundedHits = new RaycastHit[8];

		private readonly HashSet<int> _ownColliderIds = new HashSet<int>();

		private bool NetworkInitialize___EarlyPuffPirates_002EBoats_002EBoatPhysicsControllerAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EBoats_002EBoatPhysicsControllerAssembly_002DCSharp_002Edll_Excuted;

		public float GlideFriction
		{
			get
			{
				return _glideFriction;
			}
			set
			{
				_glideFriction = Mathf.Max(0f, value);
			}
		}

		public float Mass
		{
			get
			{
				if (!(_rb != null))
				{
					return 0f;
				}
				return _rb.mass;
			}
			set
			{
				if (_rb != null)
				{
					_rb.mass = Mathf.Max(0.1f, value);
				}
			}
		}

		public bool NoGasWhenAirborne
		{
			get
			{
				return _noGasWhenAirborne;
			}
			set
			{
				_noGasWhenAirborne = value;
			}
		}

		public float GasRampSeconds
		{
			get
			{
				return _gasRampSeconds;
			}
			set
			{
				_gasRampSeconds = Mathf.Max(0.01f, value);
			}
		}

		public float MinGasForce
		{
			get
			{
				return _minGasForce;
			}
			set
			{
				_minGasForce = Mathf.Max(0f, value);
			}
		}

		public float GasReleaseSeconds
		{
			get
			{
				return _gasReleaseSeconds;
			}
			set
			{
				_gasReleaseSeconds = Mathf.Max(0.01f, value);
			}
		}

		public AnimationCurve GasRampCurve
		{
			get
			{
				if (!(_config != null))
				{
					return null;
				}
				return _config.GasRampCurve;
			}
		}

		public AnimationCurve GasReleaseCurve
		{
			get
			{
				if (!(_config != null))
				{
					return null;
				}
				return _config.GasReleaseCurve;
			}
		}

		public float WaterRideHeight
		{
			get
			{
				if (_waterRide == null)
				{
					return _waterRideHeight;
				}
				return _waterRide.RideHeight;
			}
			set
			{
				_waterRideHeight = Mathf.Max(0.05f, value);
				if (_waterRide != null)
				{
					_waterRide.RideHeight = _waterRideHeight;
				}
				if (_groundStabilizer != null)
				{
					_groundStabilizer.WaterRideHeight = _waterRideHeight;
				}
			}
		}

		public float WaterBobAmplitude
		{
			get
			{
				if (_waterRide == null)
				{
					return _waterBobAmplitude;
				}
				return _waterRide.BobAmplitude;
			}
			set
			{
				_waterBobAmplitude = Mathf.Max(0f, value);
				if (_waterRide != null)
				{
					_waterRide.BobAmplitude = _waterBobAmplitude;
				}
			}
		}

		public float WaterBobFrequency
		{
			get
			{
				if (_waterRide == null)
				{
					return _waterBobFrequency;
				}
				return _waterRide.BobFrequency;
			}
			set
			{
				_waterBobFrequency = Mathf.Max(0f, value);
				if (_waterRide != null)
				{
					_waterRide.BobFrequency = _waterBobFrequency;
				}
			}
		}

		public float WaterBobWaveLength
		{
			get
			{
				if (_waterRide == null)
				{
					return _waterBobWaveLength;
				}
				return _waterRide.BobWaveLength;
			}
			set
			{
				_waterBobWaveLength = Mathf.Max(0.05f, value);
				if (_waterRide != null)
				{
					_waterRide.BobWaveLength = _waterBobWaveLength;
				}
			}
		}

		public float WaterBobSpeedThreshold
		{
			get
			{
				if (_waterRide == null)
				{
					return _waterBobSpeedThreshold;
				}
				return _waterRide.BobSpeedThreshold;
			}
			set
			{
				_waterBobSpeedThreshold = Mathf.Max(0f, value);
				if (_waterRide != null)
				{
					_waterRide.BobSpeedThreshold = _waterBobSpeedThreshold;
				}
			}
		}

		public float WaterBobFadeRange
		{
			get
			{
				if (_waterRide == null)
				{
					return _waterBobFadeRange;
				}
				return _waterRide.BobFadeRange;
			}
			set
			{
				_waterBobFadeRange = Mathf.Max(0.01f, value);
				if (_waterRide != null)
				{
					_waterRide.BobFadeRange = _waterBobFadeRange;
				}
			}
		}

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EBoats_002EBoatPhysicsController_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			if (_config == null)
			{
				Debug.LogError("[BoatPhysicsController] No BoatPhysicsConfig assigned on '" + base.name + "' — boat physics will not function correctly.", this);
			}
			_boat = GetComponent<NetworkBoat>();
			_rb = _boat.Rigidbody;
			if (base.IsServerInitialized && _rb != null && !_rb.isKinematic)
			{
				_rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			}
			if (_rb != null)
			{
				_rb.centerOfMass = new Vector3(0f, _config.CenterOfMassY, 0f);
			}
			_buoyancy = GetComponent<BuoyantBody>();
			if (_buoyancy != null)
			{
				_buoyancy.DrivenExternally = true;
			}
			_ownColliderIds.Clear();
			Collider[] componentsInChildren = GetComponentsInChildren<Collider>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i] != null)
				{
					_ownColliderIds.Add(componentsInChildren[i].GetInstanceID());
				}
			}
			_glideFriction = _config.GlideFriction;
			_noGasWhenAirborne = _config.NoGasWhenAirborne;
			_waterRideHeight = _config.WaterRideHeight;
			_gasRampSeconds = _config.GasRampSeconds;
			_minGasForce = _config.MinGasForce;
			_gasReleaseSeconds = _config.GasReleaseSeconds;
			_waterBobAmplitude = _config.WaterBobAmplitude;
			_waterBobFrequency = _config.WaterBobFrequency;
			_waterBobWaveLength = _config.WaterBobWaveLength;
			_waterBobSpeedThreshold = _config.WaterBobSpeedThreshold;
			_waterBobFadeRange = _config.WaterBobFadeRange;
			_waterRide = new BoatWaterRide(_rb, _boat, _config.WaterSurfaceMask, IsSurfaceHit)
			{
				RideHeight = _waterRideHeight,
				ProbeAboveHull = _config.WaterProbeAboveHull,
				Spring = _config.WaterSpring,
				Damping = _config.WaterDamping,
				SampleRadius = _config.WaterSampleRadius,
				MinUpDot = _config.WaterMinUpDot,
				RideRayStartHeight = _config.RideRayStartHeight,
				AntiTunnelSnap = _config.AntiTunnelSnap,
				PointCount = _config.WaterSamplePointCount,
				BobAmplitude = _waterBobAmplitude,
				BobFrequency = _waterBobFrequency,
				BobWaveLength = _waterBobWaveLength,
				BobSpeedThreshold = _waterBobSpeedThreshold,
				BobFadeRange = _waterBobFadeRange
			};
			_groundStabilizer = new BoatGroundStabilizer(_rb, _boat, _config.WaterSurfaceMask, IsSurfaceHit)
			{
				ConformToGround = _config.ConformToGround,
				AlignSpring = _config.GroundAlignSpring,
				AlignDamping = _config.GroundAlignDamping,
				AlignMinUpDot = _config.GroundAlignMinUpDot,
				MaxTiltDeg = _config.GroundAlignMaxTiltDeg,
				FlipUprightDot = _config.FlipUprightDot,
				FlipRecoverLift = _config.FlipRecoverLift,
				TiltClampMargin = _config.TiltClampMargin,
				TiltClampLerp = _config.TiltClampLerp,
				WaterProbeAboveHull = _config.WaterProbeAboveHull,
				WaterRideHeight = _waterRideHeight
			};
		}

		protected override void TimeManager_OnTick()
		{
			if (!base.IsServerInitialized || _boat.IsDead)
			{
				return;
			}
			ApplyBiomePhysics();
			RaceState instance = RaceState.Instance;
			bool flag = LevelBuilder.Instance == null || LevelBuilder.UsesBuoyancy(LevelBuilder.Instance.CurrentMap);
			bool flag2 = ComputeGrounded();
			if ((_buoyancy != null && _buoyancy.InWater) & flag)
			{
				_buoyancy.ApplyBuoyancy((float)base.TimeManager.TickDelta, drawForceArrows: true);
			}
			else
			{
				_waterRide.Apply(base.transform, drawForceArrows: true);
				_groundStabilizer.ApplyGroundAlign(base.transform);
				_groundStabilizer.RecoverIfFlipped(base.transform);
				_groundStabilizer.ClampTilt(base.transform);
				if (((_glideFriction > 0f) & flag2) && _rb != null)
				{
					Vector3 linearVelocity = _rb.linearVelocity;
					Vector3 vector = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
					_rb.AddForce(-vector * _glideFriction, ForceMode.Acceleration);
				}
			}
			if (_rb != null && _boat.BaseMass > 0f)
			{
				bool flag3 = _boat.HasStatus(BoatStatusFlags.MassHeavy);
				bool flag4 = _boat.HasStatus(BoatStatusFlags.MassLight);
				bool num = _boat.HasStatus(BoatStatusFlags.Plank);
				float num2 = (flag3 ? 1.6f : (flag4 ? 0.7f : 1f));
				if (num)
				{
					num2 *= 1.2f;
				}
				float num3 = _boat.BaseMass * num2;
				_rb.mass = num3;
				if (_buoyancy != null)
				{
					_buoyancy.DisplacementMass = num3;
				}
				_rb.centerOfMass = new Vector3(0f, _config.CenterOfMassY, 0f);
				if (num)
				{
					Vector3 linearVelocity2 = _rb.linearVelocity;
					Vector3 vector2 = new Vector3(linearVelocity2.x, 0f, linearVelocity2.z);
					_rb.AddForce(-vector2 * 2f, ForceMode.Acceleration);
				}
			}
			if (instance != null && !instance.CanMove)
			{
				ClearGasVisuals();
				return;
			}
			BoatDefinition definition = _boat.Definition;
			float attachmentRadius = _boat.AttachmentRadius;
			float maxSpeed = _boat.MaxSpeed;
			float num4 = ((definition != null) ? definition.GasEfficiency : 1f);
			float num5 = (float)base.TimeManager.TickDelta;
			float num6 = ((instance != null) ? instance.NormalGasForce : _config.NormalGasForce);
			float swirlBias = ((instance != null) ? instance.SwirlBias : _config.SwirlBias);
			float num7 = (_boat.HasStatus(BoatStatusFlags.GasBoost) ? 1.5f : 1f) * (_boat.HasStatus(BoatStatusFlags.GasNerf) ? 0.55f : 1f);
			num6 *= num7;
			float gasRampSeconds = _gasRampSeconds;
			float minGasForce = _minGasForce;
			float num8 = (_boat.HasStatus(BoatStatusFlags.Slow) ? _config.SlowMultiplier : 1f);
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (piratePlayer == null || piratePlayer.BoatIndex != _boat.BoatIndex)
				{
					continue;
				}
				bool flag5 = false;
				bool mousePuff = false;
				float appliedForce = 0f;
				float powerFraction = 0f;
				if (!piratePlayer.IsStunned && piratePlayer.TryGetServerInput(out var input))
				{
					bool normalGas = input.NormalGas;
					if (normalGas)
					{
						if (!piratePlayer.Gas.NormalTapConfirmedHold)
						{
							if (!piratePlayer.Gas.NormalTapWindowOpen)
							{
								piratePlayer.Gas.BeginNormalTapWindow();
							}
							if (piratePlayer.Gas.AdvanceNormalTapWindow(num5, _config.NormalGasTapThresholdSeconds))
							{
								piratePlayer.Gas.ConfirmNormalHold();
							}
						}
					}
					else
					{
						if (piratePlayer.Gas.NormalTapWindowOpen)
						{
							Vector3 vector3 = GasForceDir(piratePlayer, input, swirlBias);
							Vector3 position = GasThrustWorldPos(piratePlayer, attachmentRadius);
							_rb.AddForceAtPosition(vector3 * _config.NormalGasTapBurstForce * num4 * num8, position, ForceMode.Impulse);
							piratePlayer.Gas.FireNormalTapBurst(_config.NormalGasTapCost);
						}
						if (piratePlayer.Gas.NormalTapWindowOpen || piratePlayer.Gas.NormalTapConfirmedHold)
						{
							piratePlayer.Gas.ResetNormalTapState();
						}
					}
					if (normalGas && piratePlayer.Gas.NormalTapConfirmedHold && piratePlayer.Gas.HasGas && (flag2 || !_noGasWhenAirborne))
					{
						Vector3 vector4 = GasForceDir(piratePlayer, input, swirlBias);
						piratePlayer.Gas.AddGasCharge(num5, gasRampSeconds);
						PirateGas.GasRamp gasRamp = piratePlayer.Gas.GasRampFactor(gasRampSeconds, _config.GasRampCurve);
						float num9 = Mathf.Lerp(minGasForce, num6 * gasRamp.DepleteMul, gasRamp.ChargeRamp);
						float num10 = num9 * num4 * num8;
						Vector3 position2 = GasThrustWorldPos(piratePlayer, attachmentRadius);
						_rb.AddForceAtPosition(vector4 * num10, position2, ForceMode.Force);
						piratePlayer.Gas.ConsumeGas(_config.NormalGasCost * num5);
						flag5 = true;
						appliedForce = num9;
						powerFraction = gasRamp.ChargeRamp;
						mousePuff = input.MousePuff;
					}
				}
				else
				{
					piratePlayer.Gas.ResetNormalTapState();
				}
				if (flag5)
				{
					piratePlayer.Gas.SetGasVisual(on: true, appliedForce, powerFraction, mousePuff);
					continue;
				}
				if (piratePlayer.Gas.IsGassing)
				{
					piratePlayer.Gas.BeginRelease();
				}
				float powerFraction2 = piratePlayer.Gas.AdvanceReleaseDecay(num5, _gasReleaseSeconds, _config.GasReleaseCurve);
				piratePlayer.Gas.SetGasVisual(on: false, 0f, powerFraction2, mousePuff: false);
				piratePlayer.Gas.RegenGas(num5);
			}
			if (_boat.IsSpeedBoosted)
			{
				_rb.AddForce(base.transform.forward * _config.ItemBoxBoostForce * num5, ForceMode.Impulse);
			}
			SoftClampSpeed(maxSpeed);
		}

		private void ApplyBiomePhysics()
		{
			if (!(_rb == null))
			{
				Vector3 windForce = BiomeController.WindForce;
				if (windForce.sqrMagnitude > 0.0001f)
				{
					_rb.AddForce(windForce, ForceMode.Acceleration);
				}
				float linearDragMul = BiomeController.LinearDragMul;
				if (linearDragMul > 1.0001f)
				{
					Vector3 linearVelocity = _rb.linearVelocity;
					Vector3 vector = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
					_rb.AddForce(-vector * (linearDragMul - 1f), ForceMode.Acceleration);
				}
			}
		}

		private void ClearGasVisuals()
		{
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (!(piratePlayer == null) && piratePlayer.BoatIndex == _boat.BoatIndex)
				{
					piratePlayer.Gas.SetGasVisual(on: false, 0f, 0f, mousePuff: false);
					piratePlayer.Gas.ResetGasCharge();
					piratePlayer.Gas.ResetNormalTapState();
				}
			}
		}

		private Vector3 GasForceDir(PiratePlayer player, PirateInput input, float swirlBias)
		{
			Vector3 outwardDir = player.GetOutwardDir(base.transform);
			Vector3 vector = Vector3.Cross(Vector3.up, outwardDir);
			float num = Mathf.Clamp(input.CircularMovement, -1f, 1f) * swirlBias;
			return (-outwardDir + vector * num).normalized;
		}

		private Vector3 GasThrustWorldPos(PiratePlayer player, float radius)
		{
			Vector3 attachWorldPos = player.GetAttachWorldPos(base.transform, radius);
			attachWorldPos.y = _rb.worldCenterOfMass.y - _config.GasForceDrop;
			return attachWorldPos;
		}

		private bool ComputeGrounded()
		{
			if (_rb == null)
			{
				return true;
			}
			if (_buoyancy != null && _buoyancy.InWater && _buoyancy.Channel != null && _buoyancy.Channel.TryWaterHeight(base.transform.position, out var surfaceY))
			{
				return base.transform.TransformPoint(new Vector3(0f, -0.5f, 0f)).y <= surfaceY + _config.GroundContactDistance;
			}
			int num = Physics.RaycastNonAlloc(base.transform.position + Vector3.up * 0.2f, maxDistance: 0.2f + _config.GroundContactDistance, direction: Vector3.down, results: _groundedHits, layerMask: _config.WaterSurfaceMask, queryTriggerInteraction: QueryTriggerInteraction.Ignore);
			for (int i = 0; i < num; i++)
			{
				if (IsSurfaceHit(_groundedHits[i]))
				{
					return true;
				}
			}
			return false;
		}

		private bool IsSurfaceHit(RaycastHit hit)
		{
			Collider collider = hit.collider;
			if (collider == null)
			{
				return false;
			}
			if (hit.rigidbody == _rb)
			{
				return false;
			}
			if (_ownColliderIds.Contains(collider.GetInstanceID()))
			{
				return false;
			}
			if (hit.rigidbody != null)
			{
				if (collider.GetComponentInParent<NetworkBoat>() != null)
				{
					return false;
				}
				if (collider.GetComponentInParent<PiratePlayer>() != null)
				{
					return false;
				}
			}
			return true;
		}

		private void SoftClampSpeed(float maxSpeed)
		{
			Vector3 linearVelocity = _rb.linearVelocity;
			Vector3 vector = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
			float magnitude = vector.magnitude;
			if (magnitude > maxSpeed && magnitude > 0.001f)
			{
				float num = magnitude - maxSpeed;
				_rb.AddForce(-vector.normalized * num * 0.5f, ForceMode.VelocityChange);
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EBoats_002EBoatPhysicsControllerAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EBoats_002EBoatPhysicsControllerAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EBoats_002EBoatPhysicsControllerAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EBoats_002EBoatPhysicsControllerAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void Awake_UserLogic_PuffPirates_002EBoats_002EBoatPhysicsController_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Tick);
		}
	}
}
