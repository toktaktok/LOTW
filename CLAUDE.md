# CLAUDE.md

## Behavior

Think before acting. Read files before editing. Be concise in output, thorough in reasoning. Edit over rewrite. No redundant file reads. No sycophantic openers/closers. No question restating. No unsolicited suggestions or scope creep. No over-engineering. ASCII only in output. If unsure, say so. Never guess file paths. User instructions override this file.

## Project

**LOTW** -- 2.5D side-scrolling game, Unity 6 (6000.3.2f1), URP, Cinemachine 3, New Input System, NavMesh.

## Commands

No build CLI. All build/test via Unity Editor. Tests: Window > General > Test Runner. EditMode tests in `Assets/Tests/EditMode/`. Dev scene: `Assets/Project/Scenes/Test/Character.unity`.

## Architecture

**Assemblies:** `Project.Scripts` (runtime, `Assets/Project/Scripts/Project.Scripts.asmdef`), `Project.Scripts.Editor` (editor-only), `EditModeTests` (tests, refs `Project.Scripts`).

**Boot:** `Bootstrapper.Execute()` via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` instantiates `Resources/SubSystemCollection.prefab` as DontDestroyOnLoad if no `SystemRoot`. Prefab holds all managers: Camera, UI, World, Core, Audio, Save, Inventory.

**Singleton:** `Singleton<T>` (Core/Singleton.cs) -- auto-creates, DontDestroyOnLoad, self-destructs duplicates. All managers extend this.

**Rail Movement:** Player moves on `RailNode` graph (max 2 neighbors, linear). `PlayerController` has delegate `StateMachine` with Idle/Move states. `MoveOnPath()` projects camera-relative input onto rail segment. `RailConnector : IInteractable` connects rail sections via warp/walk to `destinationNode`, optional camera switch.

**Interaction:** `PlayerInteractor` uses `Physics.OverlapSphereNonAlloc` (radius `WorldDefines.InteractionDistance = 2f`) each frame. `HudUI` shows hint. `IInteractable.Interact(gameObject)` is the interface.

**Camera:** `CameraManager` sets Cinemachine priority (inactive=10, active=20). `CameraTrigger` (BoxCollider) switches on enter/exit. `BillboardHandler` subscribes to `CinemachineCore.CameraUpdatedEvent`.

**UI:** `UIManager` uses `Awaitable` with `Queue<Func<Awaitable>>` for serialized ops. Push/pop page stack. `BaseUI` base class supports Fade (CanvasGroup) or Animation (Animator) transitions. Prefabs in `Prefabs/UI/`, registered in `UIManager.uiPrefabs` on SubSystemCollection (looked up by type). Panels: HudUI, DialogueUI, InventoryUI.

**Dialogue:** `DialogueData` (Data/Structs.cs) has `DialogueLine[]` (speaker+text). `DialogueUI` modes: prompt (confirm/cancel) and dialogue (multi-line via `SetupDialogue()`/`AdvanceLine()`). `NPC : WorldObject, IInteractable` uses dialogue mode.

**Audio:** `AudioManager` -- BGM (crossfade), SFX. Volume = MasterVolume * BgmVolume/SfxVolume. Listens to `GameInstance.OnSettingsChanged`.

**Save:** `SaveManager` serializes `SaveData` (scene, entrance, inventory, flags, timestamp) to JSON in `persistentDataPath/Saves/`. 3 slots. Integrates with `GameInstance.SelectSaveSlot()`.

**Inventory:** `InventoryManager` -- slot-based, stacking. `ItemData` (id, name, desc, icon, maxStack). `ItemDatabase` ScriptableObjects in `Resources/Items/`. `ToSaveData()`/`LoadFromSaveData()` for persistence.

**GameInstance:** Global settings (volumes, language) via PlayerPrefs. Current save slot. Fires `OnSettingsChanged`.

**Defines:** Magic numbers in `Data/Defines.cs` as `readonly struct`: WorldDefines, CameraDefines, AnimDefines, UIDefines, SceneDefines. Editor gizmos in `Editor/Data/Defines.cs` (ToolDefines).

**Data:** `Data/Structs.cs` -- DialogueLine, DialogueData, ItemData, ItemSlot, SaveData. `Data/ItemDatabase.cs` -- ScriptableObject wrapper (CreateAssetMenu: `LOTW/Item Data`).

**Namespaces:** Mirror folder paths -- `Project.Scripts.Core(.Managers)`, `Project.Scripts.System.World/.UI`, `Project.Scripts.Content.Controller/.World/.UI`, `Project.Scripts.Data`, `Project.Scripts.Editor`.
