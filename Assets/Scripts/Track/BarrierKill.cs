using FartTogether.Raft;
using UnityEngine;

namespace FartTogether.Track
{
    public enum DeathCause
    {
        None = 0,
        Wall = 1,
        Obstacle = 2,
        Void = 3
    }

    /// <summary>
    /// Replicates the authentic hazard barrier and kill trigger mechanics from:
    /// - _reference/PuffPirates/Track/BarrierKill.cs (lines 14-44, 140-163)
    /// Used by thorns, swinging stamps/hammers, and track boundary hazards.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class BarrierKill : MonoBehaviour
    {
        /*
        // ==============================================================================
        // VERBATIM _reference/PuffPirates/Track/BarrierKill.cs (lines 14-44)
        // ==============================================================================
        [Tooltip("Respawn sonrası dokunulmazlık (sn). Duvar dibinde respawn döngüsünü engeller.")]
        [SerializeField]
        private float _invulnSeconds = 2f;

        [SerializeField]
        private bool _respawnAtStart = true;

        [Tooltip("Death cause reported to the boat (picks the death sound). None = automatic: solid collision → Wall, trigger → Obstacle.")]
        [SerializeField]
        private DeathCause _cause;

        public static bool LogKills = false;

        public void Configure(bool respawnAtStart, float invulnSeconds = 2f, DeathCause cause = DeathCause.None)
        {
            _respawnAtStart = respawnAtStart;
            _invulnSeconds = invulnSeconds;
            _cause = cause;
        }

        private void OnCollisionEnter(Collision c)
        {
            bool flag = c.contactCount > 0;
            ContactPoint contactPoint = (flag ? c.GetContact(0) : default(ContactPoint));
            Kill(c.collider, flag ? contactPoint.thisCollider : GetComponent<Collider>(), flag ? new Vector3?(contactPoint.point) : ((Vector3?)null), "Collision", (_cause == DeathCause.None) ? DeathCause.Wall : _cause);
        }

        private void OnTriggerEnter(Collider other)
        {
            Kill(other, GetComponent<Collider>(), null, "Trigger", (_cause != DeathCause.None) ? _cause : DeathCause.Obstacle);
        }
        // ==============================================================================
        */

        public static bool GloballyEnabled = true;

        [Tooltip("Invulnerability time following respawn in seconds (original _invulnSeconds = 2f).")]
        [SerializeField] private float _invulnSeconds = 2.0f;

        [Tooltip("If true, respawns at track start; if false, respawns at latest recorded checkpoint.")]
        [SerializeField] private bool _respawnAtStart = false;

        [Tooltip("Death cause reported to the boat (picks audio/effect).")]
        [SerializeField] private DeathCause _cause = DeathCause.None;

        public void Configure(bool respawnAtStart, float invulnSeconds = 2f, DeathCause cause = DeathCause.None)
        {
            _respawnAtStart = respawnAtStart;
            _invulnSeconds = invulnSeconds;
            _cause = cause;
        }

        private void OnCollisionEnter(Collision c)
        {
            bool flag = c.contactCount > 0;
            ContactPoint contactPoint = (flag ? c.GetContact(0) : default);
            Kill(c.collider, flag ? contactPoint.thisCollider : GetComponent<Collider>(), flag ? contactPoint.point : (Vector3?)null, "Collision", (_cause == DeathCause.None) ? DeathCause.Wall : _cause);
        }

        private void OnTriggerEnter(Collider other)
        {
            Kill(other, GetComponent<Collider>(), null, "Trigger", (_cause != DeathCause.None) ? _cause : DeathCause.Obstacle);
        }

        /*
        // ==============================================================================
        // VERBATIM _reference/PuffPirates/Track/BarrierKill.cs (lines 140-163)
        // ==============================================================================
        private void Kill(Collider hit, Collider self, Vector3? contact, string kind, DeathCause cause)
        {
            if (!GloballyEnabled)
            {
                return;
            }
            NetworkBoat componentInParent = hit.GetComponentInParent<NetworkBoat>();
            if (componentInParent == null || !componentInParent.IsServerInitialized || componentInParent.IsInvulnerable)
            {
                return;
            }
            RaceTrack instance = RaceTrack.Instance;
            if (!(instance == null))
            {
                LogKill(kind, this, self, componentInParent, contact);
                int num = ((!_respawnAtStart) ? componentInParent.CurrentCheckpointId : 0);
                if (!instance.TryGetSafeRespawnPose(num, 1.5f, out var position, out var rotation))
                {
                    Transform checkpoint = instance.GetCheckpoint(num);
                    position = ((checkpoint != null) ? (checkpoint.position + Vector3.up * 1.5f) : new Vector3(0f, 2f, 0f));
                    rotation = ((checkpoint != null) ? checkpoint.rotation : Quaternion.identity);
                }
                componentInParent.RespawnAt(position, rotation, _invulnSeconds, cause);
            }
        }
        // ==============================================================================
        */
        private void Kill(Collider hit, Collider self, Vector3? contact, string kind, DeathCause cause)
        {
            if (!GloballyEnabled) return;

            RaftRespawn raft = hit.GetComponentInParent<RaftRespawn>();
            if (raft == null) return;

            string hierarchy = RaftRespawn.GetHierarchyPath(gameObject.transform);
            raft.Respawn($"BarrierKill ({cause}, {kind})", hierarchy, self);
        }
    }
}
