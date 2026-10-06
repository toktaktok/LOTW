# 프레임워크 코드의 프로젝트 의존성 분리

## 2026-10-06 12:19 | refactor/framework-asmdef
- 작업: 범용 시스템 코드를 `Game/Assets/Project/Scripts/Framework/`로 옮기고 어셈블리 `Project.Scripts.Framework`를 만들었다.
- 작업: 옮긴 파일은 19개다. Singleton, StateMachine, SignalBus, Easing, AwaitableExtensions, JsonArrayHelper, Bootstrapper, SaveSystem, FlagStore, SystemRoot, UIManager, AudioManager, SceneTransitionManager, FlagManager, SignalManager, CoreManager, BaseUI, AudioLibrary, UI 열거형 3개(UIState, UITransitionMode, UILayer)다.
- 작업: `.meta` 파일을 같이 옮겨서 프리팹과 에셋의 참조 GUID는 그대로다.
- 작업: BaseUI는 AnimDefines 대신 자체 애니메이터 해시를 쓴다. AnimDefines의 ShowID, HideID는 지웠다.
- 작업: AudioManager는 GameInstance를 참조하지 않는다. IVolumeProvider에 OnVolumeChanged 이벤트를 추가했다.
- 작업: SceneTransitionManager에서 DefaultSceneTransitionHandler를 뺐다. LOTW 구현은 `Content/World/GameSceneTransitionHandler.cs`에 있다.
- 작업: `Core/GameBootstrap.cs`가 AfterSceneLoad 시점에 볼륨 제공자와 씬 전환 핸들러를 주입한다.
- 작업: Project.Scripts, Project.Scripts.Editor, EditModeTests 어셈블리가 Framework를 참조한다. 소비자 파일 76곳의 using을 고쳤다.
- 작업: `Game/CLAUDE.md`와 `.claude/rules/csharp-conventions.md`에 Framework 계층을 적었다.
- 결과: 오프라인 `dotnet build`가 어셈블리 4개에서 성공했다. Framework 어셈블리는 Project.Scripts를 참조하지 않는다.
- 결과: Fable 리뷰에서 8건이 나왔다. SceneExitZone의 using이 `#if UNITY_EDITOR` 안에 들어간 문제 1건(플레이어 빌드 실패)을 고쳤다. 불필요한 using 4곳, 공백만 바뀐 파일 2개, 고아 상수 2개, 규칙 문서 1줄을 고쳤다. LOTW 메뉴 문자열 2곳과 핸들러 위치 1건은 그대로 뒀다.
- 결과: Unity 에디터(6000.3.10f1)에서 새 어셈블리가 컴파일 오류 없이 컴파일되었다.
- 결과: EditMode 테스트 276개가 모두 통과했다.
- 결과: Title 씬 Play 모드에서 AudioManager의 볼륨 제공자는 GameInstance이고, 씬 전환 핸들러는 GameSceneTransitionHandler였다.
- 결과: 마스터 볼륨을 0.5로 바꾸면 BGM 소스 볼륨이 0.5가 되었다. 값을 되돌리면 1로 돌아왔다.
- 결과: Title에서 Character 씬으로 전환한 뒤에도 제공자와 핸들러가 그대로였다.
- 확인 안 함: 플레이어 빌드. 소리가 실제로 나는지는 듣지 못했다.
- 확인 안 함: Title 씬 콘솔 오류 "[SaveSystem] Failed to load slot 0: empty file". 세이브 슬롯 0 파일이 비어 있다. 이번 변경과 무관해 보인다.
- 다음: 없음
