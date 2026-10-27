using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FartTogether.Editor
{
    /// <summary>
    /// Editor tool requested by user to fix stepped/constant animation playback:
    /// Rewrites keyframe tangents on ripped obstacle clips to smooth modes (ClampedAuto or Linear)
    /// using AnimationUtility.SetKeyLeftTangentMode / SetKeyRightTangentMode.
    /// Preserves original timings, angles, and wrap modes.
    /// Accessible via menu "FartTogether/Fix Clip Tangents".
    /// </summary>
    public static class ObstacleClipTangentFixer
    {
        // Clips with continuous 360-degree rotation at constant speed
        private static readonly HashSet<string> ContinuousSpinClips = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Scene_4.anim",   // FT_B5_DoubleSweep (continuous 360° rotation)
            "Scene_6.anim",   // FT_C5_Windmill (continuous 360° rotation)
            "Scene_7.anim",   // FT_RotorCross (continuous 360° rotation)
            "Scene_14.anim",  // FT_SpikeWheel (continuous 360° rotation)
        };

        // All mechanical obstacle clips
        private static readonly string[] ObstacleClipFiles = new string[]
        {
            "Scene.anim",     // ft_animated_obstacle_f14
            "Scene_0.anim",   // Prop / obstacle
            "Scene_1.anim",   // ft_animated_obstacle_f23_1
            "Scene_2.anim",   // Prop / obstacle
            "Scene_3.anim",   // Prop / obstacle
            "Scene_4.anim",   // FT_B5_DoubleSweep
            "Scene_6.anim",   // FT_C5_Windmill
            "Scene_7.anim",   // FT_RotorCross
            "Scene_8.anim",   // FT_B2_TippingBar
            "Scene_10.anim",  // Prop / obstacle
            "Scene_11.anim",  // FT_C1_TippingHammer
            "Scene_12.anim",  // Prop / obstacle
            "Scene_13.anim",  // ft_animated_obstacle_f3
            "Scene_14.anim",  // FT_SpikeWheel
            "Scene_15.anim",  // ft_animated_obstacle_f13
            "Scene_16.anim",  // FT_C2_PendulumMace
            "Scene_17.anim"   // ft_animated_obstacle_f10
        };

        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            // Run automatically once on compile if not already run
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("FartTogether_TangentsFixed_v1", false))
                {
                    SessionState.SetBool("FartTogether_TangentsFixed_v1", true);
                    FixAllObstacleClipTangents();
                }
            };
        }

        [MenuItem("FartTogether/Fix Clip Tangents", priority = 5)]
        public static void FixTangentsMenu()
        {
            FixAllObstacleClipTangents();
        }

        public static void FixAllObstacleClipTangents()
        {
            int fixedClipCount = 0;
            int fixedCurveCount = 0;
            int fixedKeyCount = 0;

            string clipDir = "Assets/Ripped/AnimationClip";

            foreach (string fileName in ObstacleClipFiles)
            {
                string assetPath = Path.Combine(clipDir, fileName).Replace("\\", "/");
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip == null)
                {
                    Debug.LogWarning($"[ObstacleClipTangentFixer] Clip not found at path: {assetPath}");
                    continue;
                }

                bool isContinuous = ContinuousSpinClips.Contains(fileName);
                AnimationUtility.TangentMode targetMode = isContinuous
                    ? AnimationUtility.TangentMode.Linear
                    : AnimationUtility.TangentMode.ClampedAuto;

                // Preserve original wrap mode
                WrapMode originalWrap = clip.wrapMode;

                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                if (bindings == null || bindings.Length == 0)
                {
                    continue;
                }

                bool clipModified = false;

                foreach (EditorCurveBinding binding in bindings)
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null || curve.length <= 1) continue;

                    Keyframe[] keys = curve.keys;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        // Clean out any -Infinity or +Infinity slopes that Unity treats as stepped
                        if (float.IsInfinity(keys[i].inTangent) || float.IsNaN(keys[i].inTangent))
                        {
                            keys[i].inTangent = 0f;
                        }
                        if (float.IsInfinity(keys[i].outTangent) || float.IsNaN(keys[i].outTangent))
                        {
                            keys[i].outTangent = 0f;
                        }
                    }
                    curve.keys = keys;

                    for (int i = 0; i < curve.length; i++)
                    {
                        curve.SmoothTangents(i, 0f);
                        AnimationUtility.SetKeyLeftTangentMode(curve, i, targetMode);
                        AnimationUtility.SetKeyRightTangentMode(curve, i, targetMode);
                        fixedKeyCount++;
                    }

                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                    fixedCurveCount++;
                    clipModified = true;
                }

                if (clipModified)
                {
                    clip.wrapMode = originalWrap;
                    EditorUtility.SetDirty(clip);
                    fixedClipCount++;
                    Debug.Log($"[ObstacleClipTangentFixer] Fixed tangents on {fileName} ({bindings.Length} curves, mode: {targetMode})");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ObstacleClipTangentFixer] Successfully fixed {fixedClipCount} clips ({fixedCurveCount} curves, {fixedKeyCount} keyframes).");
        }
    }
}
