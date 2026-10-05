# LOTW 코어 루프 로드맵

근거: 기획 `Docs/DesignNotes.md` (Notion "로비"), 코드 감사 (2026-10-05), 데이터 구조 `Docs/DataFlow.md`.
목표: Ch1 데모를 처음부터 끝까지 플레이 가능하게 만드는 시스템. 장르는 "추리게임의 형식을 차용한 어드벤처", 탐정 행동은 개별 '창'의 은유적 미니게임으로 나타난다.

## 코어 루프 (기획 기준)

1. 스토리 비트/컷신 -> 시간대 진행 (시계가 아니라 스토리에 묶임)
2. 탐색: 의뢰 있는 NPC 는 땀 제스처, 커서는 당근 -> 상호작용 대상 위에서 돋보기
3. 대화: 첫 만남이면 수첩에 이름/프로필 기록. 선택지 = 사담 / 조사 / 부탁 / 종료
4. 조사 -> 알리바이 미니게임 -> 수첩에 알리바이/증거 기록
5. 부탁 -> 서브 의뢰 -> 보상 (황금벌레 장치 부품)
6. 수첩 정리: 사건별 알리바이, 의문점, 사건 블록 연결
7. 지역 게이트: 핵심 주민 수첩 정보가 차면 다음으로
8. 메인 비트 진행 (다음 시간대 / 지그 만남) -> 반복

## 병행 작업

- 미니게임 프레임워크(`t3code/minigame-framework-design`, `Docs/MinigameFramework.md`)를 이 브랜치에 머지함 (2026-10-05). 충돌은 양쪽 추가분을 모두 남겨 해결.
- 머지 후 조정: `MinigameContext` 에 `PlaySequence`/`SaveGame` 전달, `SequencePlayer` 대화 스텝은 `minigame:` 로 대화가 잠시 닫혀도 끝으로 보지 않음.
- 메인 체크아웃(main)에는 미니게임 파일 사본과 DOTween 이 커밋되지 않은 채 남아 있음. 이 브랜치를 main 에 합치기 전에 그 사본을 정리해야 충돌이 나지 않음.

## 단계

상태: [x] 완료, [~] 진행 중, [ ] 대기

### 0단계. 기반 정리
- [x] `DataManager` 테이블 동기 로드 (첫 프레임 null / `@key` 경합 제거)
- [x] `CoreManager.OnApplicationPause` 가 플레이어가 연 일시정지를 풀던 버그
- [x] `BaseUI` 애니메이션 대기, `CameraManager` 블렌드 대기를 unscaled 시간으로 (timeScale 0 에서 멈춤 방지)
- [ ] (미니게임 브랜치 머지 후) 입력 단일 소유자: Player 맵에 `Notebook`(Q), `Pause`(Esc) 추가, `UI/Cancel` 로 최상단 페이지 닫기. `PlayerController`/`PlayerInteractor`/`DialogueUI` 각자 만드는 `PlayerControls` 통합
- [x] `UIManager`: `Get<T>()`, `Close<T>()`, `IsOpen<T>()` 추가. `PlayerInteractor` 의 `FindFirstObjectByType<HudUI>` 제거
- [x] `Dialogue.xlsx` 가 8행, `Dialogue.json` 이 50행으로 어긋나 있던 것 복구 (JSON 기준 재생성, 변환 왕복 일치 확인). 이전에는 ConvertTable.bat 실행 시 Plaza 대화가 지워졌음
- [ ] `NPC.Interact`: 페이지 push 실패 시 카메라 복구
- [ ] (미니게임 브랜치 머지 후) `GameInstance`, `FlagManager`, `SceneTransitionManager` 를 SubSystemCollection 에 배치

### 1단계. 스토리 진행 상태 (의뢰 / 챕터 / 시간대)
상태는 전부 플래그(`StoryKeys`)로 둔다. 이미 세이브되는 `SaveData.flags` 에 실리고 대화 조건으로 바로 읽힌다.
- [x] `Quest` 테이블: type(main/sub), chapter, giverId, title, summary, objectiveIds
- [x] `QuestObjective` 테이블: text, conditions (대화 조건 문법을 매번 평가. 비어 있으면 자동 완료 안 됨)
- [x] `QuestLog` (정적 조회): 상태, 진행 중 목록(메인 먼저), 목표, 진행도
- [x] 챕터/시간대 키 `chapter`, `timeSlot` (`StoryKeys`). 시간대 전환 연출은 6단계
- [x] 대화 액션 `startQuest/completeQuest`, 조건 `quest:id[=1|2|3]`
- [x] Plaza NPC 대사 연결: 촌장(메인 1 시작), 검맨(서브 101 얼굴 트기 시작), 주민 첫 대화마다 `meet`
- [ ] 챕터 종료 시 이전 챕터 의뢰 잠금 (기획 미정: 취소 / 잠금)
- [ ] 동시 활성 상한, 주민당 1개 검증 (테이블 테스트로 할지 런타임으로 할지)
- [ ] 대화 명령을 사전 기반 레지스트리로 (동사가 더 늘면)

### 2단계. 탐정 수첩 데이터
- [x] `Notebook` 테이블: category(document/profile/alibi/question/todo), caseId, subjectId, title, body
- [x] `NotebookLog` (정적 조회): 해금 목록(사건별, 취소선 하단), 안읽음 수, 읽음 처리
- [x] 대화 액션 `addNote:id`, `strikeNote:id`, `meet:characterId`, 조건 `note:id`, `met:characterId`
- [x] 첫 만남 -> 주민 프로필 해금 (대화 행 액션 `meet:npc_x;addNote:id`)
- [x] 테이블 교차 검증 테스트 (`StoryTableTests`): 목표/의뢰/수첩 ID 참조, 챕터당 메인 1개
- [ ] 본문 `@key` 다국어화 (지금은 한국어 원문)

### 3단계. HUD 와 수첩 UI
- [x] HUD 코드: 수첩 버튼(안읽음 배지), 의뢰 목록(최대 3, 메인 굵게 첫 줄), 알림 큐("수첩에 기록됨", 새 의뢰, 의뢰 완료), 상호작용 힌트(기존). 플래그 변경을 모아 LateUpdate 에서 갱신
- [x] `NotebookUI` 코드: 탭(표지/의뢰/주민/알리바이/의문점/서류), 왼쪽 목록 + 오른쪽 상세, 표지 포스트잇(진행 중 의뢰의 다음 목표), 의뢰 상세(요약, 진행 %, 완료 목표 취소선), 사건별 머리줄과 취소선 하단, 클릭 시 읽음 처리
- [x] `NotebookPageBuilder` (순수 화면 내용 생성) + 테스트, `LocalizedText` (고정 라벨 '@키'), Text 키 `ui.notebook.*`, `ui.toast.*`
- [ ] (미니게임 브랜치 머지 후) 메뉴 `LOTW/UI/Build Notebook UI` 실행: `PF_NotebookUI` 생성, `PF_HudUI` 에 수첩 버튼/의뢰 목록/알림 추가, `uiPrefabs` 등록. 이후 Editor 에서 배치/가독성 확인
- [ ] 의뢰 상세의 증거 목록, 맨 뒤 페이지(완료 의뢰/보상) -- 보상/증거 데이터가 생기면
- [ ] 목록 스크롤 (항목이 한 페이지를 넘으면)
- [ ] 대화 중 열기: 지금은 `HasBlockingPage` 면 버튼 무시. 기획대로 대화 중에도 즉시 표시하려면 입력 통합(0단계) 후
- [ ] 스타일: 중간 톤 크래프트 브라운 종이(크림 금지), 보라 금지, 알약 버튼 금지, Galmuri11, PPU 25 픽셀. 수첩 스프라이트는 400px 를 3배 정수 배율

### 4단계. 저장
- [x] `SaveData` 확장: version(`SaveDefines.Version`, `SaveManager.Upgrade` 로 옛 세이브 변환), playTime(timeScale 0 중 정지). 의뢰/수첩/만남은 플래그라 따로 필드 불필요
- [x] 불러온 횟수 = 플래그 `loadCount` (`LoadAndApply` 가 올림). 지그 메타 대사는 `flag:loadCount>=n` 조건으로
- [x] 새 게임 리셋 `SaveManager.NewGame(slot)`: 플래그(의뢰/수첩 포함), 인벤토리, 플레이 시간. 불러오기/리셋은 변경 이벤트를 내지 않아 알림이 쏟아지지 않음
- [x] I/O 예외 처리: 임시 파일에 쓰고 교체(쓰는 중 종료돼도 기존 세이브 보존), 빈/깨진 파일은 null. `SaveSystem<T>` 를 별도 파일로 분리, 테스트용 rootPath
- [ ] 위치: 레일 노드 좌표 대신 `entranceId` 로 복원. 저장 지점(사무소 등)에 입구 ID 를 두는 방식으로 6단계에서 결정
- [ ] `ISaveable` 등록 방식: 저장 대상이 인벤토리/플래그 둘뿐이라 보류. 세 번째 대상이 생기면 도입
- [ ] 플레이 시간이 타이틀 화면에서도 늘어남 -> 5단계 타이틀 씬에서 제외

### 5단계. 메인 화면 / 설정 / 일시정지
- [x] 코드: `TitleUI` (서류가방 종이 3장 이어하기 / 새로하기 / 설정, `TitleSheet` 가 선택 시 올라옴, 키보드 좌우 이동), `TitleScene` (플레이 시간 정지, BGM, 메뉴 열기)
- [x] 이어하기 = 가장 최근 세이브(`SaveSlotSelector.FindLatest`), 없으면 잠김. 종이 아래 장소 / 플레이 시간 표시. 새로하기는 세이브가 있으면 덮어쓰기 확인
- [x] `SettingsUI` (마스터/BGM/SFX 슬라이더, 언어 전환), `PauseUI` (계속 / 설정 / 타이틀로 + 확인, 열린 동안 일시정지), 범용 `ConfirmUI` (기본 선택 '아니오')
- [x] 에디터 메뉴 `LOTW/UI/Build Title And Menus`: 프리팹 4개, uiPrefabs 등록, `Scenes/Title.unity` 생성(추가 모드로 열고 저장 후 닫음), 빌드 인덱스 0. UI 빌더 공용 함수 `UIBuildUtil`
- [ ] (미니게임 브랜치 머지 후) 위 메뉴 실행, 플레이 모드에서 종이 위치/서류가방 크기 맞추기
- [ ] (입력 통합 후) Esc 로 `PauseUI` 열기/닫기, UI/Cancel 로 설정/확인 닫기
- [ ] 슬롯 선택 UI: 지금은 슬롯 하나를 이어 쓰는 방식(새 게임 = 마지막 슬롯). 여러 슬롯이 필요하면 `PeekSave` 로 목록 화면
- [ ] 서류가방 열림 애니메이션(시트 8프레임, `RawImage.uvRect` 로 프레임 전환), 마지막 저장 장소에 따른 배경 연출(카페/기차)
- [ ] 새 게임 첫 씬 `SceneDefines.NewGameStart` = Plaza 임시. 6단계 프롤로그 씬으로 교체

### 6단계. 프롤로그와 시퀀스
- [x] `Sequence` 테이블 + `SequencePlayer`: 대화 / 대기 / 페이드 아웃·인 / 카메라 / 씬 이동 / 액션 스텝을 nextId 로 연결, 스텝별 conditions. 씬이 바뀌어도 이어짐, 재생 중 플레이어 입력/수첩 버튼 차단
- [x] 시작 경로: 대화 액션 `sequence:id`(대화가 닫힌 뒤), `SequenceTrigger`(씬 시작 / 트리거 진입), 새 게임 = `NewGame` 후 프롤로그(`StoryDefines.PrologueSequenceId`)
- [x] `SceneTransitionManager.FadeAsync`: 씬 전환 없는 페이드. 가린 채로 씬 이동하면 깜박임 없이 이어짐
- [x] 프롤로그 초안(시퀀스 1-11, 대화 9000-9022): 밤 버스 도착 -> 언덕 -> 촌장 브리핑(메인 의뢰 1, 촌장 프로필) -> 7시 사무소 오픈. 지금은 모두 Plaza 에서 진행
- [x] 시간대 `TimeSlot`(플래그 timeSlot), 저녁 전환 시퀀스 100-102 (아직 아무도 부르지 않음)
- [ ] 사무소(2F) / 버스 정류장 / 언덕 씬이 생기면 프롤로그 scene 스텝과 카메라 스텝 교체
- [x] 시간대/진행에 따라 오브젝트 켜고 끄기: `StoryConditionToggle` (조건 문법, 예 `flag:timeSlot==3`)
- [ ] 시간대별 배경음 전환, 조명 색 (해 방향은 고정 유지) -- 저녁/밤 씬 아트가 생기면
- [ ] 자막 카드 스텝 (검은 화면 위 날짜 문구) -- 지금은 이름 없는 내레이션 대화로 대신함
- [ ] 저장 지점: 사무소 책상 등 `Inspectable` 에서 `SaveCurrent` (7단계)

### 7단계. 월드 상호작용 보강
- [x] `Inspectable`: 조사하기 -> 내레이션 대화. 수첩 기록은 행의 `addNote:id`, 저장은 새 액션 `save` (현재 슬롯, HUD 알림 `@ui.toast.saved`). 책상 대화 초안 9100-9103
- [ ] 사무소 씬에 책상 `Inspectable`(dialogueId 9100) 배치 -- 사무소 씬 필요
- [x] NPC `characterId` (Plaza 6명 지정) + 대화 횟수 플래그 `talk.{id}` (말을 걸 때마다 +1). 검맨 1110-1113 에 횟수별 한마디 예시
- [ ] 주민별 챕터당 한마디 5개 대사 작성 (분기 행 + `flag:talk.{id}` / `flag:chapter`)
- [ ] 의뢰 보유 시 표정 변경 -- 표정 스프라이트 필요. 대사 변경은 지금도 `quest:` 조건으로 가능
- [x] 의뢰 땀 표시 `QuestMarkIndicator`: 지금 챕터에 시작 전 의뢰(giverId)가 있으면 머리 위 땀 2프레임. 메뉴 `LOTW/UI/Add Quest Mark To NPC Base` (미니게임 머지 후 실행)
- [x] 지역 게이트 `StoryGate` (`IInteractionGate`): 같은 오브젝트의 `RailConnector`/`SceneExitZone` 이동 전에 조건 검사, 막히면 내레이션 9200
- [ ] 다음 지역(중간 거주지)이 생기면 광장 출구에 게이트 조건(핵심 주민 수첩 항목) 지정
- [ ] (미니게임 머지 후) 커서 당근 / 상호작용 대상 위 돋보기 (`TX_UI_Cursor_Carrot`, `TX_UI_Magnify_Single`) -- SubSystemCollection 에 둘 소프트웨어 커서

### 8단계. 창 미니게임과 추리
- [x] 미니게임 창 프레임 (머지됨, 줄넘기 샘플)
- [ ] 알리바이 미니게임 (설득/압박 선택)
- [ ] 수첩 사건 블록 연결 추리 (지그 범인 결론은 완성 가능, 보물 사건 전체는 의도적으로 불완전)

## 결정 필요 (기획 충돌)

- 도난 날짜: 알리바이 페이지 기준(12/10 도난, 12/12 발견, 12/16 Ch1 시작) 권장
- 첫 의뢰 주체: 촌장(메인) + 검맨(튜토리얼 서브 101) 로 가정
- 도착 수단: 버스 / 택시
- 동시 활성 의뢰 상한: 4(메인+서브3) / 3 -> 일단 메인1 + 서브3, HUD 표시는 3줄
- 챕터 종료 시 미완료 의뢰 처리
- 대화 선택지 세트 (사담/조사/부탁/종료 로 가정)
- 보물의 정체
