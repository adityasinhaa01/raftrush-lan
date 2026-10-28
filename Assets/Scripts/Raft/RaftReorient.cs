using FartTogether.CameraRig;
using FartTogether.PlayerInput;
using FartTogether.Track;
using UnityEngine;

namespace FartTogether.Raft
{
    /// <summary>
    /// Reorients the raft in place to face down the track toward the next checkpoint.
    /// Uprights the hull, zeroes angular velocity and linear momentum, and snaps the chase camera.
    /// Preserves water buoyancy, gas fuel, and checkpoint progress.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class RaftReorient : MonoBehaviour
    {
        [Header("Tuning")]
        [Tooltip("Cooldown period in seconds between facing resets to prevent spamming.")]
        [SerializeField] private float _cooldownSeconds = 0.35f;

        [Tooltip("Slight upward lift applied during reorientation to prevent hull clipping ground/water.")]
        [SerializeField] private float _liftOffset = 0.10f;

        [Tooltip("Radius around raft to probe for nearby obstacles/walls before rotating.")]
        [SerializeField] private float _wallCheckRadius = 1.8f;

        [Tooltip("Maximum allowed penetration depth before a turn against a wall is rejected.")]
        [SerializeField] private float _maxPushoutDistance = 0.9f;

        [Header("Input Source")]
        [Tooltip("Optional explicit input source component. If null, searches GameObject for IRaftReorientInput.")]
        [SerializeField] private MonoBehaviour _inputComponent = null;

        [Header("HUD Hint")]
        [Tooltip("If true and no UI text is hooked up, renders a lightweight corner hint.")]
        [SerializeField] private bool _showOnGuiHint = true;

        private Rigidbody _rb;
        private RaftRespawn _respawn;
        private Collider _hullCollider;
        private IRaftReorientInput _inputSource;
        private float _lastReorientTime = -10f;

        public float CooldownRemaining => Mathf.Max(0f, (_lastReorientTime + _cooldownSeconds) - Time.time);
        public bool CanReorient => CooldownRemaining <= 0.001f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _respawn = GetComponent<RaftRespawn>();
            _hullCollider = GetComponentInChildren<Collider>();

            if (_inputComponent is IRaftReorientInput explicitInput)
            {
                _inputSource = explicitInput;
            }
            else
            {
                _inputSource = GetComponent<IRaftReorientInput>();
            }
        }

        private void Start()
        {
            if (_inputSource == null)
            {
                _inputSource = GetComponent<IRaftReorientInput>();
                if (_inputSource == null)
                {
                    _inputSource = gameObject.AddComponent<KeyboardRaftReorientInput>();
                }
            }
        }

        private void Update()
        {
            bool requested = _inputSource != null && _inputSource.IsReorientRequested;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool rawKey = (kb != null && kb.rKey.wasPressedThisFrame) || UnityEngine.Input.GetKeyDown(KeyCode.R);
#else
            bool rawKey = UnityEngine.Input.GetKeyDown(KeyCode.R);
#endif

            if (requested || rawKey)
            {
                TryResetFacing(isFromKey: true, rawKey: rawKey, inputSourceRequested: requested);
            }
        }

        /// <summary>
        /// Attempts to reset the raft facing in place toward the next checkpoint.
        /// </summary>
        public bool TryResetFacing()
        {
            return TryResetFacing(isFromKey: false, rawKey: false, inputSourceRequested: false);
        }

        public bool TryResetFacing(bool isFromKey, bool rawKey, bool inputSourceRequested)
        {
            Vector3 pos = _rb != null ? _rb.position : transform.position;
            float currentYaw = _rb != null ? _rb.rotation.eulerAngles.y : transform.eulerAngles.y;
            int cpIndex = _respawn != null ? _respawn.CurrentCheckpointIndex : -1;
            Vector3 channelTangent = TrackCenterline.GetChannelTangent(pos, cpIndex, out Vector3 closestPoint);
            float distToCenterline = Vector3.Distance(pos, closestPoint);
            float targetYaw = channelTangent.sqrMagnitude > 0.001f 
                ? Quaternion.LookRotation(channelTangent, Vector3.up).eulerAngles.y 
                : currentYaw;

            string keyInfo = isFromKey ? $"Key Received (rawKey={rawKey}, inputSource={inputSourceRequested})" : "Programmatic Request";
            Debug.Log($"[RaftReorient] R-Press Event: {keyInfo}, CanReorient={CanReorient}, CooldownRemaining={CooldownRemaining:F2}s, RaftPos={pos}, NearestClPt={closestPoint}, DistToCenterline={distToCenterline:F2}m, CurrentYaw={currentYaw:F1}°, TargetYaw={targetYaw:F1}°");

            if (!CanReorient)
            {
                Debug.LogWarning($"[RaftReorient] Turn REJECTED: Cooldown active ({CooldownRemaining:F2}s remaining).");
                return false;
            }

            // 1. Calculate target rotation along channel tangent (following curves/bends)
            Quaternion targetRot;
            if (channelTangent.sqrMagnitude > 0.001f)
            {
                targetRot = Quaternion.LookRotation(channelTangent, Vector3.up);
            }
            else if (RaceTrack.Instance != null)
            {
                targetRot = RaceTrack.Instance.DeriveTrackHeading(cpIndex, _rb.position, _rb.rotation);
            }
            else
            {
                // Fallback: horizontal forward
                Vector3 fwd = transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
                targetRot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
            }

            // Guarantee upright orientation (zero pitch and roll)
            Vector3 euler = targetRot.eulerAngles;
            targetRot = Quaternion.Euler(0f, euler.y, 0f);

            // 2. Compute candidate position with small lift to avoid bottom clipping
            Vector3 candidatePos = _rb.position + Vector3.up * _liftOffset;

            // 3. Wall collision and penetration safety check
            if (_hullCollider != null && _hullCollider.enabled)
            {
                int waterLayer = LayerMask.NameToLayer("Water");
                int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
                int layerMask = ~((waterLayer >= 0 ? 1 << waterLayer : 0) | (ignoreRaycastLayer >= 0 ? 1 << ignoreRaycastLayer : 0));

                Collider[] nearby = Physics.OverlapSphere(candidatePos, _wallCheckRadius, layerMask, QueryTriggerInteraction.Ignore);
                Vector3 totalPushout = Vector3.zero;

                foreach (Collider other in nearby)
                {
                    if (other == null || other == _hullCollider) continue;
                    if (other.isTrigger) continue;
                    if (waterLayer >= 0 && other.gameObject.layer == waterLayer) continue;
                    if (other.name == "~SurfaceProbe") continue;
                    if (other.transform.IsChildOf(transform) || other.attachedRigidbody == _rb) continue;

                    if (Physics.ComputePenetration(
                        _hullCollider, candidatePos, targetRot,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 pushDir, out float pushDist))
                    {
                        // Depenetration against obstacles must be strictly horizontal (preserving water level)
                        pushDir.y = 0f;
                        string otherPath = RaftRespawn.GetHierarchyPath(other.transform);
                        if (pushDist > _maxPushoutDistance)
                        {
                            Debug.LogWarning($"[RaftReorient] Turn REJECTED by depenetration: raft hull would penetrate '{other.name}' ({otherPath}) by {pushDist:F2}m > max allowed {_maxPushoutDistance:F2}m. RaftPos={pos}, ClPt={closestPoint}, Dist={distToCenterline:F2}m, CurrentYaw={currentYaw:F1}°, TargetYaw={targetYaw:F1}°.");
                            return false;
                        }

                        if (pushDir.sqrMagnitude > 0.001f)
                        {
                            totalPushout += pushDir.normalized * (pushDist + 0.04f);
                        }
                    }
                }

                totalPushout.y = 0f;
                candidatePos += totalPushout;
            }

            // 4. Apply reorientation and zero out velocities
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.position = candidatePos;
            _rb.rotation = targetRot;
            transform.SetPositionAndRotation(candidatePos, targetRot);

            // 5. Smoothly snap the chase camera directly behind the raft
            if (ChaseCamera.Instance != null)
            {
                ChaseCamera.Instance.SnapToTarget();
            }
            else
            {
                FindAnyObjectByType<ChaseCamera>()?.SnapToTarget();
            }

            _lastReorientTime = Time.time;
            Debug.Log($"<color=green>[RaftReorient] Turn APPLIED! RaftPos={pos} -> {candidatePos}, ClPt={closestPoint}, DistToCl={distToCenterline:F2}m, CurrentYaw={currentYaw:F1}° -> TargetYaw={euler.y:F1}°, Tangent={channelTangent}, Target CP={cpIndex + 1}, Cooldown={_cooldownSeconds}s.</color>");
            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!TrackCenterline.ShowDebugGizmos) return;

            int cpIndex = _respawn != null ? _respawn.CurrentCheckpointIndex : -1;
            Vector3 pos = _rb != null ? _rb.position : transform.position;
            Vector3 tangent = TrackCenterline.GetChannelTangent(pos, cpIndex, out Vector3 closestPt);

            // 1. Orange link line to closest point on track centerline
            Gizmos.color = new Color(1.0f, 0.5f, 0.0f, 0.85f);
            Gizmos.DrawLine(pos, closestPt);
            Gizmos.DrawSphere(closestPt, 0.4f);

            // 2. Bright cyan heading arrow from raft center
            Gizmos.color = Color.cyan;
            Vector3 arrowEnd = pos + tangent * 4.5f;
            Gizmos.DrawLine(pos, arrowEnd);

            // Arrowhead
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
            Vector3 headBase = arrowEnd - tangent * 1.0f;
            Gizmos.DrawLine(arrowEnd, headBase + right * 0.5f);
            Gizmos.DrawLine(arrowEnd, headBase - right * 0.5f);
        }
#endif

        private void OnGUI()
        {
            if (!_showOnGuiHint) return;

            // If RaceHUD is present, it handles the hint cleanly in UI space
            if (FindAnyObjectByType<FartTogether.GameUI.RaceHUD>() != null) return;

            // Fallback: draw a subtle hint in the bottom-left corner
            int width = 160;
            int height = 26;
            int x = 20;
            int y = Screen.height - 40;

            GUI.color = CanReorient ? new Color(1f, 1f, 1f, 0.75f) : new Color(0.7f, 0.7f, 0.7f, 0.4f);
            string label = CanReorient ? "R: Reset facing" : $"R: Reset facing ({CooldownRemaining:F1}s)";
            GUI.Label(new Rect(x, y, width, height), label);
            GUI.color = Color.white;
        }
    }
}
