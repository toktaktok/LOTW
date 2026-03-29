# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**LOTW** - A 2.5D side-scrolling game built in Unity 6 (6000.3.2f1) using URP, Cinemachine 3, the New Input System, and NavMesh.

## Commands

This is a Unity project with no build CLI scripts. All building and testing goes through the Unity Editor.

**Run tests (Unity Test Framework):**
- Unity Editor > Window > General > Test Runner
- No test files exist yet; `com.unity.test-framework` 1.6.0 is installed

**Open the project:**
- Open Unity Hub, add the project root, and launch with Unity 6000.3.2f1

**Active development scene:** `Assets/Project/Scenes/Test/Character.unity`

## Architecture

### Boot Flow

`Bootstrapper.Execute()` runs via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` before any scene loads. It checks for `SystemRoot` and, if absent, instantiates `Resources/SubSystemCollection.prefab` as `DontDestroyOnLoad`. This prefab holds all manager singletons (`CameraManager`, `UIManager`, `WorldManager`, `CoreManager`). No scene needs a manual manager setup.

### Singleton Pattern

`Singleton<T>` (in `Core/Singleton.cs`) is the base for all managers. It auto-creates if missing, marks itself `DontDestroyOnLoad`, and self-destructs on duplicates. All managers extend this.

### Rail Movement System

The player moves along a graph of `RailNode` waypoints (max 2 neighbors each — strictly linear, no branching). `PlayerController` owns a delegate-based `StateMachine<PlayerController>` with **Idle** and **Move** states. In `MoveOnPath()`, camera-relative input is projected onto the current rail segment to find direction; a correction vector keeps the character on the line. When progress `t >= 1.0`, the base node advances.

`RailConnector : IInteractable` is the mechanism for connecting separate rail sections — it either warps or walks the player to a `destinationNode` and optionally triggers a camera switch.

### Interaction

`PlayerInteractor` polls `InputSystem_Actions.Player.Interact` each frame and calls `Physics.OverlapSphereNonAlloc` (radius: `WorldDefines.InteractionDistance = 2f`) to find the closest `IInteractable`. `IInteractable.Interact(gameObject)` is the only interface method.

### Camera System

`CameraManager` controls Cinemachine priority: inactive cameras sit at priority **10**, the active camera is raised to **20**. `CameraTrigger` (BoxCollider) switches cameras on enter/exit. `BillboardHandler` subscribes to `CinemachineCore.CameraUpdatedEvent` to keep sprites camera-facing.

### UI System

`UIManager` uses Unity 6's `Awaitable` (not coroutines or Tasks) with a `Queue<Func<Awaitable>>` to serialize operations. UI is managed as a push/pop page stack. `BaseUI` is the abstract base for all panels, supporting **Fade** (CanvasGroup alpha) or **Animation** (Animator trigger) transitions. UI prefabs are loaded from `Resources/UI/{TypeName}.prefab`.

### Key Defines

All magic numbers live in `Data/Defines.cs` as `readonly struct` types: `WorldDefines`, `CameraDefines`, `AnimDefines`, `UIDefines`. Editor gizmo constants are in `Editor/Data/Defines.cs` (`ToolDefines`).

### Namespace Convention

Namespaces mirror folder paths under `Assets/Project/Scripts/`:
- `Project.Scripts.Core` / `Project.Scripts.Core.Managers`
- `Project.Scripts.System.World` / `Project.Scripts.System.UI`
- `Project.Scripts.Content.Controller` / `Project.Scripts.Content.World`
- `Project.Scripts.Editor`
