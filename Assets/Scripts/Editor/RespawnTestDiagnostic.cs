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
    public static class RespawnTestDiagnostic
    {


        [MenuItem("FartTogether/Run Respawn Fall Diagnostic", priority = 6)]
        public static void RunDiagnosticMenu()
        {
            RunDiagnostic();
        }

        public static void RunDiagnostic()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != "Assets/Scenes/Race_M2.unity")
            {
                currentScene = EditorSceneManager.OpenScene("Assets/Scenes/Race_M2.unity", OpenSceneMode.Single);
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("RESPAWN FALL TRACE AT CHECKPOINT 1 (-30.23, 1.51, -10.80)");
            sb.AppendLine("================================================================================");

            GameObject raft = GameObject.Find("Raft");
            Rigidbody rb = raft.GetComponent<Rigidbody>();
            RaftBuoyancy buoyancy = raft.GetComponent<RaftBuoyancy>();
            RaftRespawn respawn = raft.GetComponent<RaftRespawn>();

            // Setup _rb on buoyancy
            var rbField = typeof(RaftBuoyancy).GetField("_rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rbField?.SetValue(buoyancy, rb);

            var applyWaterRideMethod = typeof(RaftBuoyancy).GetMethod("ApplyWaterRide", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Find Checkpoint 1
            Transform cp1 = GameObject.Find("Checkpoint_Start (1)")?.transform;
            sb.AppendLine($"CP1 transform found: {cp1 != null}");
            if (cp1 != null)
            {
                sb.AppendLine($"CP1 world pos: {cp1.position}, rot: {cp1.rotation.eulerAngles}");
            }

            // Verify RaceTrack
            FartTogether.Track.RaceTrack raceTrack = raft.scene.GetRootGameObjects()
                [0].GetComponentInChildren<FartTogether.Track.RaceTrack>(true);
            sb.AppendLine($"RaceTrack found: {raceTrack != null}");
            if (raceTrack != null)
            {
                raceTrack.AutoDiscoverTrackObjects();
                sb.AppendLine($"RaceTrack registered checkpoints: {raceTrack.CheckpointCount}");
            }

            // Verify authentic ft_animated_obstacle_f10 in Gust_Canyon_1
            GameObject f10 = GameObject.Find("ft_animated_obstacle_f10");
            sb.AppendLine("\n--- FT_ANIMATED_OBSTACLE_F10 VERIFICATION ---");
            sb.AppendLine($"ft_animated_obstacle_f10 found: {f10 != null}");
            if (f10 != null)
            {
                sb.AppendLine($"  Pos: {f10.transform.position}, Rot: {f10.transform.rotation.eulerAngles}");
                var animCtrl = f10.GetComponent<FartTogether.Obstacles.ObstacleAnimationController>();
                sb.AppendLine($"  ObstacleAnimationController: {animCtrl != null}");
                var smr = f10.GetComponentInChildren<SkinnedMeshRenderer>(true);
                sb.AppendLine($"  SkinnedMeshRenderer: {smr != null}, mat: {smr?.sharedMaterial?.name}, updateWhenOffscreen: {smr?.updateWhenOffscreen}");
            }

            SimulationMode prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            try
            {
                // Register Checkpoint 1 and trigger authentic Respawn()
                if (cp1 != null)
                {
                    respawn.RegisterCheckpoint(1, cp1);
                }
                respawn.Respawn();

                sb.AppendLine("\n--- RESPAWN POSE VERIFICATION ---");
                sb.AppendLine($"Initial Respawn State: pos={raft.transform.position}, rot={raft.transform.rotation.eulerAngles}, vel={rb.linearVelocity}");

                float dt = 0.02f;
                float logTimer = 0f;
                float simTime = 0f;

                for (int step = 0; step < 150; step++) // 3 seconds
                {
                    applyWaterRideMethod?.Invoke(buoyancy, null);

                    Physics.Simulate(dt);
                    simTime += dt;
                    logTimer += dt;

                    if (logTimer >= 0.1f)
                    {
                        logTimer = 0f;
                        Vector3 pos = raft.transform.position;
                        Vector3 vel = rb.linearVelocity;

                        // Raycast check down from hull
                        RaycastHit[] hits = Physics.RaycastAll(pos + Vector3.up * 0.45f, Vector3.down, 5f, ~0, QueryTriggerInteraction.Collide);
                        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                        var hitNames = new List<string>();
                        foreach (var h in hits)
                        {
                            hitNames.Add($"{h.collider.name}(y={h.point.y:F2})");
                        }
                        string hitStr = hitNames.Count > 0 ? string.Join(", ", hitNames) : "NONE";

                        sb.AppendLine($"t={simTime:F2}s | pos=({pos.x:F2}, {pos.y:F2}, {pos.z:F2}) | vel=({vel.x:F2}, {vel.y:F2}, {vel.z:F2}) | hits=[{hitStr}]");

                        if (pos.y < -15f)
                        {
                            sb.AppendLine($"\n--> RAFT FELL BELOW Y = -15m at t={simTime:F2}s!");
                            break;
                        }
                    }
                }
            }
            finally
            {
                Physics.simulationMode = prevMode;
            }

            string outPath = Path.Combine(Directory.GetCurrentDirectory(), "docs/respawn-fall-trace.txt");
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[RespawnTestDiagnostic] Saved report to {outPath}");
        }
    }
}
