# 씬 세팅용 일회성 툴 정리

## 2026-10-05 19:45 | t3code/bab31dc8
- 작업: `Editor/Plaza/`의 툴 6개를 지웠다. 대상은 `PlazaVoxelPrefabs`, `PlazaRenderSetup`, `PlazaDialoguePrefabSetup`, `PlazaPlayerPrefab`, `PlazaSceneBuilder`, `PlazaLayout`이다.
- 작업: 일회성 패치 툴 3개를 지웠다. 대상은 `QuestMarkBuilder`, `MinigameWindowPrefabBuilder`, `JumpRopeSampleBuilder`이다.
- 원인: `Plaza.unity`는 생성 뒤에 직접 수정되었다. `PlazaSceneBuilder`를 다시 실행하면 Generated 루트를 지워서 그 수정이 사라진다.
- 작업: `PrefabPathFor`를 `VoxelPrefabCreator`로 옮겼다. `ToolDefines.PlazaPrefabFolder`의 이름을 `VoxelPrefabFolder`로 바꿨다.
- 작업: 쓰지 않는 `ToolDefines` 상수를 지웠다. 테스트 `PlazaNpcDialogueIds_Exist`를 지웠다. 이 테스트의 데이터 원본인 `PlazaLayout`이 없어졌다.
- 작업: `Game/CLAUDE.md`, `Docs/MinigameFramework.md`, `Docs/Roadmap.md`, 주석 2곳에서 지운 메뉴의 언급을 지웠다.
- 결과: 생성된 에셋(씬, 프리팹, 머티리얼, 볼륨 프로파일)은 그대로 있다. UI 빌더 3개(`MenuUIBuilder`, `NotebookUIBuilder`, `UIBuildUtil`)는 남겼다.
- 결과: 오프라인 `dotnet build`가 `Project.Scripts`, `Project.Scripts.Editor`, `EditModeTests`에서 성공했다.
- 결과: Fable 리뷰에서 3건이 나왔다. 오래된 메뉴 언급 2건을 고쳤다. 테스트 대체 1건은 적용하지 않았다.
- 확인 안 함: Unity 에디터 재임포트와 EditMode 테스트. 에디터를 조작하지 않았다.
- 다음: 에디터에서 컴파일을 확인하고 EditMode 테스트를 실행한다.

## 2026-10-05 19:50 | t3code/cleanup-setup-tools
- 작업: PR #10의 충돌을 풀려고 `origin/main`을 병합했다.
- 원인: main의 분수 작업이 Plaza 툴 4개(`PlazaLayout`, `PlazaRenderSetup`, `PlazaSceneBuilder`, `PlazaVoxelPrefabs`)를 고쳤다. 이 브랜치는 그 파일을 지웠다.
- 작업: 4개 파일을 지우는 쪽으로 충돌을 풀었다. 분수는 `PF_FountainSet`과 `Plaza.unity`에 이미 있어서 빌더 수정이 필요 없다.
- 결과: 지운 툴과 상수를 참조하는 코드와 문서는 없다. 오프라인 `dotnet build`가 `EditModeTests`와 그 의존 어셈블리에서 성공했다.
- 확인 안 함: Unity 에디터 재임포트와 EditMode 테스트. 에디터를 조작하지 않았다.
- 다음: 에디터에서 컴파일을 확인하고 EditMode 테스트를 실행한다.
