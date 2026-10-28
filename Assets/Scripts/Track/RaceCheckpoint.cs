using FartTogether.Raft;
using UnityEngine;

namespace FartTogether.Track
{
    /// <summary>
    /// Checkpoint trigger along the race track.
    /// Replicates checkpoint tracking from:
    /// - _reference/PuffPirates/Track/LevelDefinition.cs (lines 129-146)
    /// - _reference/PuffPirates/Track/RaceZone.cs (lines 58-64)
    /// Registers the raft's progress and updates its safe respawn pose.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class RaceCheckpoint : MonoBehaviour
    {
        [Tooltip("Sequential 0-based checkpoint index along the track (0 = start, 7 = last before finish).")]
        [SerializeField] private int _checkpointIndex = 0;

        [Header("Visualization")]
        [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);

        public int CheckpointIndex
        {
            get => _checkpointIndex;
            set => _checkpointIndex = value;
        }

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            RaftRespawn respawn = other.GetComponentInParent<RaftRespawn>();
            if (respawn != null)
            {
                respawn.RegisterCheckpoint(_checkpointIndex, transform);
            }
        }

        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.color = _gizmoColor;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.85f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
