# Raft Rush LAN

> **A small Unity hobby project about chaotic raft racing, teamwork, and questionable propulsion.**

Raft Rush LAN is an experimental, fan-made game development project exploring how a cooperative raft-racing experience could be recreated with Unity and eventually played over a local area network.

The idea is simple: a group of players share one raft, coordinate their inputs, navigate obstacle-filled tracks, and try to reach the finish line without turning the whole journey into a disaster.

This is a personal, non-commercial learning project. It is being developed to explore Unity gameplay programming, physics, input handling, scene construction, multiplayer networking, and collaborative game development.

**Status:** Work in progress  
**Engine:** Unity 6  
**Language:** C#  
**Project type:** Hobby / experimental game development

---

## Table of Contents

- [Project Overview](#project-overview)
- [Goals](#goals)
- [Current Development Status](#current-development-status)
- [Planned Features](#planned-features)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Controls](#controls)
- [Development Roadmap](#development-roadmap)
- [Multiplayer Direction](#multiplayer-direction)
- [Contributing](#contributing)
- [Development Guidelines](#development-guidelines)
- [Known Limitations](#known-limitations)
- [Disclaimer](#disclaimer)
- [License](#license)

---

## Project Overview

Raft Rush LAN is built around the idea of a cooperative racing game where multiple players work together to move a shared raft through a challenging environment.

Instead of controlling separate vehicles, players contribute to the movement of a single vehicle. Their inputs need to work together, making coordination an important part of the experience.

The intended gameplay combines:

- Shared raft movement and physics.
- Multiple player positions and directional thrust.
- Obstacle-filled racing tracks.
- Water currents and elevation changes.
- Cooperative gameplay and chaotic situations.
- Local multiplayer without requiring a dedicated online game server.

The initial focus is on getting the core gameplay systems working reliably before expanding the project into a complete multiplayer experience.

## Goals

This project is primarily an opportunity to learn by building.

The main goals are:

1. **Gameplay programming:** Develop reusable C# systems for input, movement, collision, and race logic.
2. **Physics:** Experiment with Rigidbody-based movement, thrust, slopes, and buoyancy-like behaviour.
3. **Track design:** Build race environments containing hazards, checkpoints, finish zones, and environmental forces.
4. **Input systems:** Start with keyboard input and investigate alternative input methods.
5. **Networking:** Explore direct local-network multiplayer and synchronization of shared gameplay state.
6. **Team collaboration:** Use Git and GitHub to coordinate changes, review code, and maintain a shared development history.
7. **Experimentation:** Keep the project fun, learn from bugs, and improve it incrementally.

## Current Development Status

The project is under active experimentation. The following checklist describes the intended milestone progression; completed items should be verified against the current build.

### Core prototype

- [x] Initial raft and seat-thrust prototype.
- [x] Keyboard-based gas input for four seats.
- [x] Initial finish-line and HUD systems.
- [x] Chase-camera prototype.
- [x] Initial race-track construction.
- [x] Water-flow zones and slope experiments.
- [x] Initial raft-floating behaviour.

### Gameplay improvements

- [ ] Refine raft physics and stability.
- [ ] Add moving hazards and obstacle interactions.
- [ ] Implement player movement within individual seat areas.
- [ ] Improve race progression and checkpoint handling.
- [ ] Add clearer feedback for collisions, thrust, and race completion.

### Multiplayer

- [ ] Implement a host-and-join workflow.
- [ ] Discover and connect to games on the same LAN.
- [ ] Synchronize raft movement and player inputs.
- [ ] Handle disconnects and host departure.
- [ ] Test multiplayer sessions across multiple computers.

### Polish

- [ ] Improve water visuals and environmental effects.
- [ ] Add sound and visual feedback for gameplay events.
- [ ] Improve the HUD and menus.
- [ ] Add more tracks and gameplay variations.
- [ ] Test performance and stability on different machines.

> A checked item indicates a prototype milestone, not necessarily a finished or production-ready feature.

## Planned Features

### Cooperative raft movement

Players share one raft. Each seat contributes thrust, and the combined input determines how the raft moves.

The design aims to reward coordination rather than independent racing.

### Obstacle courses

The track design may include:

- Static barriers and walls.
- Moving gates and swinging hazards.
- Spiked obstacles.
- Slopes, ramps, and drops.
- Water currents and environmental forces.
- Checkpoints and finish zones.

### Local multiplayer

The long-term goal is to let friends play together over the same Wi-Fi network or a local hotspot.

The intended experience does not require a dedicated public game server. The exact networking implementation remains a work in progress.

### Alternative input methods

Keyboard controls provide a simple starting point. Microphone-based input is an experimental future possibility, not a currently guaranteed feature.

## Technology Stack

| Technology | Purpose |
|---|---|
| Unity 6 | Game engine and editor |
| C# | Gameplay and tooling code |
| Universal Render Pipeline | Rendering pipeline |
| Unity Input System | Input handling |
| Git | Version control |
| GitHub | Collaboration and project tracking |
| Netcode for GameObjects + Unity Transport | Proposed LAN networking stack |

The networking stack is a planned direction and may change as development progresses.

## Project Structure

The project follows Unity's standard project layout.

```text
raft-rush-lan/
├── Assets/
│   ├── Scripts/
│   │   ├── Raft/
│   │   ├── Input/
│   │   ├── Track/
│   │   ├── Water/
│   │   ├── Camera/
│   │   ├── UI/
│   │   └── Editor/
│   ├── Scenes/
│   ├── Materials/
│   └── InputSystem_Actions.inputactions
├── Packages/
│   └── manifest.json
├── ProjectSettings/
├── .gitignore
└── README.md
```

The listed script subfolders describe the intended organization; the actual repository layout may differ as the project evolves.

### Folder responsibilities

- `Scripts/Raft/` — raft movement, physics, and seat-based thrust.
- `Scripts/Input/` — keyboard input and future input integrations.
- `Scripts/Track/` — race progression, checkpoints, and finish-line logic.
- `Scripts/Water/` — water-flow zones and related behaviour.
- `Scripts/Camera/` — gameplay camera behaviour.
- `Scripts/UI/` — HUD and interface logic.
- `Scripts/Editor/` — Unity Editor utilities and scene-building tools.
- `Scenes/` — Unity scene files.
- `Materials/` — project materials.

Unity `.meta` files should remain alongside their associated assets to preserve GUID references.

## Getting Started

### Prerequisites

Before opening the project, install:

- Unity Hub.
- The compatible Unity Editor version specified by the repository's project settings and documentation.
- Git.
- A compatible desktop operating system and graphics setup.

### 1. Clone the repository

```bash
git clone https://github.com/YOUR_USERNAME/raft-rush-lan.git
cd raft-rush-lan
```

Replace `YOUR_USERNAME` with the actual GitHub account name.

### 2. Open the project

Open Unity Hub, select **Add project from disk**, and choose the cloned project directory.

Use the compatible Unity version. Allow Unity to import assets and regenerate its local `Library` data.

### 3. Check dependencies

Open `Packages/manifest.json` and confirm that the required Unity packages are available.

Resolve any missing packages or compilation errors before running a scene.

### 4. Run the prototype

Open the relevant development scene, select the Game view, and press Play.

The current keyboard prototype uses number keys to activate the four seat inputs. Refer to the Controls section below.

### Asset availability

The public repository should contain only material that can legitimately be redistributed.

If a scene depends on assets that are not included, the project may require compatible, independently obtained assets or replacement placeholders. The repository should document any such requirements clearly.

## Controls

The initial single-player testing prototype uses keyboard keys to simulate the four raft seats.

| Key | Action |
|---|---|
| `1` | Activate seat 1 thrust |
| `2` | Activate seat 2 thrust |
| `3` | Activate seat 3 thrust |
| `4` | Activate seat 4 thrust |

The prototype is intended to demonstrate combined thrust from multiple seats. The exact seat layout and input behaviour may change during development.

These controls are for the current prototype and should not be interpreted as the final multiplayer control scheme.

## Development Roadmap

### Phase 1 — Core mechanics

Focus on basic raft movement, seat-based thrust, camera behaviour, and race completion.

### Phase 2 — Environment and track systems

Improve the track layout, environmental forces, slopes, water interaction, and race boundaries.

### Phase 3 — Hazards and gameplay feedback

Introduce moving obstacles, collision feedback, checkpoint progression, and more reliable race-state handling.

### Phase 4 — LAN multiplayer

Develop host-and-join functionality, input synchronization, shared raft state, and multiplayer testing.

### Phase 5 — Presentation and polish

Improve visuals, audio, menus, onboarding, performance, and overall stability.

This roadmap is flexible. Milestones may be reordered or redesigned as technical challenges are discovered.

## Multiplayer Direction

The intended multiplayer model is a small local session in which one player hosts and others join over the local network.

Potential networking responsibilities include:

- Host session creation.
- Client connection and disconnection.
- Synchronization of player inputs.
- Synchronization of the shared raft state.
- Consistent race progression and checkpoint state.
- Handling network interruptions gracefully.

A local network does not automatically guarantee secure or reliable networking. Authority, synchronization, latency, and disconnect handling will need to be designed and tested.

Multiplayer remains a development goal until the complete workflow works reliably across separate machines.

## Contributing

Contributions, suggestions, bug reports, and experiments are welcome, subject to the repository's applicable contribution and asset-sharing rules.

### Suggested workflow

1. Fork the repository or obtain collaborator access.
2. Create a feature branch.
3. Make a focused change.
4. Test the change in Unity.
5. Commit the relevant files.
6. Open a Pull Request describing the change and how it was tested.

Example:

```bash
git switch -c feature/raft-physics
# Make and test your changes.

git add .
git commit -m "Improve raft physics"
git push -u origin feature/raft-physics
```

For direct collaborators, branch protection and Pull Requests can help prevent accidental changes to the main branch.

### Unity collaboration guidelines

- Keep `.meta` files committed.
- Do not commit generated `Library`, `Temp`, or build-cache folders.
- Avoid changing the same scene or prefab simultaneously when possible.
- Communicate before modifying shared gameplay systems.
- Include reproduction steps when reporting bugs.
- Keep commits small and descriptive.
- Do not upload credentials, private configuration, or material you lack permission to redistribute.

## Known Limitations

This is a hobby project, not a finished commercial game.

Depending on the current build, you may encounter:

- Incomplete gameplay systems.
- Physics instability or unexpected collisions.
- Placeholder visuals and unfinished effects.
- Missing assets in a fresh checkout.
- Unity version or package compatibility issues.
- Multiplayer features that are not yet implemented.
- Temporary debugging tools and development-only behaviour.

Bug reports and constructive feedback are useful, particularly when they include the Unity version, reproduction steps, and relevant logs.

## Disclaimer

Raft Rush LAN is an unofficial, non-commercial hobby project created for experimentation and learning.

It is not affiliated with, endorsed by, or sponsored by the creators or publishers of any existing game.

Any third-party game names or references are used only to explain the project's general inspiration. This repository does not grant permission to redistribute third-party intellectual property.

Original-game assets, audio, models, textures, animations, decompiled code, and other proprietary material are not intended to be redistributed through this repository unless the relevant rights holder has explicitly authorized that distribution.

## License

A license should be added after the repository's contents have been reviewed.

A permissive license such as MIT may be appropriate for original code authored for this project, provided that the code is yours to license and does not incorporate third-party code subject to incompatible terms.

Any license applies only to material the project owner has the legal right to license. It does not grant rights to third-party games, assets, names, or trademarks.

## Credits

This project is a personal learning experiment built around the challenge of designing a cooperative raft-racing game.

Thanks to everyone who tests the prototype, reports bugs, shares ideas, and contributes improvements.

**Built for fun. Maintained for learning. Powered by teamwork and questionable raft physics.**
