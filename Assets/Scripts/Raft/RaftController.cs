using System;
using FartTogether.PlayerInput;
using UnityEngine;

namespace FartTogether.Raft
{
    /// <summary>
    /// Rigidbody-based Raft controller with 4 perimeter seat points.
    /// Each seat applies reaction gas thrust at its position according to Newton's 3rd law.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class RaftController : MonoBehaviour
    {
        [System.Serializable]
        public struct SeatPoint
        {
            [Tooltip("Display name of the seat (e.g. Rear-Right).")]
            public string name;

            [Tooltip("Angle on the raft rim in degrees (0 = +Z Front, 90 = +X Right, 180 = -Z Back, 270 = -X Left).")]
            public float angleDeg;

            [Tooltip("Optional custom transform. If unassigned, position is derived from AngleDeg and AttachmentRadius.")]
            public Transform seatTransform;
        }

        [Header("Physics Tuning (BoatPhysicsConfig & BoatDefinition References)")]
        [Tooltip("Raft mass in kg.")]
        [SerializeField] private float _mass = 40f;

        [Tooltip("Linear damping / drag.")]
        [SerializeField] private float _linearDamping = 0.4f;

        [Tooltip("Angular damping / drag (prevents infinite spinning).")]
        [SerializeField] private float _angularDamping = 2.0f;

        [Tooltip("Low center of mass Y (keel). Negative Y rights the boat naturally under gravity.")]
        [SerializeField] private float _centerOfMassY = -0.7f;

        [Tooltip("Maximum horizontal speed in m/s (~50 km/h at 14 m/s).")]
        [SerializeField] private float _maxSpeed = 14f;

        [Header("Gas Thrust Tuning")]
        [Tooltip("Maximum thrust force in Newtons applied at full ramp.")]
        [SerializeField] private float _normalGasForce = 900f;

        [Tooltip("Distance below center of mass where force is applied (0 = COM height, neutral pitch).")]
        [SerializeField] private float _gasForceDrop = 0f;

        [Tooltip("Raft circular radius for seats.")]
        [SerializeField] private float _attachmentRadius = 1.3f;

        [Header("Upright Stabilization")]
        [Tooltip("Spring torque helping the raft stay upright on slopes and water.")]
        [SerializeField] private float _uprightTorque = 18f;

        [Tooltip("Damping for upright spring to avoid oscillation.")]
        [SerializeField] private float _uprightDamping = 4f;

        [Header("Seats")]
        [SerializeField] private SeatPoint[] _seats = new SeatPoint[4]
        {
            new SeatPoint { name = "Seat 1 (Rear-Right)", angleDeg = 135f, seatTransform = null },
            new SeatPoint { name = "Seat 2 (Rear-Left)", angleDeg = 225f, seatTransform = null },
            new SeatPoint { name = "Seat 3 (Front-Left)", angleDeg = 315f, seatTransform = null },
            new SeatPoint { name = "Seat 4 (Front-Right)", angleDeg = 45f, seatTransform = null }
        };

        [Header("Input Source")]
        [Tooltip("Reference to the gas input source component. If null, will search on this GameObject or scene.")]
        [SerializeField] private MonoBehaviour _inputSourceComponent = null;

        private Rigidbody _rb;
        private IGasInputSource _inputSource;

        public Rigidbody Rigidbody => _rb;
        public float CurrentSpeed => _rb != null ? _rb.linearVelocity.magnitude : 0f;
        public IGasInputSource InputSource => _inputSource;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            ConfigureRigidbody();

            ResolveInputSource();
        }

        private void OnValidate()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                ConfigureRigidbody();
            }
        }

        private void ConfigureRigidbody()
        {
            if (_rb == null) return;

            _rb.mass = _mass;
            _rb.linearDamping = _linearDamping;
            _rb.angularDamping = _angularDamping;
            _rb.centerOfMass = new Vector3(0f, _centerOfMassY, 0f);
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void ResolveInputSource()
        {
            if (_inputSourceComponent is IGasInputSource source)
            {
                _inputSource = source;
            }
            else
            {
                _inputSource = GetComponent<IGasInputSource>();
                if (_inputSource == null)
                {
                    _inputSource = FindAnyObjectByType<GasInput>();
                }
            }
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;

            if (_inputSource == null)
            {
                ResolveInputSource();
                if (_inputSource == null) return;
            }

            ApplyGasForces();
            ApplyUprightTorque();
            SoftClampSpeed();
        }

        private void ApplyGasForces()
        {
            for (int i = 0; i < _seats.Length; i++)
            {
                float thrustFraction = _inputSource.GetThrustFraction(i);
                if (thrustFraction <= 0.001f) continue;

                Vector3 thrustWorldPos = GetSeatWorldPosition(i);
                Vector3 thrustDir = GetSeatThrustDirection(i);

                float forceMagnitude = _normalGasForce * thrustFraction;
                _rb.AddForceAtPosition(thrustDir * forceMagnitude, thrustWorldPos, ForceMode.Force);
            }
        }

        /// <summary>
        /// Calculates seat world position based on custom Transform or perimeter circle.
        /// Height is adjusted relative to the Rigidbody center of mass minus GasForceDrop.
        /// </summary>
        public Vector3 GetSeatWorldPosition(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= _seats.Length) return transform.position;

            SeatPoint seat = _seats[seatIndex];
            Vector3 worldPos;
            if (seat.seatTransform != null)
            {
                worldPos = seat.seatTransform.position;
            }
            else
            {
                float rad = seat.angleDeg * Mathf.Deg2Rad;
                Vector3 localPos = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * _attachmentRadius;
                worldPos = transform.TransformPoint(localPos);
            }

            // Apply thrust at COM height minus drop to eliminate unwanted bow pitch
            worldPos.y = _rb.worldCenterOfMass.y - _gasForceDrop;
            return worldPos;
        }

        /// <summary>
        /// Reaction thrust direction pointing inward opposite to radial outward expulsion.
        /// </summary>
        public Vector3 GetSeatThrustDirection(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= _seats.Length) return transform.forward;

            SeatPoint seat = _seats[seatIndex];
            float rad = seat.angleDeg * Mathf.Deg2Rad;
            // Outward vector is (sin θ, 0, cos θ) in boat local space.
            // Gas farts outward behind player -> reaction force is -outward = (-sin θ, 0, -cos θ).
            Vector3 localThrustDir = new Vector3(-Mathf.Sin(rad), 0f, -Mathf.Cos(rad));
            Vector3 worldThrustDir = transform.TransformDirection(localThrustDir);
            worldThrustDir.y = 0f;

            return worldThrustDir.sqrMagnitude > 0.001f ? worldThrustDir.normalized : transform.forward;
        }

        private void ApplyUprightTorque()
        {
            // Upright spring torque to softly maintain upright posture
            Vector3 currentUp = transform.up;
            Vector3 targetUp = Vector3.up;

            Vector3 axis = Vector3.Cross(currentUp, targetUp);
            float angle = Mathf.Asin(Mathf.Clamp01(axis.magnitude));

            if (axis.sqrMagnitude > 0.0001f)
            {
                Vector3 torque = axis.normalized * (angle * _uprightTorque) - _rb.angularVelocity * _uprightDamping;
                _rb.AddTorque(torque, ForceMode.Acceleration);
            }
        }

        private void SoftClampSpeed()
        {
            Vector3 vel = _rb.linearVelocity;
            Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);
            float speed = horizontalVel.magnitude;

            if (speed > _maxSpeed && speed > 0.001f)
            {
                float excess = speed - _maxSpeed;
                _rb.AddForce(-horizontalVel.normalized * (excess * 0.5f), ForceMode.VelocityChange);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_seats == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _attachmentRadius);

            for (int i = 0; i < _seats.Length; i++)
            {
                Vector3 seatPos = Application.isPlaying && _rb != null ? GetSeatWorldPosition(i) : DerivedSeatPosEditor(i);
                Vector3 thrustDir = Application.isPlaying && _rb != null ? GetSeatThrustDirection(i) : DerivedThrustDirEditor(i);

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(seatPos, 0.15f);

                Gizmos.color = Color.green;
                Gizmos.DrawRay(seatPos, thrustDir * 1.0f);
            }
        }

        private Vector3 DerivedSeatPosEditor(int index)
        {
            if (index < 0 || index >= _seats.Length) return transform.position;
            SeatPoint seat = _seats[index];
            if (seat.seatTransform != null) return seat.seatTransform.position;

            float rad = seat.angleDeg * Mathf.Deg2Rad;
            Vector3 localPos = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * _attachmentRadius;
            return transform.TransformPoint(localPos);
        }

        private Vector3 DerivedThrustDirEditor(int index)
        {
            if (index < 0 || index >= _seats.Length) return transform.forward;
            SeatPoint seat = _seats[index];
            float rad = seat.angleDeg * Mathf.Deg2Rad;
            Vector3 localThrustDir = new Vector3(-Mathf.Sin(rad), 0f, -Mathf.Cos(rad));
            return transform.TransformDirection(localThrustDir).normalized;
        }
    }
}
