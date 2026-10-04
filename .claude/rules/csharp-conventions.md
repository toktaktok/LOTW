---
paths:
  - "Assets/Project/Scripts/**/*.cs"
  - "Assets/Tests/**/*.cs"
---

# LOTW C# Conventions

Applies to this repo only. Skip generated files (`InputSystem_Actions.cs`).

## Formatting
- No space between control keyword and paren: `if(x)`, `else if(x)`, `for(...)`, `foreach(...)`, `while(...)`, `switch(...)`, `catch(...)`, `using(...)`.
- Single-statement body goes on the next line, indented, no braces:
  ```csharp
  if(target == null)
      return;
  ```
  Never `if(x) return;` on one line. Multi-statement bodies use braces.
- Allman braces, 4-space indent, spaces around binary operators.
- Method calls and declarations: no space before `(` (`Foo(a)`, `void Foo(int a)`).

## Naming
- Types, methods, properties, events, enum members, constants: PascalCase.
- Private non-serialized fields: `_camelCase`.
- `[SerializeField] private` fields: plain camelCase, no prefix.
- Serializable data struct fields: public camelCase (`maxStack`, `speaker`).
- Interfaces: `I` prefix. Events: `On` + noun/past (`OnSettingsChanged`). Handlers: `On` + action (`OnConfirm`).
- Async (Awaitable) public API: `Async` suffix (`ShowAsync`). Coroutines and internal fades: `Routine` suffix.
- FSM transitions: `TransitionTo<State>()`.
- Verbs: `Get`/`TryGet` (return null if missing), `Has`/`Is` for checks, `Set` for mutation, `Init()` for WorldObject setup, `Setup...()` for UI.

## Structure
- Namespace mirrors folder: `Project.Scripts.{Core|System|Content|Data|Editor}...`.
- Layers: `Core` framework/managers, `System` reusable world/UI bases, `Content` game-specific, `Data` plain data.
- One class per file. Small data types live in `Data/Structs.cs`, `Data/Enums.cs`, `Data/Defines.cs`.
- Member order: serialized settings, runtime state, properties, Unity lifecycle, public API, private helpers, editor gizmos.
- `#region` and `/// <summary>` are optional; keep them where they already exist.

## Unity patterns
- Managers extend `Singleton<T>`; override as `protected override void Awake() { base.Awake(); ... }`.
- Inspector fields are `[SerializeField] private`, grouped with `[Header]`, `[Tooltip]` in Korean when helpful.
- Expose read-only state via expression-bodied properties or `{ get; private set; }`; collections as `IReadOnlyList`/`IReadOnlyCollection`.
- No magic numbers: put them in `Data/Defines.cs` (`readonly struct` with `const`/`static readonly`); editor values in `ToolDefines`.
- Cache Animator parameters with `Animator.StringToHash` in `AnimDefines`.
- Resources paths follow `Resources/{Category}/{TypeName}`.
- Log as `Debug.Log*($"[ClassName] message")`.
- Static events: invoke with `?.Invoke()`, subscribe in `OnEnable`/`Awake`, unsubscribe in `OnDisable`/`OnDestroy`.
- Null-check Unity objects with `== null`, not `?.`.
- Editor-only code inside `#if UNITY_EDITOR`.
- ScriptableObjects use `[CreateAssetMenu(menuName = "LOTW/...")]`.
