# 코어 게임플레이 루프 개선

## 2026-10-05 10:46 | t3code/core-gameplay-loop
- 작업: 테이블 로드를 동기 방식으로 바꾸었다. 일시정지 중 플레이어 정지를 유지했다. UI 대기를 unscaled 시간으로 바꾸었다.
- 작업: 퀘스트와 수첩 상태를 플래그에 저장했다. 대화 동사 `startQuest`, `completeQuest`, `addNote`, `strikeNote`, `meet`를 추가했다.
- 작업: `Quest`, `QuestObjective`, `Notebook` 테이블을 추가했다.
- 결과: 커밋 866b209, 6d8fb4e, fe08793.
- 확인 안 함: Unity EditMode 테스트. 배치모드 Unity가 라이선스 오류로 실행되지 않았다.
- 다음: 수첩 UI를 만든다.

## 2026-10-05 11:15 | t3code/core-gameplay-loop
- 작업: `NotebookUI`, HUD 퀘스트 목록, HUD 토스트를 추가했다. 메뉴 `LOTW/UI/Build Notebook UI`를 추가했다.
- 작업: 저장 파일에 버전, 플레이 시간, `loadCount`를 추가했다. `SaveManager.NewGame`을 추가했다.
- 작업: `TitleUI`, `SettingsUI`, `PauseUI`, `ConfirmUI`를 추가했다. 메뉴 `LOTW/UI/Build Title And Menus`를 추가했다.
- 결과: 커밋 10be591, 0a6cb23, 5005add.
- 확인 안 함: UI 빌드 메뉴 2개. 미니게임 브랜치 머지 전이라 실행하지 않았다.
- 다음: 프롤로그 시퀀스를 만든다.

## 2026-10-05 11:43 | t3code/core-gameplay-loop
- 작업: `Sequence` 테이블과 `SequencePlayer`를 추가했다. 새 게임이 프롤로그(대화 9000-9022)를 재생한다.
- 작업: `Inspectable`, 대화 액션 `save`, `talk.{characterId}` 플래그를 추가했다.
- 작업: `QuestMarkIndicator`와 메뉴 `LOTW/UI/Add Quest Mark To NPC Base`를 추가했다.
- 작업: `Plaza.unity`의 NPC 6명에 `characterId`를 YAML로 넣었다.
- 작업: `StoryGate`(`IInteractionGate`)와 `StoryConditionToggle`을 추가했다.
- 결과: 커밋 30e1713, 14c1947, c68945c, 94f6756.
- 확인 안 함: 게임 실행. `StoryGate`와 `StoryConditionToggle`은 씬에 배치하지 않았다.
- 다음: 미니게임 브랜치를 머지한다.

## 2026-10-05 11:56 | t3code/core-gameplay-loop
- 작업: `t3code/minigame-framework-design`(83a5e9d)를 머지했다. 충돌 5개를 양쪽 내용을 모두 살려 해결했다.
- 작업: 입력 액션 `Notebook`(Q)과 `Pause`(Esc)를 추가했다. `MenuInput`과 `BaseUI.OnCancel`을 추가했다.
- 작업: HUD 수첩 버튼에 키 표시를 추가했다.
- 결과: 커밋 213aea4, a2885c1. 오프라인 빌드에서 오류가 0개였다.
- 결과: 오프라인 러너에서 테스트 58개가 통과했다. 7개가 실패했다. 7개 모두 오프라인에서 실행할 수 없는 LogAssert 테스트다.
- 확인 안 함: Q/Esc 입력, 프롤로그 재생. Unity 에디터가 닫혀 있었다.
- 다음: main에 머지한다.

## 2026-10-05 12:20 | t3code/core-gameplay-loop
- 작업: 다른 세션이 이 브랜치를 main에 머지했다(24f1864, bdd6472).
- 작업: main(313d051)을 이 브랜치에 fast-forward로 머지했다.
- 작업: 세션 로그 규칙에 따라 이 로그 파일을 만들었다.
- 결과: `main..HEAD`에 커밋이 0개다. 이 브랜치의 작업이 모두 main에 있다.
- 확인 안 함: EditMode 테스트, 게임 실행. Unity 에디터가 닫혀 있었다.
- 다음: 에디터에서 UI 빌드 메뉴 3개를 실행한다. EditMode 테스트를 실행한다. 새 게임에서 프롤로그와 Q/Esc 입력을 확인한다.

## 2026-10-05 13:44 | t3code/core-gameplay-loop
- 작업: 플레이 중 캐릭터가 움직이지 않는 문제를 조사했다.
- 결과: Plaza 플레이에서 `Time.timeScale`이 0이었다. `CoreManager.IsPaused`가 true였다. PauseUI는 열려 있지 않았다.
- 원인: `runInBackground`가 0이다. 에디터가 포커스를 잃으면 `OnApplicationPause(true)`가 게임을 멈춘다. 커밋 866b209 이후 포커스가 돌아와도 풀리지 않았다.
- 작업: `CoreManager`가 앱 때문에 멈춘 경우를 기록한다. 포커스가 돌아오면 그 경우만 푼다. PauseUI로 연 일시정지는 유지한다.
- 다음: 에디터에서 Plaza를 플레이하고 이동을 확인한다.

## 2026-10-05 13:50 | t3code/core-gameplay-loop
- 작업: 실행 중인 에디터에서 6b3056b 수정을 확인했다.
- 결과: 포커스가 없는 상태로 플레이하면 `_pausedByApplication`이 true였다. `OnApplicationPause(false)`를 호출하면 `timeScale`이 1로 돌아왔다.
- 결과: `PauseGame`으로 멈춘 뒤 포커스를 잃고 돌아와도 `IsPaused`가 true로 남았다.
- 확인 안 함: 실제 키 입력으로 걷기. CLI로 에디터 창에 포커스를 줄 수 없었다.
- 다음: 사용자가 Plaza를 플레이하고 이동을 확인한다.
