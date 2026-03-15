# Project Information

## 1. Project Overview
**Name**: Unity-Chase
**Type**: Unity 3D Project
**Pipeline**: Universal Render Pipeline (URP)
**Current State**: Core systems implemented (Bootstrap, GameState, Player/Vehicle basics).

This document serves as the single source of truth for the project's architecture, design, and state. All agents and developers should refer to and update this document when making significant changes.

## 2. High-Level Design (HLD)

### Architecture Patterns
- **Singleton Pattern**: Used for global managers (`GameStateManager`, `CameraExclusiveStateGate`, `SaveManager`).
- **State Machine**: The `GameStateManager` and `RC1FlowOrchestrator` use enums to manage session and flow states.
- **Separation of Concerns**: 
    - `_Core`: Orchestration, state, and foundational logic.
    - `_Systems`: Gameplay mechanics (Player, Vehicle, Police).
    - `_Data`: Asset-based data containers (ScriptableObjects).

### Directory Structure & Rationale
- `Agents/`: **Documentation and Agent Context.** (This file lives here).
- `Assets/_Core/`:
    - `Bootstrap/`: Scene loading and camera exclusivity.
    - `GameState/`: Global session state and save/load coordination.
    - `Startup/`: Application-level initialization.
- `Assets/_Systems/`:
    - `Player/`: Spawning, camera binding, and lifecycle (Death/Busted).
    - `Vehicle/`: Interaction logic (Entry/Exit) and occupancy management.
    - `Police/`: AI and pursuit logic.
    - `World/`: Environment systems and checkpoints.
- `Assets/_Data/`:
    - `Session/`: ScriptableObjects tracking runtime state.
    - `Save/`: Persistent data management.
- `Assets/_World/`:
    - `Scenes/`: Master scene list (`Bootstrap`, `BankPrologue_Timeline`, `EscapeStart`).

## 3. Low-Level Design (LLD)

### Core Components
- **RC1FlowOrchestrator**: Manages the additive loading sequence: `Bootstrap` -> `BankPrologue` (Cutscene) -> `EscapeStart` (Gameplay).
- **CameraExclusiveStateGate**: Ensures only one `MainCamera` and `AudioListener` are active during transitions to prevent rendering conflicts.
- **GameStateManager**: Central hub for session lifecycle. Listens for player events (Death/Busted) and coordinates restarts.

### Data Structures
- **SessionRuntimeData**: ScriptableObject tracking:
    - `currentSessionState`: (Booting, Cutscene, Active, Transition, Restarting, Ended).
    - `sessionID`: Unique identifier for the current run.
    - `completedCheckpoints`: List of IDs reached by the player.
- **SaveData**: Persistent storage for progress and settings.

### Key Flows
1. **Bootstrapping**: Starts at `Bootstrap.unity`. `RC1FlowOrchestrator` loads the cutscene.
2. **Gameplay Transition**: Triggered by a signal (Timeline/UI). `EscapeStart` is loaded additively, cutscene is unloaded, and cameras are swapped.
3. **Session Reset**: `GameStateManager` detects death/arrest and triggers either a full restart or a checkpoint reload.

---
*Last Updated: 2026-02-16*
