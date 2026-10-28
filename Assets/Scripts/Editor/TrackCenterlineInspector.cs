using System.Text;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FartTogether.Track;

namespace FartTogether.Editor
{
    public static class TrackCenterlineInspector
    {
        [MenuItem("FartTogether/Inspect Track Geometry", priority = 20)]
        public static void InspectTrack()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Race_M2.unity")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/Race_M2.unity", OpenSceneMode.Single);
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== TRACK GEOMETRY INSPECTION ===");

            // 1. Checkpoints
            var checkpoints = Object.FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None);
            System.Array.Sort(checkpoints, (a, b) => a.CheckpointIndex.CompareTo(b.CheckpointIndex));
            sb.AppendLine($"\n--- CHECKPOINTS ({checkpoints.Length}) ---");
            foreach (var cp in checkpoints)
            {
                sb.AppendLine($"CP {cp.CheckpointIndex}: name='{cp.name}', pos={cp.transform.position}, rot={cp.transform.eulerAngles}, fwd={cp.transform.forward}");
            }

            // 2. Gust Canyons
            sb.AppendLine("\n--- GUST CANYONS ---");
            GameObject gust = GameObject.Find("Level_Platform/Gust");
            if (gust != null)
            {
                for (int i = 0; i < gust.transform.childCount; i++)
                {
                    Transform t = gust.transform.GetChild(i);
                    BoxCollider bc = t.GetComponent<BoxCollider>();
                    Vector3 size = bc != null ? bc.size : Vector3.zero;
                    sb.AppendLine($"Canyon: '{t.name}', pos={t.position}, rot={t.eulerAngles}, fwd={t.forward}, size={size}");
                }
            }

            // 3. Raft Spawn & Finish
            GameObject raft = GameObject.Find("Raft");
            if (raft != null)
            {
                sb.AppendLine($"\nRaft Spawn: pos={raft.transform.position}, rot={raft.transform.eulerAngles}, fwd={raft.transform.forward}");
            }
            GameObject finish = GameObject.Find("FinishZone") ?? GameObject.Find("Level_Platform/Finish");
            if (finish != null)
            {
                sb.AppendLine($"Finish: name='{finish.name}', pos={finish.transform.position}, rot={finish.transform.eulerAngles}");
            }

            string outPath = Path.Combine(Directory.GetCurrentDirectory(), "docs/track-geometry-inspection.txt");
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[TrackCenterlineInspector] Saved inspection to {outPath}");
        }

        [MenuItem("FartTogether/Test Track Centerline Tangents", priority = 21)]
        public static void TestTrackCenterline()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== TRACK CENTERLINE TANGENT TEST ===");

            var testSpots = new (string Name, Vector3 Pos, int CP)[]
            {
                ("Straight channel (Spawn)", new Vector3(-15.55f, 1.50f, 90.80f), 0),
                ("Straight channel (Mid)", new Vector3(-15.43f, 1.50f, 60.00f), 0),
                ("Bend before Gust 1 (Target)", new Vector3(-25.00f, 1.00f, 34.00f), 0),
                ("Bend before Gust 1 (Exit)", new Vector3(-28.00f, 1.00f, 20.00f), 0),
                ("Inside Gust Canyon 1", new Vector3(-11.00f, 1.00f, -10.66f), 1),
                ("Turn into Gust Canyon 3", new Vector3(48.85f, 10.00f, -20.00f), 2),
                ("Inside Gust Canyon 3", new Vector3(48.85f, 10.00f, -65.84f), 2),
                ("Straight channel to Finish", new Vector3(300.00f, 9.35f, -103.02f), 6)
            };

            foreach (var (name, pos, cp) in testSpots)
            {
                Vector3 tan = TrackCenterline.GetChannelTangent(pos, cp, out Vector3 cl);
                Quaternion rot = Quaternion.LookRotation(tan, Vector3.up);
                float yaw = rot.eulerAngles.y;
                string line = $"{name,-30} @ {pos} -> tan: ({tan.x,5:F2}, {tan.y,5:F2}, {tan.z,5:F2}) | yaw: {yaw,5:F1}° | cl: {cl}";
                sb.AppendLine(line);
                Debug.Log($"[TrackCenterlineTest] {line}");
            }

            string outPath = Path.Combine(Directory.GetCurrentDirectory(), "docs/track-centerline-test.txt");
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[TrackCenterlineInspector] Saved test results to {outPath}");
        }
    }
}
