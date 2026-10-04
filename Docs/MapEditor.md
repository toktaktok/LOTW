# In-PlayMode Map Editor

## Context

Goal: drive the player around in Unity **Play Mode**, edit RailNodes and surrounding props, and **save the result as a real map** that survives exiting Play Mode. The hard constraint is that Unity discards all Play Mode *scene* changes on exit, so persistence cannot rely on the scene — it must write a data file during play.

Design chosen by a judged 3-way comparison (codebase-fit / persistence-robustness / authoring-UX all picked the same approach): **a map is a JSON data file**, mirroring the existing `SaveManager` idiom (`JsonUtility.ToJson` + `File.WriteAllText` to `persistentDataPath`) and the `DataManager`/`JsonArrayHelper` Resources table pipeline. A single in-memory `MapModel` is the source of truth; live `RailNode`s are a pure view rebuilt from it. The authoritative save happens **during** Play Mode (build-safe, no `UnityEditor`), so it never depends on the fragile Play->Edit boundary.

Two pragmatic simplifications vs. the raw design (per CLAUDE.md "Simplicity First"), to make the feature run with **zero scene/asset wiring**:
- Input is read directly from `Keyboard.current`/`Mouse.current` (New Input System devices), not an InputActions asset — no `.inputactions` GUI edit, no generated-code dependency.
- `MapEditorController` self-bootstraps via `[RuntimeInitializeOnLoadMethod]`; nodes are built with `AddComponent<RailNode>` (no prefab); placeables load from `Resources/MapPrefabs/{prefabId}` and are skipped if absent. No ScriptableObject registry.

## Data model (JsonUtility-friendly: no refs, no Dictionary, no polymorphism)

- `MapData : TableRowData` (`Data/Table/MapData.cs`) — on-disk shape AND the DataManager-loadable default row. Fields: `mapName`, `nextId`, `NodeEntry[] nodes`, `EdgeEntry[] edges`, `PlaceableEntry[] placeables`.
- `NodeEntry { int id; Vector3 position; Color nodeColor; float radius; }`
- `EdgeEntry { int a; int b; }` — undirected rail link by node id (rail topology is an integer adjacency list; resolved to `RailNode` instances at rebuild).
- `PlaceableEntry { string prefabId; Vector3 position; Vector3 eulerAngles; int objectID; int linkedNodeId(-1=none); string entranceId; string targetScene; string targetEntranceId; }`
- On disk (user maps): `persistentDataPath/Maps/map_{name}.json` = a single `MapData` object.
- Shipped default: `Assets/Project/Resources/Table/Map.json` = a **1-element array** `[ {..} ]` so `DataManager`+`JsonArrayHelper.FromJson<MapData>` reads it unchanged. This object-vs-array asymmetry lives in exactly one place: `MapSerializer.WrapAsDefaultArray`.

## Persistence bridge

PRIMARY (runtime, build-safe, completes during Play Mode): Save -> `JsonUtility.ToJson(data,true)` -> `File.WriteAllText(persistentDataPath/Maps/map_{name}.json)`. Reload in-session: `MapSerializer.Load` -> `MapModel.FromData` -> `MapBuilder.ClearBuilt()`+`Build()`. The user sees the persisted result without leaving Play Mode.

SECONDARY (editor-only, optional, non-load-bearing): a "promote to default" toggle fires `MapSerializer.OnPromoteRequested`. `MapDefaultPromoter` (Editor asmdef) listens on `EditorApplication.playModeStateChanged`; on `ExitingPlayMode`, if requested, it copies the saved JSON (wrapped as a 1-element array) into `Resources/Table/Map.json` and `AssetDatabase.Refresh()`. Pure copy+wrap+Refresh; never captures the scene. `AssetDatabase`/`EditorApplication` are confined to the Editor asmdef.

## Phased file plan

- **P1 data**: `MapData.cs` (new), `MapModel.cs` (new); `RailNode.Disconnect` (add), `WorldObject.SetObjectID` (add), `Defines.cs` `MapDefines` (add).
- **P2 serialize**: `MapSerializer.cs` (new), `MapValidation.cs` (new), `MapRoundTripTests.cs` (new, EditMode).
- **P3 build**: `MapBuilder.cs` (new); `RailConnector.SetDestinationNode` (add), `SceneEntrance.SetStartNode/SetEntranceId` (add), `PlayerController.SetBaseNode` + Start null-guard (add).
- **P4 in-play editor**: `MapEditorController.cs` (new, self-bootstrapped, device-polled input, OnGUI overlay).
- **P5 pipeline + promote**: `DataManager.LoadAllTables` += `LoadTable<MapData>("Map")`; `Resources/Table/Map.json` (new seed); `Editor/Map/MapDefaultPromoter.cs` (new, editor-only).

## Controls (Play Mode)

`F2` toggle edit mode | `1` place-node / `2` select-move / `3` connect / `4` delete submode | left-click acts per submode | `F5` save | `F6` reload saved | `P` toggle promote-to-default. While editing, gameplay input (`PlayerController`, `PlayerInteractor`) is disabled.

## Verification (Unity Editor — no build CLI)

1. Recompile: Console shows zero errors; runtime asmdef still has no `UnityEditor` reference.
2. EditMode tests (Window > General > Test Runner): `MapRoundTripTests` green — round-trip preserves edges symmetrically, `linkedNodeId` stays `-1`, `nextId` persists; validation flags a 3rd-edge / duplicate-objectID map.
3. Play Mode in `Scenes/Test/Character.unity`: press `F2`, confirm player stops moving and no NPC prompt; place 2 nodes, connect (HUD shows 1/2 then 2/2), 3rd connect rejected (warning); move/delete work; gizmo lines stay consistent.
4. Tight loop: `F5` -> file appears at `persistentDataPath/Maps/map_*.json`; `F6` rebuilds the same graph without leaving Play Mode.
5. Default pipeline: cold boot logs `Map: N개 행 로드`; `DataManager.GetRows<MapData>()` returns the seed.
6. Promote (optional): edit + `F5` + `P`, stop Play Mode -> `Resources/Table/Map.json` updated + appears in Project window; never-saved case no-ops.

## Decided defaults (were open questions)

- The map table is the **one table not round-tripped through Excel** (convert_table cannot express nested node/edge arrays); the promoter writes `Resources/Table/Map.json` directly.
- Scope: **one** shipped default map (`dataId=1`) + arbitrarily-named user maps in `persistentDataPath`. Multi-map-per-scene is deferred (`MapData.mapName`/`dataId` already support it).
