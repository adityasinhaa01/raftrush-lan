using FartTogether.Raft;
using UnityEngine;

namespace FartTogether.Water
{
    /// <summary>
    /// Trigger zone that applies directional water current or wind gust forces to the raft.
    /// Replicates the mechanics from original decompiled code:
    /// - _reference/PuffPirates/Obstacles/GustZone.cs:
    ///   rigidbody.AddForce(transform.TransformDirection(_localDir) * _force * dt, ForceMode.Impulse)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class WaterFlowZone : MonoBehaviour
    {
        [Header("Flow Configuration")]
        [Tooltip("Local direction of water flow (relative to this GameObject's rotation).")]
        [SerializeField] private Vector3 _localDirection = Vector3.right;

        [Tooltip("Force magnitude in Newtons. Quoted from original GustZone: defaults up to 950N, scene instances tune between 60N and 200N.")]
        [SerializeField] private float _flowForce = 150f;

        [Tooltip("Optional descriptive name of the water current (e.g. Canyon Forward Flow, Canyon Counter Flow).")]
        [SerializeField] private string _zoneDescription = "Water Current";

        [Header("Debug Visualization")]
        [Tooltip("Draw gizmo arrow in Scene view indicating direction and strength.")]
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private Color _gizmoColor = new Color(0.1f, 0.7f, 1.0f, 0.4f);

        public Vector3 LocalDirection
        {
            get => _localDirection;
            set => _localDirection = value.normalized;
        }

        public float FlowForce
        {
            get => _flowForce;
            set => _flowForce = value;
        }

        public string ZoneDescription => _zoneDescription;

        public Vector3 WorldFlowDirection => transform.TransformDirection(_localDirection.normalized);

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null)
            {
                RaftController raft = other.GetComponentInParent<RaftController>();
                if (raft != null)
                {
                    rb = raft.GetComponent<Rigidbody>();
                }
            }

            if (rb != null)
            {
                // AddForce with ForceMode.Force applies continuous acceleration (force * dt in FixedUpdate)
                // Exactly matching GustZone.cs: AddForce(dir * force * dt, ForceMode.Impulse)
                Vector3 worldDir = WorldFlowDirection;
                rb.AddForce(worldDir * _flowForce, ForceMode.Force);
            }
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;

            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = _gizmoColor;
            Gizmos.matrix = Matrix4x4.identity;

            Vector3 center = col.bounds.center;
            Vector3 worldDir = WorldFlowDirection;

            // Draw flow direction arrows
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.8f);
            Gizmos.DrawRay(center, worldDir * 3.0f);
            Gizmos.DrawSphere(center + worldDir * 3.0f, 0.25f);

            // Draw wire bounds of the flow box
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = _gizmoColor;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.9f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
