using System.IO;
using FartTogether.CameraRig;
using FartTogether.PlayerInput;
using FartTogether.Raft;
using FartTogether.Track;
using FartTogether.GameUI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace FartTogether.Editor
{
    /// <summary>
    /// Automated builder for Milestone 1 test scene: Assets/Scenes/Race_M1.unity.
    /// Accessible via menu item "FartTogether/Build Milestone 1 Scene (Race_M1)".
    /// Uses URP Lit materials saved in Assets/Materials/M1/ for reliable rendering in Unity 6 URP.
    /// </summary>
    public static class RaceM1SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Race_M1.unity";

        // Asset paths
        private const string TrackMeshPath = "Assets/Ripped/Mesh/LEVEL1.asset";
        private const string BoatMeshPath = "Assets/Ripped/Mesh/GM_3D_BoatMain.asset";
        private const string BoatColMeshPath = "Assets/Ripped/Mesh/GM_3D_BoatMain_Col.asset";
        private const string FontPath = "Assets/Ripped/Resources/fonts & materials/LiberationSans SDF.asset";
        private const string FallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        // Material paths in Assets/Materials/M1/
        private const string TrackMatPath = "Assets/Materials/M1/M1_Track.mat";
        private const string RaftMatPath = "Assets/Materials/M1/M1_Raft.mat";
        private const string SeatRedMatPath = "Assets/Materials/M1/M1_Seat_Red.mat";
        private const string SeatBlueMatPath = "Assets/Materials/M1/M1_Seat_Blue.mat";
        private const string SeatYellowMatPath = "Assets/Materials/M1/M1_Seat_Yellow.mat";
        private const string SeatGreenMatPath = "Assets/Materials/M1/M1_Seat_Green.mat";
        private const string FinishMatPath = "Assets/Materials/M1/M1_FinishZone.mat";

        [MenuItem("FartTogether/Build Milestone 1 Scene (Race_M1)", priority = 1)]
        public static void BuildSceneMenu()
        {
            BuildMilestone1Scene(openAndSave: true);
        }

        [InitializeOnLoadMethod]
        private static void AutoBuildIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath))
                {
                    BuildMilestone1Scene(openAndSave: true);
                }
            };
        }

        public static void BuildMilestone1Scene(bool openAndSave)
        {
            // 1. Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Add Lighting & Environment
            SetupLighting();

            // 3. Add Track
            GameObject trackObj = SetupTrack();

            // 4. Add Raft
            GameObject raftObj = SetupRaft();

            // 5. Add Finish Zone
            SetupFinishZone();

            // 6. Add Camera
            SetupCamera(raftObj.transform);

            // 7. Add UI Canvas & HUD
            SetupHUD(raftObj.GetComponent<RaftController>(), raftObj.GetComponent<GasInput>());

            // 8. Save Scene
            if (openAndSave)
            {
                string dir = Path.GetDirectoryName(ScenePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[RaceM1SceneBuilder] Successfully built and saved {ScenePath}!</color>");
            }
        }

        private static Material GetOrCreateMaterial(string assetPath, Color color, bool transparent = false)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (mat == null)
            {
                string dir = Path.GetDirectoryName(assetPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }

                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else
            {
                if (mat.shader != shader && shader != null)
                {
                    mat.shader = shader;
                }
            }

            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            if (transparent)
            {
                mat.SetFloat("_Surface", 1.0f); // 1 = Transparent
                mat.SetFloat("_Blend", 0.0f);   // 0 = Alpha blend
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0.0f);
                mat.SetFloat("_Smoothness", 0.1f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_Surface", 0.0f); // 0 = Opaque
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1.0f);
                mat.SetFloat("_Smoothness", 0.25f);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetupLighting()
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1.0f, 0.98f, 0.94f);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Add UniversalAdditionalLightData for URP
            lightObj.AddComponent<UniversalAdditionalLightData>();

            // Global Volume for subtle post processing
            GameObject volumeObj = new GameObject("Global Volume");
            Volume volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;

            // Ambient lighting in trilight mode for balanced contrast
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.65f, 0.70f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.50f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.28f, 0.25f);
        }

        private static GameObject SetupTrack()
        {
            GameObject track = new GameObject("Track_LEVEL1");
            track.transform.position = Vector3.zero;
            track.transform.rotation = Quaternion.identity;
            track.transform.localScale = new Vector3(2f, 2f, 2f);

            MeshFilter mf = track.AddComponent<MeshFilter>();
            MeshRenderer mr = track.AddComponent<MeshRenderer>();
            MeshCollider mc = track.AddComponent<MeshCollider>();

            Mesh trackMesh = AssetDatabase.LoadAssetAtPath<Mesh>(TrackMeshPath);
            // Mid grey-green track material (not white)
            Material trackMat = GetOrCreateMaterial(TrackMatPath, new Color(0.35f, 0.44f, 0.38f));

            if (trackMesh != null)
            {
                mf.sharedMesh = trackMesh;
                mc.sharedMesh = trackMesh;
            }
            else
            {
                Debug.LogWarning($"[RaceM1SceneBuilder] Could not find track mesh at {TrackMeshPath}");
            }

            mr.sharedMaterial = trackMat;
            mc.convex = false; // Concave track collision
            return track;
        }

        private static GameObject SetupRaft()
        {
            GameObject raft = new GameObject("Raft");
            // Start position facing down the track (-Z)
            raft.transform.position = new Vector3(0f, 1.5f, -2f);
            raft.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            Rigidbody rb = raft.AddComponent<Rigidbody>();
            rb.mass = 40f;
            rb.linearDamping = 0.4f;
            rb.angularDamping = 2.0f;
            rb.centerOfMass = new Vector3(0f, -0.7f, 0f);
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Raft Controller & Gas Input
            GasInput gasInput = raft.AddComponent<GasInput>();
            RaftController raftController = raft.AddComponent<RaftController>();

            // Mesh Collider on raft
            Mesh colMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BoatColMeshPath);
            if (colMesh != null)
            {
                MeshCollider mc = raft.AddComponent<MeshCollider>();
                mc.sharedMesh = colMesh;
                mc.convex = true;
            }
            else
            {
                // Fallback sphere collider
                SphereCollider sc = raft.AddComponent<SphereCollider>();
                sc.radius = 1.3f;
                sc.center = new Vector3(0f, -0.2f, 0f);
            }

            // Visual Model Child
            GameObject model = new GameObject("Model");
            model.transform.SetParent(raft.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            MeshFilter mf = model.AddComponent<MeshFilter>();
            MeshRenderer mr = model.AddComponent<MeshRenderer>();

            Mesh boatMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BoatMeshPath);
            // Bright orange raft material
            Material raftMat = GetOrCreateMaterial(RaftMatPath, new Color(1.0f, 0.45f, 0.05f));

            if (boatMesh != null) mf.sharedMesh = boatMesh;
            mr.sharedMaterial = raftMat;

            // Add visible 3D Seat Markers with 4 distinct colors (Red, Blue, Yellow, Green)
            Material seatRed = GetOrCreateMaterial(SeatRedMatPath, new Color(0.92f, 0.18f, 0.18f));
            Material seatBlue = GetOrCreateMaterial(SeatBlueMatPath, new Color(0.18f, 0.45f, 0.95f));
            Material seatYellow = GetOrCreateMaterial(SeatYellowMatPath, new Color(0.95f, 0.85f, 0.15f));
            Material seatGreen = GetOrCreateMaterial(SeatGreenMatPath, new Color(0.15f, 0.85f, 0.30f));

            CreateSeatMarker(raft.transform, "Seat1_RearRight", 135f, seatRed);
            CreateSeatMarker(raft.transform, "Seat2_RearLeft", 225f, seatBlue);
            CreateSeatMarker(raft.transform, "Seat3_FrontLeft", 315f, seatYellow);
            CreateSeatMarker(raft.transform, "Seat4_FrontRight", 45f, seatGreen);

            return raft;
        }

        private static void CreateSeatMarker(Transform parent, string name, float angleDeg, Material mat)
        {
            GameObject seatObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            seatObj.name = name;
            seatObj.transform.SetParent(parent, false);

            // Destroy collider so markers do not interfere with raft physics/raycasts
            Collider col = seatObj.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }

            float rad = angleDeg * Mathf.Deg2Rad;
            // Radius ~1.25m on raft rim, placed at Y=0.4m so spheres are clearly visible on top of hull
            seatObj.transform.localPosition = new Vector3(Mathf.Sin(rad) * 1.25f, 0.4f, Mathf.Cos(rad) * 1.25f);
            seatObj.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
            seatObj.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

            MeshRenderer mr = seatObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
            }
        }

        private static void SetupFinishZone()
        {
            GameObject finishObj = new GameObject("FinishZone");
            // End of Level1 track
            finishObj.transform.position = new Vector3(-28f, 1.0f, -220f);
            finishObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            BoxCollider bc = finishObj.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(24f, 8f, 8f);

            finishObj.AddComponent<FinishZone>();

            // Visible semi-transparent green cube matching trigger bounds
            GameObject visualCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualCube.name = "FinishZoneVisual";
            visualCube.transform.SetParent(finishObj.transform, false);
            visualCube.transform.localPosition = Vector3.zero;
            visualCube.transform.localRotation = Quaternion.identity;
            visualCube.transform.localScale = bc.size;

            // Remove collider from visual cube so it does not block the raft
            Collider col = visualCube.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }

            Material finishMat = GetOrCreateMaterial(FinishMatPath, new Color(0.1f, 0.9f, 0.2f, 0.35f), transparent: true);
            MeshRenderer mr = visualCube.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = finishMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void SetupCamera(Transform target)
        {
            GameObject camObj = new GameObject("Main Camera");
            UnityEngine.Camera cam = camObj.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<UniversalAdditionalCameraData>();

            ChaseCamera chase = camObj.AddComponent<ChaseCamera>();
            chase.Target = target;
            chase.Distance = 6.0f;
            chase.Height = 2.8f;
            chase.LookOffset = new Vector3(0f, 0.6f, 1.2f);
            chase.SnapToTarget();

            camObj.tag = "MainCamera";
        }

        private static void SetupHUD(RaftController raft, GasInput gasInput)
        {
            // EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
            }

            // Speed Text (Top-Left)
            TextMeshProUGUI speedText = CreateText(canvasObj.transform, "SpeedText", "SPEED: 0.0 km/h", font, 36, TextAlignmentOptions.TopLeft);
            RectTransform speedRect = speedText.rectTransform;
            speedRect.anchorMin = new Vector2(0f, 1f);
            speedRect.anchorMax = new Vector2(0f, 1f);
            speedRect.pivot = new Vector2(0f, 1f);
            speedRect.anchoredPosition = new Vector2(40f, -40f);
            speedRect.sizeDelta = new Vector2(400f, 50f);

            // Debug Text (Top-Left below SpeedText)
            TextMeshProUGUI debugText = CreateText(canvasObj.transform, "DebugText", "POS: (0.0, 0.0, 0.0) | KEYS: None", font, 20, TextAlignmentOptions.TopLeft);
            RectTransform debugRect = debugText.rectTransform;
            debugRect.anchorMin = new Vector2(0f, 1f);
            debugRect.anchorMax = new Vector2(0f, 1f);
            debugRect.pivot = new Vector2(0f, 1f);
            debugRect.anchoredPosition = new Vector2(40f, -88f);
            debugRect.sizeDelta = new Vector2(700f, 35f);
            debugText.color = new Color(0.85f, 0.90f, 0.85f, 1.0f);

            // Timer Text (Top-Center)
            TextMeshProUGUI timerText = CreateText(canvasObj.transform, "TimerText", "TIME: 00:00.00", font, 44, TextAlignmentOptions.Top);
            RectTransform timerRect = timerText.rectTransform;
            timerRect.anchorMin = new Vector2(0.5f, 1f);
            timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            timerRect.anchoredPosition = new Vector2(0f, -40f);
            timerRect.sizeDelta = new Vector2(400f, 70f);

            // Gas Container (Bottom-Center)
            GameObject gasPanel = new GameObject("GasPanel");
            gasPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform gasRect = gasPanel.AddComponent<RectTransform>();
            gasRect.anchorMin = new Vector2(0.5f, 0f);
            gasRect.anchorMax = new Vector2(0.5f, 0f);
            gasRect.pivot = new Vector2(0.5f, 0f);
            gasRect.anchoredPosition = new Vector2(0f, 40f);
            gasRect.sizeDelta = new Vector2(800f, 120f);

            TextMeshProUGUI[] gasTexts = new TextMeshProUGUI[4];
            string[] seatNames = { "P1 [Key 1] Rear-R", "P2 [Key 2] Rear-L", "P3 [Key 3] Front-L", "P4 [Key 4] Front-R" };
            float[] xOffsets = { -300f, -100f, 100f, 300f };

            for (int i = 0; i < 4; i++)
            {
                TextMeshProUGUI gt = CreateText(gasPanel.transform, $"GasText_{i + 1}", $"{seatNames[i]}\n100%", font, 22, TextAlignmentOptions.Center);
                RectTransform gr = gt.rectTransform;
                gr.anchorMin = new Vector2(0.5f, 0.5f);
                gr.anchorMax = new Vector2(0.5f, 0.5f);
                gr.pivot = new Vector2(0.5f, 0.5f);
                gr.anchoredPosition = new Vector2(xOffsets[i], 0f);
                gr.sizeDelta = new Vector2(190f, 90f);
                gasTexts[i] = gt;
            }

            // Finish Banner (Center)
            TextMeshProUGUI finishBanner = CreateText(canvasObj.transform, "FinishBanner", "FINISH!\nTIME: 00:00.00", font, 64, TextAlignmentOptions.Center);
            RectTransform finishRect = finishBanner.rectTransform;
            finishRect.anchorMin = new Vector2(0.5f, 0.5f);
            finishRect.anchorMax = new Vector2(0.5f, 0.5f);
            finishRect.pivot = new Vector2(0.5f, 0.5f);
            finishRect.anchoredPosition = Vector2.zero;
            finishRect.sizeDelta = new Vector2(700f, 250f);
            finishBanner.color = new Color(1.0f, 0.85f, 0.1f);
            finishBanner.gameObject.SetActive(false);

            // Race HUD Component
            RaceHUD hud = canvasObj.AddComponent<RaceHUD>();
            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("_raft").objectReferenceValue = raft;
            so.FindProperty("_gasInputComponent").objectReferenceValue = gasInput;
            so.FindProperty("_speedText").objectReferenceValue = speedText;
            so.FindProperty("_timerText").objectReferenceValue = timerText;
            so.FindProperty("_debugText").objectReferenceValue = debugText;
            so.FindProperty("_finishBannerText").objectReferenceValue = finishBanner;

            SerializedProperty gasTextsProp = so.FindProperty("_gasFuelTexts");
            for (int i = 0; i < 4; i++)
            {
                gasTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = gasTexts[i];
            }
            so.ApplyModifiedProperties();
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            if (font != null) tmp.font = font;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }
    }
}
