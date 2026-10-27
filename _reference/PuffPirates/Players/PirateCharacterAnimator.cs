using UnityEngine;

namespace PuffPirates.Players
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(PiratePlayer))]
	[RequireComponent(typeof(Animator))]
	public sealed class PirateCharacterAnimator : MonoBehaviour
	{
		private const float MinimumDeltaTime = 0.004166667f;

		private const float MinimumMove = -1f;

		private const float MaximumMove = 1f;

		private static readonly int MoveParameter = Animator.StringToHash("Move");

		private static readonly int IsGassingParameter = Animator.StringToHash("IsGassing");

		private static readonly int IsStunnedParameter = Animator.StringToHash("IsStunned");

		private static readonly int GasPowerParameter = Animator.StringToHash("GasPower");

		[Header("Referans")]
		[SerializeField]
		private Animator _animator;

		[Header("Hareket Okuma")]
		[Tooltip("Bu açısal hızda hareket animasyonu tam eğime ulaşır.")]
		[SerializeField]
		[Min(1f)]
		private float _fullMoveSpeedDeg = 100f;

		[SerializeField]
		[Min(0.01f)]
		private float _moveSmoothTime = 0.08f;

		private PiratePlayer _player;

		private float _lastAttachmentAngle;

		private float _smoothedMove;

		private float _moveSmoothVelocity;

		private bool _hasAngleSample;

		private void Awake()
		{
			_player = GetComponent<PiratePlayer>();
			if (_animator == null)
			{
				_animator = GetComponent<Animator>();
			}
			if (!(_animator == null))
			{
				_animator.applyRootMotion = false;
				_animator.updateMode = AnimatorUpdateMode.Normal;
				_animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
			}
		}

		private void OnEnable()
		{
			_hasAngleSample = false;
			_smoothedMove = 0f;
			_moveSmoothVelocity = 0f;
		}

		private void LateUpdate()
		{
			if (CanAnimate())
			{
				float move = SampleMove();
				bool isStunned = _player.IsStunned;
				bool isGassing = _player.Gas.IsGassing;
				PushParameters(move, isStunned, isGassing);
			}
		}

		private bool CanAnimate()
		{
			if (_player != null && _animator != null)
			{
				return _animator.runtimeAnimatorController != null;
			}
			return false;
		}

		private float SampleMove()
		{
			float visualAngle = _player.VisualAngle;
			float num = Mathf.Max(Time.deltaTime, 0.004166667f);
			float target = 0f;
			if (_hasAngleSample)
			{
				target = Mathf.Clamp(Mathf.DeltaAngle(_lastAttachmentAngle, visualAngle) / num / _fullMoveSpeedDeg, -1f, 1f);
			}
			_lastAttachmentAngle = visualAngle;
			_hasAngleSample = true;
			_smoothedMove = Mathf.SmoothDamp(_smoothedMove, target, ref _moveSmoothVelocity, _moveSmoothTime, float.PositiveInfinity, num);
			return _smoothedMove;
		}

		private void PushParameters(float move, bool stunned, bool gassing)
		{
			float gasPowerFraction = _player.Gas.GasPowerFraction;
			_animator.SetFloat(MoveParameter, move);
			_animator.SetBool(IsGassingParameter, gassing);
			_animator.SetBool(IsStunnedParameter, stunned);
			_animator.SetFloat(GasPowerParameter, gasPowerFraction);
		}
	}
}
