# Project Information

## 1. Project Overview
**Name**: My project (Placeholder Name)
**Type**: Unity 3D Project
**Pipeline**: Universal Render Pipeline (URP)
**Current State**: Initial scaffolding / Template based.

This document serves as the single source of truth for the project's architecture, design, and state. All agents and developers should refer to and update this document when making significant changes.

## 2. High-Level Design (HLD)

### Architecture Patterns
*To be defined as the project evolves.*
Currently follows a structured folder separation concern:
- **Core**: Essential framework and base classes.
- **Systems**: Functional modules (managers, controllers).
- **Data**: ScriptableObjects and data containers.
- **World**: Scene hierarchy and environment assets.

### Directory Structure & Rationale
- `Agents/`: **Documentation and Agent Context.** (This file lives here).
- `Assets/_Core/`: Core systems and foundational code.
- `Assets/_Systems/`: Game systems (e.g., Audio, Input, UI Logic).
- `Assets/_Data/`: Data-driven assets.
    - `Player/`: Player-specific data.
    - `Save/`: Save system data structure.
    - `Session/`: Runtime session data.
    - `Vehicle/`: Vehicle configurations.
    - `World/`: World state data.
- `Assets/_World/`: Scene files and level-specific assets.
    - `Bootstrap`: The entry point scene.

## 3. Low-Level Design (LLD)

### Core Components
*Pending implementation.*

### Data Structures
- **Player Data**: (Empty)
- **Vehicle Data**: (Empty)

### Key Flows
- **Bootstrapping**: The game starts at `Assets/_World/Bootstrap.unity`. (Specific logic pending).

---
*Last Updated: 2026-02-01*
