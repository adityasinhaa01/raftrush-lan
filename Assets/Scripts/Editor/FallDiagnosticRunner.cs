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
    public static class FallDiagnosticRunner
    {
        [MenuItem("FartTogether/Diagnose Gust Canyon 1 Fall", priority = 5)]
        public static void RunDiagnostic()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != "Assets/Scenes/Race_M2.unity")
            {
                currentScene = EditorSceneManager.OpenScene("Assets/Scenes/Race_M2.unity", OpenSceneMode.Single);
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("DIAGNOSTIC LOG: GUST CANYON 1 & CHECKPOINT 1 FALL ANALYSIS");
            sb.AppendLine("================================================================================");

            // 1. Check ~SurfaceProbe
            GameObject probeObj = GameObject.Find("~SurfaceProbe");
            sb.AppendLine($"[Probe Check] GameObject.Find('~SurfaceProbe'): {(probeObj != null ? "FOUND" : "NULL")}");
            if (probeObj != null)
            {
                Collider col = probeObj.GetComponent<Collider>();
                MeshCollider mc = probeObj.GetComponent<MeshCollider>();
                sb.AppendLine($"  Probe activeInHierarchy: {probeObj.activeInHierarchy}, layer: {probeObj.layer} ({LayerMask.LayerToName(probeObj.layer)})");
                sb.AppendLine($"  Collider: {(col != null ? col.GetType().Name : "NULL")}, enabled: {col?.enabled}, isTrigger: {col?.isTrigger}");
                if (mc != null)
                {
                    sb.AppendLine($"  MeshCollider sharedMesh: {(mc.sharedMesh != null ? mc.sharedMesh.name : "NULL")}, convex: {mc.convex}");
                    if (mc.sharedMesh != null)
                    {
                        sb.AppendLine($"  Mesh bounds: center={mc.sharedMesh.bounds.center}, extents={mc.sharedMesh.bounds.extents}");
                    }
                }
            }

            // 2. Check Plane under Gust_Canyon_1
            GameObject canyon1 = GameObject.Find("Gust_Canyon_1");
            sb.AppendLine($"[Canyon 1 Check] Gust_Canyon_1: {(canyon1 != null ? "FOUND" : "NULL")}");
            if (canyon1 != null)
            {
                sb.AppendLine($"  World pos: {canyon1.transform.position}, rotation: {canyon1.transform.eulerAngles}");
                BoxCollider bc = canyon1.GetComponent<BoxCollider>();
                sb.AppendLine($"  BoxCollider: {(bc != null ? $"size={bc.size}, center={bc.center}, isTrigger={bc.isTrigger}" : "NULL")}");
                Transform planeTrans = canyon1.transform.Find("Plane");
                sb.AppendLine($"  Child 'Plane': {(planeTrans != null ? "FOUND" : "NULL")}");
                if (planeTrans != null)
                {
                    MeshFilter mf = planeTrans.GetComponent<MeshFilter>();
                    MeshRenderer mr = planeTrans.GetComponent<MeshRenderer>();
                    Collider c = planeTrans.GetComponent<Collider>();
                    sb.AppendLine($"    MeshFilter mesh: {mf?.sharedMesh?.name}, MeshRenderer enabled: {mr?.enabled}, material: {mr?.sharedMaterial?.name}");
                    sb.AppendLine($"    Collider on Plane: {(c != null ? c.GetType().Name : "NONE")}");
                }
            }

            // 3. Test Raycasts along the path from Checkpoint 1 (-30.23, 1.51, -10.80) to Gust_Canyon_1 (-11.03, 1.10, -10.66) and beyond
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("RAYCAST PROBES: Downward from y = 1.51 and 2.50 across X in [-32, 5]");
            sb.AppendLine("--------------------------------------------------------------------------------");

            float[] testXs = new float[] { -32f, -30.23f, -28f, -25f, -20f, -15f, -13f, -11.03f, -9f, -7f, -3f, 0f, 10f, 18.57f };
            float z = -10.66f;

            foreach (float x in testXs)
            {
                if (x > 5f) z = 4.34f; // Canyon 2 area

                // Raycast from 1.55m (current RaftBuoyancy start) and 3.5m (original BoatWaterRide start)
                Vector3 origin1 = new Vector3(x, 1.55f, z);
                Vector3 origin2 = new Vector3(x, 3.50f, z);

                // Direct against ~SurfaceProbe if available
                bool probeHit1 = false;
                RaycastHit pHit1 = default;
                if (probeObj != null && probeObj.GetComponent<Collider>() != null)
                {
                    probeHit1 = probeObj.GetComponent<Collider>().Raycast(new Ray(origin1, Vector3.down), out pHit1, 5f);
                }

                // Physics.RaycastAll down
                RaycastHit[] allHits = Physics.RaycastAll(origin2, Vector3.down, 10f, ~0, QueryTriggerInteraction.Collide);
                Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));

                var hitDesc = new List<string>();
                foreach (var h in allHits)
                {
                    hitDesc.Add($"{h.collider.name} (layer={h.collider.gameObject.layer}, y={h.point.y:F2}, trig={h.collider.isTrigger})");
                }

                string hitStr = hitDesc.Count > 0 ? string.Join("; ", hitDesc) : "NOTHING HIT";
                sb.AppendLine($"X={x:F2}, Z={z:F2} | ProbeHit={(probeHit1 ? $"YES(y={pHit1.point.y:F2})" : "NO")} | AllHits: {hitStr}");
            }

            string outPath = Path.Combine(Directory.GetCurrentDirectory(), "docs/fall-diagnostic-log.txt");
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[FallDiagnosticRunner] Diagnostic written to {outPath}\n{sb}");
        }
    }
}
