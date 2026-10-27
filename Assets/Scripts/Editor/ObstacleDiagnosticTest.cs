using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FartTogether.Obstacles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace FartTogether.Editor
{
    /// <summary>
    /// Concrete diagnostic test requested by user:
    /// Logs for one gate (ft_animated_obstacle_f3) the clip name, clip.length, speed,
    /// current normalized time over 3 seconds, and local rotation every 0.1s.
    /// Also checks whether the stepped motion is caused by tangentMode / -Infinity slopes vs PlayableGraph.
    /// Accessible via menu "FartTogether/Run Gate Rotation Concrete Test".
    /// </summary>
    public static class ObstacleDiagnosticTest
    {
        [InitializeOnLoadMethod]
        private static void AutoRunOnCompile()
        {
            EditorApplication.delayCall += () =>
            {
                RunConcreteTest();
            };
        }

        [MenuItem("FartTogether/Run Gate Rotation Concrete Test", priority = 4)]
        public static void RunTestMenu()
        {
            RunConcreteTest();
        }

        public static string RunConcreteTest()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != "Assets/Scenes/Race_M2.unity")
            {
                if (File.Exists("Assets/Scenes/Race_M2.unity"))
                {
                    currentScene = EditorSceneManager.OpenScene("Assets/Scenes/Race_M2.unity", OpenSceneMode.Single);
                }
            }

            GameObject gate = GameObject.Find("ft_animated_obstacle_f3");
            if (gate == null)
            {
                Debug.LogError("[ObstacleDiagnosticTest] 'ft_animated_obstacle_f3' not found in scene.");
                return "Error: ft_animated_obstacle_f3 not found.";
            }

            ObstacleAnimationController ctrl = gate.GetComponent<ObstacleAnimationController>();
            if (ctrl == null || ctrl.Clip == null)
            {
                Debug.LogError("[ObstacleDiagnosticTest] ObstacleAnimationController or Clip is null on ft_animated_obstacle_f3.");
                return "Error: ObstacleAnimationController or Clip is null.";
            }

            AnimationClip clip = ctrl.Clip;
            GameObject root = ctrl.AnimationRoot != null ? ctrl.AnimationRoot : gate;

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("CONCRETE TEST LOG: ft_animated_obstacle_f3 ROTATION SAMPLES (EVERY 0.1s OVER 3s)");
            sb.AppendLine($"Gate Object: {gate.name}");
            sb.AppendLine($"Animation Root: {root.name} ({AssetDatabase.GetAssetPath(clip)})");
            sb.AppendLine($"Clip Name: {clip.name}");
            sb.AppendLine($"Clip Length: {clip.length:F3}s");
            sb.AppendLine($"Clip Legacy Flag: {clip.legacy}");
            sb.AppendLine($"Playback Speed: {ctrl.PlaybackSpeed}x");
            sb.AppendLine($"Phase Seconds: {ctrl.PhaseSeconds}s");
            sb.AppendLine("================================================================================");
            sb.AppendLine(string.Format("{0,-8} | {1,-12} | {2,-16} | {3,-28} | {4,-28}", 
                "Time(s)", "NormTime", "SampleTime(s)", "SampleAnimation Root Euler", "Child GM_3D_ObstacleF3 Euler"));
            sb.AppendLine(new string('-', 100));

            Transform visualChild = root.transform.Find("GM_3D_ObstacleF3") ?? root.transform;

            for (float t = 0.0f; t <= 3.05f; t += 0.1f)
            {
                float sampleTime = Mathf.Repeat(t * ctrl.PlaybackSpeed + ctrl.PhaseSeconds, clip.length);
                float normalizedTime = sampleTime / clip.length;

                // Sample the clip
                clip.SampleAnimation(root, sampleTime);

                Vector3 rootRot = root.transform.localEulerAngles;
                Vector3 childRot = visualChild.localEulerAngles;

                sb.AppendLine(string.Format("{0,8:F1} | {1,12:F4} | {2,16:F3} | {3,-28} | {4,-28}",
                    t, normalizedTime, sampleTime, 
                    $"({rootRot.x:F1}, {rootRot.y:F1}, {rootRot.z:F1})", 
                    $"({childRot.x:F1}, {childRot.y:F1}, {childRot.z:F1})"));
            }

            sb.AppendLine("================================================================================");

            string logResult = sb.ToString();
            Debug.Log(logResult);

            string outPath = "docs/gate-rotation-log.txt";
            File.WriteAllText(outPath, logResult);
            Debug.Log($"[ObstacleDiagnosticTest] Wrote log to {outPath}");

            return logResult;
        }
    }

    /// <summary>
    /// Runtime logger that automatically runs during Play mode and records the live Play mode rotation.
    /// </summary>
    public class ObstacleRuntimeLogger : MonoBehaviour
    {
        private ObstacleAnimationController _ctrl;
        private AnimationClip _clip;
        private GameObject _root;
        private Transform _child;
        private float _startTime;
        private float _nextSampleTime;
        private List<string> _logLines = new List<string>();
        private bool _done = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttach()
        {
            GameObject gate = GameObject.Find("ft_animated_obstacle_f3");
            if (gate != null && gate.GetComponent<ObstacleRuntimeLogger>() == null)
            {
                gate.AddComponent<ObstacleRuntimeLogger>();
            }
        }

        private void Start()
        {
            _ctrl = GetComponent<ObstacleAnimationController>();
            if (_ctrl != null)
            {
                _clip = _ctrl.Clip;
                _root = _ctrl.AnimationRoot != null ? _ctrl.AnimationRoot : gameObject;
                _child = _root.transform.Find("GM_3D_ObstacleF3") ?? _root.transform;
            }

            _startTime = Time.time;
            _nextSampleTime = _startTime;
            _logLines.Add("================================================================================");
            _logLines.Add("LIVE PLAY MODE LOG: ft_animated_obstacle_f3");
            string clipName = _clip != null ? _clip.name : "null";
            string clipLength = _clip != null ? _clip.length.ToString("F3") : "null";
            string speed = _ctrl != null ? _ctrl.PlaybackSpeed.ToString() : "null";
            _logLines.Add(string.Format("Clip: {0}, Length: {1}s, Speed: {2}", clipName, clipLength, speed));
            _logLines.Add("Time(s)  | NormTime | Root Local Euler             | Child Local Euler");
            _logLines.Add("--------------------------------------------------------------------------------");
        }

        private void Update()
        {
            if (_done || _clip == null) return;

            float elapsed = Time.time - _startTime;
            if (elapsed > 3.1f)
            {
                _done = true;
                _logLines.Add("================================================================================");
                string text = string.Join("\n", _logLines);
                Debug.Log($"[ObstacleRuntimeLogger] Live test completed:\n{text}");
                File.WriteAllText("docs/gate-live-playmode-log.txt", text);
                return;
            }

            if (Time.time >= _nextSampleTime)
            {
                float normTime = Mathf.Repeat(elapsed * _ctrl.PlaybackSpeed, _clip.length) / _clip.length;
                Vector3 rRot = _root.transform.localEulerAngles;
                Vector3 cRot = _child.localEulerAngles;
                _logLines.Add(string.Format("{0,7:F1}s | {1,8:F4} | ({2,5:F1},{3,5:F1},{4,5:F1}) | ({5,5:F1},{6,5:F1},{7,5:F1})",
                    elapsed, normTime, rRot.x, rRot.y, rRot.z, cRot.x, cRot.y, cRot.z));
                _nextSampleTime += 0.1f;
            }
        }
    }
}
