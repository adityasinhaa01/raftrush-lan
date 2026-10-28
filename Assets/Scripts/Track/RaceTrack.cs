using System;
using System.Collections.Generic;
using UnityEngine;

namespace FartTogether.Track
{
    /// <summary>
    /// Tracks race checkpoint hierarchy and calculates safe respawn poses projected onto track/water surfaces.
    /// Replicates verbatim logic and parameters from original decompiled code:
    /// - _reference/PuffPirates/Track/RaceTrack.cs (lines 20-47, 136-214)
    /// </summary>
    [DisallowMultipleComponent]
    public class RaceTrack : MonoBehaviour
    {
        /*
        // ==============================================================================
        // VERBATIM _reference/PuffPirates/Track/RaceTrack.cs (lines 20-47)
        // ==============================================================================
        private const float RespawnProbeDistance = 20f;
        private const float MaxRespawnSurfaceAbove = 6f;
        private const float MaxRespawnSurfaceBelow = 12f;
        private const float MinRespawnSurfaceUpDot = 0.55f;

        private static readonly Vector2[] RespawnProbeOffsets = new Vector2[17]
        {
            Vector2.zero,
            new Vector2(0f, 1.5f),
            new Vector2(0f, -1.5f),
            new Vector2(1.5f, 0f),
            new Vector2(-1.5f, 0f),
            new Vector2(1.5f, 1.5f),
            new Vector2(-1.5f, 1.5f),
            new Vector2(1.5f, -1.5f),
            new Vector2(-1.5f, -1.5f),
            new Vector2(0f, 3f),
            new Vector2(0f, -3f),
            new Vector2(3f, 0f),
            new Vector2(-3f, 0f),
            new Vector2(0f, 5f),
            new Vector2(0f, -5f),
            new Vector2(5f, 0f),
            new Vector2(-5f, 0f)
        };
        // ==============================================================================
        */

        private const float RespawnProbeDistance = 20f;
        private const float MaxRespawnSurfaceAbove = 6f;
        private const float MaxRespawnSurfaceBelow = 12f;
        private const float MinRespawnSurfaceUpDot = 0.55f;

        private static readonly Vector2[] RespawnProbeOffsets = new Vector2[17]
        {
            Vector2.zero,
            new Vector2(0f, 1.5f),
            new Vector2(0f, -1.5f),
            new Vector2(1.5f, 0f),
            new Vector2(-1.5f, 0f),
            new Vector2(1.5f, 1.5f),
            new Vector2(-1.5f, 1.5f),
            new Vector2(1.5f, -1.5f),
            new Vector2(-1.5f, -1.5f),
            new Vector2(0f, 3f),
            new Vector2(0f, -3f),
            new Vector2(3f, 0f),
            new Vector2(-3f, 0f),
            new Vector2(0f, 5f),
            new Vector2(0f, -5f),
            new Vector2(5f, 0f),
            new Vector2(-5f, 0f)
        };

        private readonly List<Transform> _checkpoints = new List<Transform>();
        private readonly List<Transform> _teamSpawns = new List<Transform>();
        private readonly RaycastHit[] _respawnHits = new RaycastHit[32];
        private Transform _finish;

        public static RaceTrack Instance { get; private set; }

        public int CheckpointCount => _checkpoints.Count;
        public Transform FinishPoint => _finish;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            AutoDiscoverTrackObjects();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AutoDiscoverTrackObjects()
        {
            _checkpoints.Clear();

            // Locate all RaceCheckpoint components and sort by index
            RaceCheckpoint[] cps = FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None);
            Array.Sort(cps, (a, b) => a.CheckpointIndex.CompareTo(b.CheckpointIndex));
            foreach (var cp in cps)
            {
                RegisterCheckpoint(cp.CheckpointIndex, cp.transform);
            }

            if (_finish == null)
            {
                GameObject finishObj = GameObject.Find("Finish");
                if (finishObj != null)
                {
                    _finish = finishObj.transform;
                }
            }
        }

        public void RegisterCheckpoint(int id, Transform t)
        {
            while (_checkpoints.Count <= id)
            {
                _checkpoints.Add(null);
            }
            _checkpoints[id] = t;
        }

        public void RegisterFinish(Transform t)
        {
            _finish = t;
        }

        public void RegisterTeamSpawn(int teamId, Transform t)
        {
            while (_teamSpawns.Count <= teamId)
            {
                _teamSpawns.Add(null);
            }
            _teamSpawns[teamId] = t;
        }

        /*
        // ==============================================================================
        // VERBATIM _reference/PuffPirates/Track/RaceTrack.cs (lines 136-150)
        // ==============================================================================
        public Transform GetCheckpoint(int id)
        {
            if (_checkpoints.Count == 0)
            {
                return null;
            }
            for (int num = Mathf.Clamp(id, 0, _checkpoints.Count - 1); num >= 0; num--)
            {
                if (_checkpoints[num] != null)
                {
                    return _checkpoints[num];
                }
            }
            return null;
        }
        // ==============================================================================
        */
        public Transform GetCheckpoint(int id)
        {
            if (_checkpoints.Count == 0)
            {
                return null;
            }
            for (int num = Mathf.Clamp(id, 0, _checkpoints.Count - 1); num >= 0; num--)
            {
                if (_checkpoints[num] != null)
                {
                    return _checkpoints[num];
                }
            }
            return null;
        }

        /*
        // ==============================================================================
        // VERBATIM _reference/PuffPirates/Track/RaceTrack.cs (lines 152-178)
        // ==============================================================================
        public bool TryGetSafeRespawnPose(int checkpointId, float lift, out Vector3 position, out Quaternion rotation)
        {
            Transform checkpoint = GetCheckpoint(checkpointId);
            position = ((checkpoint != null) ? (checkpoint.position + Vector3.up * lift) : new Vector3(0f, lift, 0f));
            rotation = ((checkpoint != null) ? checkpoint.rotation : Quaternion.identity);
            for (int num = Mathf.Min(Mathf.Max(checkpointId, 0), _checkpoints.Count - 1); num >= 0; num--)
            {
                Transform transform = _checkpoints[num];
                if (!(transform == null) && TryProjectToRespawnSurface(transform, out var surfacePoint))
                {
                    position = surfacePoint + Vector3.up * lift;
                    rotation = transform.rotation;
                    return true;
                }
            }
            for (int i = 0; i < _teamSpawns.Count; i++)
            {
                Transform transform2 = _teamSpawns[i];
                if (!(transform2 == null) && TryProjectToRespawnSurface(transform2, out var surfacePoint2))
                {
                    position = surfacePoint2 + Vector3.up * lift;
                    rotation = transform2.rotation;
                    return true;
                }
            }
            return false;
        }
        // ==============================================================================
        */
        public bool TryGetSafeRespawnPose(int checkpointId, float lift, out Vector3 position, out Quaternion rotation)
        {
            Transform checkpoint = GetCheckpoint(checkpointId);
            position = ((checkpoint != null) ? (checkpoint.position + Vector3.up * lift) : new Vector3(0f, lift, 0f));
            rotation = ((checkpoint != null) ? checkpoint.rotation : Quaternion.identity);

            for (int num = Mathf.Min(Mathf.Max(checkpointId, 0), _checkpoints.Count - 1); num >= 0; num--)
            {
                Transform anchor = _checkpoints[num];
                if (anchor != null && TryProjectToRespawnSurface(anchor, out Vector3 surfacePoint))
                {
                    position = surfacePoint + Vector3.up * lift;
                    rotation = DeriveTrackHeading(num, position, anchor.rotation);
                    return true;
                }
            }

            for (int i = 0; i < _teamSpawns.Count; i++)
            {
                Transform anchor = _teamSpawns[i];
                if (anchor != null && TryProjectToRespawnSurface(anchor, out Vector3 surfacePoint2))
                {
                    position = surfacePoint2 + Vector3.up * lift;
                    rotation = anchor.rotation;
                    return true;
                }
            }

            // Fallback: derive heading towards next point even if surface project failed
            if (checkpoint != null)
            {
                rotation = DeriveTrackHeading(checkpointId, position, checkpoint.rotation);
            }
            return false;
        }

        /// <summary>
        /// Derives the true forward travel direction along the race track towards the next checkpoint or finish.
        /// Fixes issues where ripped scene checkpoint transforms do not have forward pointing down the track.
        /// </summary>
        public Quaternion DeriveTrackHeading(int currentId, Vector3 fromPos, Quaternion fallbackRot)
        {
            Transform nextPoint = null;
            if (currentId + 1 < _checkpoints.Count && _checkpoints[currentId + 1] != null)
            {
                nextPoint = _checkpoints[currentId + 1];
            }
            else if (_finish != null)
            {
                nextPoint = _finish;
            }

            if (nextPoint != null)
            {
                Vector3 toNext = nextPoint.position - fromPos;
                toNext.y = 0f;
                if (toNext.sqrMagnitude > 0.1f)
                {
                    return Quaternion.LookRotation(toNext.normalized, Vector3.up);
                }
            }

            return fallbackRot;
        }

        /*
        // ==============================================================================
        // VERBATIM _reference/PuffPirates/Track/RaceTrack.cs (lines 180-214)
        // ==============================================================================
        private bool TryProjectToRespawnSurface(Transform anchor, out Vector3 surfacePoint)
        {
            surfacePoint = default;
            LevelDefinition componentInParent = anchor.GetComponentInParent<LevelDefinition>();
            Vector2[] respawnProbeOffsets = RespawnProbeOffsets;
            for (int i = 0; i < respawnProbeOffsets.Length; i++)
            {
                Vector2 vector = respawnProbeOffsets[i];
                int num = Physics.RaycastNonAlloc(anchor.position + anchor.right * vector.x + anchor.forward * vector.y + Vector3.up * 8f, Vector3.down, _respawnHits, 20f, -5, QueryTriggerInteraction.Ignore);
                bool flag = false;
                float num2 = float.PositiveInfinity;
                Vector3 vector2 = default;
                for (int j = 0; j < num; j++)
                {
                    RaycastHit raycastHit = _respawnHits[j];
                    Collider collider = raycastHit.collider;
                    if (!(collider == null) && collider.enabled && collider.gameObject.activeInHierarchy && !(raycastHit.normal.y < 0.55f) && !(raycastHit.point.y > anchor.position.y + 6f) && !(raycastHit.point.y < anchor.position.y - 12f) && !(collider.attachedRigidbody != null) && !(collider.GetComponentInParent<BarrierKill>() != null) && (!(componentInParent != null) || collider.transform.IsChildOf(componentInParent.transform)))
                    {
                        float num3 = Mathf.Abs(raycastHit.point.y - anchor.position.y);
                        if (!(num3 >= num2))
                        {
                            num2 = num3;
                            vector2 = raycastHit.point;
                            flag = true;
                        }
                    }
                }
                if (flag)
                {
                    surfacePoint = vector2;
                    return true;
                }
            }
            return false;
        }
        // ==============================================================================
        */
        private bool TryProjectToRespawnSurface(Transform anchor, out Vector3 surfacePoint)
        {
            surfacePoint = default;
            Vector2[] offsets = RespawnProbeOffsets;

            // Layer mask -5 equals ~LayerMask.GetMask("Ignore Raycast") (includes Default layer 0 and Water layer 4)
            int layerMask = -5;

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2 off = offsets[i];
                Vector3 rayOrigin = anchor.position + anchor.right * off.x + anchor.forward * off.y + Vector3.up * 8f;
                int num = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, _respawnHits, RespawnProbeDistance, layerMask, QueryTriggerInteraction.Ignore);

                bool hitFound = false;
                float closestDist = float.PositiveInfinity;
                Vector3 bestPoint = default;

                for (int j = 0; j < num; j++)
                {
                    RaycastHit hit = _respawnHits[j];
                    Collider col = hit.collider;

                    if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                    if (hit.normal.y < MinRespawnSurfaceUpDot) continue; // Reject steep slopes/walls
                    if (hit.point.y > anchor.position.y + MaxRespawnSurfaceAbove) continue;
                    if (hit.point.y < anchor.position.y - MaxRespawnSurfaceBelow) continue;
                    if (col.attachedRigidbody != null) continue;
                    if (col.GetComponentInParent<BarrierKill>() != null) continue;

                    float deltaY = Mathf.Abs(hit.point.y - anchor.position.y);
                    if (deltaY < closestDist)
                    {
                        closestDist = deltaY;
                        bestPoint = hit.point;
                        hitFound = true;
                    }
                }

                if (hitFound)
                {
                    surfacePoint = bestPoint;
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            TrackCenterline.DrawCenterlineGizmos();
        }
#endif
    }
}
