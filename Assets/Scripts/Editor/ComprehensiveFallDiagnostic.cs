using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FartTogether.Raft;
using FartTogether.Track;
using FartTogether.Water;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FartTogether.Editor
{
    public static class ComprehensiveFallDiagnostic
    {
        public static void RunDiagnostic()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != "Assets/Scenes/Race_M2.unity")
            {
                currentScene = EditorSceneManager.OpenScene("Assets/Scenes/Race_M2.unity", OpenSceneMode.Single);
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("COMPREHENSIVE FALL DIAGNOSTIC REPORT: GUST_CANYON_1 & CHECKPOINT 1");
            sb.AppendLine("================================================================================");

            // 1. Inspect floor & water colliders under Checkpoint 1 (-30.23, -10.80), Gust_Canyon_1 (-11.03, -10.66), and Gust_Canyon_2 (18.57, 4.34)
            sb.AppendLine("\n--- SECTION 1: FLOOR & WATER COLLIDERS COMPARISON ---");
            InspectColumn(sb, "Checkpoint 1 Respawn", new Vector3(-30.23f, 5.0f, -10.80f));
            InspectColumn(sb, "Between CP1 & Canyon 1 (X=-20)", new Vector3(-20.0f, 5.0f, -10.66f));
            InspectColumn(sb, "Gust_Canyon_1 Center", new Vector3(-11.03f, 5.0f, -10.66f));
            InspectColumn(sb, "Gust_Canyon_1 Entrance (X=-18.5)", new Vector3(-18.5f, 5.0f, -10.66f));
            InspectColumn(sb, "Gust_Canyon_1 Exit (X=-3.5)", new Vector3(-3.5f, 5.0f, -10.66f));
            InspectColumn(sb, "Gust_Canyon_2 Center", new Vector3(18.57f, 5.0f, 4.34f));

            // 2. Inspect Plane under Gust_Canyon_1 and Gust_Canyon_2
            sb.AppendLine("\n--- SECTION 2: PLANE UNDER GUST_CANYON_1 vs GUST_CANYON_2 ---");
            InspectPlane(sb, "Gust_Canyon_1");
            InspectPlane(sb, "Gust_Canyon_2");

            // 3. Inspect ~SurfaceProbe and channel wall colliders
            sb.AppendLine("\n--- SECTION 3: SURFACE PROBE & CHANNEL WALL COLLIDERS ---");
            InspectWaterChannelAndWalls(sb);

            // 4. Live Physics Simulation with Gas Input & Flow Forces
            sb.AppendLine("\n--- SECTION 4: LIVE 0.1s PHYSICS TRACE (FROM 2s BEFORE GUST_CANYON_1 TO FALL) ---");
            SimulateFallTrace(sb);

            string outPath = Path.Combine(Directory.GetCurrentDirectory(), "docs/comprehensive-fall-diagnostic.txt");
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[ComprehensiveFallDiagnostic] Saved report to {outPath}");
        }

        private static void InspectColumn(StringBuilder sb, string label, Vector3 origin)
        {
            sb.AppendLine($"\n[Raycast Column: {label} at (X={origin.x:F2}, Z={origin.z:F2}) from Y={origin.y:F2} down 30m]");
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 30f, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            if (hits.Length == 0)
            {
                sb.AppendLine("  NO COLLIDERS HIT!");
            }
            foreach (var h in hits)
            {
                sb.AppendLine($"  - Hit: '{h.collider.name}' (Layer={h.collider.gameObject.layer}: {LayerMask.LayerToName(h.collider.gameObject.layer)}), " +
                              $"isTrigger={h.collider.isTrigger}, Type={h.collider.GetType().Name}, HitY={h.point.y:F3}, Normal={h.normal}");
            }
        }

        private static void InspectPlane(StringBuilder sb, string canyonName)
        {
            GameObject canyon = GameObject.Find(canyonName);
            if (canyon == null)
            {
                sb.AppendLine($"[InspectPlane] {canyonName}: NOT FOUND");
                return;
            }
            sb.AppendLine($"[InspectPlane] {canyonName}: WorldPos={canyon.transform.position}, Rot={canyon.transform.eulerAngles}");
            Transform plane = canyon.transform.Find("Plane");
            if (plane == null)
            {
                sb.AppendLine("  Child 'Plane': NOT FOUND");
                return;
            }
            MeshFilter mf = plane.GetComponent<MeshFilter>();
            MeshRenderer mr = plane.GetComponent<MeshRenderer>();
            Collider col = plane.GetComponent<Collider>();
            sb.AppendLine($"  Child 'Plane': Active={plane.gameObject.activeSelf}, Mesh={mf?.sharedMesh?.name}, " +
                          $"RendererEnabled={mr?.enabled}, Material={mr?.sharedMaterial?.name}, Collider={col?.GetType().Name ?? "NONE"}");
        }

        private static void InspectWaterChannelAndWalls(StringBuilder sb)
        {
            GameObject probe = GameObject.Find("~SurfaceProbe");
            if (probe != null)
            {
                MeshCollider mc = probe.GetComponent<MeshCollider>();
                sb.AppendLine($"[~SurfaceProbe] Active={probe.activeInHierarchy}, Layer={probe.layer}, " +
                              $"MeshCollider={mc != null}, Convex={mc?.convex}, isTrigger={mc?.isTrigger}, Mesh={mc?.sharedMesh?.name}");
                if (mc != null && mc.sharedMesh != null)
                {
                    sb.AppendLine($"  Bounds: Center={mc.sharedMesh.bounds.center}, Extents={mc.sharedMesh.bounds.extents}");
                }
            }

            // Find all wall colliders around Z = -10.66
            var allCols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            sb.AppendLine("\nColliders near Gust_Canyon_1 bounding box (X in [-20, 0], Z in [-16, -5]):");
            foreach (var c in allCols)
            {
                if (c is MeshCollider mc && mc.sharedMesh != null)
                {
                    Bounds b = mc.bounds;
                    if (b.min.x < 0f && b.max.x > -20f && b.min.z < -5f && b.max.z > -16f)
                    {
                        sb.AppendLine($"  - Wall/Track: '{c.name}' in '{c.transform.parent?.name}', Convex={mc.convex}, isTrigger={mc.isTrigger}, " +
                                      $"Bounds: min={b.min}, max={b.max}");
                    }
                }
            }
        }

        private static void SimulateFallTrace(StringBuilder sb)
        {
            GameObject raft = GameObject.Find("Raft");
            if (raft == null)
            {
                sb.AppendLine("ERROR: Raft not found!");
                return;
            }

            Rigidbody rb = raft.GetComponent<Rigidbody>();
            RaftBuoyancy buoyancy = raft.GetComponent<RaftBuoyancy>();
            WaterFlowZone canyon1Flow = GameObject.Find("Gust_Canyon_1")?.GetComponent<WaterFlowZone>();

            // Setup simulation mode
            SimulationMode prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            try
            {
                // Position raft 2s before reaching Gust_Canyon_1
                // Gust_Canyon_1 box is X in [-18.53, -3.53]. At 7 m/s, 2s before is X = -32.5 (near Checkpoint 1 at -30.23).
                raft.transform.position = new Vector3(-30.23f, 0.85f, -10.80f);
                raft.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Traveling East (+X) along track
                rb.position = raft.transform.position;
                rb.rotation = raft.transform.rotation;
                rb.linearVelocity = new Vector3(5f, 0f, 0f); // 5 m/s forward
                rb.angularVelocity = Vector3.zero;

                float dt = 0.02f;
                float logTimer = 0f;
                float simTime = 0f;

                // Reflection or invoke methods
                var applyWaterRideMethod = typeof(RaftBuoyancy).GetMethod("ApplyWaterRide", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var rbField = typeof(RaftBuoyancy).GetField("_rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                rbField?.SetValue(buoyancy, rb);

                sb.AppendLine($"Start Pose: pos={raft.transform.position}, vel={rb.linearVelocity}");

                for (int step = 0; step < 400; step++) // Up to 8 seconds
                {
                    // 1. Gas force (full forward throttle 900N matching single-player W key)
                    rb.AddForce(new Vector3(900f, 0f, 0f), ForceMode.Force);

                    // 2. RaftBuoyancy FixedUpdate
                    applyWaterRideMethod?.Invoke(buoyancy, null);

                    // 3. WaterFlowZone force if inside Gust_Canyon_1
                    if (canyon1Flow != null)
                    {
                        Collider flowCol = canyon1Flow.GetComponent<Collider>();
                        if (flowCol != null && flowCol.bounds.Contains(rb.position))
                        {
                            rb.AddForce(canyon1Flow.WorldFlowDirection * canyon1Flow.FlowForce, ForceMode.Force);
                        }
                    }

                    // 4. Physics step
                    Physics.Simulate(dt);
                    simTime += dt;
                    logTimer += dt;

                    if (logTimer >= 0.1f)
                    {
                        logTimer = 0f;
                        Vector3 pos = raft.transform.position;
                        Vector3 vel = rb.linearVelocity;

                        // Check all colliders touched via OverlapSphere
                        Collider[] touched = Physics.OverlapSphere(pos, 1.2f, ~0, QueryTriggerInteraction.Ignore);
                        var touchedNames = new List<string>();
                        foreach (var c in touched)
                        {
                            if (c.transform != raft.transform && !c.transform.IsChildOf(raft.transform))
                            {
                                touchedNames.Add(c.name);
                            }
                        }

                        // Check buoyancy raycasts (8 sample points around perimeter)
                        float probeAbove = 0.45f;
                        float maxDist = probeAbove + 0.3f + 0.85f; // 1.6m
                        int hitsCount = 0;
                        var rayDetails = new List<string>();

                        for (int p = 0; p < 8; p++)
                        {
                            float angle = (float)p / 8f * Mathf.PI * 2f;
                            Vector3 sampleWorld = raft.transform.TransformPoint(new Vector3(Mathf.Cos(angle) * 1.0f, -0.4f, Mathf.Sin(angle) * 1.0f));
                            Vector3 rayOrig = sampleWorld + Vector3.up * probeAbove;

                            if (Physics.Raycast(rayOrig, Vector3.down, out RaycastHit bhit, maxDist, ~0, QueryTriggerInteraction.Ignore))
                            {
                                hitsCount++;
                                rayDetails.Add($"P{p}:{bhit.collider.name}(y={bhit.point.y:F2})");
                            }
                            else
                            {
                                rayDetails.Add($"P{p}:MISS");
                            }
                        }

                        string touchStr = touchedNames.Count > 0 ? string.Join(",", touchedNames) : "None";
                        string rayStr = string.Join(" ", rayDetails);

                        sb.AppendLine($"t={simTime:F2}s | pos=({pos.x:F2}, {pos.y:F2}, {pos.z:F2}) | vel=({vel.x:F2}, {vel.y:F2}, {vel.z:F2}) | " +
                                      $"touching=[{touchStr}] | BuoyancyHits={hitsCount}/8 | Rays=[{rayStr}]");

                        if (pos.y < -15f)
                        {
                            sb.AppendLine($"\n--> RAFT DROPPED BELOW Y = -15m at t={simTime:F2}s (Pos: {pos}, Vel: {vel})!");
                            break;
                        }
                    }
                }
            }
            finally
            {
                Physics.simulationMode = prevMode;
            }
        }
    }
}
