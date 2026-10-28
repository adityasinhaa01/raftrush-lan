using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FartTogether.Obstacles
{
    /// <summary>
    /// Opt-in diagnostic tool for measuring authentic transform rotations for animated obstacles during live Play mode.
    /// Runs ONLY when explicitly invoked via the Editor menu: "FartTogether > Run Obstacle Motion Log".
    /// Records for 3.0s, writes report to docs/obstacle-playmode-measured-log.txt, and NEVER stops Play mode.
    /// </summary>
    public class ObstacleMotionPlayModeLogger : MonoBehaviour
    {
        private class TrackedObstacle
        {
            public string ObstacleName;
            public string YamlLine;
            public string RealClipName;
            public string TargetTransformPath;
            public Transform TargetTransform;
            public ObstacleAnimationController Controller;
            public List<string> LogLines = new List<string>();
        }

        private readonly List<TrackedObstacle> _tracked = new List<TrackedObstacle>();
        private float _startTime;
        private float _nextSampleTime;
        private bool _isRecording = false;
        private bool _finished = false;

        public bool IsRecording => _isRecording;

        /// <summary>
        /// Starts the 3-second recording. Called exclusively by the opt-in menu item.
        /// </summary>
        public void StartLogging()
        {
            if (_isRecording) return;
            InitTrackedObstacles();
            _startTime = Time.time;
            _nextSampleTime = _startTime;
            _isRecording = true;
            _finished = false;
            Debug.Log("[ObstacleMotionPlayModeLogger] Started opt-in 3.0s Play mode measurement (sampling every 0.1s). Play mode will NOT be stopped.");
        }

        private void InitTrackedObstacles()
        {
            _tracked.Clear();

            AddTracked("ft_animated_obstacle_f3", "GM_3D_ObstacleF3",
                "Level_Platform.prefab Line 74013 (_clip: Scene_13.anim, _playbackSpeed: 1, _phaseSeconds: 0)",
                "Scene_13.anim (guid: 99d0fb74b03de7445af81516811c03c8)");

            AddTracked("ft_animated_obstacle_f10", "Armature.001/Bone",
                "Level_Platform.prefab Line 68861 (_clip: Scene_17.anim, _playbackSpeed: 1, _phaseSeconds: 0)",
                "Scene_17.anim (guid: 8d5bc74bbaacca740a703b64faaec807)");

            AddTracked("ft_animated_obstacle_f10_1", "Armature.001/Bone",
                "Level_Platform.prefab Line 72354 (_clip: Scene_17.anim, _playbackSpeed: 1.2, _phaseSeconds: 0)",
                "Scene_17.anim (guid: 8d5bc74bbaacca740a703b64faaec807)");

            AddTracked("ft_animated_obstacle_f13", "GM_3D_ObstacleF13",
                "Level_Platform.prefab Line 73933 (_clip: Scene_15.anim, _playbackSpeed: 1, _phaseSeconds: 0)",
                "Scene_15.anim (guid: 75e589a5808e32340999ba444ac90809)");

            AddTracked("ft_animated_obstacle_f14", "GM_3D_ObstacleF14_P1",
                "Level_Platform.prefab Line 72017 (_clip: Scene.anim, _playbackSpeed: 1, _phaseSeconds: 0)",
                "Scene.anim (guid: 4730c44e43f302e4caab55b919e9be9e)");

            AddTracked("ft_animated_obstacle_f23_1", "GM_3D_ObstacleF23",
                "Level_Platform.prefab Line 76811 (_clip: Scene_1.anim, _playbackSpeed: 1, _phaseSeconds: 0)",
                "Scene_1.anim (guid: cf6c21932a26ea147a387b6f9a4fe4ad)");
        }

        private void AddTracked(string obsName, string childPath, string yamlLine, string clipName)
        {
            GameObject obj = GameObject.Find(obsName);
            if (obj == null)
            {
                Debug.LogWarning($"[ObstacleMotionPlayModeLogger] Could not find GameObject '{obsName}'.");
                return;
            }

            ObstacleAnimationController ctrl = obj.GetComponent<ObstacleAnimationController>();
            Transform targetTr = null;

            if (ctrl != null && ctrl.AnimationRoot != null)
            {
                targetTr = ctrl.AnimationRoot.transform.Find(childPath);
            }
            if (targetTr == null)
            {
                targetTr = obj.transform.Find(childPath);
            }
            if (targetTr == null)
            {
                foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == childPath || t.name == Path.GetFileName(childPath))
                    {
                        targetTr = t;
                        break;
                    }
                }
            }

            TrackedObstacle item = new TrackedObstacle
            {
                ObstacleName = obsName,
                YamlLine = yamlLine,
                RealClipName = clipName,
                TargetTransformPath = childPath,
                TargetTransform = targetTr,
                Controller = ctrl
            };

            _tracked.Add(item);
        }

        private void Update()
        {
            if (!_isRecording || _finished) return;

            float elapsed = Time.time - _startTime;

            if (Time.time >= _nextSampleTime && elapsed <= 3.05f)
            {
                SampleAll(elapsed);
                _nextSampleTime += 0.1f;
            }

            if (elapsed > 3.1f)
            {
                _finished = true;
                _isRecording = false;
                FinalizeReport();

                Debug.Log("[ObstacleMotionPlayModeLogger] 3.0s Play mode measurement completed. Report saved to docs/obstacle-playmode-measured-log.txt. Play mode continues running.");

                // Self-cleanup the temporary runner GameObject. NEVER stop Play mode!
                Destroy(gameObject);
            }
        }

        private void SampleAll(float elapsed)
        {
            foreach (var t in _tracked)
            {
                if (t.TargetTransform != null)
                {
                    Vector3 euler = t.TargetTransform.localEulerAngles;
                    t.LogLines.Add(string.Format("  t={0,4:F1}s | localEulerAngles: ({1,6:F1}, {2,6:F1}, {3,6:F1}) | rotZ: {4,6:F1}°",
                        elapsed, euler.x, euler.y, euler.z, euler.z));
                }
                else
                {
                    t.LogLines.Add(string.Format("  t={0,4:F1}s | TargetTransform NULL", elapsed));
                }
            }
        }

        private void FinalizeReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("REAL MEASURED PLAY-MODE OBSTACLE MOTION LOG (EVERY 0.1s FOR 3.0s)");
            sb.AppendLine("STATUS: REAL MEASURED RUNTIME PLAY-MODE VALUES (OPT-IN RUN)");
            sb.AppendLine("MEASUREMENT METHOD: Live PlayableGraph / AnimationClipPlayable evaluation in Unity Play Mode");
            sb.AppendLine("================================================================================");

            foreach (var t in _tracked)
            {
                sb.AppendLine();
                sb.AppendLine($"--- OBSTACLE: {t.ObstacleName} ---");
                sb.AppendLine($"Target Transform: {t.TargetTransformPath} -> {(t.TargetTransform != null ? "BOUND OK" : "MISSING")}");
                sb.AppendLine($"Prefab YAML Source: {t.YamlLine}");
                sb.AppendLine($"Real Clip Name: {t.RealClipName}");
                if (t.Controller != null && t.Controller.Clip != null)
                {
                    sb.AppendLine($"Clip Length: {t.Controller.Clip.length:F3}s, Speed: {t.Controller.PlaybackSpeed}x, Phase: {t.Controller.PhaseSeconds}s");
                }
                sb.AppendLine("Samples (31 total from t=0.0s to 3.0s):");
                foreach (string line in t.LogLines)
                {
                    sb.AppendLine(line);
                }
            }

            sb.AppendLine("================================================================================");

            string report = sb.ToString();
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "docs");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            string outPath = Path.Combine(outDir, "obstacle-playmode-measured-log.txt");
            File.WriteAllText(outPath, report);

            Debug.Log($"[ObstacleMotionPlayModeLogger] Saved measured Play-mode report to: {outPath}");
        }

#if UNITY_EDITOR
        [MenuItem("FartTogether/Run Obstacle Motion Log", priority = 3)]
        public static void RunObstacleMotionLogMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ObstacleMotionPlayModeLogger] Unity is NOT in Play mode. Please press Play first, then click 'FartTogether > Run Obstacle Motion Log'.");
                EditorUtility.DisplayDialog("Run Obstacle Motion Log",
                    "Unity is not currently in Play mode.\n\nPlease press Play first, then select 'FartTogether > Run Obstacle Motion Log' while the game is running.",
                    "OK");
                return;
            }

            ObstacleMotionPlayModeLogger existing = UnityEngine.Object.FindAnyObjectByType<ObstacleMotionPlayModeLogger>();
            if (existing != null && existing._isRecording)
            {
                Debug.LogWarning("[ObstacleMotionPlayModeLogger] Obstacle motion logging is already in progress.");
                return;
            }

            GameObject runner = new GameObject("~ObstacleMotionPlayModeLogger");
            var logger = runner.AddComponent<ObstacleMotionPlayModeLogger>();
            logger.StartLogging();
        }
#endif
    }
}
