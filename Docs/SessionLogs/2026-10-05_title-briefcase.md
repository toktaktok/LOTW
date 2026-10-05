# 타이틀 서류가방 버튼 가림

## 2026-10-05 15:30 | main
- 작업: 타이틀 화면에서 서류가방이 종이 버튼 3개를 가리는 문제를 고쳤다.
- 원인: 종이가 가방 이미지 뒤에 있었다. 열린 뚜껑도 같은 이미지라서 종이를 올려도 가려졌다.
- 작업: `MenuUIBuilder`가 종이를 가방 앞에 그린다. 새 `SheetClip`(`RectMask2D`)이 가방 앞면 윗변(y 24) 아래를 자른다.
- 작업: 종이 아래 기준을 y -200에서 -40으로 올렸다. 종이 너비를 150에서 130으로 줄여 20 간격을 두었다.
- 결과: 에디터에서 `PF_TitleUI.prefab`을 같은 경로에 다시 만들었다. GUID `7d5cf2e6...`가 그대로다.
- 결과: 1920x1080 렌더에서 이어하기, 새로하기, 설정 종이가 보였다.
- 확인 안 함: 플레이 모드에서 클릭과 새 게임 프롤로그 진행. 사용자가 직접 확인한다.
- 확인 안 함: 왼쪽 종이와 가방 윗변 사이에 틈이 있다. 윗변은 기울었고 자르는 선은 수평이다.
- 다음: 타이틀에서 새로하기를 눌러 프롤로그 시퀀스를 확인한다.

## 2026-10-05 16:10 | main
- 작업: Title 시작 시 `TitleUI.Awake` 32행의 NullReferenceException을 고쳤다.
- 원인: 프리팹을 만들 때 부모 `TitleUI.Awake`가 자식 `TitleSheet.Awake`보다 먼저 실행되었다. `TitleSheet.Button`이 null이었다.
- 원인: 예외 때문에 버튼 리스너가 등록되지 않았다. 그래서 버튼을 눌러도 반응이 없었다.
- 작업: `TitleSheet.Button`이 처음 접근할 때 `GetComponent<Button>()`으로 가져온다.
- 결과: 에디터 플레이에서 예외가 나오지 않았다. 새로하기를 누르자 Plaza로 이동했다. `SequencePlayer.IsPlaying`이 true였고 `DialogueUI`가 열렸다.
- 확인 안 함: 프롤로그 대화 9000-9022를 끝까지 진행하는 것. 사용자가 직접 확인한다.
- 참고: 자동화 중에는 에디터 포커스가 없어서 게임 루프가 멈췄다. 확인할 때만 런타임에서 `Application.runInBackground`를 켰다. `PlayerSettings`는 바뀌지 않았다.
- 다음: 프롤로그를 끝까지 플레이한다.

## 2026-10-05 17:40 | fix/title-menu
- 작업: main 작업 트리에서 브랜치 `fix/title-menu`를 만들었다. 사용자가 worktree 대신 이 방식을 골랐다.
- 작업: 타이틀에 종료 종이를 추가했다. 확인 팝업에서 예를 누르면 종료한다. Text 키 `ui.title.quit`, `ui.title.quit.confirm`을 추가했다.
- 원인: 타이틀로 돌아와도 프롤로그 시퀀스가 남았다. 이어하기 뒤에 프롤로그 대화가 열렸다.
- 작업: `SequencePlayer.Stop()`을 추가했다. `TitleScene.Start`가 이 함수를 부른다.
- 작업: `UIManager`가 새 페이지를 열 때 아래 페이지의 선택을 기억하고 상호작용을 끈다. 위 페이지를 닫으면 둘 다 되돌린다.
- 작업: 설정 창이 열리면 슬라이더를 선택한다. 타이틀이 처음 보일 때 선택된 종이가 올라온다.
- 작업: `CreateMenuPanel`의 `childControlWidth`를 껐다. 설정과 일시정지 버튼의 너비가 0이던 문제를 고쳤다.
- 결과: 에디터 플레이에서 새로하기, 이어하기, 설정, 종료, 일시정지에서 타이틀로 가기가 정상이었다.
- 결과: EditMode 테스트 250개가 통과했다. 이 결과는 리뷰 수정 전의 결과다. Python 테스트 11개가 통과했다.
- 결과: Fable 리뷰에서 5건이 나왔다. 5건 모두 에디터에서 재현했고 고쳤다.
- 확인 안 함: 리뷰 수정 뒤 일시정지 흐름의 재확인과 EditMode 재실행. 사용자가 에디터에서 Character 씬을 플레이하기 시작해서 중단했다.
- 참고: 편집 모드에서 실행된 상태 조회가 Title 씬에 `UIManager`, `SceneTransitionManager`를 만들었다. 두 오브젝트는 지웠다. Title 씬은 dirty 상태로 남아 있고 저장하지 않았다.
- 다음: 에디터가 비면 일시정지 흐름을 다시 확인하고 EditMode 테스트를 실행한다. 그 뒤 커밋한다.

## 2026-10-05 18:30 | fix/title-menu
- 작업: 리뷰 수정 뒤 타이틀과 일시정지 흐름을 에디터 플레이에서 다시 확인했다.
- 결과: 확인, 설정, 일시정지 창이 열린 동안 아래 UI로 키보드 이동이 새지 않았다.
- 결과: 종료 확인에서 아니오를 누르자 선택이 `Sheet_2`로 돌아왔다. 설정을 닫자 선택이 타이틀 종이로 돌아왔다.
- 결과: 이어하기가 Plaza를 불러왔다. 일시정지 > 설정 > Esc 뒤에 선택이 `ResumeButton`으로 돌아왔다.
- 결과: EditMode 테스트 250개가 통과했다.
- 확인 안 함: 세이브가 없을 때 새로하기에서 왼쪽 이동. 세이브 파일을 옮겨야 해서 하지 않았다.
- 참고: 이 커밋에 넣지 않은 변경이 있다. 폰트 에셋 3개, `PF_NPC_Base`, `PF_DialogueUI`, `Plaza.unity`, `ProjectSettings.asset`(`runInBackground: 1`)이다.
- 다음: 사용자가 프롤로그를 끝까지 플레이한다.

## 2026-10-05 19:00 | fix/title-menu
- 작업: Plaza 주민 5명의 스프라이트를 원래 이미지로 복구했다. 대상은 Catty, PhoneKids, Seal, Mouse, Mayor다.
- 원인: `PF_NPC_Base`의 기본 `profile`이 `CharacterProfile_Gumman`이었다. 모든 NPC 인스턴스가 이 값을 물려받았다.
- 원인: `NPC.OnValidate`와 `ApplyProfile`이 씬의 스프라이트를 Gumman 스프라이트로 덮어썼다. 표시 이름도 Gumman이 되었다.
- 작업: `PF_NPC_Base`의 `profile`을 비웠다. Plaza의 `NPC_Gumman`에만 Gumman 프로필을 지정했다.
- 작업: `Plaza.unity`의 스프라이트 참조 5개를 커밋 상태로 되돌렸다.
- 참고: `PF_NPC_Base`에 있던 `QuestMark` 자식과 새 필드 직렬화를 같이 커밋했다. 메뉴 `LOTW/UI/Add Quest Mark To NPC Base`가 만든 변경이다.
- 결과: 편집 모드와 플레이 모드에서 NPC 6명이 각자의 스프라이트를 보였다.
- 다음: 없음.

## 2026-10-05 19:20 | main
- 작업: `fix/title-menu`를 main에 병합했다. 병합 커밋은 `369931b`다. 로컬 브랜치 `fix/title-menu`를 지웠다.
- 작업: 남은 에디터 변경을 커밋했다. 대상은 폰트 에셋 3개, `PF_DialogueUI`, `ProjectSettings.asset`이다.
- 참고: `PF_DialogueUI`에 `ConfirmButton`이 생겼다. 텍스트 3개의 폰트가 Galmuri11에서 DungGeunMo로 바뀌었다.
- 참고: `ProjectSettings.asset`의 `runInBackground`가 1이 되었다. 빌드에서 창이 포커스를 잃어도 게임이 계속 돈다.
- 확인 안 함: 바뀐 폰트가 의도한 것인지. 사용자가 확인한다.
- 다음: main을 `origin/main`에 push할지 사용자가 정한다.

## 2026-10-05 19:40 | main
- 작업: main을 `origin/main`에 push했다. 대상 커밋은 `590281b`다.
- 작업: 사용자가 DungGeunMo 폰트 변경이 의도라고 확인했다.
- 작업: `PlayerSettings.runInBackground`를 0으로 되돌렸다. 에디터 API로 바꾸고 저장했다.
- 다음: 없음.
