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

- 미니게임 프레임워크(8단계의 '창')는 별도 브랜치 `t3code/minigame-framework-design` (워크트리 t3code-89a2f4d0, `Docs/MinigameFramework.md`)에서 진행 중. 그쪽이 `InputSystem_Actions`(Minigame 맵), `SubSystemCollection.prefab`, `DialogueUI`, `DialogueCommands`(`minigame:`/`var:`), `Enums.cs`, `IInteractable.CanInteract` 를 고친다.
- 이 브랜치는 머지 전까지 입력 액션 에셋, SubSystemCollection 프리팹, DialogueUI 를 고치지 않는다. 새 UI 프리팹은 에디터 메뉴 빌더로 만들고 머지 후 실행한다. `DialogueCommands`/`Enums.cs` 는 양쪽 모두 끝에 추가만 해서 충돌이 나도 합치기 쉽다.

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
- [ ] `SaveData` 확장: version, playTime, 레일 노드/위치, quests, notebook, loadCount(지그 메타 대사용)
- [ ] 저장 대상 등록 방식(`ISaveable`)으로 `SaveCurrent`/`LoadAndApply` 하드코딩 제거
- [ ] 새 게임 리셋 (플래그/인벤토리/의뢰/수첩)
- [ ] I/O 예외 처리, 저장 위치 = 사무소 등 지정 지점 (타이틀 연출이 마지막 저장 위치를 따름)

### 5단계. 메인 화면 / 설정 / 일시정지
- [ ] Title 씬(빌드 인덱스 0): 사무소 서류가방, 종이 3장 이어하기 / 새로하기 / 설정, 선택된 종이가 살짝 올라옴
- [ ] 슬롯 UI (`PeekSave` 로 시간/장소 표시)
- [ ] 설정 UI (마스터/BGM/SFX, 언어)
- [ ] 일시정지 메뉴 (Esc): 계속 / 설정 / 타이틀로

### 6단계. 프롤로그와 시퀀스
- [ ] 시퀀스 실행기: 대화 / 카메라 / 페이드 / 대기 / 씬 이동 / 플래그 스텝을 순서대로 (테이블 또는 SO)
- [ ] 프롤로그: 밤 버스 도착 -> 언덕 오르기 -> 2F 사무소에서 촌장 브리핑 -> 7시 사무소 오픈
- [ ] 시간대 전환 연출 (저녁으로 페이드)

### 7단계. 월드 상호작용 보강
- [ ] `Inspectable` 오브젝트: 책상 서류/팸플릿 -> 수첩 기록
- [ ] NPC 대화 횟수별 한마디 (챕터당 5개), 의뢰 보유 시 표정/대사 변경
- [ ] 의뢰 표시 (말풍선/땀), 커서 당근/돋보기

### 8단계. 창 미니게임과 추리
- [~] 미니게임 창 프레임: 병행 브랜치에서 진행 중
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
