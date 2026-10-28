using System;
using System.Collections.Generic;
using UnityEngine;

namespace FartTogether.Track
{
    /// <summary>
    /// Represents the authored track centerline along the water channel for Level_Platform.
    /// Provides continuous channel tangents, closest path points, and forward progress alignment.
    /// Replaces straight-line checkpoint vectors with true channel curve tangents on bends.
    /// </summary>
    public static class TrackCenterline
    {
        public const string GizmoPrefKey = "FartTogether_ShowTrackCenterlineGizmos";

        /// <summary>
        /// Authored 3D centerline waypoints along Level_Platform from Start (CP0) to Finish.
        /// Derived from ripped track geometry, water bounds, canyon corridors, and checkpoints.
        /// </summary>
        private static readonly Vector3[] CenterlineWaypoints = new Vector3[]
        {
            // --- Segment 0 -> 1: Start to Checkpoint 1 (Gust Canyon 1 Entrance) ---
            // Start straight channel (heading South, yaw = 180°, Z decreasing)
            new Vector3(-15.43f, 1.50f, 91.00f), // CP0 / Start
            new Vector3(-15.43f, 1.50f, 66.65f),
            new Vector3(-15.43f, 1.50f, 51.71f),

            // Right-hand bend before Gust Canyon 1 (South-South-West curve towards CP1)
            new Vector3(-18.00f, 1.50f, 43.00f),
            new Vector3(-22.50f, 1.50f, 33.50f), // Bend channel center between rock banks
            new Vector3(-28.50f, 1.50f, 20.00f),
            new Vector3(-30.23f, 1.00f, -10.80f), // CP1 / Arch

            // --- Segment 1 -> 2: Gust Canyon 1 into Gust Canyon 2 & Checkpoint 2 ---
            // Turn East into Gust Canyon 1 (flows East along Z ≈ -10.66)
            new Vector3(-25.00f, 1.00f, -10.66f),
            new Vector3(-11.03f, 1.00f, -10.66f), // Inside Gust 1 center / hammer
            new Vector3(  0.00f, 1.00f, -10.66f),

            // Turn North-East through Gust Canyon 2 to Checkpoint 2
            new Vector3(  3.60f, 1.00f,  -8.70f),
            new Vector3( 18.57f, 1.10f,   4.34f), // Gust 2 center
            new Vector3( 34.50f, 1.00f,  12.00f),
            new Vector3( 42.03f, 1.00f,  19.63f), // CP2

            // --- Segment 2 -> 3: 90° Turn into Gust Canyon 3 & Checkpoint 3 ---
            // 90 degree turn right into uphill slope & Gust Canyon 3 (heading South, yaw = 180°)
            new Vector3( 48.85f,  3.00f,  10.00f),
            new Vector3( 48.85f,  5.50f,  -5.65f), // Slope entrance
            new Vector3( 48.85f, 10.00f, -42.93f), // Turn into Gust 3 (48.85, 10, -20 to -42)
            new Vector3( 48.85f, 10.00f, -65.84f), // Gust 3 center
            new Vector3( 49.25f,  9.51f, -105.14f), // CP3

            // --- Segment 3 -> 4: Turn East towards Gust Canyon 7 & Checkpoint 4 ---
            new Vector3( 70.28f,  9.88f, -101.69f), // Midpoint between A1BI2 and A1BD2 rock banks
            new Vector3(105.13f,  9.93f,  -91.49f), // Gust 7 center
            new Vector3(122.64f,  9.43f,  -91.50f), // CP4

            // --- Segment 4 -> 5: Curve North-East to Checkpoint 5 ---
            new Vector3(145.24f,  9.40f,  -68.57f), // FT_B5_DoubleSweep (1) channel center
            new Vector3(161.54f,  9.40f,  -52.17f), // GM_3D_ObstacleF10 arch entrance
            new Vector3(174.39f,  9.28f,  -58.92f), // CP5

            // --- Segment 5 -> 6: Curve South-East to Checkpoint 6 ---
            new Vector3(174.33f,  9.40f,  -69.06f), // FT_RotorCross (2)_1 center
            new Vector3(186.25f,  9.40f,  -91.06f), // FT_RotorCross (2)_2 center
            new Vector3(199.47f,  9.47f, -103.11f), // CP6

            // --- Segment 6 -> 7 -> Finish: Straight East to Finish Line ---
            new Vector3(270.00f,  9.35f, -103.02f),
            new Vector3(336.15f,  9.35f, -103.02f), // CP7
            new Vector3(439.21f,  8.50f, -103.12f), // FinishZone
            new Vector3(461.12f,  8.70f, -104.02f), // Finish
        };

        /// <summary>
        /// Checkpoint waypoint indices corresponding to CP 0 through CP 7, and Finish.
        /// </summary>
        private static readonly int[] CheckpointWaypointIndices = new int[]
        {
            0,  // CP 0: Checkpoint_Start
            6,  // CP 1: Checkpoint_Start (1)
            13, // CP 2: Checkpoint_Start (1)_1
            18, // CP 3: Checkpoint_Start (2)
            21, // CP 4: Checkpoint_Start (3)
            24, // CP 5: Checkpoint_Start (5)
            27, // CP 6: Checkpoint_Start (5)_1
            29, // CP 7: Checkpoint_Start (5)_2
            31  // Finish
        };

        private static readonly Vector2[] CachedSegmentTangents;
        private static readonly Vector2[] CachedVertexTangents;

        static TrackCenterline()
        {
            int n = CenterlineWaypoints.Length;
            CachedSegmentTangents = new Vector2[n - 1];
            CachedVertexTangents = new Vector2[n];

            for (int i = 0; i < n - 1; i++)
            {
                Vector2 seg = new Vector2(
                    CenterlineWaypoints[i + 1].x - CenterlineWaypoints[i].x,
                    CenterlineWaypoints[i + 1].z - CenterlineWaypoints[i].z
                );
                CachedSegmentTangents[i] = seg.sqrMagnitude > 1e-8f ? seg.normalized : Vector2.down;
            }

            CachedVertexTangents[0] = CachedSegmentTangents[0];
            for (int i = 1; i < n - 1; i++)
            {
                Vector2 sum = CachedSegmentTangents[i - 1] + CachedSegmentTangents[i];
                CachedVertexTangents[i] = sum.sqrMagnitude > 1e-8f ? sum.normalized : CachedSegmentTangents[i];
            }
            CachedVertexTangents[n - 1] = CachedSegmentTangents[n - 2];
        }

        public static IReadOnlyList<Vector3> Waypoints => CenterlineWaypoints;

        public static bool ShowDebugGizmos
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(GizmoPrefKey, false);
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Calculates the horizontal channel tangent vector at the given world position.
        /// Projects position onto the closest centerline segment and smoothly interpolates the curve tangent.
        /// Enforces progress sign alignment so the tangent always points toward the next checkpoint along the track.
        /// </summary>
        /// <param name="worldPosition">Raft's current world position.</param>
        /// <param name="currentCheckpointIndex">Current checkpoint index of the raft (-1 if unassigned).</param>
        /// <param name="closestPoint">Outputs the closest 3D point on the track centerline.</param>
        /// <returns>Normalized horizontal tangent vector (Y=0) pointing forward down the channel.</returns>
        public static Vector3 GetChannelTangent(Vector3 worldPosition, int currentCheckpointIndex, out Vector3 closestPoint)
        {
            Vector2 p = new Vector2(worldPosition.x, worldPosition.z);
            int n = CenterlineWaypoints.Length;

            float minDistanceSq = float.MaxValue;
            int bestSegment = 0;
            float bestT = 0f;
            Vector3 bestClosest3D = CenterlineWaypoints[0];

            for (int i = 0; i < n - 1; i++)
            {
                Vector3 a3 = CenterlineWaypoints[i];
                Vector3 b3 = CenterlineWaypoints[i + 1];
                Vector2 a = new Vector2(a3.x, a3.z);
                Vector2 b = new Vector2(b3.x, b3.z);
                Vector2 ab = b - a;
                float segLenSq = ab.sqrMagnitude;

                if (segLenSq < 1e-8f) continue;

                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / segLenSq);
                Vector2 proj = a + t * ab;
                float distSq = (p - proj).sqrMagnitude;

                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    bestSegment = i;
                    bestT = t;
                    bestClosest3D = Vector3.Lerp(a3, b3, t);
                }
            }

            closestPoint = bestClosest3D;

            // 1. Calculate continuous smooth tangent along the curve
            Vector2 v0 = CachedVertexTangents[bestSegment];
            Vector2 v1 = CachedVertexTangents[bestSegment + 1];
            Vector2 blended2D = Vector2.Lerp(v0, v1, bestT);
            if (blended2D.sqrMagnitude < 1e-6f)
            {
                blended2D = CachedSegmentTangents[bestSegment];
            }
            blended2D.Normalize();

            Vector3 tangent3D = new Vector3(blended2D.x, 0f, blended2D.y);

            // 2. Sign enforcement: ensure positive alignment with progress towards next checkpoint
            // Find the target waypoint index for the next checkpoint
            int targetWaypointIndex = CenterlineWaypoints.Length - 1;
            if (currentCheckpointIndex >= 0 && currentCheckpointIndex + 1 < CheckpointWaypointIndices.Length)
            {
                targetWaypointIndex = CheckpointWaypointIndices[currentCheckpointIndex + 1];
            }

            // Path direction along polyline: vector from current segment towards target waypoint
            Vector3 pathDir;
            if (targetWaypointIndex > bestSegment)
            {
                // Moving forward along polyline
                Vector3 nextWp = CenterlineWaypoints[Mathf.Min(bestSegment + 1, targetWaypointIndex)];
                pathDir = nextWp - bestClosest3D;
            }
            else
            {
                // Near or past checkpoint: use the segment direction
                pathDir = CenterlineWaypoints[Mathf.Min(bestSegment + 1, n - 1)] - CenterlineWaypoints[bestSegment];
            }
            pathDir.y = 0f;

            if (pathDir.sqrMagnitude > 1e-6f)
            {
                pathDir.Normalize();
                if (Vector3.Dot(tangent3D, pathDir) < 0f)
                {
                    tangent3D = -tangent3D;
                }
            }

            return tangent3D.normalized;
        }

        /// <summary>
        /// Calculates the upright rotation (zero pitch and roll) matching the channel tangent.
        /// </summary>
        public static Quaternion GetChannelRotation(Vector3 worldPosition, int currentCheckpointIndex, out Vector3 closestPoint)
        {
            Vector3 tangent = GetChannelTangent(worldPosition, currentCheckpointIndex, out closestPoint);
            if (tangent.sqrMagnitude < 1e-4f)
            {
                return Quaternion.identity;
            }
            return Quaternion.LookRotation(tangent, Vector3.up);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draws the full track centerline and waypoint nodes in the Scene view.
        /// Opt-in toggle controlled via FartTogether > Toggle Track Centerline Gizmos.
        /// </summary>
        public static void DrawCenterlineGizmos()
        {
            if (!ShowDebugGizmos) return;

            int n = CenterlineWaypoints.Length;
            if (n < 2) return;

            // Draw centerline polyline
            Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.95f); // Bright cyan
            for (int i = 0; i < n - 1; i++)
            {
                Gizmos.DrawLine(CenterlineWaypoints[i], CenterlineWaypoints[i + 1]);
            }

            // Draw waypoints
            for (int i = 0; i < n; i++)
            {
                bool isCheckpoint = false;
                for (int c = 0; c < CheckpointWaypointIndices.Length; c++)
                {
                    if (CheckpointWaypointIndices[c] == i)
                    {
                        isCheckpoint = true;
                        break;
                    }
                }

                if (isCheckpoint)
                {
                    Gizmos.color = new Color(0.2f, 1.0f, 0.3f, 1.0f); // Bright green for checkpoints
                    Gizmos.DrawSphere(CenterlineWaypoints[i], 0.7f);
                }
                else
                {
                    Gizmos.color = new Color(1.0f, 0.85f, 0.2f, 0.85f); // Gold for intermediate bends
                    Gizmos.DrawSphere(CenterlineWaypoints[i], 0.35f);
                }

                // Draw tangent vectors along the centerline
                Vector3 tan = new Vector3(CachedVertexTangents[i].x, 0f, CachedVertexTangents[i].y) * 2.0f;
                Gizmos.color = new Color(1.0f, 0.4f, 0.1f, 0.75f);
                Gizmos.DrawLine(CenterlineWaypoints[i], CenterlineWaypoints[i] + tan);
            }
        }
#endif
    }
}
