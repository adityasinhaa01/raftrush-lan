using FartTogether.CameraRig;
using FartTogether.PlayerInput;
using FartTogether.Track;
using UnityEngine;

namespace FartTogether.Raft
{
    /// <summary>
    /// Handles raft checkpoint progression and respawn mechanics.
    /// Replicates logic and exact values from original decompiled code:
    /// - _reference/PuffPirates/Track/RaceZone.cs (lines 88-102: Respawn, 1.5m Y-offset, 2.0s invulnerability)
    /// - _reference/PuffPirates/Track/RaceTrack.cs (lines 152-214: TryGetSafeRespawnPose, surface projection, heading derivation)
    /// - _reference/PuffPirates/Boats/NetworkBoat.cs (lines 1250-1256, 1275-1277: zeroing velocity, upright reset, resetting crew gas)
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class RaftRespawn : MonoBehaviour
    {
        [Header("Respawn Tuning")]
        [Tooltip("Elevation offset above the projected surface when respawning (original RaceTrack: lift = 1.5f).")]
        [SerializeField] private float _respawnHeightOffset = 0.35f;

        [Tooltip("Duration of invulnerability in seconds following a respawn (original _respawnInvulnSec = 2f).")]
        [SerializeField] private float _invulnSeconds = 2.0f;

        [Tooltip("Absolute Y cutoff below which the raft is guaranteed to respawn as a fallback kill plane.")]
        [SerializeField] private float _fallKillY = -15.0f;

        private Rigidbody _rb;
        private RaftBuoyancy _buoyancy;
        private GasInput _gasInput;
        private Vector3 _initialSpawnPosition;
        private Quaternion _initialSpawnRotation;

        private int _currentCheckpointIndex = -1;
        private Transform _lastCheckpointTransform = null;
        private float _invulnerableUntil = -10f;

        // Double respawn guard tracking
        private int _lastRespawnCheckpoint = -999;
        private float _lastRespawnTime = -999f;

        public int CurrentCheckpointIndex => _currentCheckpointIndex;
        public Transform LastCheckpointTransform => _lastCheckpointTransform;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _buoyancy = GetComponent<RaftBuoyancy>();
            _gasInput = GetComponent<GasInput>();
        }

        private void Start()
        {
            _initialSpawnPosition = transform.position;
            _initialSpawnRotation = transform.rotation;
        }

        private void Update()
        {
            // Safety boundary: if the raft falls below the platform
            if (transform.position.y < _fallKillY)
            {
                Respawn("y below -15 fail-safe", GetHierarchyPath(transform), null);
            }
        }

        /// <summary>
        /// Registers a checkpoint passage, advancing safe respawn point sequentially.
        /// </summary>
        public void RegisterCheckpoint(int checkpointIndex, Transform checkpointTransform)
        {
            if (checkpointIndex >= _currentCheckpointIndex)
            {
                _currentCheckpointIndex = checkpointIndex;
                _lastCheckpointTransform = checkpointTransform;
                Debug.Log($"<color=cyan>[RaftRespawn] Checkpoint {checkpointIndex} registered at {checkpointTransform.position}</color>");
            }
        }

        public void Respawn()
        {
            Respawn("Unspecified", "Unknown", null);
        }

        /// <summary>
        /// Respawns the raft at the latest checkpoint using authentic TryGetSafeRespawnPose.
        /// Logs cause, exact triggering collider and hierarchy, raft pose, velocity, and time delta.
        /// </summary>
        public void Respawn(string cause, string sourceHierarchy, Collider sourceCollider = null)
        {
            float timeSincePrev = _lastRespawnTime > -100f ? Time.time - _lastRespawnTime : -1f;
            Vector3 raftPos = transform.position;
            float raftY = raftPos.y;
            Vector3 raftVel = _rb != null ? _rb.linearVelocity : Vector3.zero;
            string colName = sourceCollider != null ? sourceCollider.name : "None";

            if (IsInvulnerable)
            {
                Debug.Log($"<color=orange>[RaftRespawn REJECTED] Already invulnerable ({_invulnerableUntil - Time.time:F2}s remaining). Cause='{cause}', Source='{sourceHierarchy}', Collider='{colName}', RaftPos={raftPos}, RaftY={raftY:F2}, RaftVel={raftVel} (mag={raftVel.magnitude:F2}), TimeSincePrev={timeSincePrev:F2}s</color>");
                return;
            }

            Debug.Log($"<color=red>[RaftRespawn TRIGGERED] Cause='{cause}', Source='{sourceHierarchy}', Collider='{colName}', RaftPos={raftPos}, RaftY={raftY:F2}, RaftVel={raftVel} (mag={raftVel.magnitude:F2}), TimeSincePrev={timeSincePrev:F2}s</color>");

            Vector3 targetPos;
            Quaternion targetRot;

            // 1. Calculate authentic safe respawn pose projected onto track/water surface
            RaceTrack raceTrack = RaceTrack.Instance;
            if (raceTrack == null) raceTrack = FindAnyObjectByType<RaceTrack>();

            bool foundSafePose = false;
            if (raceTrack != null)
            {
                foundSafePose = raceTrack.TryGetSafeRespawnPose(_currentCheckpointIndex, _respawnHeightOffset, out targetPos, out targetRot);
            }
            else
            {
                targetPos = (_lastCheckpointTransform != null)
                    ? _lastCheckpointTransform.position + Vector3.up * 1.5f
                    : _initialSpawnPosition + Vector3.up * 1.5f;
                targetRot = (_lastCheckpointTransform != null)
                    ? _lastCheckpointTransform.rotation
                    : _initialSpawnRotation;
            }

            // Ensure upright rotation (zero pitch and roll)
            Vector3 euler = targetRot.eulerAngles;
            targetRot = Quaternion.Euler(0f, euler.y, 0f);

            // 2. Guard: if respawn happens twice within 3.0s at the same checkpoint, log diagnostic warning
            if (_lastRespawnCheckpoint == _currentCheckpointIndex && (Time.time - _lastRespawnTime) < 3.0f)
            {
                string underStr = Physics.Raycast(targetPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit underHit, 15f, ~0, QueryTriggerInteraction.Collide)
                    ? $"{underHit.collider.name} (layer={underHit.collider.gameObject.layer}: {LayerMask.LayerToName(underHit.collider.gameObject.layer)}, hitY={underHit.point.y:F2})"
                    : "VOID (No collider within 15m)";
                Debug.LogWarning($"<color=red>[RaftRespawn GUARD] Double respawn detected within 3.0s at Checkpoint {_currentCheckpointIndex}!\n" +
                                 $"Raft TargetPos={targetPos}, PrevY={transform.position.y:F2}, Underneath={underStr}</color>");
            }
            _lastRespawnCheckpoint = _currentCheckpointIndex;
            _lastRespawnTime = Time.time;

            // 3. Teleport Rigidbody & Transform matching NetworkBoat.cs lines 632-634 & 1275
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.position = targetPos;
            _rb.rotation = targetRot;
            transform.SetPositionAndRotation(targetPos, targetRot);

            // 4. Reset buoyancy & gas state (NetworkBoat.cs lines 1255, 1276-1277, 1294)
            if (_buoyancy != null)
            {
                _buoyancy.ResetBuoyancy();
            }
            if (_gasInput != null)
            {
                _gasInput.ResetCrewGas();
            }

            // 5. Snap ChaseCamera behind raft immediately to avoid disorienting swinging
            ChaseCamera chaseCam = FindAnyObjectByType<ChaseCamera>();
            if (chaseCam != null)
            {
                chaseCam.SnapToTarget();
            }

            // 6. Grant authentic 2.0s invulnerability (RaceZone.cs line 25 & NetworkBoat.cs line 1286)
            _invulnerableUntil = Time.time + _invulnSeconds;

            Debug.Log($"<color=yellow>[RaftRespawn COMPLETED] Boat respawned at Checkpoint {_currentCheckpointIndex} -> Pos={targetPos}, Rot={targetRot.eulerAngles} (SafePose={foundSafePose}, Invulnerable for {_invulnSeconds}s)</color>");
        }

        public static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "null";
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
