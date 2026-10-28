using UnityEditor;
using UnityEngine;
using FartTogether.Track;

namespace FartTogether.Editor
{
    /// <summary>
    /// Editor menu toggle for drawing track centerline and raft heading debug gizmos in Scene view.
    /// Default is OFF. Opt-in via menu: FartTogether > Toggle Track Centerline Gizmos.
    /// </summary>
    public static class TrackCenterlineGizmoMenu
    {
        private const string MenuItemPath = "FartTogether/Toggle Track Centerline Gizmos";

        [MenuItem(MenuItemPath, priority = 30)]
        public static void ToggleCenterlineGizmos()
        {
            bool current = EditorPrefs.GetBool(TrackCenterline.GizmoPrefKey, false);
            bool newState = !current;
            EditorPrefs.SetBool(TrackCenterline.GizmoPrefKey, newState);
            SceneView.RepaintAll();
            Debug.Log($"[TrackCenterline] Centerline debug gizmos: {(newState ? "ENABLED (Cyan=Centerline, Green=Checkpoints, Orange=Tangents, Raft=Cyan Arrow)" : "DISABLED")}.");
        }

        [MenuItem(MenuItemPath, true)]
        public static bool ToggleCenterlineGizmosValidate()
        {
            Menu.SetChecked(MenuItemPath, EditorPrefs.GetBool(TrackCenterline.GizmoPrefKey, false));
            return true;
        }
    }
}
