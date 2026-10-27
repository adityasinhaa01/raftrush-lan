# Project: Fart Together LAN clone (Unity 6, URP)

## Goal
Personal, non-commercial clone of "Fart Together" for playing with 2 friends over the same Wi-Fi/hotspot. No Steam, no backend, no internet, no dedicated server. One player hosts, others join by IP.

## The game (what we are cloning)
RACE mode only. 2-4 players share ONE raft. Each player's "fart" (gas) pushes the raft, so players must coordinate to steer along a track.
Track features: thorns along the sides (hazard), static and moving obstacles in the middle, steep uphill slopes, and water channels where the flow goes along or against the raft's direction.

## Rules
- Write ALL new code from scratch in Assets/Scripts/. Do NOT copy files from _reference/ into Assets/.
- _reference/ is decompiled code from the original game. Read it to understand logic, values and structure, then write clean new code. Most useful: Boats/, Players/PirateGas*, Track/, Water/, Obstacles/, Modes/YarisRules.cs, Networking/RaceState.cs
- Networking: Netcode for GameObjects + Unity Transport (UDP, direct IP). Host = server + client.
- Never use Steamworks, Addressables, or any online service.
- Assets (models, audio, materials, track FBX) are already in Assets/. Reuse them, do not recreate.
- Work in small milestones. After each one, stop and tell me exactly what to test in the Unity editor.
- Keep scripts small and single-purpose. No giant manager classes.

## Milestones
1. Raft + track, single player, keyboard gas, finish line
2. Water flow (along/against) + steep slopes
3. Thorns + static/moving obstacles
4. LAN: host + 2-3 clients drive one raft together
5. Mic input, polish

## FIDELITY RULES (highest priority, added after Milestone 1)
The final game must look and feel IDENTICAL to the original. No placeholders from now on.
- Use the ORIGINAL assets from Assets/Ripped/ (meshes, prefabs in GameObject/, materials, audio, animations, particles) for everything: raft, track, obstacles, thorns, props, water, sky, UI.
- Layout source of truth = the ripped scenes in Assets/Ripped/PuffPirates/Scenes/ (PuffTest, MenuScene, LobbyScene, Bootstrap). Read object positions, rotations, scales and prefab references from them. Do NOT invent your own track layout.
- Gameplay values (speeds, forces, timings, ranges, cooldowns) must come from _reference/ code. Quote the original file and value in a comment when you copy a number.
- Shaders are the LAST milestone. Until then, simple URP/Lit materials with the original textures and colours are fine. Do not try to fix or rewrite original shaders yet.
- When something in the original is unclear, say so and ask me. Do not guess and make it look "close enough".
- Every milestone ends with a short list: "what matches the original" and "what is still different".
