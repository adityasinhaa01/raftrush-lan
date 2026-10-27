# Milestone 1 Plan: Single-Player Raft on Track with Keyboard Gas

Based on decompiled references: `Boats/BoatPhysicsController.cs`, `BoatPhysicsConfig.cs`, `BoatDefinition.cs`, `BoatWaterRide.cs`, `Players/PirateGas.cs`, `Players/PirateGasConfig.cs`, `Track/RaceTrack.cs`, `RaceZone.cs`, `FbxTracks.cs`, `Modes/YarisRules.cs`, and `Networking/RaceState.cs`.

---

## 1. Gas Thrust Mechanics (Per Seat)
* **Seat Layout & Positions**: 4 seats arranged symmetrically around the raft perimeter (radius $R \approx 1.3\text{ m}$):
  * **Seat 1 (Rear-Right, ~135°)**: $(+0.92, Y_{com}, -0.92)$
  * **Seat 2 (Rear-Left, ~225°)**: $(-0.92, Y_{com}, -0.92)$
  * **Seat 3 (Front-Left, ~315°)**: $(-0.92, Y_{com}, +0.92)$
  * **Seat 4 (Front-Right, ~45°)**: $(+0.92, Y_{com}, +0.92)$
  * Height offset: Applied at COM height (`GasForceDrop = 0`), preventing bow-diving pitch torque.
* **Thrust Direction**:
  * Outward vector from center to seat is $\vec{u}_{outward} = (\sin\theta, 0, \cos\theta)$.
  * Gas expels backwards (radially outward) behind the seated player, creating reaction thrust inward: $-\vec{u}_{outward} = (-\sin\theta, 0, -\cos\theta)$ rotated into world space.
  * Firing **Rear-Left (Key 2)** pushes forward-right $\rightarrow$ raft yaws **Left**.
  * Firing **Rear-Right (Key 1)** pushes forward-left $\rightarrow$ raft yaws **Right**.
  * Firing **both rear seats (Keys 1+2)** cancels lateral forces and sums forward thrust $\rightarrow$ accelerates straight forward.
  * Front seats (Keys 3 & 4) push backward $\rightarrow$ provide reverse, braking, and pivot assistance.
* **Force & Ramp**:
  * Max Force: $900\text{ N}$ (or $235\text{ N}$ default tune in `RaceState`).
  * Continuous hold ramps force from $100\text{ N}$ (`MinGasForce`) to full thrust over $3.0\text{ s}$ (`GasRampSeconds`) linearly.
* **Stamina / Gas Fuel & Cooldown**:
  * Max Gas: $100\text{ units}$ per seat.
  * Consumption: $12\text{ units/sec}$ during hold (~$8.3\text{ s}$ max continuous burn).
  * Depletion & Lockout: Reaching 0 locks the seat until refilled to $35\%$ (`GasRecoverPercent = 0.35`).
  * Regen Delay: After gas consumption stops, regen pauses for $1.0\text{ s}$ (`GasRegenDelay`).
  * Regen Rate: $18\text{ units/sec}$ (`GasRegenPerSec`) once delay elapses (~$5.5\text{ s}$ for full refill).

---

## 2. Key Physics Values
* **Mass**: $40\text{ kg}$ (`BoatDefinition.Mass`) / $60.2\text{ kg}$ (`BoatSizes` M).
* **Center of Mass (Keel)**: Local $Y = -0.7\text{ m}$. Low COM creates natural gravitational righting torque to prevent tipping over.
* **Drag / Damping**:
  * Linear Damping (`linearDamping`): $0.4$ (slippery water glide).
  * Angular Damping (`angularDamping`): $2.0$ (prevents endless spinning while allowing responsive steering).
  * Glide friction: $0.1$ horizontal deceleration.
* **Speed Limiting**:
  * Max Speed: $14\text{ m/s} \approx 50\text{ km/h}$.
  * Soft clamp: If speed $> 14\text{ m/s}$, applies counter-force proportional to excess velocity.

---

## 3. Checkpoints & Finish Line
* **Trigger Zones**: `FinishZone` (and optional `CheckpointZone`) with `Collider.isTrigger = true`.
* **Detection**: On `OnTriggerEnter` of the raft's Rigidbody, stops race clock, marks state as finished, records elapsed time formatted as `MM:SS.ms`, and triggers finish UI announcement.
* **Timer**: Precision timer calculating `Time.time - startTime` while the race is running.

---

## 4. Planned New Scripts (`Assets/Scripts/`)
1. **`Assets/Scripts/Raft/RaftController.cs`**:
   Rigidbody-based raft controller holding 4 seat transforms, applying force at position, COM keel tuning, linear/angular damping, and soft speed clamp.
2. **`Assets/Scripts/Input/IGasInputSource.cs`**:
   Clean decoupled interface `bool IsGasPressed(int seatIndex)` and `float GetGasPower(int seatIndex)` allowing microphone or AI inputs to swap in later without touching raft logic.
3. **`Assets/Scripts/Input/KeyboardGasInput.cs`**:
   Monitors Keys 1-4, tracks individual gas energy pools ($0\text{--}100$), handles depletion/recovery thresholds, regen delay, and ramp timers.
4. **`Assets/Scripts/Track/FinishZone.cs`**:
   Trigger collider script that detects the raft, halts race timing, records the final race duration, and triggers HUD finish screen.
5. **`Assets/Scripts/UI/RaceHUD.cs`**:
   TextMeshPro HUD displaying current speed, 4 gas fuel bars with refill/lockout status, live race clock, and finish overlay.
6. **`Assets/Scripts/Camera/ChaseCamera.cs`**:
   Smooth 3rd-person follow camera tracking the raft with distance, elevation, forward-looking rotation, and position smoothing.
