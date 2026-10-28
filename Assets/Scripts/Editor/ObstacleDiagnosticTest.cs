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

            string[] targetObstacleNames = new string[]
            {
                "ft_animated_obstacle_f10",
                "ft_animated_obstacle_f10_1",
                "ft_animated_obstacle_f13",
                "ft_animated_obstacle_f14",
                "ft_animated_obstacle_f23_1"
            };

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("CONCRETE VERIFICATION: 5 OBSTACLE ROTATION/MOTION SAMPLES OVER 3 SECONDS (EVERY 0.1s)");
            sb.AppendLine("================================================================================");

            foreach (string obsName in targetObstacleNames)
            {
                GameObject obj = GameObject.Find(obsName);
                if (obj == null)
                {
                    sb.AppendLine($"[ObstacleDiagnosticTest] '{obsName}' not found in scene.");
                    continue;
                }

                ObstacleAnimationController ctrl = obj.GetComponent<ObstacleAnimationController>();
                if (ctrl == null || ctrl.Clip == null)
                {
                    sb.AppendLine($"[ObstacleDiagnosticTest] ObstacleAnimationController or Clip is null on {obsName}.");
                    continue;
                }

                AnimationClip clip = ctrl.Clip;
                GameObject root = ctrl.AnimationRoot != null ? ctrl.AnimationRoot : obj;

                // Find key child / bone transforms to inspect
                Transform visualChild = null;
                Transform boneChild = null;
                if (obsName == "ft_animated_obstacle_f10" || obsName == "ft_animated_obstacle_f10_1")
                {
                    visualChild = root.transform.Find("GM_3D_ObstacleF10");
                    boneChild = root.transform.Find("Armature.001/Bone");
                }
                else if (obsName == "ft_animated_obstacle_f13")
                {
                    visualChild = root.transform.Find("GM_3D_ObstacleF13");
                }
                else if (obsName == "ft_animated_obstacle_f14")
                {
                    visualChild = root.transform.Find("GM_3D_ObstacleF14_P1");
                }
                else if (obsName == "ft_animated_obstacle_f23_1")
                {
                    visualChild = root.transform.Find("GM_3D_ObstacleF23");
                }

                sb.AppendLine();
                sb.AppendLine($"--- OBSTACLE: {obsName} ---");
                sb.AppendLine($"Animation Root: {root.name} ({AssetDatabase.GetAssetPath(clip)})");
                sb.AppendLine($"Clip Name: {clip.name}, Length: {clip.length:F3}s, Speed: {ctrl.PlaybackSpeed}x, Phase: {ctrl.PhaseSeconds}s");
                sb.AppendLine($"Key Child: {(visualChild != null ? visualChild.name : "none")}, Key Bone: {(boneChild != null ? boneChild.name : "none")}");
                sb.AppendLine(string.Format("{0,-8} | {1,-10} | {2,-24} | {3,-24} | {4,-24}",
                    "Time(s)", "NormTime", "Root Local Euler", "Child Local Euler", "Bone Local Euler"));
                sb.AppendLine(new string('-', 100));

                for (float t = 0.0f; t <= 3.05f; t += 0.1f)
                {
                    float sampleTime = Mathf.Repeat(t * ctrl.PlaybackSpeed + ctrl.PhaseSeconds, clip.length);
                    float normalizedTime = sampleTime / clip.length;

                    clip.SampleAnimation(root, sampleTime);

                    Vector3 rootRot = root.transform.localEulerAngles;
                    Vector3 childRot = visualChild != null ? visualChild.localEulerAngles : Vector3.zero;
                    Vector3 boneRot = boneChild != null ? boneChild.localEulerAngles : Vector3.zero;

                    sb.AppendLine(string.Format("{0,8:F1} | {1,10:F4} | {2,-24} | {3,-24} | {4,-24}",
                        t, normalizedTime,
                        $"({rootRot.x:F1}, {rootRot.y:F1}, {rootRot.z:F1})",
                        visualChild != null ? $"({childRot.x:F1}, {childRot.y:F1}, {childRot.z:F1})" : "N/A",
                        boneChild != null ? $"({boneRot.x:F1}, {boneRot.y:F1}, {boneRot.z:F1})" : "N/A"));
                }
            }

            sb.AppendLine("================================================================================");

            string logResult = sb.ToString();
            Debug.Log(logResult);

            string outPath = "docs/obstacles-rotation-log.txt";
            File.WriteAllText(outPath, logResult);
            Debug.Log($"[ObstacleDiagnosticTest] Wrote multi-obstacle log to {outPath}");

            return logResult;
        }
    }
}
