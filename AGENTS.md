# AGENTS

## Project Scope
- Unity project: `CONVOY-SURVIVAL`
- Target: playable vertical-slice prototype with:
  - Hub scene (on-foot + truck customization + run start)
  - MainRun scene (endless road vehicle run with zombies, pickups, objectives, trading posts)

## Hard Rules
- Do not edit `.unity` or `.prefab` YAML files directly.
- Create scenes, prefabs, GameObjects, and assets through Unity C# runtime/editor scripts.
- Do not modify `Library`, `Temp`, `Logs`, or `obj`.
- Do not add paid or external dependencies.
- Use primitive-based placeholder visuals when art is missing.

## Folder Conventions
- Runtime code: `Assets/_Project/Scripts/...`
- Editor automation: `Assets/_Project/Scripts/Editor/...`
- Generated scenes: `Assets/_Project/Scenes/Hub.unity`, `Assets/_Project/Scenes/MainRun.unity`
- Generated assets/prefabs/materials/data:
  - `Assets/_Project/Prefabs`
  - `Assets/_Project/Materials`
  - `Assets/_Project/Data`

## Technical Direction
- Keep systems simple, modular, and inspector-driven.
- Prefer serialized fields and straightforward MonoBehaviour wiring.
- Use ScriptableObjects for upgrade/config data where useful.
- Reuse existing Input System package; avoid project-wide input breakage.
- Prefer a working prototype over architecture complexity.

## Bootstrap Requirement
- Provide menu command:
  - `Tools/Convoy Survival/Bootstrap Prototype`
- Bootstrap must:
  - Ensure required folders exist
  - Create basic materials
  - Generate placeholder prefabs (truck, zombie, pickup, gate, trading props, hazards, waypoint)
  - Generate and wire `Hub` and `MainRun` scenes
  - Add generated scenes to Build Settings

## Implementation Priorities
1. Core run loop (movement, spawning, collisions, durability/fuel, pickups, distance)
2. Hub flow (on-foot movement, customization stations, enter truck to start run)
3. Economy/progression (scrap, upgrades, session state between scenes)
4. Objectives + trading post cadence (radio waypoint events + trading post every 5000m)
