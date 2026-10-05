# CLAUDE.md (Game)

Unity project. Paths below are relative to `Game/`.

## Project

**LOTW** -- 2.5D side-scrolling game, Unity 6 (6000.3.2f1), URP, Cinemachine 3, New Input System, NavMesh.

## Commands

No build CLI. All build/test via Unity Editor. Tests: Window > General > Test Runner. EditMode tests in `Assets/Tests/EditMode/`. Dev scene: `Assets/Project/Scenes/Test/Character.unity`.

## Architecture

**Assemblies:** `Project.Scripts` (runtime, `Assets/Project/Scripts/Project.Scripts.asmdef`), `Project.Scripts.Editor` (editor-only), `EditModeTests` (tests, refs `Project.Scripts`).

**Boot:** `Bootstrapper.Execute()` via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` instantiates `Resources/SubSystemCollection.prefab` as DontDestroyOnLoad if no `SystemRoot`. Prefab holds all managers: Camera, UI, World, Core, Audio, Save, Inventory, Data.

**Singleton:** `Singleton<T>` (Core/Singleton.cs) -- auto-creates, DontDestroyOnLoad, self-destructs duplicates. All managers extend this.

**Rail Movement:** Player moves on `RailNode` graph (max 2 neighbors, linear). `PlayerController` has delegate `StateMachine` with Idle/Move states. `MoveOnPath()` projects camera-relative input onto rail segment. `RailConnector : IInteractable` connects rail sections via warp/walk to `destinationNode`, optional camera switch.

**Interaction:** `PlayerInteractor` uses `Physics.OverlapSphereNonAlloc` (radius `WorldDefines.InteractionDistance = 2f`) each frame. `HudUI` shows hint. `IInteractable.Interact(gameObject)` is the interface.

**Camera:** `CameraManager` sets Cinemachine priority (inactive=10, active=20). `CameraTrigger` (BoxCollider) switches on enter/exit. `BillboardHandler` subscribes to `CinemachineCore.CameraUpdatedEvent`.

**UI:** `UIManager` uses `Awaitable` with `Queue<Func<Awaitable>>` for serialized ops. Push/pop page stack. `BaseUI` base class supports Fade (CanvasGroup) or Animation (Animator) transitions. Prefabs in `Prefabs/UI/`, registered in `UIManager.uiPrefabs` on SubSystemCollection (looked up by type). Panels: HudUI, DialogueUI, InventoryUI, NotebookUI. `System/UI/LocalizedText` resolves a fixed `@key` label on enable and on language change.

**Dialogue:** Table-driven. `DataManager` loads `Resources/Table/*.json` (generated from `Table/Excel/*.xlsx` by `Table/ConvertTable.bat`; each sheet needs an Excel Table) once at boot and caches rows by `dataId` until quit. `Data.Table.DialogueData` row: speakerName, text, `nextId` (-1 ends), `choiceIds`, `conditions`, `actions`. `System/Dialogue/DialogueCommands` parses conditions (`flag:key>=2;!item:rose;quest:101=2;note:201;met:npc_gumman`) and actions (`setFlag/addFlag/clearFlag/giveItem/takeItem/sfx/bgm/startQuest/completeQuest/addNote/strikeNote/meet`); a row with empty text + choiceIds is a router that jumps to the first choice whose conditions pass (else `nextId`). `ManagerDialogueContext` binds them to Flag/Inventory/Audio managers. `NPC` holds a start `dialogueId` and a `CharacterProfile` (Data/Characters, sprite/animator/name); place `PF_NPC_Base` per NPC; optional `dialogueCamera` is blended in during dialogue and restored after. `DialogueUI.SetupDialogue(row, ..., onFinished)` follows `nextId`, filters choices by conditions, fills the `ChoicePanel` buttons (max 3, built by `LOTW/Plaza/3 Setup Dialogue Prefab`).

**Story (quests, notebook):** State lives in flags (`Data/StoryKeys`: `quest.{id}` = `QuestState`, `note.{id}` = `NoteState`, `met.{characterId}`, `metCount`, `chapter`, `timeSlot`), so it is saved with `SaveData.flags` and readable from dialogue conditions. Tables `Quest` (type main/sub, chapter, giverId, objectiveIds), `QuestObjective` (text, conditions evaluated live; empty = never auto-done), `Notebook` (category document/profile/alibi/question/todo, caseId, subjectId). Read via static `Content/Story/QuestLog` and `NotebookLog`; listen to `FlagManager.OnFlagChanged` with `StoryKeys.IsQuestKey/IsNoteKey`. UI: `NotebookUI` (tabs `NotebookTab`, list + detail, cover sticky notes) and `HudUI` (notebook button with unread badge, up to `UIDefines.HudQuestLines` quests, toasts) build their content with the pure `NotebookPageBuilder`; prefabs come from menu `LOTW/UI/Build Notebook UI`. Roadmap: `Docs/Roadmap.md`, data map: `Docs/DataFlow.md`.

**Localization:** `Core/Localization.Resolve(text)` turns `@key` into the current-language string from `Text` table (`TextData`: key, ko, en; falls back to ko, shows `@key` if missing). Non-`@` strings pass through. Used for dialogue text/speaker, prompts, HUD hint.

**Audio:** `AudioManager` -- BGM (crossfade), SFX. Volume = MasterVolume * BgmVolume/SfxVolume. Listens to `GameInstance.OnSettingsChanged`. `PlayBGM/PlaySFX(string key)` look up `AudioLibrary` (Data/Audio/AudioLibrary_Main, key = clip name) assigned on SubSystemCollection. `SceneBgm` component plays a clip on scene start.

**Save:** `SaveManager` serializes `SaveData` (version, playTime, scene, entrance, inventory, flags, timestamp) via `SaveSystem<T>` (temp file + replace, returns null on unreadable files) to JSON in `persistentDataPath/Saves/`. `SaveDefines.MaxSlots` slots. Story state rides in flags. `LoadAndApply` bumps flag `loadCount`; `NewGame(slot)` clears flags/inventory/playTime; `Upgrade` migrates old versions. Integrates with `GameInstance.SelectSaveSlot()`. `SaveCurrent(slot)` collects scene, `SceneTransitionManager.CurrentEntranceId`, inventory, flags; `LoadAndApply(slot)` restores them and transitions.

**Inventory:** `InventoryManager` -- slot-based, stacking. `ItemData` (id, name, desc, icon, maxStack) built by `TableItemDataProvider` from `Item` table (`ItemTableData`); icon sprite loaded from `Resources/Items/{icon}`. `ToSaveData()`/`LoadFromSaveData()` for persistence.

**WorldObject IDs:** `WorldManager.Register` gives objects with `objectID` 0 or a duplicate ID a negative runtime ID; map-placed objects keep positive IDs from `MapModel.nextId`.

**GameInstance:** Global settings (volumes, language) via PlayerPrefs. Current save slot. Fires `OnSettingsChanged`.

**Defines:** Magic numbers in `Data/Defines.cs` as `readonly struct`: WorldDefines, CameraDefines, AnimDefines, UIDefines, SceneDefines. Editor gizmos in `Editor/Data/Defines.cs` (ToolDefines).

**Data:** `Data/Structs.cs` -- ItemData, ItemSlot, SaveData. `Data/CharacterProfile.cs`, `Data/AudioLibrary.cs` -- ScriptableObjects (`LOTW/Character Profile`, `LOTW/Audio Library`), assets in `Assets/Project/Data/`. `Data/Table/` -- table row classes (`TableRowData` subclasses: Dialogue, Map, ItemTable, Text, Quest, QuestObjective, Notebook).

**Namespaces:** Mirror folder paths -- `Project.Scripts.Core(.Managers)`, `Project.Scripts.System.World/.UI`, `Project.Scripts.Content.Controller/.World/.UI`, `Project.Scripts.Data`, `Project.Scripts.Editor`.
