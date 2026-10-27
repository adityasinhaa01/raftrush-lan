using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FartTogether.Obstacles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FartTogether.Editor
{
    /// <summary>
    /// Diagnoses obstacle animation setup across Level_Platform in Race_M2.
    /// Accessible via menu item "FartTogether/Dump Obstacle Animation Info".
    /// Writes detailed diagnosis to docs/obstacle-anim-dump.txt.
    /// </summary>
    public static class ObstacleAnimationDumper
    {
        private const string ScenePath = "Assets/Scenes/Race_M2.unity";
        private const string OutputPath = "docs/obstacle-anim-dump.txt";

        [MenuItem("FartTogether/Dump Obstacle Animation Info", priority = 3)]
        public static void DumpObstacleAnimationInfo()
        {
            // Ensure Race_M2 scene is loaded
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != ScenePath)
            {
                if (File.Exists(ScenePath))
                {
                    currentScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogError($"[ObstacleAnimationDumper] Scene not found at {ScenePath}. Build it first.");
                    return;
                }
            }

            GameObject[] rootObjects = currentScene.GetRootGameObjects();
            GameObject levelPlatform = null;
            foreach (var root in rootObjects)
            {
                if (root.name.Equals("Level_Platform", StringComparison.OrdinalIgnoreCase))
                {
                    levelPlatform = root;
                    break;
                }
            }

            if (levelPlatform == null)
            {
                Debug.LogError("[ObstacleAnimationDumper] 'Level_Platform' root object not found in Race_M2 scene.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("LEVEL_PLATFORM OBSTACLE ANIMATION DIAGNOSTIC DUMP");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Scene: {currentScene.path}");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            // Collect all objects under Level_Platform
            Transform[] allTransforms = levelPlatform.GetComponentsInChildren<Transform>(true);

            // Set of target objects to inspect:
            // Objects with Animation, Animator, ObstacleAnimationController, or targeted by ObstacleAnimationController
            var inspectedObjects = new List<GameObject>();
            var inspectedSet = new HashSet<GameObject>();

            foreach (Transform t in allTransforms)
            {
                GameObject go = t.gameObject;
                bool hasAnim = go.GetComponent<Animation>() != null;
                bool hasAnimator = go.GetComponent<Animator>() != null;
                bool hasObstacleCtrl = go.GetComponent<ObstacleAnimationController>() != null;
                bool hasOtherCustom = go.GetComponent<FloatingBobObstacle>() != null ||
                                      go.GetComponent<BouncingSpike>() != null ||
                                      go.GetComponent<HazardBarrier>() != null;

                if (hasAnim || hasAnimator || hasObstacleCtrl || hasOtherCustom)
                {
                    if (inspectedSet.Add(go))
                    {
                        inspectedObjects.Add(go);
                    }
                }
            }

            sb.AppendLine($"Total inspected GameObjects under Level_Platform: {inspectedObjects.Count}");
            sb.AppendLine();

            int index = 0;
            foreach (GameObject go in inspectedObjects)
            {
                index++;
                string hierarchyPath = GetHierarchyPath(go.transform);
                sb.AppendLine($"--------------------------------------------------------------------------------");
                sb.AppendLine($"[{index}] GameObject: {go.name}");
                sb.AppendLine($"    Hierarchy Path: {hierarchyPath}");

                // Attached custom scripts
                var customScripts = new List<string>();
                if (go.GetComponent<ObstacleAnimationController>() != null) customScripts.Add("ObstacleAnimationController");
                if (go.GetComponent<FloatingBobObstacle>() != null) customScripts.Add("FloatingBobObstacle");
                if (go.GetComponent<BouncingSpike>() != null) customScripts.Add("BouncingSpike");
                if (go.GetComponent<HazardBarrier>() != null) customScripts.Add("HazardBarrier");

                sb.AppendLine($"    Custom Scripts: {(customScripts.Count > 0 ? string.Join(", ", customScripts) : "None")}");

                // 1. ObstacleAnimationController details
                ObstacleAnimationController ctrl = go.GetComponent<ObstacleAnimationController>();
                if (ctrl != null)
                {
                    sb.AppendLine($"    -- ObstacleAnimationController Component --");
                    sb.AppendLine($"       Assigned Clip: {(ctrl.Clip != null ? ctrl.Clip.name : "null")}");
                    if (ctrl.Clip != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(ctrl.Clip);
                        sb.AppendLine($"       Clip Asset Path: {assetPath}");
                        sb.AppendLine($"       Clip.legacy: {ctrl.Clip.legacy}");
                        sb.AppendLine($"       Clip.wrapMode: {ctrl.Clip.wrapMode}");
                        sb.AppendLine($"       Clip Length: {ctrl.Clip.length:F2}s");
                    }
                    sb.AppendLine($"       Animation Root: {(ctrl.AnimationRoot != null ? GetHierarchyPath(ctrl.AnimationRoot.transform) : "null (defaults to self)")}");
                    sb.AppendLine($"       Playback Speed: {ctrl.PlaybackSpeed}x");
                    sb.AppendLine($"       Phase Seconds: {ctrl.PhaseSeconds}s");
                    sb.AppendLine($"       Wrap Mode: {ctrl.WrapMode}");
                }

                // 2. Animation (Legacy) details
                Animation anim = go.GetComponent<Animation>();
                if (anim != null)
                {
                    sb.AppendLine($"    -- UnityEngine.Animation (Legacy) Component --");
                    sb.AppendLine($"       Default Clip (clip): {(anim.clip != null ? anim.clip.name : "null")}");
                    sb.AppendLine($"       Play Automatically: {anim.playAutomatically}");
                    sb.AppendLine($"       Wrap Mode: {anim.wrapMode}");

                    var clipList = new List<AnimationClip>();
                    foreach (AnimationState state in anim)
                    {
                        if (state.clip != null) clipList.Add(state.clip);
                    }

                    sb.AppendLine($"       Clips In State List ({clipList.Count}):");
                    if (clipList.Count == 0 && anim.clip != null)
                    {
                        clipList.Add(anim.clip);
                    }

                    foreach (var c in clipList)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(c);
                        sb.AppendLine($"         * Clip: '{c.name}', legacy={c.legacy}, wrapMode={c.wrapMode}, path={assetPath}");
                        CheckCurveBindings(go.transform, c, sb, "           ");
                    }

                    if (clipList.Count == 0)
                    {
                        sb.AppendLine($"         [WARNING] Animation component has 0 clips in list and default clip is null!");
                    }
                }

                // 3. Animator details
                Animator animator = go.GetComponent<Animator>();
                if (animator != null)
                {
                    sb.AppendLine($"    -- UnityEngine.Animator Component --");
                    sb.AppendLine($"       RuntimeAnimatorController: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null (Playables driven)")}");
                    sb.AppendLine($"       Avatar: {(animator.avatar != null ? animator.avatar.name : "null")}");
                    sb.AppendLine($"       Enabled: {animator.enabled}");
                    sb.AppendLine($"       Culling Mode: {animator.cullingMode}");
                    sb.AppendLine($"       Apply Root Motion: {animator.applyRootMotion}");
                }

                // If this object has an ObstacleAnimationController, check curve bindings against its configured animation root
                if (ctrl != null && ctrl.Clip != null)
                {
                    Transform evalRoot = ctrl.AnimationRoot != null ? ctrl.AnimationRoot.transform : go.transform;
                    sb.AppendLine($"    -- Curve Target Evaluation (relative to '{evalRoot.name}') --");
                    CheckCurveBindings(evalRoot, ctrl.Clip, sb, "       ");
                }

                sb.AppendLine();
            }

            // Write to docs/obstacle-anim-dump.txt
            string fullOutputPath = Path.Combine(Directory.GetCurrentDirectory(), OutputPath);
            string dir = Path.GetDirectoryName(fullOutputPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(fullOutputPath, sb.ToString());
            Debug.Log($"<color=green>[ObstacleAnimationDumper] Successfully wrote diagnostic dump to {OutputPath} ({inspectedObjects.Count} objects analyzed).</color>");
        }

        private static void CheckCurveBindings(Transform root, AnimationClip clip, StringBuilder sb, string indent)
        {
            if (clip == null || root == null) return;

            EditorCurveBinding[] floatBindings = AnimationUtility.GetCurveBindings(clip);
            EditorCurveBinding[] objBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

            var uniquePaths = new HashSet<string>();
            foreach (var b in floatBindings) uniquePaths.Add(b.path);
            foreach (var b in objBindings) uniquePaths.Add(b.path);

            sb.AppendLine($"{indent}Curve Binding Paths ({uniquePaths.Count}):");
            int foundCount = 0;
            int missingCount = 0;

            foreach (string path in uniquePaths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    sb.AppendLine($"{indent}  [MATCH] \"\" (targets root '{root.name}')");
                    foundCount++;
                }
                else
                {
                    Transform target = root.Find(path);
                    if (target != null)
                    {
                        sb.AppendLine($"{indent}  [MATCH] \"{path}\" -> Found: '{target.name}'");
                        foundCount++;
                    }
                    else
                    {
                        // Check if child exists under a sub-child or parent
                        Transform deepSearch = FindChildRecursive(root, path);
                        if (deepSearch != null)
                        {
                            sb.AppendLine($"{indent}  [MISMATCH-DEEP] \"{path}\" -> Found at '{GetHierarchyPath(deepSearch)}' but NOT direct relative path!");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}  [MISSING] \"{path}\" -> Target does NOT exist under '{root.name}'");
                        }
                        missingCount++;
                    }
                }
            }

            sb.AppendLine($"{indent}Result: {foundCount} matched, {missingCount} missing.");
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            // If path contains slashes, take last segment
            string searchName = name;
            int slash = name.LastIndexOf('/');
            if (slash >= 0) searchName = name.Substring(slash + 1);

            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(searchName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
            return null;
        }

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null) return string.Empty;
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return path;
        }
    }
}
