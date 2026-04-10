# Skill: Unity C# Architecture Enforcement
Use this skill to guide your implementation decisions when modifying or creating new features in the Unity `Chase` project.

## Purpose
To maintain the strict separation of concerns between `_Core` infrastructure and `_Systems` domain logic, preventing tightly coupled code.

## Competencies
- Recognizes the boundaries of the `_Core` and `_Systems` layers.
- Promotes Event-Driven design methodologies using Relays and Binders rather than monolithic Update loops.
- Enforces test-driven development (TDD) by making sure `*Tests.cs` scripts are generated alongside new features.
- Can identify proper Coordinator/Orchestrator usage for managing complex transitions.
