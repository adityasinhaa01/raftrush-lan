using UnityEngine;

namespace FartTogether.Obstacles
{
    /// <summary>
    /// Replicates BounceObstacle.cs from _reference/PuffPirates/Obstacles/BounceObstacle.cs.
    /// Deflects and knocks back the raft away from the obstacle center upon impact.
    /// </summary>
    [DisallowMultipleComponent]
    public class BouncingSpike : MonoBehaviour
    {
        [Tooltip("Velocity change impulse (m/s) applied to the raft away from obstacle (original _knockbackImpulse = 2.0).")]
        [SerializeField] private float _knockbackImpulse = 2.0f;

        [Tooltip("Minimum seconds between consecutive impact impulses (original _sfxCooldownSeconds = 0.15s).")]
        [SerializeField] private float _cooldownSeconds = 0.15f;

        private float _nextHitAllowed;

        public float KnockbackImpulse { get => _knockbackImpulse; set => _knockbackImpulse = value; }

        private void OnCollisionEnter(Collision collision)
        {
            ApplyKnockback(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            ApplyKnockback(other);
        }

        private void ApplyKnockback(Collider other)
        {
            if (Time.time < _nextHitAllowed) return;

            Rigidbody rb = other.attachedRigidbody;
            if (rb == null) return;

            Vector3 awayDir = rb.position - transform.position;
            awayDir.y = 0f;

            if (awayDir.sqrMagnitude > 0.001f)
            {
                rb.AddForce(awayDir.normalized * _knockbackImpulse, ForceMode.VelocityChange);
                _nextHitAllowed = Time.time + _cooldownSeconds;
            }
        }
    }
}
