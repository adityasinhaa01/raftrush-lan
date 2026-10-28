using System;
using UnityEngine;

namespace FartTogether.Obstacles
{
    /// <summary>
    /// Synchronizes a moving obstacle collider node (e.g. *_Col, collider, ~KillTrigger) 1:1 with its
    /// corresponding animated visible mesh node in lockstep with Unity PhysX physics simulation.
    /// 
    /// Ensures:
    /// 1. Collider transform matches the animated mesh transform at all times (position mismatch ~0, angle mismatch ~0).
    /// 2. Kinematic Rigidbody is moved via MovePosition/MoveRotation each FixedUpdate so PhysX resolves continuous velocity contact.
    /// 3. Scene view wireframe gizmos render in exact sync with the visible mesh.
    /// 4. Respects authentic ripped prefab hierarchy (no destructive re-parenting required).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ObstacleColliderSync : MonoBehaviour
    {
        [Tooltip("The visible animated mesh or bone transform that this collider tracks.")]
        [SerializeField] private Transform _targetMesh;

        [Tooltip("Authentic relative position offset at rest relative to target mesh.")]
        [SerializeField] private Vector3 _restRelativePos = Vector3.zero;

        [Tooltip("Authentic relative rotation offset at rest relative to target mesh.")]
        [SerializeField] private Quaternion _restRelativeRot = Quaternion.identity;

        [SerializeField] private Rigidbody _rigidbody;

        private bool _isInitialized = false;

        public Transform TargetMesh { get => _targetMesh; set => _targetMesh = value; }
        public Vector3 RestRelativePos { get => _restRelativePos; set => _restRelativePos = value; }
        public Quaternion RestRelativeRot { get => _restRelativeRot; set => _restRelativeRot = value; }
        public Rigidbody AttachedRigidbody => _rigidbody;

        private void Awake()
        {
            EnsureRigidbody();
            if (_targetMesh != null && !_isInitialized)
            {
                CalculateRestOffset();
            }
        }

        private void OnEnable()
        {
            EnsureRigidbody();
            SyncPose(isFixedUpdate: false);
        }

        public void Configure(Transform targetMesh)
        {
            _targetMesh = targetMesh;
            EnsureRigidbody();
            CalculateRestOffset();
            SyncPose(isFixedUpdate: false);
        }

        public void Configure(Transform targetMesh, Vector3 restRelativePos, Quaternion restRelativeRot)
        {
            _targetMesh = targetMesh;
            _restRelativePos = restRelativePos;
            _restRelativeRot = restRelativeRot;
            _isInitialized = true;
            EnsureRigidbody();
            SyncPose(isFixedUpdate: false);
        }

        private void EnsureRigidbody()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void CalculateRestOffset()
        {
            if (_targetMesh == null) return;

            // Compute local relative offset from targetMesh to this collider at rest
            _restRelativePos = Quaternion.Inverse(_targetMesh.rotation) * (transform.position - _targetMesh.position);
            _restRelativeRot = Quaternion.Inverse(_targetMesh.rotation) * transform.rotation;
            _isInitialized = true;
        }

        public void SyncPose(bool isFixedUpdate)
        {
            if (_targetMesh == null) return;

            Vector3 worldPos = _targetMesh.position + _targetMesh.rotation * _restRelativePos;
            Quaternion worldRot = _targetMesh.rotation * _restRelativeRot;

            transform.position = worldPos;
            transform.rotation = worldRot;

            if (_rigidbody != null)
            {
                if (isFixedUpdate && Application.isPlaying)
                {
                    _rigidbody.MovePosition(worldPos);
                    _rigidbody.MoveRotation(worldRot);
                }
                else
                {
                    _rigidbody.position = worldPos;
                    _rigidbody.rotation = worldRot;
                }
            }
        }

        private void FixedUpdate()
        {
            if (_targetMesh == null) return;
            SyncPose(isFixedUpdate: true);
        }

        private void LateUpdate()
        {
            if (_targetMesh == null) return;
            SyncPose(isFixedUpdate: false);
        }
    }
}
