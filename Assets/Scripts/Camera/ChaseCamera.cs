using UnityEngine;

namespace FartTogether.CameraRig
{
    /// <summary>
    /// Smooth chase camera that follows the raft from behind and above, looking forward down the track.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChaseCamera : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Target transform to follow (typically the Raft).")]
        [SerializeField] private Transform _target;

        [Header("Offsets")]
        [Tooltip("Distance behind the target.")]
        [SerializeField] private float _distance = 6.0f;

        [Tooltip("Height above the target.")]
        [SerializeField] private float _height = 2.8f;

        [Tooltip("Look ahead target offset relative to the raft.")]
        [SerializeField] private Vector3 _lookOffset = new Vector3(0f, 0.6f, 1.2f);

        [Header("Damping")]
        [Tooltip("Smooth time for position following.")]
        [SerializeField] private float _positionSmoothTime = 0.12f;

        [Tooltip("Rotation follow speed.")]
        [SerializeField] private float _rotationSpeed = 8.0f;

        private Vector3 _currentVelocity;

        public static ChaseCamera Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        public float Distance
        {
            get => _distance;
            set => _distance = value;
        }

        public float Height
        {
            get => _height;
            set => _height = value;
        }

        public Vector3 LookOffset
        {
            get => _lookOffset;
            set => _lookOffset = value;
        }

        private void Start()
        {
            if (_target == null)
            {
                var raft = FindAnyObjectByType<Raft.RaftController>();
                if (raft != null)
                {
                    _target = raft.transform;
                }
            }

            if (_target != null)
            {
                SnapToTarget();
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Calculate desired position behind the target based on target forward
            Vector3 forward = _target.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 targetPosition = _target.position - forward * _distance + Vector3.up * _height;

            // Smooth position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, _positionSmoothTime);

            // Look towards target with look offset
            Vector3 lookTarget = _target.position + _target.TransformDirection(_lookOffset);
            Vector3 lookDir = (lookTarget - transform.position).normalized;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Immediately snaps the camera to the optimal chase position without damping.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;

            Vector3 forward = _target.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            transform.position = _target.position - forward * _distance + Vector3.up * _height;

            Vector3 lookTarget = _target.position + _target.TransformDirection(_lookOffset);
            Vector3 lookDir = (lookTarget - transform.position).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            }

            _currentVelocity = Vector3.zero;
        }
    }
}
