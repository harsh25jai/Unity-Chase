# Chase: Coding Standards & Rules

## 1. Modularity & Encapsulation
- Scripts must reside within clearly defined boundaries (`_Core` vs `_Systems/[Domain]`).
- Single Responsibility Principle (SRP) must be strictly enforced. Controllers/Orchestrators manage flow; Binders/Gates manage access; Validators check state.

## 2. Testing Requirements
- **Extensive Unit/Integration Testing:** Every major system MUST have a robust set of tests.
- Place test scripts inside a `Tests/` directory within the module's folder (e.g., `Assets/_Systems/Police/Tests/`).
- Suffix test scripts with `Tests` (e.g., `PoliceEscalationControllerTests.cs`).

## 3. Dependency Injection & Object Binding
- Avoid using destructive singletons or heavy use of `GameObject.Find()`. 
- Favor clear reference bindings in the Inspector using specialized Binder or Resolver components (e.g., `SpawnAlignmentResolver`).

## 4. Scene & Flow Validation
- The use of static validation classes (like `TimelineBindingValidator` and `SceneReferenceValidator`) ensures integrity during build and runtime. Future AI modifications must pass or extend these validators.

## 5. Naming Conventions
- **Classes:** `PascalCase`.
- **Interfaces:** Prefix with `I` (e.g., `INotificationReceiver`).
- Components dealing with transitions should be named `*Orchestrator` or `*Coordinator`.
- Components preventing state access should be named `*Gate` or `*Guard`.
