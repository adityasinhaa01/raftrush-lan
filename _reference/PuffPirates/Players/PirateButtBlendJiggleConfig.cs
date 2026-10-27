using UnityEngine;

namespace PuffPirates.Players
{
	[CreateAssetMenu(fileName = "PirateButtBlendJiggleConfig", menuName = "GamesMrkt/Pirate/ButtBlendJiggleConfig")]
	public class PirateButtBlendJiggleConfig : ScriptableObject
	{
		[Header("Butt region (mesh-local, BIND pose)")]
		[Tooltip("Center of the butt mask in the bind mesh (entered by measurement).")]
		[SerializeField]
		private Vector3 _maskCenter = new Vector3(0f, 0.342f, -0.096f);

		[SerializeField]
		private Vector3 _maskRadii = new Vector3(0.222f, 0.185f, 0.109f);

		[Tooltip("How many world units of offset the blendshape produces at weight=100 (weight = offset/unit*100).")]
		[SerializeField]
		[Min(0.01f)]
		private float _shapeUnit = 0.12f;

		[Header("Gas vibration (while farting)")]
		[Tooltip("Master switch for the X/Y/Z jiggle + balloon inflate blendshape offsets below. Off = no gas wobble/inflate at all.")]
		[SerializeField]
		private bool _gasJiggleEnabled = true;

		[SerializeField]
		[Min(1f)]
		private float _minFrequency = 8f;

		[SerializeField]
		[Min(1f)]
		private float _maxFrequency = 13f;

		[SerializeField]
		[Min(0f)]
		private float _sideAmplitude = 0.05f;

		[SerializeField]
		[Min(0f)]
		private float _verticalAmplitude = 0.02f;

		[SerializeField]
		[Min(0f)]
		private float _backAmplitude = 0.035f;

		[SerializeField]
		[Min(0f)]
		private float _inflateAmplitude = 0.03f;

		[Tooltip("Constant balloon-style butt swell while farting (world units, outward along the normal). Stacks ON TOP of the vibration — 'inflates like a balloon'. 0 = off (vibration only).")]
		[SerializeField]
		[Min(0f)]
		private float _balloonAmplitude = 0.06f;

		[SerializeField]
		[Min(0.01f)]
		private float _attackSpeed = 22f;

		[SerializeField]
		[Min(0.01f)]
		private float _releaseSpeed = 12f;

		[SerializeField]
		[Range(0.3f, 1f)]
		private float _minGasStrength = 0.55f;

		[Header("Butt swell (ButBone bone — driven by instantaneous gas force)")]
		[Tooltip("Master switch for the ButBone scale swell + tremble below. Off = ButBone stays at scale 1 (bone growth disabled).")]
		[SerializeField]
		private bool _boneSwellEnabled = true;

		[Tooltip("Maps the gas ramp fraction (X: 0..1 = PirateGas.GasPowerFraction, i.e. current/max thrust — 0 = just pressed, 1 = fully ramped up, same value GasVisuals/PirateCharacterAnimator drive their own force-based effects from) to the swell amount (Y: 0..1) used to Lerp toward _gasPeakScale. No time/duration involved — a pure current-vs-max-thrust reading each frame, so the butt tracks how hard gas is currently being pushed, not how long it's been held.")]
		[SerializeField]
		private AnimationCurve _boneSwellCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		[Tooltip("Scale ButBone swells to when the force ratio (run through _boneSwellCurve) reaches 1. Values are the clip peak (Y swells). Settles back to 1 as force drops back toward 0.")]
		[SerializeField]
		private Vector3 _gasPeakScale = new Vector3(1.4f, 1.4f, 1.4f);

		[Tooltip("ButBone trembling while swollen (scale +/- ratio, multiplied by the current swell amount). 0 = no trembling.")]
		[SerializeField]
		[Min(0f)]
		private float _gasTrembleAmplitude = 0.05f;

		[SerializeField]
		[Min(0.1f)]
		private float _gasTrembleFrequency = 12f;

		[Header("Butt reddening (But_Slide material, submesh 1 — RESERVOIR-based, via tiling)")]
		[Tooltip("Master switch for the reddening tiling effect below. Off = tiling stays at _redTilingMin (base look).")]
		[SerializeField]
		private bool _rednessEnabled = true;

		[Tooltip("Submesh index of the But_Slide material (order of Pirate.prefab SMR.sharedMaterials).")]
		[SerializeField]
		private int _redMaterialIndex = 1;

		[Tooltip("Base tiling while the reservoir is FULL (the material's authored value, red-on-top/white-gradient-below).")]
		[SerializeField]
		private Vector2 _redTilingMin = new Vector2(0f, 0.1f);

		[Tooltip("Tiling when the reservoir is fully EMPTY (curve=1) — as Y increases the gradient repeats more often, so more red shows on the butt.")]
		[SerializeField]
		private Vector2 _redTilingMax = new Vector2(4f, 1.3f);

		[Tooltip("X = reservoir DEPLETION ratio (0 = full, 1 = empty). Y = reddening intensity (0..1). Tied directly to the reservoir level (GasEnergy/MaxGas), not to elapsed time — reddens as the reservoir drains regardless of whether gas is held down, and fades naturally as it regens. Default: 0 up to 60%, then a linear rise.")]
		[SerializeField]
		private AnimationCurve _redCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.6f, 0f, 0f, 2.5f), new Keyframe(1f, 1f, 2.5f, 0f));

		[Header("Movement sway (boat acceleration/turning)")]
		[Tooltip("Master switch for the boat-inertia sway below. Off = no movement-driven offset (spring resets to zero).")]
		[SerializeField]
		private bool _movementSwayEnabled = true;

		[SerializeField]
		[Min(0f)]
		private float _moveInertia = 0.015f;

		[SerializeField]
		[Min(0f)]
		private float _turnSway = 0.003f;

		[SerializeField]
		[Min(0.01f)]
		private float _moveMaxOffset = 0.09f;

		[SerializeField]
		[Min(1f)]
		private float _moveStiffness = 90f;

		[SerializeField]
		[Min(0f)]
		private float _moveDamping = 11f;

		public Vector3 MaskCenter => _maskCenter;

		public Vector3 MaskRadii => _maskRadii;

		public float ShapeUnit => _shapeUnit;

		public bool GasJiggleEnabled => _gasJiggleEnabled;

		public float MinFrequency => _minFrequency;

		public float MaxFrequency => _maxFrequency;

		public float SideAmplitude => _sideAmplitude;

		public float VerticalAmplitude => _verticalAmplitude;

		public float BackAmplitude => _backAmplitude;

		public float InflateAmplitude => _inflateAmplitude;

		public float BalloonAmplitude => _balloonAmplitude;

		public float AttackSpeed => _attackSpeed;

		public float ReleaseSpeed => _releaseSpeed;

		public float MinGasStrength => _minGasStrength;

		public bool BoneSwellEnabled => _boneSwellEnabled;

		public AnimationCurve BoneSwellCurve => _boneSwellCurve;

		public Vector3 GasPeakScale => _gasPeakScale;

		public float GasTrembleAmplitude => _gasTrembleAmplitude;

		public float GasTrembleFrequency => _gasTrembleFrequency;

		public bool RednessEnabled => _rednessEnabled;

		public int RedMaterialIndex => _redMaterialIndex;

		public Vector2 RedTilingMin => _redTilingMin;

		public Vector2 RedTilingMax => _redTilingMax;

		public AnimationCurve RedCurve => _redCurve;

		public bool MovementSwayEnabled => _movementSwayEnabled;

		public float MoveInertia => _moveInertia;

		public float TurnSway => _turnSway;

		public float MoveMaxOffset => _moveMaxOffset;

		public float MoveStiffness => _moveStiffness;

		public float MoveDamping => _moveDamping;
	}
}
