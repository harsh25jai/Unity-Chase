# Chase: System Architecture

The project employs a cleanly separated, domain-driven module structure primarily divided into `_Core` and `_Systems` namespaces within `Assets/`. 

## 1. Layers & Service Boundaries

### `_Core` (Infrastructure & Orchestration)
Handles the fundamental game lifecycle, global state coordination, initialization, and core backend validation.
- **Bootstrap:** Manages initial scene loading, camera exclusivity (`CameraExclusivityEnforcer`), cutscene relays, and the initial RC1 flow logic (`RC1FlowOrchestrator`).
- **GameState:** Global orchestrators (`GameStateManager`, `SaveLoadCoordinator`, `RestartResolver`). Enforces global state blocking (`ChaseSaveBlockGate`).
- **Startup:** Dev tooling and debugging (`DevFailureInjector`, `SaveLoadDebugController`).
- **Validation:** Static validation classes for scene and timeline integrity (`SceneReferenceValidator`, `TimelineBindingValidator`).

### `_Systems` (Game Domain Logic)
Contains encapsulated features and gameplay mechanics.
- **Checkpoints:** Manages checkpoint progression (`CheckpointTriggerSystem`, `CheckpointTriggerBinder`).
- **World:** World boundaries, distance tracking, and motion gates (`SessionDistanceTracker`, `WorldProgressionController`).
- **Player:** Player state, camera routing (`PerspectiveStateRouter`), and spawning alignment (`SpawnAlignmentResolver`).
- **Police:** AI pursuit logic, escalation triggers, and presence spawning (`PoliceEscalationController`, `PursuitStateBridge`).
- **Vehicle:** Occupancy binding, entry stability, and state management (`VehicleOccupancyGuard`, `VehicleStateController`).

## 2. Data Flow & Patterns
- **Event-Driven & Decoupled:** Systems do not typically rely on monolithic `Update` loops polling for state. Instead, they rely on event relays (e.g., `CutsceneSignalRelay`), Unity Events, and system binders (e.g., `CheckpointTriggerBinder`).
- **Coordinators/Orchestrators:** Complex transitions are managed via centralized coordinators (e.g., `CameraTakeoverCoordinator`, `RC1FlowOrchestrator`) which instruct smaller controllers.
