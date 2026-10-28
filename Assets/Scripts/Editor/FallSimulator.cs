using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FartTogether.Raft;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FartTogether.Editor
{
    public static class FallSimulatorRunner
    {
        [MenuItem("FartTogether/Run Live Fall Trace in Play Mode", priority = 6)]
        public static void RunSimulation()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != "Assets/Scenes/Race_M2.unity")
            {
                currentScene = EditorSceneManager.OpenScene("Assets/Scenes/Race_M2.unity", OpenSceneMode.Single);
            }

            GameObject raft = GameObject.Find("Raft");
            if (raft == null)
            {
                Debug.LogError("Raft not found!");
                return;
            }

            Rigidbody rb = raft.GetComponent<Rigidbody>();
            RaftBuoyancy buoyancy = raft.GetComponent<RaftBuoyancy>();
            RaftRespawn respawn = raft.GetComponent<RaftRespawn>();

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("LIVE PHYSX PLAY-MODE FALL TRACE LOG: GUST_CANYON_1 & CHECKPOINT 1 (EVERY 0.1s)");
            sb.AppendLine("================================================================================");

            // Enable automatic simulation mode temporarily or simulate manually
            SimulationMode prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            // Scenario 1: Raft approaches Gust_Canyon_1 moving East (+X) from X = -20
            raft.transform.position = new Vector3(-20f, 0.85f, -10.66f);
            raft.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Facing +X
            rb.linearVelocity = new Vector3(6f, 0f, 0f);
            rb.angularVelocity = Vector3.zero;

            sb.AppendLine("--- SCENARIO 1: ENTERING GUST_CANYON_1 FROM X = -20 ---");

            float dt = 0.02f;
            float logTimer = 0f;
            float totalTime = 0f;

            for (int step = 0; step < 250; step++) // 5 seconds
            {
                // Call FixedUpdate on Raft components
                // RaftBuoyancy applies water forces in FixedUpdate
                // We use reflection or call Physics.Simulate
                Physics.Simulate(dt);
                totalTime += dt;
                logTimer += dt;

                if (logTimer >= 0.1f)
                {
                    logTimer = 0f;
                    Vector3 pos = raft.transform.position;
                    Vector3 vel = rb.linearVelocity;

                    // Raycast check down
                    RaycastHit[] hits = Physics.RaycastAll(pos + Vector3.up * 0.45f, Vector3.down, 2.5f, ~0, QueryTriggerInteraction.Collide);
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    var hitNames = new List<string>();
                    foreach (var h in hits)
                    {
                        hitNames.Add($"{h.collider.name}(y={h.point.y:F2})");
                    }
                    string hitStr = hitNames.Count > 0 ? string.Join(", ", hitNames) : "MISS";

                    sb.AppendLine($"t={totalTime:F2}s | pos=({pos.x:F2}, {pos.y:F2}, {pos.z:F2}) | vel=({vel.x:F2}, {vel.y:F2}, {vel.z:F2}) | hits=[{hitStr}]");

                    if (pos.y < -15f)
                    {
                        sb.AppendLine($"--> RAFT FELL BELOW -15m at t={totalTime:F2}s! Pos: {pos}");
                        break;
                    }
                }
            }

            // Scenario 2: Raft respawned at Checkpoint 1 (-30.23, 1.51, -10.80)
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("--- SCENARIO 2: RESPAWN AT CHECKPOINT 1 (-30.23, 1.51, -10.80) ---");
            sb.AppendLine("--------------------------------------------------------------------------------");

            raft.transform.position = new Vector3(-30.23f, 1.51f, -10.80f);
            raft.transform.rotation = Quaternion.identity;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            totalTime = 0f;
            logTimer = 0f;

            for (int step = 0; step < 150; step++) // 3 seconds
            {
                Physics.Simulate(dt);
                totalTime += dt;
                logTimer += dt;

                if (logTimer >= 0.1f)
                {
                    logTimer = 0f;
                    Vector3 pos = raft.transform.position;
                    Vector3 vel = rb.linearVelocity;

                    RaycastHit[] hits = Physics.RaycastAll(pos + Vector3.up * 0.45f, Vector3.down, 2.5f, ~0, QueryTriggerInteraction.Collide);
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    var hitNames = new List<string>();
                    foreach (var h in hits)
                    {
                        hitNames.Add($"{h.collider.name}(y={h.point.y:F2})");
                    }
                    string hitStr = hitNames.Count > 0 ? string.Join(", ", hitNames) : "MISS";

                    sb.AppendLine($"t={totalTime:F2}s | pos=({pos.x:F2}, {pos.y:F2}, {pos.z:F2}) | vel=({vel.x:F2}, {vel.y:F2}, {vel.z:F2}) | hits=[{hitStr}]");

                    if (pos.y < -15f)
                    {
                        sb.AppendLine($"--> RAFT FELL BELOW -15m at t={totalTime:F2}s! Pos: {pos}");
                        break;
                    }
                }
            }

            Physics.simulationMode = prevMode;

            string outPath = Path.Combine(Directory.GetCurrentDirectory(), "docs/gust-fall-trace-playmode.txt");
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[FallSimulator] Log successfully written to {outPath}\n{sb}");
        }
    }
}
