using System;
using System.Collections.Generic;
using System.IO;
using FartTogether.CameraRig;
using FartTogether.GameUI;
using FartTogether.Obstacles;
using FartTogether.PlayerInput;
using FartTogether.Raft;
using FartTogether.Track;
using FartTogether.Water;
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
    /// Automated builder for Milestone 2 test scene: Assets/Scenes/Race_M2.unity.
    /// Accessible via menu item "FartTogether/Build Milestone 2 Scene (Race_M2)".
    /// Uses the authentic Level_Platform track from the original game with authentic coordinates,
    /// WaterFlowZones for water currents (along and against), solid mesh colliders on the steep slope,
    /// and clean URP/Lit materials mapped with original textures.
    /// </summary>
    public static class RaceM2SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Race_M2.unity";

        // Asset Paths from Assets/Ripped/
        private const string LevelPlatformPrefabPath = "Assets/Ripped/GameObject/Level_Platform.prefab";
        private const string BoatMeshPath = "Assets/Ripped/Mesh/GM_3D_BoatMain.asset";
        private const string BoatColMeshPath = "Assets/Ripped/Mesh/GM_3D_BoatMain_Col.asset";
        private const string GradientTexPath = "Assets/Ripped/Texture2D/Gradient Pallete.png";
        private const string BgrTexPath = "Assets/Ripped/Texture2D/bgr_default_tex.png";
        private const string FoamTexPath = "Assets/Ripped/Texture2D/Foam2.png";
        private const string FontPath = "Assets/Ripped/Resources/fonts & materials/LiberationSans SDF.asset";
        private const string FallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        // Material Paths in Assets/Materials/M2/
        private const string MainPaletMatPath = "Assets/Materials/M2/M2_Main_Palet.mat";
        private const string EnvMatPath = "Assets/Materials/M2/M2_Environment.mat";
        private const string WaterMatPath = "Assets/Materials/M2/M2_Water.mat";
        private const string FogMatPath = "Assets/Materials/M2/M2_Fog.mat";
        private const string RaftMatPath = "Assets/Materials/M2/M2_Raft.mat";
        private const string SeatRedMatPath = "Assets/Materials/M2/M2_Seat_Red.mat";
        private const string SeatBlueMatPath = "Assets/Materials/M2/M2_Seat_Blue.mat";
        private const string SeatYellowMatPath = "Assets/Materials/M2/M2_Seat_Yellow.mat";
        private const string SeatGreenMatPath = "Assets/Materials/M2/M2_Seat_Green.mat";
        private const string FinishMatPath = "Assets/Materials/M2/M2_FinishZone.mat";

        // Original Start and Finish Coordinates (from PuffTest.unity Level_Platform)
        // Raft starts at Level_Platform/Spawns center facing -Z down the track
        private static readonly Vector3 RaftSpawnPosition = new Vector3(-15.55f, 1.50f, 90.80f);
        private static readonly Quaternion RaftSpawnRotation = Quaternion.Euler(0f, 180f, 0f);

        // Finish Line Banner at Level_Platform/GM_3D_Finish
        private static readonly Vector3 FinishPosition = new Vector3(439.21f, 8.50f, -103.12f);
        private static readonly Quaternion FinishRotation = Quaternion.Euler(0f, -90f, 0f);
        private static readonly Vector3 FinishZoneSize = new Vector3(16f, 8f, 6f);

        [MenuItem("FartTogether/Build Milestone 2 Scene (Race_M2)", priority = 2)]
        public static void BuildSceneMenu()
        {
            BuildMilestone2Scene(openAndSave: true);
        }

        [MenuItem("FartTogether/Fix Animation Clips Legacy Flag (Option B)", priority = 10)]
        public static void FixAnimationClipsLegacyMenu()
        {
            FixAnimationClipsLegacy();
        }

        public static void FixAnimationClipsLegacy()
        {
            string animDir = "Assets/Ripped/AnimationClip";
            if (!Directory.Exists(animDir)) return;

            string[] animFiles = Directory.GetFiles(animDir, "*.anim");
            int fixedCount = 0;
            foreach (string file in animFiles)
            {
                string text = File.ReadAllText(file);
                if (text.Contains("m_Legacy: 1"))
                {
                    text = text.Replace("m_Legacy: 1", "m_Legacy: 0");
                    File.WriteAllText(file, text);
                    AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
                    fixedCount++;
                }
            }
            if (fixedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[RaceM2SceneBuilder] Converted and reimported {fixedCount} AnimationClips from m_Legacy: 1 to m_Legacy: 0.");
            }
        }

        [InitializeOnLoadMethod]
        private static void AutoBuildIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath))
                {
                    BuildMilestone2Scene(openAndSave: true);
                }
            };
        }

        public static void BuildMilestone2Scene(bool openAndSave)
        {
            Debug.Log("[RaceM2SceneBuilder] Starting Milestone 2 Scene Build...");

            // Ensure all clips have m_Legacy: 0 for AnimationClipPlayable compatibility
            FixAnimationClipsLegacy();

            // 1. Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Add Lighting & Sun
            SetupLighting();

            // 3. Instantiate Authentic Track (Level_Platform)
            GameObject trackObj = SetupTrack();

            // 4. Configure Water Flow Zones (Gust_Canyon_1, 2, 3, 7, etc.)
            SetupWaterFlowZones(trackObj);

            // 4.5. Configure Authentic Moving Obstacles, Floating Water Obstacles & Spikes
            SetupObstacles(trackObj);

            // 5. Spawn Raft at authentic spawn coordinates
            GameObject raftObj = SetupRaft(out RaftController raft, out GasInput gasInput);

            // 6. Setup Finish Zone at GM_3D_Finish banner coordinates
            SetupFinishZone();

            // 7. Setup Camera following Raft
            SetupCamera(raftObj.transform);

            // 8. Setup HUD Canvas with speed, timer, gas fuel bars, and debug position
            SetupHUD(raft, gasInput);

            // 9. Save Scene
            if (openAndSave)
            {
                string dir = Path.GetDirectoryName(ScenePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log($"<color=green>[RaceM2SceneBuilder] Successfully saved Milestone 2 scene to {ScenePath}!</color>");
            }
        }

        private static void SetupLighting()
        {
            // Directional Sunlight
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.88f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Ambient lighting settings
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.65f, 0.78f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.60f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.38f, 0.40f);
        }

        private static GameObject SetupTrack()
        {
            GameObject levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPlatformPrefabPath);
            if (levelPrefab == null)
            {
                Debug.LogError($"[RaceM2SceneBuilder] Could not find Level_Platform prefab at {LevelPlatformPrefabPath}!");
                return null;
            }

            // Instantiate prefab at world origin
            GameObject trackObj = (GameObject)PrefabUtility.InstantiatePrefab(levelPrefab);
            trackObj.name = "Level_Platform";
            trackObj.transform.position = Vector3.zero;
            trackObj.transform.rotation = Quaternion.identity;
            trackObj.transform.localScale = Vector3.one;

            // Completely unpack so we can configure colliders and materials without prefab variance issues
            PrefabUtility.UnpackPrefabInstance(trackObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Prepare URP Lit materials with original textures
            Texture2D gradientTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GradientTexPath);
            Texture2D bgrTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BgrTexPath);
            Texture2D foamTex = AssetDatabase.LoadAssetAtPath<Texture2D>(FoamTexPath);

            Material mainPaletMat = GetOrCreateMaterial(MainPaletMatPath, Color.white, transparent: false, mainTex: gradientTex);
            Material envMat = GetOrCreateMaterial(EnvMatPath, Color.white, transparent: false, mainTex: bgrTex);
            
            // Authentic mid-blue water (mostly opaque, high smoothness, white ripple texture Foam2.png)
            Material waterMat = GetOrCreateMaterial(WaterMatPath, new Color(0.12f, 0.58f, 0.86f, 0.95f), transparent: true, mainTex: foamTex, tiling: new Vector2(16f, 16f), smoothness: 0.85f);
            
            Material fogMat = GetOrCreateMaterial(FogMatPath, new Color(0.86f, 0.95f, 1.0f, 0.45f), transparent: true);

            // Step A: Configure and heal all collision meshes (_Col, .Col, WallCol, SolidCollider, SurfaceProbe)
            MeshFilter[] allMeshFilters = trackObj.GetComponentsInChildren<MeshFilter>(true);
            int collidersHealed = 0;

            foreach (MeshFilter mf in allMeshFilters)
            {
                if (mf.sharedMesh == null) continue;

                string objName = mf.name;
                bool isColMesh = objName.EndsWith("_Col", StringComparison.OrdinalIgnoreCase) ||
                                 objName.EndsWith(".Col", StringComparison.OrdinalIgnoreCase) ||
                                 objName.EndsWith("WallCol", StringComparison.OrdinalIgnoreCase) ||
                                 objName.Equals("~SolidCollider", StringComparison.OrdinalIgnoreCase) ||
                                 objName.Equals("~SurfaceProbe", StringComparison.OrdinalIgnoreCase);

                if (isColMesh)
                {
                    MeshCollider mc = mf.GetComponent<MeshCollider>();
                    if (mc == null)
                    {
                        mc = mf.gameObject.AddComponent<MeshCollider>();
                    }
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;

                    // SurfaceProbe is the authentic curved water mesh collider used by WaterChannel for raycast ride height.
                    // Keep it as a trigger on Layer 4 (Water) so the physical raft hull glides across without bumping,
                    // while RaftBuoyancy raycasts against it for authentic suspension & bobbing.
                    if (objName.Equals("~SurfaceProbe", StringComparison.OrdinalIgnoreCase))
                    {
                        mc.isTrigger = true;
                        mf.gameObject.layer = 4; // Water layer
                    }
                    else
                    {
                        mc.isTrigger = false;
                    }
                    collidersHealed++;

                    // Hide mesh renderer on collider-only geometry so it doesn't obstruct visual meshes
                    MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        mr.enabled = false;
                    }
                }
            }
            Debug.Log($"[RaceM2SceneBuilder] Configured {collidersHealed} authentic MeshColliders (including steep slope FT_Static_A1Y1 and water walls).");

            // Step B: Replace missing/broken shaders on visual MeshRenderers with clean URP/Lit materials
            MeshRenderer[] allRenderers = trackObj.GetComponentsInChildren<MeshRenderer>(true);
            int materialsUpdated = 0;

            foreach (MeshRenderer mr in allRenderers)
            {
                // Skip disabled collision renderers
                if (!mr.enabled) continue;

                Material[] existingMats = mr.sharedMaterials;
                if (existingMats == null || existingMats.Length == 0) continue;

                Material[] newMats = new Material[existingMats.Length];
                for (int m = 0; m < existingMats.Length; m++)
                {
                    Material srcMat = existingMats[m];
                    string matName = srcMat != null ? srcMat.name : "";
                    string goName = mr.gameObject.name;

                    if (matName.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        matName.IndexOf("akıntı", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        matName.IndexOf("Wavy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        goName.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        newMats[m] = waterMat;
                    }
                    else if (matName.IndexOf("Environment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             matName.IndexOf("bgr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             goName.IndexOf("Boulder", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        newMats[m] = envMat;
                    }
                    else if (matName.IndexOf("Fog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             matName.IndexOf("Cloud", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        newMats[m] = fogMat;
                    }
                    else
                    {
                        newMats[m] = mainPaletMat;
                    }
                    materialsUpdated++;
                }
                mr.sharedMaterials = newMats;
            }
            Debug.Log($"[RaceM2SceneBuilder] Mapped {materialsUpdated} material instances to URP/Lit with original textures.");

            return trackObj;
        }

        private static void SetupWaterFlowZones(GameObject trackObj)
        {
            if (trackObj == null) return;

            // Flow configuration matching original decompiled values from _reference/PuffPirates/Obstacles/GustZone.cs
            // and PuffTest.unity scene parameters:
            // Gust_Canyon_1: Pos=(-11.03, 1.1, -10.66), Rot=(0, 180, 0), Size=(15, 4, 11), Force=200N (Along/Forward)
            // Gust_Canyon_2: Pos=(18.57, 1.1, 4.34), Rot=(0, 0, 0), Size=(15, 4, 11), Force=100N (Against/Reverse)
            // Gust_Canyon_3: Pos=(48.8, 10, -65.8), Rot=(0, 90, 0), Size=(49.6, 4, 11), Force=100N (Turn)
            // Gust_Canyon_7: Pos=(105.1, 9.9, -91.5), Size=(23.3, 4, 11), Force=60N (Upper Current)

            Transform[] allChildren = trackObj.GetComponentsInChildren<Transform>(true);
            int flowZonesConfigured = 0;

            foreach (Transform t in allChildren)
            {
                string name = t.name;

                if (name.StartsWith("Gust_Canyon_", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Gust", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Gust2", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Gust3", StringComparison.OrdinalIgnoreCase))
                {
                    BoxCollider bc = t.GetComponent<BoxCollider>();
                    if (bc == null)
                    {
                        bc = t.gameObject.AddComponent<BoxCollider>();
                        bc.size = new Vector3(15f, 4f, 11f);
                    }
                    bc.isTrigger = true;

                    WaterFlowZone flowZone = t.GetComponent<WaterFlowZone>();
                    if (flowZone == null)
                    {
                        flowZone = t.gameObject.AddComponent<WaterFlowZone>();
                    }

                    if (name.Equals("Gust_Canyon_1", StringComparison.OrdinalIgnoreCase))
                    {
                        // Forward thrust along water canyon channel
                        flowZone.LocalDirection = Vector3.right;
                        flowZone.FlowForce = 200f; // Original: 200N
                    }
                    else if (name.Equals("Gust_Canyon_2", StringComparison.OrdinalIgnoreCase))
                    {
                        // Counter thrust against the raft motion
                        flowZone.LocalDirection = Vector3.right;
                        flowZone.FlowForce = 100f; // Original: 100N
                    }
                    else if (name.Equals("Gust_Canyon_3", StringComparison.OrdinalIgnoreCase))
                    {
                        flowZone.LocalDirection = Vector3.right;
                        flowZone.FlowForce = 100f; // Original: 100N
                    }
                    else if (name.Equals("Gust_Canyon_7", StringComparison.OrdinalIgnoreCase))
                    {
                        flowZone.LocalDirection = Vector3.right;
                        flowZone.FlowForce = 60f; // Original: 60N
                    }
                    else
                    {
                        flowZone.LocalDirection = Vector3.right;
                        flowZone.FlowForce = 100f;
                    }

                    flowZonesConfigured++;
                }
                else if (name.Equals("FT_DemoWaterChannel", StringComparison.OrdinalIgnoreCase))
                {
                    // Ensure the main water channel volume has a gentle forward drift
                    Transform volumeTr = t.Find("Volume");
                    if (volumeTr != null)
                    {
                        BoxCollider bc = volumeTr.GetComponent<BoxCollider>();
                        if (bc != null) bc.isTrigger = true;

                        WaterFlowZone baseWaterFlow = volumeTr.GetComponent<WaterFlowZone>();
                        if (baseWaterFlow == null)
                        {
                            baseWaterFlow = volumeTr.gameObject.AddComponent<WaterFlowZone>();
                        }
                        baseWaterFlow.LocalDirection = Vector3.forward;
                        baseWaterFlow.FlowForce = 40f; // Gentle baseline water current
                        flowZonesConfigured++;
                    }
                }
            }

            Debug.Log($"[RaceM2SceneBuilder] Configured {flowZonesConfigured} WaterFlowZones in track water channels.");
        }

        private static void SetupObstacles(GameObject trackObj)
        {
            if (trackObj == null) return;

            // Authentic obstacle definitions extracted from Level_Platform YAML:
            // (GameObject Name, Animation Clip GUID, Playback Speed, Phase Seconds)
            var animObstacleDefs = new (string name, string clipGuid, float speed, float phase)[]
            {
                ("FT_C1_TippingHammer", "2ad78df2e0a200b4bbe437ca3200fded", 1.0f, 1.90f),
                ("FT_C1_TippingHammer (4)", "2ad78df2e0a200b4bbe437ca3200fded", 1.0f, 1.91f),
                ("FT_C1_TippingHammer (7)", "2ad78df2e0a200b4bbe437ca3200fded", 1.0f, 1.91f),
                ("FT_C2_PendulumMace", "2d58d664085ed93468abfb4d12552362", 1.0f, 2.43f),
                ("FT_B2_TippingBar", "a6129432ab98eac48be1340abb4976e9", 1.0f, 0.31f),
                ("FT_B5_DoubleSweep", "1899a666e23c4f14e8f7392e9ab6ff37", 0.5f, 0.83f),
                ("FT_B5_DoubleSweep (1)", "1899a666e23c4f14e8f7392e9ab6ff37", 0.5f, 0.83f),
                ("FT_C5_Windmill", "c3a45a9c99df9d84d9d194c5b291eda7", 0.25f, 3.53f),
                ("FT_C5_Windmill (1)", "c3a45a9c99df9d84d9d194c5b291eda7", 0.25f, 3.53f),
                ("FT_C5_Windmill (4)", "c3a45a9c99df9d84d9d194c5b291eda7", 0.25f, 3.53f),
                ("FT_RotorCross", "85dfdb54fb862b24492eaccf1ec4900d", 1.0f, 0.00f),
                ("FT_RotorCross (1)", "85dfdb54fb862b24492eaccf1ec4900d", 1.0f, 0.00f),
                ("FT_RotorCross (2)", "85dfdb54fb862b24492eaccf1ec4900d", 1.0f, 0.00f),
                ("FT_RotorCross (2)_1", "85dfdb54fb862b24492eaccf1ec4900d", 0.4f, 0.00f),
                ("FT_RotorCross (2)_2", "85dfdb54fb862b24492eaccf1ec4900d", 0.4f, 0.00f),
                ("FT_RotorCross (3)", "85dfdb54fb862b24492eaccf1ec4900d", 1.0f, 0.00f),
                ("FT_RotorCross (4)", "85dfdb54fb862b24492eaccf1ec4900d", 1.0f, 0.00f),
                ("FT_SpikeWheel", "7ae8b14fd71318a4ba5ee3b8cfa66630", 1.0f, 4.60f),
                ("ft_animated_obstacle_f10", "8d5bc74bbaacca740a703b64faaec807", 1.0f, 0.00f),
                ("ft_animated_obstacle_f10_1", "8d5bc74bbaacca740a703b64faaec807", 1.2f, 0.00f),
                ("ft_animated_obstacle_f3", "99d0fb74b03de7445af81516811c03c8", 1.0f, 0.00f),
                ("ft_animated_obstacle_f13", "75e589a5808e32340999ba444ac90809", 1.0f, 0.00f),
                ("ft_animated_obstacle_f14", "4730c44e43f302e4caab55b919e9be9e", 1.0f, 0.00f),
                ("ft_animated_obstacle_f23_1", "cf6c21932a26ea147a387b6f9a4fe4ad", 1.0f, 0.00f)
            };

            int animObstaclesConfigured = 0;
            int bobObstaclesConfigured = 0;
            int bounceSpikesConfigured = 0;
            int hazardBarriersConfigured = 0;

            Transform[] allChildren = trackObj.GetComponentsInChildren<Transform>(true);
            var childrenByName = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            foreach (var tr in allChildren)
            {
                if (!childrenByName.ContainsKey(tr.name))
                {
                    childrenByName[tr.name] = tr;
                }
            }

            // 1. Configure all 24 Animated Mechanical Obstacles
            foreach (var def in animObstacleDefs)
            {
                if (childrenByName.TryGetValue(def.name, out Transform tr))
                {
                    string clipPath = AssetDatabase.GUIDToAssetPath(def.clipGuid);
                    AnimationClip clip = !string.IsNullOrEmpty(clipPath) ? AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) : null;

                    if (clip != null)
                    {
                        // Find authentic inner animation root: Visual or GM_3D_ObstacleF*
                        Transform animRoot = tr.Find("Visual") ?? tr.Find("visual");
                        if (animRoot == null)
                        {
                            foreach (Transform child in tr)
                            {
                                if (child.name.StartsWith("GM_3D_ObstacleF", StringComparison.OrdinalIgnoreCase))
                                {
                                    animRoot = child;
                                    break;
                                }
                            }
                        }
                        GameObject rootGo = animRoot != null ? animRoot.gameObject : tr.gameObject;

                        // Disable any conflicting empty/unassigned legacy Animation component from FBX import
                        if (rootGo.TryGetComponent<Animation>(out var legacyAnim))
                        {
                            legacyAnim.playAutomatically = false;
                            legacyAnim.enabled = false;
                        }

                        ObstacleAnimationController animCtrl = tr.GetComponent<ObstacleAnimationController>();
                        if (animCtrl == null) animCtrl = tr.gameObject.AddComponent<ObstacleAnimationController>();

                        animCtrl.Configure(clip, rootGo, def.speed, def.phase);
                        animObstaclesConfigured++;

                        // Ensure all child colliders under this animated obstacle are active & solid
                        Collider[] cols = tr.GetComponentsInChildren<Collider>(true);
                        foreach (Collider c in cols)
                        {
                            c.enabled = true;
                        }
                    }
                }
            }

            // 2. Configure Floating Water Obstacles (FakeWaveBob instances) & Spikes & Hazards
            foreach (Transform tr in allChildren)
            {
                if (tr.name.StartsWith("ft_animated_obstacle_f11", StringComparison.OrdinalIgnoreCase))
                {
                    FloatingBobObstacle bob = tr.GetComponent<FloatingBobObstacle>();
                    if (bob == null) bob = tr.gameObject.AddComponent<FloatingBobObstacle>();

                    bob.Amplitude = 0.25f;
                    bob.Speed = 2.5f;
                    bob.Wavelength = 6.0f;
                    bobObstaclesConfigured++;
                }
                else if (tr.name.StartsWith("Spike", StringComparison.OrdinalIgnoreCase) &&
                         (tr.name.Equals("Spike", StringComparison.OrdinalIgnoreCase) ||
                          tr.name.Equals("Spike_4", StringComparison.OrdinalIgnoreCase) ||
                          tr.name.Equals("Spike_5", StringComparison.OrdinalIgnoreCase) ||
                          tr.name.Equals("Spike_6", StringComparison.OrdinalIgnoreCase)))
                {
                    BouncingSpike spike = tr.GetComponent<BouncingSpike>();
                    if (spike == null) spike = tr.gameObject.AddComponent<BouncingSpike>();

                    spike.KnockbackImpulse = 2.0f;
                    bounceSpikesConfigured++;
                }
                else if (tr.name.Equals("~KillTrigger", StringComparison.OrdinalIgnoreCase) ||
                         tr.name.IndexOf("Thorn", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    HazardBarrier barrier = tr.GetComponent<HazardBarrier>();
                    if (barrier == null) barrier = tr.gameObject.AddComponent<HazardBarrier>();

                    barrier.InvulnSeconds = 2.0f;
                    barrier.RespawnAtStart = false;
                    hazardBarriersConfigured++;
                }
            }

            Debug.Log($"[RaceM2SceneBuilder] Configured Obstacles: {animObstaclesConfigured} Animated Mechanical Obstacles, {bobObstaclesConfigured} Floating Water Obstacles, {bounceSpikesConfigured} Bouncing Spikes, {hazardBarriersConfigured} Hazard Barriers.");
        }

        private static GameObject SetupRaft(out RaftController raft, out GasInput gasInput)
        {
            GameObject raftObj = new GameObject("Raft");
            raftObj.transform.position = RaftSpawnPosition;
            raftObj.transform.rotation = RaftSpawnRotation;

            // Raft Rigidbody with authentic physics values from BoatPhysicsConfig.cs & BoatDefinition.cs
            Rigidbody rb = raftObj.AddComponent<Rigidbody>();
            rb.mass = 40f;              // BoatDefinition.Mass = 40kg
            rb.linearDamping = 0.4f;     // Low drag water glide
            rb.angularDamping = 2.0f;    // Responsive yaw damping
            rb.centerOfMass = new Vector3(0f, -0.7f, 0f); // BoatPhysicsConfig._centerOfMassY = -0.7f (bottom heavy keel)
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Visual Model (child with Euler 90, 0, 0 so the round raft lies completely flat on the water)
            // Quoted from Boat.prefab: Model pos=(0, -0.87, 0), rot=(90, 0, 0)
            GameObject visualObj = new GameObject("VisualModel");
            visualObj.transform.SetParent(raftObj.transform, false);
            visualObj.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            visualObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Lays flat!
            visualObj.transform.localScale = Vector3.one;

            Mesh boatMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BoatMeshPath);
            MeshFilter mf = visualObj.AddComponent<MeshFilter>();
            mf.sharedMesh = boatMesh;

            // Original material with Gradient Pallete.png (green deck, cream ring, cyan bumper, orange ropes)
            Texture2D gradientTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GradientTexPath);
            Material raftMat = GetOrCreateMaterial(RaftMatPath, Color.white, transparent: false, mainTex: gradientTex);
            MeshRenderer mr = visualObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = raftMat;

            // Raft Collider (child with Euler 90, 0, 0 matching Boat.prefab GM_3D_BoatMain_Col)
            GameObject colObj = new GameObject("GM_3D_BoatMain_Col");
            colObj.transform.SetParent(raftObj.transform, false);
            colObj.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            colObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Lays flat!
            colObj.transform.localScale = Vector3.one;

            Mesh colMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BoatColMeshPath);
            MeshCollider mc = colObj.AddComponent<MeshCollider>();
            mc.sharedMesh = colMesh != null ? colMesh : boatMesh;
            mc.convex = true;

            // RaftController
            raft = raftObj.AddComponent<RaftController>();

            // GasInput
            gasInput = raftObj.AddComponent<GasInput>();

            // RaftBuoyancy (authentic spring-damper ride and gentle wave bobbing)
            RaftBuoyancy buoyancy = raftObj.AddComponent<RaftBuoyancy>();

            // Setup 4 invisible seat transforms for thrust physics (NO colored cylinder markers)
            string[] seatNames = { "Seat 1 (Rear-Right)", "Seat 2 (Rear-Left)", "Seat 3 (Front-Left)", "Seat 4 (Front-Right)" };
            float[] seatAngles = { 135f, 225f, 315f, 45f };

            RaftController.SeatPoint[] seatPoints = new RaftController.SeatPoint[4];
            for (int i = 0; i < 4; i++)
            {
                float rad = seatAngles[i] * Mathf.Deg2Rad;
                Vector3 localPos = new Vector3(Mathf.Sin(rad) * 1.3f, 0f, Mathf.Cos(rad) * 1.3f);

                GameObject seatPointObj = new GameObject($"SeatPoint_P{i + 1}");
                seatPointObj.transform.SetParent(raftObj.transform, false);
                seatPointObj.transform.localPosition = localPos;
                seatPointObj.transform.localRotation = Quaternion.identity;

                seatPoints[i] = new RaftController.SeatPoint
                {
                    name = seatNames[i],
                    angleDeg = seatAngles[i],
                    seatTransform = seatPointObj.transform
                };
            }

            SerializedObject soRaft = new SerializedObject(raft);
            SerializedProperty seatsProp = soRaft.FindProperty("_seats");
            if (seatsProp != null && seatsProp.isArray)
            {
                for (int i = 0; i < 4; i++)
                {
                    SerializedProperty seatElem = seatsProp.GetArrayElementAtIndex(i);
                    seatElem.FindPropertyRelative("name").stringValue = seatPoints[i].name;
                    seatElem.FindPropertyRelative("angleDeg").floatValue = seatPoints[i].angleDeg;
                    seatElem.FindPropertyRelative("seatTransform").objectReferenceValue = seatPoints[i].seatTransform;
                }
                soRaft.ApplyModifiedProperties();
            }

            return raftObj;
        }

        private static void SetupFinishZone()
        {
            GameObject finishObj = new GameObject("FinishZone");
            finishObj.transform.position = FinishPosition;
            finishObj.transform.rotation = FinishRotation;

            BoxCollider bc = finishObj.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = FinishZoneSize;

            finishObj.AddComponent<FinishZone>();

            // Add visible semi-transparent green cube so finish line is clearly distinguishable
            GameObject visualCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualCube.name = "FinishVisualVolume";
            visualCube.transform.SetParent(finishObj.transform, false);
            visualCube.transform.localPosition = Vector3.zero;
            visualCube.transform.localRotation = Quaternion.identity;
            visualCube.transform.localScale = FinishZoneSize;

            Collider col = visualCube.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

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
            cam.farClipPlane = 1200f;

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
            TextMeshProUGUI debugText = CreateText(canvasObj.transform, "DebugText", "POS: (-15.5, 1.5, 90.8) | KEYS: None", font, 20, TextAlignmentOptions.TopLeft);
            RectTransform debugRect = debugText.rectTransform;
            debugRect.anchorMin = new Vector2(0f, 1f);
            debugRect.anchorMax = new Vector2(0f, 1f);
            debugRect.pivot = new Vector2(0f, 1f);
            debugRect.anchoredPosition = new Vector2(40f, -88f);
            debugRect.sizeDelta = new Vector2(800f, 35f);
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

        private static Material GetOrCreateMaterial(string path, Color color, bool transparent = false, Texture2D mainTex = null, Vector2? tiling = null, float smoothness = 0.15f)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit == null) urpLit = Shader.Find("Standard");

                mat = new Material(urpLit);
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            if (mainTex != null)
            {
                mat.SetTexture("_BaseMap", mainTex);
                if (tiling.HasValue)
                {
                    mat.SetTextureScale("_BaseMap", tiling.Value);
                }
            }
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f); // 1 = Transparent
                mat.SetFloat("_Blend", 0f);   // 0 = Alpha
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                mat.SetFloat("_Surface", 0f);
                mat.SetInt("_SrcBlend", (int)BlendMode.One);
                mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.renderQueue = (int)RenderQueue.Geometry;
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            mat.SetFloat("_Smoothness", smoothness);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
