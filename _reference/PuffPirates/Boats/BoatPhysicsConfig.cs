using UnityEngine;

namespace PuffPirates.Boats
{
	[CreateAssetMenu(fileName = "BoatPhysicsConfig", menuName = "GamesMrkt/Boat/PhysicsConfig")]
	public class BoatPhysicsConfig : ScriptableObject
	{
		[Header("Gas Force")]
		[SerializeField]
		private float _normalGasForce = 900f;

		[Tooltip("Gas thrust is applied at CENTER-OF-MASS HEIGHT → a horizontal force produces NO pitch torque → the bow doesn't dive under hard thrust. Applying it this far BELOW the COM instead lifts the bow slightly on thrust (0 = neutral; raise to increase lift). Force application point only; the pirate's visual is unaffected.")]
		[SerializeField]
		[Min(0f)]
		private float _gasForceDrop;

		[Tooltip("Shapes how thrust ramps up while gas is held, over GasRampSeconds below. X = 0 (just pressed) → 1 (held for the full ramp duration), Y = force fraction from the floor (MinGasForce) to the ceiling (NormalGasForce). Default is a straight line (today's linear ramp); an ease-in/ease-out or S-curve gives the thrust a different feel.")]
		[SerializeField]
		private AnimationCurve _gasRampCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		[Tooltip("Seconds of continuous gas to go from MinGasForce (just pressed) to full thrust. Runtime-tunable per boat instance from the F2 debug panel.")]
		[SerializeField]
		[Range(0.2f, 8f)]
		private float _gasRampSeconds = 3f;

		[Tooltip("Thrust floor the instant gas is first pressed (before the ramp climbs it up to NormalGasForce). Runtime-tunable per boat instance from the F2 debug panel.")]
		[SerializeField]
		[Range(0f, 2000f)]
		private float _minGasForce = 100f;

		[Tooltip("Mirror of GasRampCurve for RELEASE: shapes how PirateGas.GasPowerFraction (the cosmetic 0..1 signal that animation/VFX/jiggle read — NOT actual thrust, which already cuts the instant input is released) fades out after gas is let go. X = 0 (just released) → 1 (fully decayed), Y = fraction of the power-at-release still showing. Default eases 1 → 0.")]
		[SerializeField]
		private AnimationCurve _gasReleaseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

		[Tooltip("Seconds for GasPowerFraction to fade from its value-at-release down to 0, shaped by GasReleaseCurve. Purely cosmetic — real thrust already stops immediately when gas is released.")]
		[SerializeField]
		[Range(0.05f, 5f)]
		private float _gasReleaseSeconds = 0.5f;

		[Header("Normal Gas Tap vs Hold")]
		[Tooltip("Normal Gas (left click) held for LESS than this counts as a TAP: a single fixed-force burst instead of the ramp. Held LONGER counts as a HOLD: nothing happens until this threshold passes (thrust/ramp only starts once tap is ruled out), then the normal ramp system runs as usual.")]
		[SerializeField]
		[Range(0.02f, 1f)]
		private float _normalGasTapThresholdSeconds = 0.1f;

		[Tooltip("Single ForceMode.Impulse magnitude applied the instant a TAP is confirmed (release before the threshold).")]
		[SerializeField]
		[Min(0f)]
		private float _normalGasTapBurstForce = 900f;

		[Tooltip("Fixed GasEnergy cost of one TAP burst (independent of NormalGasCost/sec, which only applies while holding).")]
		[SerializeField]
		[Min(0f)]
		private float _normalGasTapCost = 10f;

		[Header("Airborne Gas Cutoff")]
		[Tooltip("ON: once the boat loses ground/water contact (airborne), gas THRUST can't be applied. OFF: thrust always applies.")]
		[SerializeField]
		private bool _noGasWhenAirborne;

		[Tooltip("If a surface exists within this distance (m) below the hull, the boat counts as 'grounded'; farther than this it's AIRBORNE → thrust cuts (while _noGasWhenAirborne is on). Raise on a bigger boat.")]
		[SerializeField]
		[Min(0.2f)]
		private float _groundContactDistance = 1.5f;

		[Header("Horizontal Friction (legacy mechanic)")]
		[Tooltip("Water/ground horizontal friction (legacy raycast mechanic, GROUNDED only). 0 = today's slippery glide; RAISE = more friction (boat slows/holds faster), toward 0 = more slippery. Horizontal velocity only.")]
		[SerializeField]
		[Min(0f)]
		private float _glideFriction = 0.1f;

		[Header("Gas Consumption (/sec)")]
		[SerializeField]
		private float _normalGasCost = 12f;

		[Header("Status Effects")]
		[SerializeField]
		private float _slowMultiplier = 0.45f;

		[Header("Item Box (Task 6)")]
		[Tooltip("Impulse applied EVERY TICK while SpeedBoostTimer is active — the SAME force pattern as BoostPad.cs (transform.forward * force * dt, ForceMode.Impulse), repeated for the duration instead of a single tick.")]
		[SerializeField]
		private float _itemBoxBoostForce = 2600f;

		[Header("Swirl (Torque)")]
		[Tooltip("Turning around the boat while thrusting biases the force tangentially → the boat swirls. 0 = no torque (boat only gets pushed, doesn't turn). Y rotation is also locked on the Rigidbody.")]
		[SerializeField]
		private float _swirlBias;

		[Header("Center of Mass (keel — physical uprighting)")]
		[Tooltip("Local Y of the Rigidbody's center of mass. NEGATIVE = lower (bottom-heavy) → the boat rights itself under gravity torque even after flipping, and leans at the correct angle on a slope (physical). Applies to both mechanics; buoyancy mode uses the same value (prefab BuoyantBody._centerOfMassY).")]
		[SerializeField]
		private float _centerOfMassY = -0.7f;

		[Header("Water Surface / Soft Contact")]
		[Tooltip("Target hull-bottom height above the surface. The spring settles into a small amount of glide. LOWER = boat rides deeper in the water (live-tunable from the F2 panel).")]
		[SerializeField]
		[Min(0.05f)]
		private float _waterRideHeight = 0.3f;

		[SerializeField]
		[Min(0.1f)]
		private float _waterProbeAboveHull = 0.45f;

		[Tooltip("Mass-independent spring acceleration. Higher = stiffer water surface.")]
		[SerializeField]
		[Min(0f)]
		private float _waterSpring = 42f;

		[SerializeField]
		[Min(0f)]
		private float _waterDamping = 7.5f;

		[SerializeField]
		[Range(0.1f, 1.1f)]
		private float _waterSampleRadius = 0.32f;

		[SerializeField]
		[Range(0f, 1f)]
		private float _waterMinUpDot = 0.55f;

		[SerializeField]
		private LayerMask _waterSurfaceMask = -1;

		[Tooltip("Number of ride/bob sample points spaced evenly around the hull's circular rim (round inflatable-boat hull → no front/back/left/right). Design-time only, not F2-tunable.")]
		[SerializeField]
		[Range(3f, 16f)]
		private int _waterSamplePointCount = 8;

		[Header("Fake Wave Bob (cosmetic, legacy mechanic)")]
		[Tooltip("How far each ride point bobs up/down from WaterRideHeight, in meters — purely cosmetic (the water surface stays flat; this only varies the spring's per-point target height so the hull looks like it's riding gentle chop). 0 = disabled. Runtime-tunable (F2 debug panel).")]
		[SerializeField]
		[Min(0f)]
		private float _waterBobAmplitude;

		[Tooltip("How fast the per-point bob cycles. Runtime-tunable (F2 debug panel).")]
		[SerializeField]
		[Min(0f)]
		private float _waterBobFrequency = 0.5f;

		[Tooltip("Distance (m) between one fake-wave crest and the next. Keep this close to the hull's diameter (2×WaterSampleRadius) or smaller so opposite sides of the boat rock in opposite directions instead of the whole hull bobbing up/down together.")]
		[SerializeField]
		[Min(0.05f)]
		private float _waterBobWaveLength = 0.6f;

		[Tooltip("Below this horizontal speed (m/s), bob plays at full amplitude; above it, amplitude fades out over WaterBobFadeRange (no hard cutoff — at speed, the soft wave-riding look reads as floaty/wrong).")]
		[SerializeField]
		[Min(0f)]
		private float _waterBobSpeedThreshold = 3f;

		[Tooltip("Speed range (m/s) above WaterBobSpeedThreshold over which bob amplitude smoothly fades to 0.")]
		[SerializeField]
		[Min(0.01f)]
		private float _waterBobFadeRange = 2f;

		[Header("Anti-Tunnel Ramp (raycast ride)")]
		[Tooltip("Height above the hull the ride raycast STARTS from. High enough that even a boat deeply embedded in a ramp still sees the surface (origin never drops below it) → support never disappears, and it doesn't clip through. Doesn't CHANGE the ride feel (clearance is normalized against this).")]
		[SerializeField]
		[Min(0.5f)]
		private float _rideRayStartHeight = 2.5f;

		[Tooltip("Once a corner sinks this far BELOW the surface: downward velocity is cut and only the part PAST the threshold is softly corrected (never a full teleport → prevents tunneling without a pop/glitch).")]
		[SerializeField]
		[Min(0.05f)]
		private float _antiTunnelSnap = 0.5f;

		[Header("Leaning on Sloped Ground (physical)")]
		[Tooltip("On: the boat leans onto the ground normal below it via spring-damper torque (leans on a slope, stands upright on flat ground).")]
		[SerializeField]
		private bool _conformToGround = true;

		[Tooltip("Alignment spring stiffness. Higher = leans onto the ground faster.")]
		[SerializeField]
		[Min(0f)]
		private float _groundAlignSpring = 9f;

		[Tooltip("Alignment damping. Cuts oscillation (too low = bounces/shakes).")]
		[SerializeField]
		[Min(0f)]
		private float _groundAlignDamping = 3f;

		[Tooltip("Surfaces steeper than this angle (a wall) don't participate in alignment — normal.up component threshold.")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _groundAlignMinUpDot = 0.2f;

		[Tooltip("Max lean (pitch/roll) angle the boat can take on a slope, in degrees. Up to this angle the boat conforms EXACTLY to the ground; beyond it, lean is HELD at this angle (the boat leans no further). 0 = unlimited, lean fully onto the ground normal.")]
		[SerializeField]
		[Range(0f, 89f)]
		private float _groundAlignMaxTiltDeg = 30f;

		[Header("Flip Recovery (legacy mechanic ONLY)")]
		[Tooltip("The boat is uprighted IN PLACE once it tips this far over (transform.up · world-up threshold; -0.25 ≈ 105° over). Yaw is preserved, velocity + angular velocity are zeroed.")]
		[SerializeField]
		[Range(-1f, 0.3f)]
		private float _flipUprightDot = -0.25f;

		[Tooltip("How many units to lift the boat by while uprighting it (so it doesn't clip into the ground; 1-2 units).")]
		[SerializeField]
		[Min(0f)]
		private float _flipRecoverLift = 1.5f;

		[Header("Tilt Ceiling Smoothing (glitch prevention)")]
		[Tooltip("A hard tilt correction only engages once lean exceeds maxTilt by this margin (degrees) — inside the margin, the soft alignment spring holds it. Margin so a boundary case doesn't snap/glitch every tick.")]
		[SerializeField]
		[Min(0f)]
		private float _tiltClampMargin = 12f;

		[Tooltip("How fast the hard correction eases toward the ceiling once the margin is exceeded (0-1 per tick). Lower = softer; never a teleport.")]
		[SerializeField]
		[Range(0.05f, 1f)]
		private float _tiltClampLerp = 0.35f;

		public float NormalGasForce => _normalGasForce;

		public float GasForceDrop => _gasForceDrop;

		public AnimationCurve GasRampCurve => _gasRampCurve;

		public float GasRampSeconds => _gasRampSeconds;

		public float MinGasForce => _minGasForce;

		public AnimationCurve GasReleaseCurve => _gasReleaseCurve;

		public float GasReleaseSeconds => _gasReleaseSeconds;

		public float NormalGasTapThresholdSeconds => _normalGasTapThresholdSeconds;

		public float NormalGasTapBurstForce => _normalGasTapBurstForce;

		public float NormalGasTapCost => _normalGasTapCost;

		public bool NoGasWhenAirborne => _noGasWhenAirborne;

		public float GroundContactDistance => _groundContactDistance;

		public float GlideFriction => _glideFriction;

		public float NormalGasCost => _normalGasCost;

		public float SlowMultiplier => _slowMultiplier;

		public float ItemBoxBoostForce => _itemBoxBoostForce;

		public float SwirlBias => _swirlBias;

		public float CenterOfMassY => _centerOfMassY;

		public float WaterRideHeight => _waterRideHeight;

		public float WaterProbeAboveHull => _waterProbeAboveHull;

		public float WaterSpring => _waterSpring;

		public float WaterDamping => _waterDamping;

		public float WaterSampleRadius => _waterSampleRadius;

		public float WaterMinUpDot => _waterMinUpDot;

		public LayerMask WaterSurfaceMask => _waterSurfaceMask;

		public int WaterSamplePointCount => _waterSamplePointCount;

		public float WaterBobAmplitude => _waterBobAmplitude;

		public float WaterBobFrequency => _waterBobFrequency;

		public float WaterBobWaveLength => _waterBobWaveLength;

		public float WaterBobSpeedThreshold => _waterBobSpeedThreshold;

		public float WaterBobFadeRange => _waterBobFadeRange;

		public float RideRayStartHeight => _rideRayStartHeight;

		public float AntiTunnelSnap => _antiTunnelSnap;

		public bool ConformToGround => _conformToGround;

		public float GroundAlignSpring => _groundAlignSpring;

		public float GroundAlignDamping => _groundAlignDamping;

		public float GroundAlignMinUpDot => _groundAlignMinUpDot;

		public float GroundAlignMaxTiltDeg => _groundAlignMaxTiltDeg;

		public float FlipUprightDot => _flipUprightDot;

		public float FlipRecoverLift => _flipRecoverLift;

		public float TiltClampMargin => _tiltClampMargin;

		public float TiltClampLerp => _tiltClampLerp;
	}
}
