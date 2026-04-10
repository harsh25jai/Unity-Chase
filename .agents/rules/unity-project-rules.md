# Unity Project AI Agent Rules

When acting as an AI assistant in the `Chase` workspace, you must adhere to the following rules:

1. **State Management:** Never suggest or implement solutions that circumvent the `RC1FlowOrchestrator` or `GameStateManager` for global state changes.
2. **Testing Enforcement:** Always run or suggest running associated component tests (such as `ReloadStressTest`) when modifying checkpoints, flow logic, or world gates.
3. **Namespaces:** Keep namespaces strictly modularized. New scripts must map to their folder structure (e.g., `Chase.Core.GameState` or `Chase.Systems.Police`).
4. **Validation:** Respect static validation classes. If you add new timeline bindings or scene references, you might need to extend `TimelineBindingValidator` or `SceneReferenceValidator`.
