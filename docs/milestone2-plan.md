# Milestone 2 Plan: Authentic Race Track, Water Flow & Steep Slopes

Based on decompiled code from `_reference/PuffPirates/Water/WaterChannel.cs`, `WaterSystem.cs`, `_reference/PuffPirates/Boats/BoatWaterRide.cs`, `_reference/PuffPirates/Obstacles/GustZone.cs`, `_reference/PuffPirates/Track/RaceTrack.cs`, `RaceZone.cs`, `FbxTracks.cs`, and scene analysis of `Assets/Ripped/PuffPirates/Scenes/`.

---

## 1. Track Scene Identification
* **Ripped Scene Analysis**:
  * `Assets/Ripped/PuffPirates/Scenes/Bootstrap.unity`: Game bootstrap loader and network initialization scene.
  * `Assets/Ripped/PuffPirates/Scenes/MenuScene.unity`: Main menu and run selection panel.
  * `Assets/Ripped/PuffPirates/Scenes/LobbyScene.unity`: Character selection and lobby stage.
  * **`Assets/Ripped/PuffPirates/Scenes/PuffTest.unity`**: Holds the **complete, fully authored race track**.
* **Track Root & Prefab**:
  * The entire race track in `PuffTest.unity` is parented under root GameObject **`Level_Platform`** (Transform `fileID: 1931`, GameObject `fileID: 687`) positioned at `(0, 0, 0)` with scale `(1, 1, 1)`.
  * The exact hierarchy is mirrored in **`Assets/Ripped/GameObject/Level_Platform.prefab`** (1,159 GameObjects, 733 mesh components). Instantiating this prefab replicates the exact authored layout with 100% geometric fidelity.

---

## 2. Track Assets, Start Position & Finish Line
* **Track Meshes & Colliders** (`Assets/Ripped/Mesh/`):
  * **Straights**: `GM_3D_ObstacleA1.asset` (Visual) + `GM_3D_ObstacleA1_Col.asset` (MeshCollider)
  * **Steep Slope**: `GM_3D_ObstacleA1Y1.asset` (Visual) + `GM_3D_ObstacleA1Y1_Col.asset` (MeshCollider)
  * **Curves / Corners**: `GM_3D_ObstacleA1KD1`, `GM_3D_ObstacleA1BD1`, `GM_3D_ObstacleA1KD2`, etc.
  * **Water Channel**: `GM_3D_Demo_Water.asset` (Surface), `GM_3D_Demo_Wall.asset` (Visual walls), `GM_3D_Demo_WallCol.asset` (Wall colliders)
  * **Finish Banner**: `GM_3D_Finish.asset` (Mesh in `Assets/Ripped/GameObject/GM_3D_Finish.prefab`)
* **Materials & Textures** (`Assets/Ripped/Material/` and `Assets/Ripped/Texture2D/`):
  * `M_Atlas_1.mat` using texture `T_Atlas_1.png` (surfaces, props, walls)
  * `M_Water.mat` (water stream visual)
  * Track colliders use non-convex MeshColliders on static track pieces matching the original setup.
* **Spawn / Start Position**:
  * In `Level_Platform/Spawns`, the team spawn cluster is centered at:
    * `Position = (-15.55, 0.89, 90.80)`
    * `Rotation = (0, 180, 0)` (facing $-Z$ straight down the track)
  * Spawn points `Spawn_0` to `Spawn_3` range from `(-19.55, 0.89, 86.80)` to `(-11.55, 0.89, 94.80)`.
* **Finish Line**:
  * Banner Position: `(439.21, 7.98, -103.12)` (`Level_Platform/Environment/Props/GM_3D_Finish`)
  * Finish Trigger: `(461.12, 8.70, -104.02)` (`Level_Platform/Checkpoints/Finish`)

---

## 3. Water Flow Physics & Steep Slopes (Original vs Implementation)

### A. Water Flow Dynamics (`WaterChannel.cs` & `GustZone.cs`)
* **Flow Direction & Force Values**:
  * In `GustZone.cs`, directional currents are defined by `_localDir` and `_force` (quoted from `_reference/PuffPirates/Obstacles/GustZone.cs` line 14: `_force = 950f`, though scene instances use tailored tuned values between $60\text{ N}$ and $200\text{ N}$).
  * Scene `PuffTest.unity` defines the water canyon zones:
    * `Gust_Canyon_1`: Force $= 200\text{ N}$, Forward along channel (`Rotation = (0, 180, 0)`)
    * `Gust_Canyon_2`: Force $= 100\text{ N}$, Opposing/reverse flow against raft
    * `Gust_Canyon_3` & `Gust_Canyon_7`: Lateral/turn guiding currents ($60\text{ N}$ to $100\text{ N}$)
* **Force Application to Raft**:
  * `GustZone.cs` applies continuous force via `OnTriggerStay`:
    ```csharp
    Vector3 dir = transform.TransformDirection(_localDir.normalized);
    rigidbody.AddForce(dir * _force * dt, ForceMode.Impulse); // dt = Time.fixedDeltaTime
    ```
    In continuous physics, this is mathematically identical to `rigidbody.AddForce(dir * _force, ForceMode.Force)`.
  * **Along flow**: Accelerates raft speed and aids steering down the canyon.
  * **Against flow**: Opposes forward motion; single-seat farting slows or stalls, requiring coordinated 2-4 seat gas thrust to overcome.

### B. Steep Slope Dynamics (`FT_Static_A1Y1` & Raft Physics)
* **Geometry**:
  * `FT_Static_A1Y1` at `(49.15, 5.54, -5.65)` bridges the lower road ($Y \approx 0.5\text{ m}$) to the elevated mesa plateau ($Y \approx 9.8\text{ m}$), with an incline angle of $\sim 20^\circ$.
* **Physics & Forces**:
  * Downhill gravity component: $F_{down} = m \cdot g \cdot \sin(\theta) = 40\text{ kg} \times 9.81\text{ m/s}^2 \times \sin(20^\circ) \approx 134.2\text{ N}$.
  * Raft gas capability: Each rear seat produces up to $225\text{ N}$ ($450\text{ N}$ combined from 2 rear seats, $900\text{ N}$ all 4). The raft easily scales the slope under thrust.
  * Without gas, the raft decelerates and slides backwards down the ramp due to gravity and low friction.
  * Anti-flip stability: Low center of mass ($Y_{com} = -0.7\text{ m}$) and active upright torque (`_uprightSpring = 18f`, `_uprightDamping = 4f`) prevent tumbling or pitching up during the transition onto the slope.
  * Real mesh collider (`GM_3D_ObstacleA1Y1_Col.asset`) ensures zero clipping or tunneling.

---

## 4. Scope Fidelity: What Rebuilds Exactly vs Uncertainties

### What Rebuilds Exactly
1. **Track Layout & Geometry**: 100% identical layout using `Assets/Ripped/GameObject/Level_Platform.prefab` from `PuffTest.unity`.
2. **Transforms & Coordinates**: Start position `(-15.55, 0.89, 90.80)`, slope coordinates `(49.15, 5.54, -5.65)`, finish coordinates `(439.21, 7.98, -103.12)`.
3. **Flow Zones (`WaterFlowZone.cs`)**: Clean implementation matching `GustZone.cs` physics (`AddForce(dir * force, ForceMode.Force)`) for both forward and reverse current sections.
4. **Solid Track Colliders**: Original convex/concave mesh colliders from `Assets/Ripped/Mesh/`.
5. **URP Lit Materials**: Clean URP/Lit materials mapped with original textures (`T_Atlas_1.png`), eliminating pink/unsupported legacy shaders.

### What Is Simplified / Deferred to Later Milestones
1. **Shaders**: Full custom water wave surface shaders and VFX shaders are scheduled for the Shader milestone per `GEMINI.md`.
2. **Multiplayer LAN coordination**: Scheduled for Milestone 4.

---

## 5. Phase 1 & Phase 2 Refinement Progress

### Phase 1: Raft & Water (Completed)
1. **Raft Orientation & Materials**:
   - Fixed raft transform rotation from standing upright to lying flat on the water (`Euler(90, 0, 0)` with scale `(1, 1, 1)`).
   - Applied authentic `Gradient Pallete.png` texture onto URP/Lit material: green deck, cream inflatable ring.
   - Removed colored visual seat placeholder cylinders; seat markers are clean invisible transforms.
2. **Raft Buoyancy**:
   - Implemented authentic multi-point spring-damper suspension and wave bobbing (`RaftBuoyancy.cs`) using authentic values from `BoatWaterRide.cs` (`_rideHeight = 0.85m`, `_springForce = 28f`, `_dampingForce = 7f`, `_waveAmplitude = 0.05m`, `_waveFrequency = 1.2rad/s`).
3. **Water Appearance**:
   - Mostly opaque mid-blue URP Lit material (`#2B72A4`) with authentic `Foam2.png` ripple rings and normal mapping.

### Phase 2: Moving Obstacles, Bobbing Logs, Spikes & Hazards (Completed)
1. **24 Animated Mechanical Obstacles (`ObstacleAnimationController.cs`)**:
   - Replicated `_reference/PuffPirates/Obstacles/ImportedObstacleAnimation.cs` using Unity's `Playables` API (`AnimationClipPlayable`).
   - Mapped all 12 authentic FBX `AnimationClip`s from `Assets/Ripped/AnimationClip/` (`Scene_1.anim` through `Scene_17.anim`).
   - Restored authentic playback speeds and deterministic phase offsets from `Level_Platform.prefab` YAML:
     - `FT_C1_TippingHammer` (3 instances): `Scene_11.anim`, speed `1.0`, phase `1.90s`
     - `FT_C5_Windmill` (3 instances): `Scene_6.anim`, speed `0.25`, phase `3.53s`
     - `FT_RotorCross` (7 instances): `Scene_7.anim`, speed `1.0` and `0.4`, phase `0.0s`
     - `FT_B5_DoubleSweep` (2 instances): `Scene_4.anim`, speed `0.5`, phase `0.83s`
     - `FT_SpikeWheel` (1 instance): `Scene_14.anim`, speed `1.0`, phase `4.60s`
     - `FT_C2_PendulumMace` (1 instance): `Scene_16.anim`, speed `1.0`, phase `2.43s`
     - `FT_B2_TippingBar` (1 instance): `Scene_8.anim`, speed `1.0`, phase `0.31s`
     - `ft_animated_obstacle_f10` (2 instances): `Scene_17.anim`, speeds `1.0` and `1.2`
     - `ft_animated_obstacle_f3`, `f13`, `f14`, `f23_1`: `Scene_13`, `Scene_15`, `Scene`, `Scene_1`
2. **9 Floating Water Obstacles (`FloatingBobObstacle.cs`)**:
   - Replicated `_reference/PuffPirates/Water/FakeWaveBob.cs` with authentic Perlin displacement (`amplitude = 0.25m`, `speed = 2.5m/s`, `wavelength = 6.0m`, `crestVariation = 0.15`).
3. **4 Bouncing Spikes (`BouncingSpike.cs`)**:
   - Replicated `_reference/PuffPirates/Obstacles/BounceObstacle.cs` with authentic knockback impulse (`2.0 m/s` away from center).
4. **18 Hazard Barriers (`HazardBarrier.cs`)**:
   - Replicated `_reference/PuffPirates/Track/BarrierKill.cs` on all lethal triggers and thorn borders (`_invulnSeconds = 2.0s`).

