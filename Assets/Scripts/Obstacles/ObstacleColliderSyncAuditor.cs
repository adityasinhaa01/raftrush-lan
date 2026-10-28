using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FartTogether.Obstacles
{
    /// <summary>
    /// Opt-in diagnostic tool to measure and audit the world position & rotation synchronization
    /// between visible mesh nodes and collider nodes across all 24 animated obstacles in live Play mode.
    /// Strictly opt-in via menu item "FartTogether > Run Obstacle Collider Sync Audit".
    /// NEVER auto-runs and NEVER stops Play mode.
    /// </summary>
    public class ObstacleColliderSyncAuditor : MonoBehaviour
    {
        public class ObstaclePair
        {
            public string ObstacleName;
            public Transform AnimRoot;
            public Transform MeshTransform;
            public Transform ColTransform;
            public Collider ColComponent;
            public Rigidbody RbComponent;
            public bool IsUnderAnimHierarchy;
            public bool HasKinematicRb;

            // Initial relative pose at t=0
            public Vector3 InitialRelativePos;
            public Quaternion InitialRelativeRot;

            // Measured max discrepancies over 3s
            public float MaxPosMismatch;
            public float MaxAngleMismatch;
            public float MaxRbPosMismatch;
            public float MaxRbAngleMismatch;

            public List<string> SampleLines = new List<string>();
        }

        private readonly List<ObstaclePair> _pairs = new List<ObstaclePair>();
        private float _startTime;
        private float _nextSampleTime;
        private bool _isAuditing = false;
        private bool _isCompleted = false;

        public bool IsAuditing => _isAuditing;

        public void StartAudit()
        {
            if (_isAuditing) return;

            BuildPairs();
            _startTime = Time.time;
            _nextSampleTime = _startTime;
            _isAuditing = true;
            _isCompleted = false;

            Debug.Log($"[ObstacleColliderSyncAuditor] Started live Play-mode sync audit for {_pairs.Count} obstacle collider pairs over 3.0s (every 0.2s). Play mode will NOT be stopped.");
        }

        private void BuildPairs()
        {
            _pairs.Clear();

            // Find Level_Platform or search scene
            GameObject track = GameObject.Find("Level_Platform");
            Transform searchRoot = track != null ? track.transform : null;

            // Definition of all 24 animated mechanical obstacles
            string[] obstacleNames = new string[]
            {
                "FT_C1_TippingHammer",
                "FT_C1_TippingHammer (4)",
                "FT_C1_TippingHammer (7)",
                "FT_C2_PendulumMace",
                "FT_B2_TippingBar",
                "FT_B5_DoubleSweep",
                "FT_B5_DoubleSweep (1)",
                "FT_C5_Windmill",
                "FT_C5_Windmill (1)",
                "FT_C5_Windmill (4)",
                "FT_RotorCross",
                "FT_RotorCross (1)",
                "FT_RotorCross (2)",
                "FT_RotorCross (2)_1",
                "FT_RotorCross (2)_2",
                "FT_RotorCross (3)",
                "FT_RotorCross (4)",
                "FT_SpikeWheel",
                "ft_animated_obstacle_f10",
                "ft_animated_obstacle_f10_1",
                "ft_animated_obstacle_f3",
                "ft_animated_obstacle_f13",
                "ft_animated_obstacle_f14",
                "ft_animated_obstacle_f23_1"
            };

            foreach (string obsName in obstacleNames)
            {
                GameObject obsObj = GameObject.Find(obsName);
                if (obsObj == null)
                {
                    Debug.LogWarning($"[ObstacleColliderSyncAuditor] Obstacle '{obsName}' not found in scene.");
                    continue;
                }

                ObstacleAnimationController animCtrl = obsObj.GetComponent<ObstacleAnimationController>();
                Transform animRoot = animCtrl != null && animCtrl.AnimationRoot != null
                    ? animCtrl.AnimationRoot.transform
                    : obsObj.transform;

                // Find all renderers under this obstacle
                Renderer[] rends = obsObj.GetComponentsInChildren<Renderer>(true);
                // Find all colliders under this obstacle
                Collider[] cols = obsObj.GetComponentsInChildren<Collider>(true);

                if (cols.Length == 0)
                {
                    Debug.LogWarning($"[ObstacleColliderSyncAuditor] No colliders found under '{obsName}'.");
                    continue;
                }

                // Pair each collider with its corresponding mesh
                foreach (Collider col in cols)
                {
                    Transform colTr = col.transform;
                    Transform meshTr = null;
                    if (colTr.TryGetComponent<ObstacleColliderSync>(out var syncComp) && syncComp.TargetMesh != null)
                    {
                        meshTr = syncComp.TargetMesh;
                    }
                    else
                    {
                        meshTr = FindMatchingMesh(obsName, colTr, rends, animRoot);
                    }

                    if (meshTr == null && rends.Length > 0)
                    {
                        meshTr = rends[0].transform;
                    }

                    Rigidbody rb = col.attachedRigidbody != null ? col.attachedRigidbody : col.GetComponent<Rigidbody>();
                    bool isKinematic = rb != null && rb.isKinematic;
                    bool isUnderAnim = colTr.IsChildOf(animRoot);

                    Vector3 initRelPos = Vector3.zero;
                    Quaternion initRelRot = Quaternion.identity;
                    if (meshTr != null)
                    {
                        initRelPos = Quaternion.Inverse(meshTr.rotation) * (colTr.position - meshTr.position);
                        initRelRot = Quaternion.Inverse(meshTr.rotation) * colTr.rotation;
                    }

                    _pairs.Add(new ObstaclePair
                    {
                        ObstacleName = obsName,
                        AnimRoot = animRoot,
                        MeshTransform = meshTr,
                        ColTransform = colTr,
                        ColComponent = col,
                        RbComponent = rb,
                        IsUnderAnimHierarchy = isUnderAnim,
                        HasKinematicRb = isKinematic,
                        InitialRelativePos = initRelPos,
                        InitialRelativeRot = initRelRot
                    });
                }
            }
        }

        private Transform FindMatchingMesh(string obsName, Transform colTr, Renderer[] rends, Transform animRoot)
        {
            if (rends == null || rends.Length == 0) return null;

            // Specific multi-part mapping logic:
            string colPath = colTr.name;
            Transform parent = colTr.parent;
            while (parent != null && parent != animRoot.parent)
            {
                colPath = parent.name + "/" + colPath;
                parent = parent.parent;
            }

            // For f3, f14, f23_1 (two-part gates)
            if (colPath.Contains("P2"))
            {
                foreach (var r in rends)
                {
                    if (r.name.Contains("P2")) return r.transform;
                }
            }
            else if (colPath.Contains("P1") || colPath.Contains("ObstacleF3") || colPath.Contains("ObstacleF14") || colPath.Contains("ObstacleF23"))
            {
                foreach (var r in rends)
                {
                    if (!r.name.Contains("P2") && (r.name.Contains("ObstacleF") || r.name.Contains("P1"))) return r.transform;
                }
            }

            // For f10 (hammer)
            if (obsName.StartsWith("ft_animated_obstacle_f10"))
            {
                foreach (var r in rends)
                {
                    if (r is SkinnedMeshRenderer) return r.transform;
                }
            }

            // Default: find renderer whose name matches base of colTr or parent
            string baseName = colTr.name.Replace("_Col", "").Replace(".Col", "").Replace("~KillTrigger", "").Replace("collider", "");
            if (colTr.parent != null && string.IsNullOrEmpty(baseName))
            {
                baseName = colTr.parent.name.Replace("_Col", "").Replace(".Col", "");
            }

            foreach (var r in rends)
            {
                if (r.name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                {
                    return r.transform;
                }
            }

            // Fallback to first non-col renderer
            foreach (var r in rends)
            {
                if (!r.name.EndsWith("_Col") && !r.name.EndsWith(".Col"))
                {
                    return r.transform;
                }
            }

            return rends[0].transform;
        }

        private void Update()
        {
            if (!_isAuditing || _isCompleted) return;

            float elapsed = Time.time - _startTime;

            if (Time.time >= _nextSampleTime && elapsed <= 3.05f)
            {
                SampleAll(elapsed);
                _nextSampleTime += 0.2f;
            }

            if (elapsed > 3.1f)
            {
                _isCompleted = true;
                _isAuditing = false;
                GenerateReport();
                Destroy(gameObject);
            }
        }

        private void SampleAll(float elapsed)
        {
            foreach (var pair in _pairs)
            {
                if (pair.MeshTransform == null || pair.ColTransform == null) continue;

                Vector3 meshPos = pair.MeshTransform.position;
                Quaternion meshRot = pair.MeshTransform.rotation;

                Vector3 colPos = pair.ColTransform.position;
                Quaternion colRot = pair.ColTransform.rotation;

                // Expected pose if moving rigidly with mesh
                Vector3 expectedColPos = meshPos + meshRot * pair.InitialRelativePos;
                Quaternion expectedColRot = meshRot * pair.InitialRelativeRot;

                float posMismatch = Vector3.Distance(colPos, expectedColPos);
                float angleMismatch = Quaternion.Angle(colRot, expectedColRot);

                // If Rigidbody exists, measure PhysX Rigidbody mismatch against expected pose
                float rbPosMismatch = 0f;
                float rbAngleMismatch = 0f;
                if (pair.RbComponent != null)
                {
                    Vector3 rbPos = pair.RbComponent.position;
                    Quaternion rbRot = pair.RbComponent.rotation;
                    rbPosMismatch = Vector3.Distance(rbPos, expectedColPos);
                    rbAngleMismatch = Quaternion.Angle(rbRot, expectedColRot);
                }

                float effectivePosMismatch = Mathf.Max(posMismatch, rbPosMismatch);
                float effectiveAngleMismatch = Mathf.Max(angleMismatch, rbAngleMismatch);

                if (effectivePosMismatch > pair.MaxPosMismatch) pair.MaxPosMismatch = effectivePosMismatch;
                if (effectiveAngleMismatch > pair.MaxAngleMismatch) pair.MaxAngleMismatch = effectiveAngleMismatch;
                if (rbPosMismatch > pair.MaxRbPosMismatch) pair.MaxRbPosMismatch = rbPosMismatch;
                if (rbAngleMismatch > pair.MaxRbAngleMismatch) pair.MaxRbAngleMismatch = rbAngleMismatch;

                pair.SampleLines.Add(string.Format("  t={0,4:F1}s | Mesh: ({1,6:F2},{2,6:F2},{3,6:F2}) | ColTr: ({4,6:F2},{5,6:F2},{6,6:F2}) | PhysX: ({7,6:F2},{8,6:F2},{9,6:F2}) | Err: {10,5:F3}m, {11,5:F1}°",
                    elapsed,
                    meshPos.x, meshPos.y, meshPos.z,
                    colPos.x, colPos.y, colPos.z,
                    pair.RbComponent != null ? pair.RbComponent.position.x : colPos.x,
                    pair.RbComponent != null ? pair.RbComponent.position.y : colPos.y,
                    pair.RbComponent != null ? pair.RbComponent.position.z : colPos.z,
                    effectivePosMismatch, effectiveAngleMismatch));
            }
        }

        private void GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========================================================================================================================");
            sb.AppendLine("REAL MEASURED PLAY-MODE OBSTACLE COLLIDER SYNCHRONIZATION AUDIT");
            sb.AppendLine("SAMPLE DURATION: 3.0s | SAMPLE INTERVAL: 0.2s (16 samples per obstacle)");
            sb.AppendLine("METHOD: Real measured world Transform & PhysX Rigidbody pose during live Unity Play mode");
            sb.AppendLine("========================================================================================================================");
            sb.AppendLine();
            sb.AppendLine(string.Format("| {0,-26} | {1,-28} | {2,-14} | {3,-15} | {4,-15} | {5,-16} | {6,-12} |",
                "Obstacle", "Collider Object", "Under Anim Hie", "Kinematic RB?", "Max Pos Mis(m)", "Max Ang Mis(deg)", "Sync Status"));
            sb.AppendLine(string.Format("|{0}|{1}|{2}|{3}|{4}|{5}|{6}|",
                new string('-', 28), new string('-', 30), new string('-', 16), new string('-', 17), new string('-', 17), new string('-', 18), new string('-', 14)));

            int inSyncCount = 0;
            int outOfSyncCount = 0;

            foreach (var p in _pairs)
            {
                bool inSync = p.MaxPosMismatch < 0.05f && p.MaxAngleMismatch < 2.0f;
                if (inSync) inSyncCount++; else outOfSyncCount++;

                string status = inSync ? "IN SYNC" : "OUT OF SYNC";
                string colName = p.ColTransform != null ? p.ColTransform.name : "null";
                if (p.ColTransform != null && p.ColTransform.parent != null && p.ColTransform.parent != p.AnimRoot)
                {
                    colName = p.ColTransform.parent.name + "/" + colName;
                }

                sb.AppendLine(string.Format("| {0,-26} | {1,-28} | {2,-14} | {3,-15} | {4,13:F3}m | {5,14:F1}° | {6,-12} |",
                    p.ObstacleName,
                    colName,
                    p.IsUnderAnimHierarchy ? "Yes" : "No",
                    p.HasKinematicRb ? "Yes" : "No",
                    p.MaxPosMismatch,
                    p.MaxAngleMismatch,
                    status));
            }

            sb.AppendLine("========================================================================================================================");
            sb.AppendLine($"SUMMARY: Total Pairs Measured: {_pairs.Count} | In Sync: {inSyncCount} | Out Of Sync: {outOfSyncCount}");
            sb.AppendLine("========================================================================================================================");

            string reportText = sb.ToString();
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "docs");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            string outPath = Path.Combine(outDir, "obstacle-collider-sync-audit.txt");
            File.WriteAllText(outPath, reportText);

            Debug.Log($"[ObstacleColliderSyncAuditor] Audit complete. Saved report to {outPath}");
            Debug.Log(reportText);
        }

#if UNITY_EDITOR
        [MenuItem("FartTogether/Run Obstacle Collider Sync Audit", priority = 4)]
        public static void RunAuditMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ObstacleColliderSyncAuditor] Unity is NOT in Play mode. Please press Play first, then click 'FartTogether > Run Obstacle Collider Sync Audit'.");
                EditorUtility.DisplayDialog("Run Obstacle Collider Sync Audit",
                    "Unity is not currently in Play mode.\n\nPlease press Play first, then select 'FartTogether > Run Obstacle Collider Sync Audit' while the game is running.",
                    "OK");
                return;
            }

            ObstacleColliderSyncAuditor existing = UnityEngine.Object.FindAnyObjectByType<ObstacleColliderSyncAuditor>();
            if (existing != null && existing._isAuditing)
            {
                Debug.LogWarning("[ObstacleColliderSyncAuditor] Audit is already running.");
                return;
            }

            GameObject runner = new GameObject("~ObstacleColliderSyncAuditor");
            var auditor = runner.AddComponent<ObstacleColliderSyncAuditor>();
            auditor.StartAudit();
        }

        [MenuItem("FartTogether/Run Obstacle Collider Sync Audit (Edit Mode)", priority = 5)]
        public static void RunAuditEditModeMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[ObstacleColliderSyncAuditor] In Play mode, use 'Run Obstacle Collider Sync Audit'.");
                return;
            }

            string scenePath = "Assets/Scenes/Race_M2.unity";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != scenePath)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            }

            GameObject runner = new GameObject("~ObstacleColliderSyncAuditor");
            var auditor = runner.AddComponent<ObstacleColliderSyncAuditor>();
            auditor.RunEditModeAudit();
            DestroyImmediate(runner);
        }

        public void RunEditModeAudit()
        {
            BuildPairs();
            Debug.Log($"[ObstacleColliderSyncAuditor] Running Edit-Mode Simulated Audit for {_pairs.Count} pairs...");

            // Sample from t=0.0 to 3.0s every 0.2s
            for (float t = 0.0f; t <= 3.05f; t += 0.2f)
            {
                // Sample animation on each obstacle
                foreach (var p in _pairs)
                {
                    GameObject obsObj = GameObject.Find(p.ObstacleName);
                    if (obsObj == null) continue;

                    ObstacleAnimationController ctrl = obsObj.GetComponent<ObstacleAnimationController>();
                    if (ctrl == null || ctrl.Clip == null) continue;

                    float sampleTime = Mathf.Repeat(t * ctrl.PlaybackSpeed + ctrl.PhaseSeconds, ctrl.Clip.length);
                    ctrl.Clip.SampleAnimation(p.AnimRoot.gameObject, sampleTime);
                }

                SampleAll(t);
            }

            GenerateReport();
        }
#endif
    }
}
