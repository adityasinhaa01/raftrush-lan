using FartTogether.Raft;
using UnityEngine;

namespace FartTogether.Obstacles
{
    /// <summary>
    /// Replicates BarrierKill.cs from _reference/PuffPirates/Track/BarrierKill.cs.
    /// Attached to thorn borders and lethal boundary triggers along the track.
    /// Detects raft contact and triggers hazard rebound with a recovery invulnerability window.
    /// </summary>
    [DisallowMultipleComponent]
    public class HazardBarrier : MonoBehaviour
    {
        [Tooltip("Duration of invulnerability in seconds following a hazard collision (original _invulnSeconds = 2.0s).")]
        [SerializeField] private float _invulnSeconds = 2.0f;

        [Tooltip("Whether to respawn at start spawn or last checkpoint (original _respawnAtStart = false).")]
        [SerializeField] private bool _respawnAtStart = false;

        private static float _lastKillTime = -10f;

        public float InvulnSeconds { get => _invulnSeconds; set => _invulnSeconds = value; }
        public bool RespawnAtStart { get => _respawnAtStart; set => _respawnAtStart = value; }

        private void OnCollisionEnter(Collision collision)
        {
            HandleContact(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleContact(other);
        }

        private void HandleContact(Collider other)
        {
            if (Time.time < _lastKillTime + _invulnSeconds) return;

            RaftController raft = other.GetComponentInParent<RaftController>();
            if (raft == null) return;

            Rigidbody rb = raft.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Kill/hazard impact: cancel forward velocity and impart a safety rebound away from the thorns
                Vector3 away = (raft.transform.position - transform.position);
                away.y = 0f;
                rb.linearVelocity = away.normalized * 4.0f;
                _lastKillTime = Time.time;
            }
        }
    }
}
