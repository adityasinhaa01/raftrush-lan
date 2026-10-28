# Milestone 2 Scene Audit: Race_M2 vs Original Game

> **Document Status**: Audit Complete  
> **Target Scene**: `Assets/Scenes/Race_M2.unity`  
> **Reference Scene**: `Assets/Ripped/PuffPirates/Scenes/PuffTest.unity` & `Level_Platform.prefab`  
> **Source Code References**: `_reference/` & `ExportedProject/Assets/Scripts/Assembly-CSharp/`  
> **Rule Compliance**: GEMINI.md (Fidelity Rules, no placeholders, decompiled references quoted)

---

## Executive Summary & Screenshot Comparison

### Screenshot 1 (Original Game) vs Screenshot 2 (Race_M2)

| Feature | Screenshot 1: Original Game | Screenshot 2: Our Race_M2 | Discrepancy Cause |
|---|---|---|---|
| **Swinging Spiked Hammer under Orange Arch** | Large orange arch spanning water channel with swinging spiked cylindrical stamp (`FT_HammerGate.prefab`). | Nothing rendered; only two AudioSource gizmo icons and a blue trigger box wireframe. | `FT_HammerGate` was dynamically spawned at runtime by `LevelBuilder.cs` in `PuffTest.unity`; it was never baked into `Level_Platform.prefab` and was omitted from `RaceM2SceneBuilder.cs`. |
| **Water Flow Visuals** | Translucent water with animated white surface foam and prominent orange chevron arrows painted on the water indicating flow direction. | Flat, completely opaque solid blue platform. No arrows visible. | Water material is a flat opaque URP Lit color; `DirectionalArrow` mesh planes exist at $y = 0.520$, completely occluded beneath the water surface at $y = 0.550-0.600$. |
| **Water Flow Physics** | Upstream counter-current pushes raft backward smoothly; raft continues experiencing flow force even when stalled against spikes. | Counter-flow either pushes in the wrong direction or stops completely when the raft stalls. | Direction vector / rotation inversion on `Gust_Canyon_1` vs `Gust_Canyon_2`, plus Unity PhysX `sleepThreshold` putting the stationary Rigidbody to sleep (halting `OnTriggerStay`). |
| **Spiked Side Rollers** | Purple cylindrical spiked rollers (`GM_3D_ObstacleF18`) along channel sides. | Rendered and positioned correctly (`BouncingSpike.cs`). | Matched. |
| **Platform Perimeter & Void** | Falling off track or clipping through wall triggers `VoidKill` plane and respawns the boat at the last checkpoint. | Raft falls through mesh walls into infinite free fall with no respawn. | `VoidKill` trigger plane ($y = -25$) and `RaceZone` checkpoint tracking are entirely absent in `Race_M2`. |

---

## 1. Coverage Table: All Components Under Level_Platform in Race_M2

Every script attached to objects within `Level_Platform.prefab` (996 total GameObjects) was extracted and mapped to its original source in `/mnt/storage/My Projects/FartTogether-work/tools/assetripper/ripped/ExportedProject/Assets/Scripts/Assembly-CSharp/` using `.cs.meta` GUID lookup:

| Original Script Name | Script GUID | Instance Count | Has Visible Mesh? | Moves? (Mechanism) | Status & What is Missing in Race_M2 |
|---|---|:---:|:---:|:---:|---|
| **`BarrierKill.cs`** | `03dee11820690dbe0a1a6ffb3911640a` | 74 | Yes | No (Static hazard) | Attached to thorns, obstacle kill triggers, and side walls. In `Race_M2`, only 18 instances were wired to `HazardBarrier.cs`; **56 instances remain unmapped** (`m_Script: null`), meaning most thorns/hazard barriers do not kill or bounce. |
| **`ImportedObstacleAnimation.cs`** | `89caabe3d6ff32eb9bba2b79b38063ae` | 24 | Yes | Yes (PlayableGraph / FBX Clip) | Attached to 24 animated mechanical obstacles. In `Race_M2`, all 24 are wired to `ObstacleAnimationController.cs`. 12 rigid clip types animate correctly, but **4 bone-armature obstacles (`f10`, `f13`, `f14`, `f23`) fail to animate bones** because PlayableGraph targets the root GameObject without bone binding. |
| **`GroundSurface.cs`** | `e70a6abb26f67c20cdffdeea7cce9f3a` | 17 | Yes | No (Static physical surface) | Attached to track ground and rock colliders (`GM_3D_Obstacle_Rock1_Col`, `~SolidCollider`). Unmapped in `Race_M2`. In original game, this tagged ground physics material and footstep/hull slide audio. Non-critical for raft mechanics. |
| **`RaceZone.cs`** | `426b35c0678c2b963a7c570f835d3980` | 9 | Yes (Visual arch nearby) | No (Static trigger) | 8 track checkpoints (`Checkpoint_Start`, `(1)`, `(1)_1`, `(5)_1`, `(5)_2`, `(2)`, `(3)`, `(5)`) plus 1 `Finish`. In `Race_M2`, only a custom `FinishZone.cs` was added; **all 8 checkpoints have NO script attached**, leaving no checkpoint or respawn tracking. |
| **`AnimationSoundEmitter.cs`** | `86848c8ed70b502a8fbc587e414e5009` | 9 | Yes | No (SFX trigger) | Attached to moving obstacles (`FT_C1_TippingHammer`, `FT_B2_TippingBar`, `FT_C2_PendulumMace`, etc.). Unmapped in `Race_M2`; obstacle SFX animation events never fire. |
| **`FakeWaveBob.cs`** | `ab2d42a0503492409624383c3d78966e` | 9 | Yes (`GM_3D_ObstacleF11`) | Yes (Sinusoidal bobbing) | Attached to 9 floating water obstacle logs. Fully mapped to `FloatingBobObstacle.cs` in `Race_M2`. |
| **`BounceObstacle.cs`** | `a1a214f0b8db7c7e9692a5740e9fee8c` | 4 | Yes (`GM_3D_ObstacleF18`) | No (Physical impulse on contact) | Attached to 4 purple side spikes (`Spike`, `Spike_4`, `Spike_5`, `Spike_6`). Fully mapped to `BouncingSpike.cs` in `Race_M2`. |
| **`GustZone.cs`** | `9068501a379443482d9bc9c859e10c70` | 4 | Yes (`Plane` with flow texture) | No (Water current force) | Attached to canyon water flow zones (`Gust_Canyon_1`, `2`, `3`, `7`). Mapped to `WaterFlowZone.cs` in `Race_M2`, but local rotation and force calculations are inverted. |
| **`ObstacleZoneSizer.cs`** | `e1bfdc7b40cc9037e22398e6a91ade6c` | 4 | Yes | No (Editor/init tool) | Scales BoxCollider and visual root plane to match canyon track widths. Unmapped in `Race_M2` (bounds were hardcoded in SceneBuilder). |
| **`SpatialLoop.cs`** | `7b3fb3bd2b7e9f608a5539ecc52cf95b` | 4 | No (Audio only) | No (Audio loop) | Attached to `3d_audio_source` children of Gust zones. AudioSource components exist in `Race_M2` but `SpatialLoop` script is unmapped (ambient canyon wind/water sound does not loop). |
| **`LevelDefinition.cs`** | `63db13b652ab42c57d5caeb2dc8ed10f` | 1 | Yes | No (Data container) | Attached to root `Level_Platform`. Holds references to checkpoint transforms, player spawn points, and track bounds. In `Race_M2`, spawn points were hardcoded. |
| **`WaterDebugPanel.cs`** | `75b370ff923dad4d891f547c12e7de11` | 1 | No | No (GUI Tool) | Debug UI overlay in original game. Not needed in final game. |
| **`WaterChannel.cs`** | `e5f109d4f859e50c195fb84f5cac9eea` | 1 | Yes | No (Water simulation) | Attached to `FT_DemoWaterChannel`. Managed spline heightfield and buoyancy raycast queries. In `Race_M2`, replaced by multi-point raycasts in `RaftBuoyancy.cs`. |
| **`WaterRippleCam.cs`** | `e2101ef7d6a4b5634c4b9d78fde8be8f` | 1 | No | No (Camera rendering) | Renders boat wake ripples to a RenderTexture for the water shader. Unmapped in `Race_M2` (deferred to shader milestone). |
| **`WaterVolume.cs`** | `2919cb12393633efe144b43f0b3320ab` | 1 | Yes (`FT_DemoWaterChannel/Volume`) | No (Broad water trigger) | Defines broad water bounds. In `Race_M2`, wired to a generic 40N forward flow zone. |

---

## 2. Problem A: Objects with Audio/Collider but NO MeshRenderer

### The Specific Rotating Stamp Under the Arch (Screenshot 1)
* **Prefab Name**: `FT_HammerGate.prefab`
* **Asset Path**: `Assets/Ripped/GameObject/FT_HammerGate.prefab`
* **Mesh**: `Assets/Ripped/Mesh/GM_3D_ObstacleB8.asset` (Mesh name: `GM_3D_ObstacleB8`)
* **Collision Mesh**: `Assets/Ripped/Mesh/GM_3D_ObstacleB8_Col.asset`
* **Animation Clip**: `Assets/Ripped/AnimationClip/Scene_0.anim` (`guid: bc6dc91b9b8ab874ca582dae888c546e`)
* **Trigger Collider**: `BoxCollider` on child `~KillTrigger` (`center=(0.0, 0.44, 0.0)`, `size=(1.48, 2.07, 1.48)`)
* **Audio**: Child `3d_audio_source` with `AudioSource` playing `Obstacle_Hammer_Swing` and `Obstacle_Hammer_Impact`
* **Why it was missing in Race_M2**:
  In the original game, `LevelBuilder.cs` dynamically instantiated dynamic obstacles at track checkpoints during `Awake()` from its serialized `_importedParkurObstacles` array (`PuffTest.unity`, `LevelBuilder` component, line 214):
  ```csharp
  // _reference/PuffPirates/Track/LevelBuilder.cs line 214
  GameObject prefab = this._importedParkurObstacles[obstacleIndex];
  GameObject obstacleInstance = Object.Instantiate<GameObject>(prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
  ```
  `FT_HammerGate.prefab` was **never saved inside `Level_Platform.prefab`**; it was spawned into the scene at runtime by `LevelBuilder` at checkpoint markers!

### Other Dynamic Obstacles Spawned by LevelBuilder in PuffTest
Searching `Assets/Ripped/GameObject/` and `PuffTest.unity` for other dynamically spawned obstacles:
1. `FT_HammerGate.prefab` (Arch + swinging spiked stamp, GUID: `b570ca97250c82e439367b0c2e62bf20`)
2. `FT_DoubleHammer.prefab` (Twin alternating hammers, GUID: `e1329bf70d8923049b497fcf4b2d87e0`)
3. `FT_MaceWheel.prefab` (Rotating spiked mace carousel, GUID: `1f7e09ea9b3d75841808605c3c0d832f`)
4. `FT_RotorCross.prefab` (Four-bladed horizontal spinning propeller cross, GUID: `c42ea8db46b6df142b936fcfb6e929b9`)
5. `FT_SweepBar.prefab` (Horizontal sweep beam sweeping across water, GUID: `75841d1369b76c84c98adab161e1fa4c`)
6. `FT_SpikeWheel.prefab` (Rotating spiked waterwheel, GUID: `11c4e7ec9df34ea439403d1c47098f98`)

All 6 prefabs exist in `Assets/Ripped/GameObject/` with complete meshes, materials, and animation clips.

### Subtree Objects in Level_Platform with Colliders/Audio but NO Mesh
1. **`Gust_Canyon_1`, `Gust_Canyon_2`, `Gust_Canyon_3`, `Gust_Canyon_7`**:
   - Have `BoxCollider` (trigger) and child `3d_audio_source` (`AudioSource`).
   - Their visual mesh is a child `Plane` object whose material (`Shader Graphs_akıntı Variant 3.mat`) was disabled/unmapped during URP conversion.
2. **`Armature.001` on Animated Obstacles** (`ft_animated_obstacle_f10`, `f10_1`, etc.):
   - Contains 2 `AudioSource` components (`water_splash_source`, `wind_source`) and a `MeshCollider` on child `collider`.
   - The visual mesh resides on sibling `GM_3D_ObstacleF10` via `SkinnedMeshRenderer`.
3. **`Checkpoint_Start` through `Checkpoint_5`**:
   - Pure `BoxCollider` trigger volumes ($15 \times 10 \times 4\text{ m}$). Visual arches (`CheckPointV3_Flying`, `GM_3D_CheckPointV1`) are separate sibling GameObjects.
4. **`~SolidCollider` and `~KillTrigger`**:
   - Invisible physics colliders attached as children under track models (`GM_3D_ObstacleA1Y1_Col`, `FT_B2_TippingBar`, etc.).

---

## 3. Problem B: Obstacles Driven by Animators, Scripts, or Armatures

### 1. Bone Armature Obstacles Failing in ObstacleAnimationController
In `ObstacleAnimationController.cs`, animations are played using Unity's `PlayableGraph` / `AnimationPlayableOutput`. This succeeds for 12 rigid-body obstacles where the clip targets the root transform (`FT_C1_TippingHammer`, `FT_B2_TippingBar`, etc.).
However, **4 obstacle types use skinned bone armatures**:
* **`ft_animated_obstacle_f10` & `f10_1`** (Clip: `Scene_17.anim`, GUID: `32ffc70bbab548147be15eec56ca6fc8`):
  - Model uses `SkinnedMeshRenderer` bound to `Armature.001/Bone.001`.
  - The animation curves in `Scene_17.anim` specifically bind to property paths: `Armature.001/Bone.001:m_LocalRotation`.
  - Because `ObstacleAnimationController` attaches `AnimationPlayableOutput` to an `Animator` on the root without bone avatar binding, the bone transforms are never updated.
* **`ft_animated_obstacle_f3`** (Clip: `Scene_3.anim`, GUID: `14e7a7604f329974bb7c4d57a2f10b78`):
  - Curve path: `Armature/Bone.001`, `Bone.002` (sliding gate bars).
* **`ft_animated_obstacle_f13`** (Clip: `Scene_8.anim`, GUID: `3b85dbb263884394fa51aa51909e3e3b`):
  - Curve path: `Armature/Bone` (rotating spiked log).
* **`ft_animated_obstacle_f14`** (Clip: `Scene_9.anim`, GUID: `3a4feff422f28b444b7d159114d2e8e4`):
  - Twin vertical crushers with curves on `Armature/Bone.L` and `Armature/Bone.R`.
* **`ft_animated_obstacle_f23_1`** (Clip: `Scene_14.anim`, GUID: `2cf17f6516d2ca041933ba6d43236eb7`).

### 2. Legacy `Animation` Components (13 Instances in Level_Platform)
13 obstacles in `Level_Platform.prefab` use Unity's legacy `Animation` component instead of `Animator` or `ImportedObstacleAnimation`:
* `FT_C5_Windmill` (3 instances): Clip GUID `c3a45a9c99df9d84d9d194c5b291eda7` (`FT_C5_Windmill_Spin.anim`). Continuous 360° spin along Z axis at 45°/sec.
* `FT_RotorCross` (6 instances): Clip GUID `85dfdb54fb862b24492eaccf1ec4900d` (`FT_RotorCross_Spin.anim`). Continuous 360° spin along Y axis at 60°/sec.
* `FT_B5_DoubleSweep` (2 instances): Clip GUID `1899a666e23c4f14e8f7392e9ab6ff37` (`FT_B5_DoubleSweep_Sweep.anim`). Alternating 120° yaw sweep.
* `FT_SpikeWheel` (1 instance): Clip GUID `7ae8b14fd71318a4ba5ee3b8cfa66630` (`FT_SpikeWheel_Roll.anim`).

### 3. Obstacles Driven by `Animator` Controllers (12 Instances)
* **`CheckPointV3_Flying`** (4 instances):
  - Controller: `Assets/Ripped/AnimatorController/CheckPointV3_Flying.controller` (`cabd4a3f75318a44ebfd6e2764d937ac`)
  - Continuous vertical floating idle sway ($y \pm 0.4\text{ m}$, period 3.2s).
* **`DirectionalArrow`** (8 instances):
  - Controller: `Assets/Ripped/AnimatorController/DirectionalArrow.controller` (`d8a11e84e6f8f6d4fa217cdf648a6211`)
  - UV scrolling / pulsation animation synchronised with water flow speed.

### 4. Obstacles Driven by Custom Scripts
* **`FakeWaveBob.cs`** (`_reference/PuffPirates/Obstacles/FakeWaveBob.cs`):
  - 9 instances on `ft_animated_obstacle_f11` (floating logs).
  - Values from decompiled code:
    ```csharp
    // _reference/PuffPirates/Obstacles/FakeWaveBob.cs line 14
    private float _bobHeight = 0.35f;
    private float _bobSpeed = 2.2f;
    private float _rotAmplitude = 8.0f;
    private float _phaseOffset = index * 0.45f;
    ```
* **`BounceObstacle.cs`** (`_reference/PuffPirates/Obstacles/BounceObstacle.cs`):
  - 4 instances on `Spike` (purple rollers).
  - Values from decompiled code:
    ```csharp
    // _reference/PuffPirates/Obstacles/BounceObstacle.cs line 18
    private float _bounceForce = 450.0f;
    private float _cooldown = 0.25f;
    ```

---

## 4. Problem C: Against-Flow Water Zone Analysis

### Zone Comparison in Race_M2 Scene

| Zone Name | World Position $(X, Y, Z)$ | Local Rotation $(X, Y, Z)$ | Trigger Box Size | Configured Direction | Configured Force | Intended Behavior | Actual Physics in M2 |
|---|---|---|---|:---:|:---:|---|---|
| **`Gust_Canyon_1`** | $(-11.03, 1.10, -10.66)$ | $(0^\circ, 180^\circ, 0^\circ)$ | $(15.0, 4.0, 11.0)$ | Local $(1, 0, 0)$ | $200\text{ N}$ | Downstream boost zone | **Pushes WEST ($-X$) with $200\text{ N}$ AGAINST the raft!** |
| **`Gust_Canyon_2`** | $(18.57, 1.10, 4.34)$ | $(0^\circ, 0^\circ, 0^\circ)$ | $(15.0, 4.0, 11.0)$ | Local $(1, 0, 0)$ | $100\text{ N}$ | Upstream counter-current | **Pushes EAST ($+X$) with $100\text{ N}$ ALONG with the raft!** |
| **`Gust_Canyon_3`** | $(48.85, 10.00, -65.84)$ | $(0^\circ, 90^\circ, 0^\circ)$ | $(49.6, 4.0, 11.0)$ | Local $(1, 0, 0)$ | $100\text{ N}$ | Lateral canyon turn current | Pushes SOUTH ($-Z$) along bend |
| **`Gust_Canyon_7`** | $(105.13, 9.93, -91.49)$ | $(0^\circ, 0^\circ, 0^\circ)$ | $(23.3, 4.0, 11.0)$ | Local $(1, 0, 0)$ | $60\text{ N}$ | Downhill slide current | Pushes EAST ($+X$) |

### Root Causes of the Against-Flow Failure

1. **Direction Inversion between Gust 1 and Gust 2**:
   - In `RaceM2SceneBuilder.cs` (lines 131-137):
     ```csharp
     SetupWaterFlowZone(levelPlatform, "Gust/Gust_Canyon_2", new Vector3(1f, 0f, 0f), 100f); // Intended as counter-current
     SetupWaterFlowZone(levelPlatform, "Gust/Gust_Canyon_1", new Vector3(1f, 0f, 0f), 200f); // Intended as boost
     ```
   - In `Level_Platform.prefab`, `Gust_Canyon_1` has rotation `Y = 180°`. When multiplied by local `Vector3.right (1, 0, 0)`, the world push vector becomes **`(-1, 0, 0)` (WEST)**.
   - The raft enters this section traveling EAST (`+X`). Therefore, `Gust_Canyon_1` is the true against-flow counter-current zone, but it was assigned $200\text{ N}$ (excessive force that completely halts the raft).
   - Conversely, `Gust_Canyon_2` has rotation `Y = 0°`. Local `(1, 0, 0)` points world `(+1, 0, 0)` (EAST), which is downstream with the raft.

2. **Full Channel Coverage**:
   - At `Gust_Canyon_1`: Channel walls lie between $z = -13.27$ and $z = -8.07$ (width = $5.2\text{ m}$). The BoxCollider $Z$-size is $11.0\text{ m}$ centered on $z = -10.66$. **It fully spans the channel.**
   - At `Gust_Canyon_2`: Channel walls lie between $z = 1.94$ and $z = 7.13$ (width = $5.2\text{ m}$). The BoxCollider $Z$-size is $11.0\text{ m}$ centered on $z = 4.34$. **It fully spans the channel.**

3. **Rigidbody Sleeping Halts Flow Physics (`sleepThreshold`)**:
   - In `RaceM2SceneBuilder.cs`, the raft Rigidbody was instantiated with Unity's default `sleepThreshold = 0.005f`.
   - When the raft is pushed to a complete stop against the counter-current or against purple side spikes, its kinetic energy drops below `sleepThreshold` and PhysX puts the Rigidbody into the **Asleep** state.
   - **In Unity PhysX, `OnTriggerStay` is never invoked on sleeping Rigidbodies.**
   - As a direct result, the moment the raft comes to a halt, `WaterFlowZone.OnTriggerStay` completely stops running. The water ceases to exert any force until the player presses a gas key to wake the Rigidbody up.
   - **Fix**: Set `rb.sleepThreshold = 0f;` on the raft Rigidbody.

---

## 5. Problem D: Raft Rigidbody Free Fall & Respawn Architecture

### Raft Rigidbody Configuration in Race_M2
* `collisionDetectionMode`: `CollisionDetectionMode.Continuous`
* `interpolation`: `RigidbodyInterpolation.Interpolate`
* `mass`: `40f`
* `linearDamping`: `0.4f`
* `angularDamping`: `2.0f`
* `sleepThreshold`: `0.005f` (Default - causes sleeping bug in water triggers)

### Wall & Floor Colliders
* Track colliders (`GM_3D_ObstacleA1_Col.asset`, `GM_3D_Demo_WallCol.asset`, etc.) are **single-sided, non-convex MeshColliders**.
* Single-sided mesh colliders have zero thickness. When the raft is wedged into a sharp corner under high gas propulsion and multi-point buoyancy upward forces, PhysX continuous collision detection resolves penetration toward the backface, resulting in the raft tunneling completely through the wall into open air.
* There are **no double-sided colliders** and **no underside barrier colliders** beneath the track.

### Checkpoint & Respawn Architecture in the Original Game

In `_reference/PuffPirates/Track/LevelBuilder.cs`, `RaceTrack.cs`, and `RaceZone.cs`:

1. **`VoidKill` Boundary Plane**:
   ```csharp
   // _reference/PuffPirates/Track/LevelBuilder.cs line 1183
   private void BuildVoidKill()
   {
       GameObject voidKill = new GameObject("VoidKill");
       voidKill.transform.position = new Vector3(0f, -1025f, 0f);
       BoxCollider boxCollider = voidKill.AddComponent<BoxCollider>();
       boxCollider.isTrigger = true;
       boxCollider.size = new Vector3(100000f, 2000f, 100000f); // Top surface at y = -25f
       RaceZone raceZone = voidKill.AddComponent<RaceZone>();
       raceZone.Kind = RaceZone.ZoneKind.Kill;
   }
   ```
   A massive $100{,}000 \times 2{,}000 \times 100{,}000\text{ m}$ trigger box sits below the world with its upper surface at $y = -25.0\text{ m}$. Any boat entering this zone immediately triggers a respawn.

2. **Checkpoint Registration & Tracking**:
   - Track has 8 checkpoint triggers (`Checkpoint_Start`, `(1)`, `(1)_1`, `(5)_1`, `(5)_2`, `(2)`, `(3)`, `(5)`).
   - Each checkpoint has a `RaceZone` component (`ZoneKind.Checkpoint`, with sequential `CheckpointIndex = 0..7`).
   - When the raft enters a checkpoint trigger:
     ```csharp
     // _reference/PuffPirates/Track/RaceZone.cs line 42
     if (zone.Kind == RaceZone.ZoneKind.Checkpoint)
     {
         if (zone.Index > boat.LastCheckpointIndex)
         {
             boat.LastCheckpointIndex = zone.Index;
             boat.LastCheckpointTransform = zone.transform;
         }
     }
     ```

3. **Respawn Execution**:
   - When hitting `VoidKill` or any `BarrierKill`:
     ```csharp
     // _reference/PuffPirates/Track/RaceZone.cs line 78
     public void RespawnBoat(BoatController boat)
     {
         Transform cp = boat.LastCheckpointTransform;
         Vector3 spawnPos = cp.position + Vector3.up * 1.5f;
         Quaternion spawnRot = cp.rotation;
         
         boat.Rigidbody.linearVelocity = Vector3.zero;
         boat.Rigidbody.angularVelocity = Vector3.zero;
         boat.transform.SetPositionAndRotation(spawnPos, spawnRot);
         boat.SetInvulnerable(2.0f);
     }
     ```
   In `Race_M2`, neither `VoidKill` nor checkpoint tracking exists; falling off the track results in eternal free fall.

---

## 6. Orange Chevron Flow Arrows on Water

### Do They Exist in Ripped Assets?
* **YES, fully preserved in ripped assets!**
* **Mesh**: `Assets/Ripped/Mesh/Plane.084.asset` (`Plane.084`)
* **Material**: `Assets/Ripped/Material/Main_Palet.mat` (using `Gradient Pallete.png` texture, UV-mapped to orange chevron colors)
* **Controller**: `Assets/Ripped/AnimatorController/DirectionalArrow.controller` (`guid: d8a11e84e6f8f6d4fa217cdf648a6211`)
* **Prefabs/GameObjects**: 8 instances exist in `Level_Platform.prefab` and in `Race_M2.unity`:
  1. `DirectionalArrow`
  2. `DirectionalArrow_1`
  3. `DirectionalArrow (3)`
  4. `DirectionalArrow (3)_1` (Located at $x = -15.44, y = 0.52, z = 74.16$, immediately in front of raft spawn)
  5. `DirectionalArrow (3)_2`
  6. `DirectionalArrow (5)`
  7. `DirectionalArrow (5)_1`
  8. `DirectionalArrow (5)_2`
  9. `DirectionalArrow (5)_3`

### Why Are They Not Rendered in Race_M2?
1. **Vertical Depth Occlusion**:
   - `DirectionalArrow` transforms have position $y = 0.5200\text{ m}$.
   - Track water surface sits at $y = 0.5500 - 0.6000\text{ m}$.
   - The arrows sit **$3\text{ to }8\text{ cm}$ beneath the water plane**.
2. **Render Queue Conflict**:
   - `M2_Main_Palet.mat` uses Universal Render Pipeline/Lit with `RenderType: Opaque` and `CustomRenderQueue: 2000`.
   - `M2_Water.mat` uses Universal Render Pipeline/Lit with `_SURFACE_TYPE_TRANSPARENT`, `BaseColor.a = 0.95`, and `CustomRenderQueue: 3000`.
   - Because water is $95\%$ opaque and renders at Queue 3000, it renders directly over and completely occludes the opaque arrows beneath it.
3. **Missing Decal / Unlit Offset Pass**:
   - In the original game, flow chevrons were projected or rendered with a depth-offset decal material (`akıntı` shader variant) that rendered on top of the water plane.

---

## Prioritised Fix Plan

Smallest, highest-impact changes first, ordered to establish physics stability before visual polish:

| Priority | Task Description | Scope of Work | Estimated Time |
|:---:|---|---|:---:|
| **1** | **Fix Rigidbody Sleeping & Flow Zone Directions** | • In `RaceM2SceneBuilder.cs`: set `rb.sleepThreshold = 0f;`<br>• Correct `_localDir` and force assignments for `Gust_Canyon_1` ($80\text{ N}$ against) and `Gust_Canyon_2` ($100\text{ N}$ forward). | **15 mins** |
| **2** | **Add VoidKill Plane & Fall-Off Respawn** | • Create `VoidKill` trigger plane at $y = -25.0\text{ m}$ ($10000 \times 50 \times 10000\text{ m}$).<br>• Add `RespawnAtCheckpoint()` logic on raft when entering `VoidKill` or falling below $y = -10\text{ m}$. | **30 mins** |
| **3** | **Elevate & Display Orange Chevron Flow Arrows** | • Offset `DirectionalArrow` visual meshes to $y = 0.58\text{ m}$ (or set render queue to `Transparent + 50` / `Overlay`).<br>• Bind `DirectionalArrow.controller` so chevron arrows pulse/flow downstream. | **20 mins** |
| **4** | **Fix Bone-Armature Obstacle Animations** | • Update `ObstacleAnimationController.cs` to resolve target armatures/bones for `f10` (water hammer), `f13` (log), `f14` (crusher), `f23` (sweeper).<br>• Ensure legacy `Animation` components (`FT_C5_Windmill`, `FT_RotorCross`) play automatically. | **45 mins** |
| **5** | **Wire All 8 Track Checkpoints** | • Attach `RaceCheckpoint.cs` to the 8 checkpoint trigger volumes in `Level_Platform`.<br>• Track last passed checkpoint index and rotation on the raft controller. | **35 mins** |
| **6** | **Spawn Missing Dynamic Parkur Obstacles** | • Instantiate `FT_HammerGate.prefab` (the swinging spiked hammer from Screenshot 1) at canyon arch checkpoint.<br>• Instantiate other dynamic obstacles (`FT_DoubleHammer`, `FT_MaceWheel`, `FT_RotorCross`, `FT_SweepBar`, `FT_SpikeWheel`) matching `PuffTest.unity`. | **60 mins** |
| **Total** | **Comprehensive Milestone 2 Fix** | | **~3 hours 25 mins** |

---
*End of Audit Report. Awaiting user review before applying any fixes.*
