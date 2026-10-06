# 미니게임 프레임워크 설계

- 작성일: 2026-10-05
- 상태: 1차 구현 완료 (12장 "구현 현황" 참고. 본문과 다른 부분은 12장이 우선)
- 근거: `GameDesign.md` 5장 "미니게임", `Game/CLAUDE.md` 아키텍처

## 1. 목표와 기획 제약

| 요구 | 설계에서의 대응 |
|---|---|
| 특정 상호작용 시 별도 팝업에서 미니게임 진행 | `MinigameWindow`(BaseUI)를 Popup 레이어에 Push |
| 현실 오브젝트를 과장/변형한 다른 동작 | 미니게임은 월드와 분리된 "스테이지"에서 돌고, 발생원(source) 정보를 받아 연출에 씀 |
| 미니게임별 보상 | 결과(outcome)마다 액션 문자열 실행 (기존 대화 DSL 재사용) |
| 윈도우 팝업 + 현미경 투영, 반투명 프레임 | 렌즈 셰이더를 씌운 RawImage + 반투명 창틀 |
| 미니게임이 입력을 가져가고, 닫기/별도 동작으로 끝내야 복귀 | 전용 `Minigame` 액션 맵. 세션 동안만 켜지고 플레이어 입력은 차단 |
| 미니게임마다 동작 조건, 완료 조건이 다름 | 시작 조건 + 결과 규칙을 데이터로 정의. 코드로 직접 종료도 가능 |
| 편집 자유도 최대 | "형식(메커닉)"과 "인스턴스(정의)"를 분리 |
| 기획 규칙: 한 번만 쓰는 형식 금지 | 같은 메커닉을 정의만 바꿔 여러 곳에 재사용하는 구조 자체가 이 규칙을 강제 |
| 눈사람 스타일 / 지그 스타일 | 창틀/렌즈 프리셋(`MinigameFrameStyle`)으로 분리 |

## 2. 핵심 개념: 형식과 인스턴스

```
Mechanic (형식, 코드 + 프리팹)        Definition (인스턴스, 데이터 에셋)
  PF_MG_Roulette                      MinigameDefinition_VendingRoulette
  - MinigameBase 상속                   - 어떤 메커닉을 쓰는지
  - 조작, 연출, 변수 갱신                - 파라미터 (속도, 칸 수, 제한 시간...)
  - 결과 판정은 하지 않음(기본)          - 시작 조건, 결과 규칙, 보상, 창틀 스타일
```

- 메커닉은 "무엇을 하는가"만 안다. 몇 점이면 성공인지, 무엇을 주는지는 모른다.
- 정의는 메커닉을 골라 숫자와 규칙을 채운다. 같은 룰렛으로 자판기, 바 칵테일, 축제 뽑기를 만들 수 있다.
- 기획자가 코드 없이 바꿀 수 있는 범위: 파라미터, 시작 조건, 성공/실패 조건, 보상, 재도전 정책, 창틀 스타일, 제목.

## 3. 전체 흐름

```
[트리거]                        [MinigameManager]                         [UI / 스테이지]
MinigameTrigger.Interact ---+
DialogueAction minigame:id -+-> TryStartAsync(def, source)
                                 1. 시작 조건 검사 (실패 시 deniedDialogueId 또는 무시)
                                 2. 스테이지 프리팹 생성 (Minigame 레이어, 월드 밖 좌표)
                                 3. MinigameSession 생성, 메커닉 Setup
                                 4. UIManager.PushPage<MinigameWindow>  --> 창 열림, 렌즈에 RT 표시
                                 5. Minigame 액션 맵 Enable              --> 플레이어는 HasBlockingPage로 이미 차단
                                 6. 매 프레임 Tick: 메커닉 갱신 -> 결과 규칙 평가
                                 7. 결과 확정 (규칙 매칭 / 메커닉 End / 닫기=Abort)
                                 8. 결과 액션 실행 (보상, 플래그), 기록 플래그 갱신
                                 9. 액션 맵 Disable, PopPage, 스테이지 파기  --> 창 닫힘
                                10. MinigameEndedSignal 발행, 호출자에게 MinigameResult 반환
```

## 4. 구성 요소

### 4.1 데이터

**`MinigameDefinition` (ScriptableObject, `LOTW/Minigame Definition`)**

| 필드 | 타입 | 설명 |
|---|---|---|
| `id` | string | 대화 액션과 기록 플래그에서 쓰는 키 (`vending_roulette`) |
| `title` | string | 미니게임 이름. `@key` 로컬라이즈. 창에는 표시하지 않는다 (2026-10-06부터 제목 줄 없음) |
| `mechanicPrefab` | MinigameBase | 스테이지 프리팹 |
| `frameStyle` | MinigameFrameStyle | 창틀/렌즈 프리셋 (눈사람, 지그 ...) |
| `layout` | MinigameWindowLayout | 창 배치와 열림 시작점 (6.2.1) |
| `screenMaterial` | Material | 투사 셰이더 교체 (선택, 6.5) |
| `stageResolution` | Vector2Int | 스테이지 RT 해상도 (선택, 0이면 기본값) |
| `startConditions` | string | 시작 조건. 대화 조건 문법 (`flag:met_zig;!flag:mg_vending_roulette_cleared`) |
| `deniedDialogueId` | int | 조건 미달 시 띄울 대화 (-1이면 상호작용 힌트 자체를 숨김) |
| `startActions` | string | 시작 시 실행 (`takeItem:coin`, `sfx:...`) |
| `outcomes` | List<MinigameOutcome> | 결과 규칙. 위에서부터 평가, 처음 맞는 규칙으로 종료 |
| `abortActions` | string | 닫기/취소 시 실행 (보통 비움, 코인 환불 등에 사용) |
| `allowAbort` | bool | false면 나가기 입력(`Minigame/Cancel`)을 무시 (취조 같은 강제 미니게임) |
| `repeatPolicy` | enum | `Unlimited`, `UntilSuccess`, `Once` |
| `timeLimit` | float | 0이면 무제한. 규칙의 `time` 변수와 별개로 창 타이머 표시용 |

**`MinigameOutcome` (Serializable, `Data/Structs.cs`)**

| 필드 | 설명 |
|---|---|
| `name` | 결과 이름 (`gold`, `silver`, `fail`). 메커닉이 이름으로 직접 종료할 때도 씀 |
| `kind` | `Success` / `Fail` (기록 플래그와 반복 정책 판단용) |
| `when` | 자동 판정 조건. 비우면 메커닉이 `End(name)`을 불러야만 이 결과가 됨 |
| `actions` | 보상/플래그 액션 (`giveItem:candy=2;addFlag:rel_zig`) |
| `followDialogueId` | 종료 후 이어갈 대화 (-1 없음) |

**타입 있는 파라미터**: 메커닉마다 필요한 값이 다르므로 정의를 상속한다.

```csharp
[CreateAssetMenu(menuName = "LOTW/Minigame/Roulette")]
public class RouletteDefinition : MinigameDefinition
{
    public int slotCount = 8;
    public float spinSpeed = 720f;
    public float slowdown = 0.9f;
}
```

인스펙터에서 그대로 편집되고, 에셋 하나에 인스턴스 하나다. 파라미터가 없는 메커닉은 기본 `MinigameDefinition`을 그대로 쓴다.

**`MinigameLibrary` (ScriptableObject)**: id -> 정의. `AudioLibrary`와 같은 방식으로 SubSystemCollection의 `MinigameManager`에 연결. 대화 액션 `minigame:id`가 이걸로 찾는다.

> SO를 쓰는 이유: 정의가 프리팹을 참조해야 하는데 테이블은 프리팹을 Resources 경로로밖에 못 가리키고, 에셋 규칙상 Resources는 최소화한다. 조건/액션은 문자열 DSL이라 테이블과 같은 문법을 그대로 쓴다.

### 4.2 런타임

**`MinigameSession`** (순수 C#, `System/Minigame/`)
- `Definition`, `Source`(발생원 GameObject / NPC 프로필), `Vars`, `Input`, `Elapsed`, `State`
- `Vars`: 메커닉이 쓰는 정수 변수판. `Set("caught", 3)`, `Add("miss")`. 기본 제공 `time`(경과 초, 정수)
- `End(string outcomeName)`, `Abort()`: 메커닉/창이 직접 종료
- `Task<MinigameResult>` 대신 `Awaitable<MinigameResult>`로 결과 대기 (프로젝트 관례)

**`MinigameRules`** (순수 C#, 테스트 대상)
- 매 Tick 후 `outcomes`를 위에서부터 `when`으로 평가
- 조건 문법은 대화와 같고 타입만 추가: `var:caught>=5`, `time>=30`, 기존 `flag:`, `item:`도 그대로 사용 가능
- 예: 
  - `gold`  when `var:caught>=10`
  - `silver` when `time>=30;var:caught>=5`
  - `fail`  when `time>=30`

**`MinigameBase`** (abstract MonoBehaviour, 스테이지 프리팹 루트)

```csharp
public abstract class MinigameBase : MonoBehaviour
{
    protected MinigameSession Session { get; private set; }

    public void Bind(MinigameSession session) { ... }
    public virtual void OnBegin() { }
    public virtual void OnTick(float deltaTime) { }
    public virtual void OnEnd(MinigameResult result) { }   // 종료 연출 (짧게)
    public virtual Camera StageCamera => ...;                // RT를 그리는 카메라
}

public abstract class MinigameBase<TDefinition> : MinigameBase
    where TDefinition : MinigameDefinition
{
    protected TDefinition Def => (TDefinition)Session.Definition;
}
```

- 판정은 기본적으로 규칙에 맡기고 메커닉은 `Vars`만 갱신한다.
- 규칙으로 표현이 어려운 경우(취조에서 특정 진술 선택, "별도 동작"으로 완료)에는 `Session.End("gold")`를 직접 부른다. 이것이 탈출구다.

**`MinigameManager`** (Singleton, SubSystemCollection)
- `IsAvailable(def)`: 시작 조건 + 반복 정책 검사 (트리거가 힌트 표시 여부에 사용)
- `TryStartAsync(def, source) -> Awaitable<MinigameResult>`
- 동시에 세션 1개만 허용. 진행 중 호출은 무시하고 경고
- 결과 처리 후 기록 플래그 자동 갱신: `mg_{id}_plays`, `mg_{id}_cleared`(Success 시 1), `mg_{id}_{outcome}`
  - 대화와 다른 미니게임이 이 플래그로 분기 가능 (`flag:mg_vending_roulette_cleared`)
  - FlagManager를 쓰므로 세이브에 자동 포함
- 씬 전환 시작 시 진행 중 세션은 Abort
- 메커닉 예외 발생 시 로그 후 Abort (게임이 멈추지 않게)

**`MinigameContext`**: `ManagerDialogueContext`를 감싸 `var:`/`time` 조회를 추가한 조건 소스.

### 4.3 트리거

**`MinigameTrigger : WorldObject, IInteractable`** (Content/World)
- `definition`, `promptText`
- `Interact` -> `MinigameManager.TryStartAsync(definition, gameObject)`
- 조건 미달 + `deniedDialogueId < 0`이면 감지 대상에서 빠지도록 `InteractionPrompt`를 비우는 대신, `PlayerInteractor`가 `IInteractable`에 선택적 `IsInteractable` 검사를 하도록 확장 (작은 변경)

**대화 액션 `minigame:id`**
- `DialogueUI`가 이 액션을 만나면 대화창을 닫고 미니게임을 연다 (공존하지 않음, 5장)
- 종료 후 결과의 `followDialogueId`가 있으면 그 행으로, 없으면 원래 `nextId`로 대화창을 다시 연다
- 취조, 설득처럼 대화 중간에 끼는 눈사람 스타일 미니게임이 이 경로를 쓴다

## 5. 입력 소유권

현재 구조: `PlayerController`, `PlayerInteractor`, `DialogueUI`가 각자 `new PlayerControls()`로 입력을 폴링하고, 앞의 둘은 `UIManager.HasBlockingPage`를 보고 스스로 멈춘다.

기획 규칙: **대화창과 미니게임 창은 공존하지 않는다.** 대화 중 미니게임이 시작되면 대화창을 닫고, 미니게임이 끝나면 결과의 `followDialogueId`(없으면 원래 다음 행)로 대화를 다시 연다. 대화 카메라는 그 사이에 복귀하지 않고 유지한다.

설계:
1. `InputSystem_Actions`에 `Minigame` 액션 맵 추가
   - `Point`(Vector2), `Click`(Button), `Navigate`(Vector2, WASD/방향키/스틱), `Submit`(Space/Enter/South), `Alt`(우클릭/West)
   - Esc는 바인딩하지 않는다 (Esc는 메타 연출 전용)
   - 메커닉은 이 맵만 본다. 키 매핑을 바꿔도 메커닉 코드는 안 바뀐다
2. `MinigameInput` 래퍼 (세션이 소유)
   - 세션 시작 시 Enable, 종료 시 Disable
   - `PointerStage`: 화면 좌표 -> 렌즈 RawImage 로컬 -> UV -> 스테이지 카메라 월드 좌표. 렌즈 왜곡이 없으므로 역변환 불필요
   - 포인터가 렌즈 밖이면 `IsPointerInLens == false`
3. 플레이어 쪽: MinigameWindow가 Popup 페이지이므로 `HasBlockingPage`로 자동 차단. 추가 작업 없음
4. 종료 수단: 나가기 입력(`Minigame/Cancel`: Backspace, 패드 B. `allowAbort`이고 `CanAbort`일 때) 또는 메커닉의 `End`. Esc는 쓰지 않는다
5. 복귀: Pop 처리 중에도 `HasBlockingPage`가 true라서 닫는 프레임의 입력이 월드로 새지 않는다 (기존 보장 그대로)

나중에 UI끼리 겹쳐야 하는 경우가 생기면(예: 대화 중 수첩을 열어 증거 제시) 개별 UI에 "내가 맨 위인지" 검사를 넣지 않는다. 대신 입력 컨텍스트 스택으로 바꾼다.
- 입력 객체 하나(`PlayerControls`)를 중앙에서 소유하고, 맨 위 컨텍스트의 액션 맵만 켠다 (Gameplay -> Dialogue -> Minigame)
- `UIManager`가 페이지를 쌓을 때 아래 페이지의 `CanvasGroup.interactable`을 끈다. EventSystem의 클릭, 선택, Submit이 아래 버튼에 가지 않는다
- 이때 `HasBlockingPage` 폴링도 같은 스택으로 대체한다

## 6. 창과 현미경 렌즈 연출

### 6.1 렌더링 방식

스테이지 프리팹을 월드에서 먼 좌표에 생성하고, 전용 `Minigame` 레이어 + 전용 카메라로 저해상도 RenderTexture(Point 필터)에 그린다. 메인 카메라는 이 레이어를 컬링한다.

- 장점: 2D 스프라이트, 물리, 3D 복셀 소품까지 메커닉 자유도가 가장 크다. 픽셀 아트 규칙(PPU 25, Point 필터, 정수 배율)을 그대로 지킨다
- 순수 uGUI로 충분한 퍼즐형(진술 고르기 등)은 스테이지 카메라 없이 창 내부 `ContentSlot`에 UI를 바로 넣는 것도 허용한다. 메커닉이 `StageCamera`를 null로 두면 창이 UI 모드로 동작

### 6.2 창 구조 (`PF_MinigameWindow`)

```
MinigameWindow (BaseUI, CanvasGroup)
  Backdrop        월드를 약하게 어둡게 (월드는 계속 보임)
  Window
    Lens          RawImage(RT) + MAT_MinigameLens
    Frame         반투명 테두리 (A안 확정). 네 변 여백이 같다. 화면만 불투명
    HudSlot       메커닉별 UI (타이머, 카운터) - 메커닉 프리팹이 채움
    ResultBanner  성공/실패 짧은 표시
```

비교 테스트(2026-10-05): A(반투명 테두리)와 C(테두리 없는 투사)를 Plaza 위에서 비교해 A로 정했다.

### 6.2.1 창 배치

정의의 `layout`(`MinigameWindowLayout`, Serializable)으로 미니게임마다 정한다.

| 필드 | 설명 |
|---|---|
| `placement` | `Center` 화면 중앙 / `Anchor` 화면 비율 좌표 / `Source` 발생원 오브젝트의 화면 위치 기준 / `SourceBounds` 발생원이 화면에서 차지하는 영역에 그대로 겹침 |
| `anchor` | `Anchor`일 때 화면 비율 좌표 (0-1, 예: 0.7, 0.55) |
| `offset` | 기준점에서의 픽셀 오프셋 (1080p 기준) |
| `openFrom` | 열림 애니메이션 시작점: `WindowCenter` / `Source` (오브젝트에서 튀어나오듯 열림) |

- 창 크기는 `stageResolution` x 정수 배율로 정해진다. 픽셀 격자를 지키려고 임의 크기는 두지 않는다
- `Source` 배치는 연 순간의 위치로 고정한다 (카메라가 움직여도 창은 따라가지 않음). 화면 밖으로 나가면 안쪽으로 밀어 넣는다
- `SourceBounds` 배치 (2026-10-06 추가, 12.1):
  - 발생원(자식 포함) 렌더러들의 3D 경계 상자를 월드 저해상도 RT 픽셀로 투영한다. 그 사각형 크기가 스테이지 해상도가 된다. `stageResolution`, `anchor`, `offset`, `displayScale`은 쓰지 않는다
  - 스테이지는 월드 RT와 같은 픽셀 격자, 같은 정수 배율로 그린다. 스테이지 1픽셀 = 월드 화면 1픽셀이다
  - 화면 밖으로 밀어 넣지 않는다. 밀면 발생원과 어긋나기 때문이다
  - 열려 있는 동안 월드 화면이 갱신될 때마다 발생원 위치를 다시 구해 따라간다. 갱신 시점은 `LowResPixelRenderer.OnViewUpdated`다 (카메라를 픽셀 격자에 맞춘 직후). 그래서 창이 월드보다 한 프레임 늦지 않다
  - 스테이지 해상도는 연 순간의 값을 유지한다. 창 크기는 스테이지 해상도 x 월드 화면 배율이다. 화면 크기가 바뀌어 배율이 바뀌면 창 크기도 바뀐다
  - 투영 크기를 정수 픽셀로 올릴 때 0.01픽셀 이하의 오차는 무시한다 (`MinigameDefines.ProjectionSizeTolerance`)
  - 투영할 수 없으면(렌더러 없음, 월드 렌더러 준비 안 됨) 정의의 `stageResolution`과 일반 배치를 쓴다

### 6.2.2 열림/닫힘 애니메이션 (조리개)

`MinigameWindow`가 `BaseUI.ShowAsync/HideAsync`를 오버라이드해 직접 연출한다. UIManager 큐 안에서 돌기 때문에 애니메이션 중에는 `HasBlockingPage`가 true이고, 입력은 열림이 끝난 뒤에 미니게임으로 넘어간다.

열림 순서:
1. 창(테두리)이 `openFrom` 지점에서 작은 크기로 시작해 제자리, 제 크기로 확장
2. 확장이 끝나면 렌즈 화면이 가운데에서 조리개처럼 열림 (셰이더 `_Iris` 0 -> 1)
3. 열림 완료 후 `Minigame` 입력 맵 Enable, 메커닉 `OnBegin`

닫힘은 역순(조리개 닫힘 -> 창 축소)이고, 끝나면 입력이 월드로 돌아간다.

- 조리개는 창과 같은 사각형이다 (원형 아님). 렌즈 셰이더의 `_IrisX`, `_IrisY`로 가운데에서 RT 픽셀 단위로 열고, 경계 1픽셀을 어둡게 해 셔터 날 느낌을 준다
- 열림 방식: 균일 확장(가로세로 동시) / 2단(가로줄이 먼저 펴지고 세로로 열림). 프레임 스타일에서 고른다
- 시간과 이징은 `MinigameFrameStyle`에 둔다 (눈사람: 차분, 지그: 튀거나 끊기게)
- 연출은 DOTween `Sequence`로 짠다 (`SetUpdate(true)`로 timeScale 무시, `SetLink`로 창 파괴 시 자동 Kill). 조리개는 `DOVirtual.Float` 진행도 하나로 몰아 `_IrisX/_IrisY`를 계산한다. 같은 프로퍼티에 트윈을 여러 개 걸면 되감기/Goto 때 서로 덮어쓴다
- `ShowAsync/HideAsync`는 시퀀스 완료를 기다린다 (`OnComplete` + `AwaitableCompletionSource`)

### 6.3 렌즈 셰이더 (`Art/Shaders/MinigameLens.shader`)

아날로그 카메라 렌즈로 투사한 정도만 낸다. 기하 왜곡은 넣지 않는다.
- 가장자리 비네팅 (모서리가 부드럽게 어두워짐)
- 약한 필름 그레인, 아주 약한 밝기 깜빡임
  - 그레인 해시는 `sin`을 쓰지 않는다 (Dave Hoskins hash12). `sin` 해시는 입력이 크면 GPU 정밀도 때문에 대각선 줄무늬가 생긴다 (2026-10-06 확인)
  - 프레임 번호는 61로 나눈 나머지를 쓴다. 해시 입력이 커지지 않게 하기 위해서다
- 선택: 가장자리 미세 색수차 (1px 이하)
- 화면 좌표와 스테이지 좌표가 선형 관계라 포인터 변환이 단순하다

### 6.4 프레임 스타일 (`MinigameFrameStyle` SO)

| 스타일 | 연출 |
|---|---|
| 눈사람 | 정적, 크래프트 종이 질감 창틀, 왜곡 거의 없음, 차분한 열림 |
| 지그 | 렌즈가 미세하게 흔들림, 왜곡/색수차 강하고 시간에 따라 일렁임, 열릴 때 글리치 |

스타일은 셰이더 파라미터 묶음 + 창틀 스프라이트 + 열림/닫힘 애니메이션이다. 색은 보라 계열을 피한다.

### 6.5 미니게임별 그래픽 스타일

미니게임마다 전혀 다른 그림체를 쓸 수 있게 세 층으로 나눈다. 별도 이펙트 시스템은 만들지 않는다.

| 층 | 무엇을 바꾸나 | 방법 | 프레임워크 작업 |
|---|---|---|---|
| 1. 오브젝트 | 스프라이트/메시 질감 (종이, 크레용, 홀로그램...) | 메커닉 프리팹 안 렌더러에 각자 머티리얼 지정 | 없음 |
| 2. 스테이지 후처리 | 스테이지 화면 전체 색보정, 블룸 등 URP 효과 | 메커닉 프리팹에 Volume을 넣고 `Minigame` 레이어에 둠 | 스테이지 카메라의 `volumeLayerMask`를 `Minigame` 레이어로 한정 |
| 3. 투사 셰이더 | RT를 창에 그리는 단계 전체 (열화상, 청사진, 망점 인쇄...) | 정의의 `screenMaterial`에 머티리얼 지정. 비우면 `MAT_MinigameLens` | 필드 1개 + 공용 HLSL |

- 2층의 볼륨 마스크 한정은 스타일과 무관하게 필요하다. 안 하면 월드의 틸트시프트 같은 전역 볼륨이 미니게임 화면에도 걸린다
- 3층 커스텀 셰이더는 `Art/Shaders/MinigameLens.hlsl`의 공용 함수(비네팅, 그레인)를 호출해 "렌즈로 투사된" 공통 질감을 유지한다. 원하면 호출하지 않아도 된다
- 정의에 `stageResolution`(선택)을 둔다. 비우면 `MinigameDefines` 기본값(픽셀 규칙). 고해상도 그림체가 필요한 미니게임만 덮어쓴다

## 7. 사례 검증 (기획 아이디어 목록 대입)

| 미니게임 | 현실 -> 변형 | 메커닉 | 시작 조건 | 완료 조건 | 보상 |
|---|---|---|---|---|---|
| 자판기 룰렛 | 자판기 버튼 -> 거대한 슬롯 룰렛 | Roulette | `item:coin` + startActions `takeItem:coin` | 메커닉이 정지 칸으로 `End("jackpot")` 등 | `giveItem:...` |
| 쓰레기통 정리 | 쓰레기통 -> 떨어지는 쓰레기 분류 | Sorting | 퀘스트 플래그 | `var:sorted>=10` 성공, `var:miss>=3` 실패 | 퀘스트 플래그 |
| 줄넘기 | 줄넘기 아이 -> 리듬 타이밍 | Rhythm | 없음 (반복 가능) | `var:combo>=20` / `var:miss>=1` | 최고 기록 플래그 |
| 황금벌레 (메인) | 우주 꼬마들의 놀이 | GoldBug | 챕터 플래그 | 정의별로 단계 상향 | 단서, 관계 점수 |
| 취조 (눈사람 스타일) | 대화 -> 종이 진술 카드 고르기 | Testimony (UI 모드) | 대화 액션 `minigame:interrogate_saus` | 메커닉이 모순 카드 선택 시 `End("break")` | `setFlag:saus_confessed`, followDialogueId |

다섯 경우 모두 "변수 갱신 + 규칙" 또는 "메커닉 직접 End"로 표현된다. 같은 Sorting 메커닉을 쓰레기통, 세탁소 빨래 분류 등에 재사용할 수 있다.

## 8. 파일 배치

```
Scripts/
  Core/Managers/MinigameManager.cs
  System/Minigame/MinigameBase.cs
  System/Minigame/MinigameSession.cs
  System/Minigame/MinigameVars.cs
  System/Minigame/MinigameRules.cs
  System/Minigame/MinigameInput.cs
  Content/Minigame/MinigameContext.cs
  Content/Minigame/{Roulette,Sorting,...}/        메커닉별 폴더
  Content/UI/MinigameWindow.cs
  Content/World/MinigameTrigger.cs
  Data/MinigameDefinition.cs, MinigameLibrary.cs, MinigameFrameStyle.cs
  Data/Structs.cs (MinigameOutcome, MinigameResult), Data/Enums.cs (MinigameOutcomeKind, MinigameRepeatPolicy, MinigameSessionState)
  Data/Defines.cs (MinigameDefines: 스테이지 원점, RT 해상도, 레이어 이름)
Assets/Project/
  Prefabs/UI/PF_MinigameWindow.prefab
  Prefabs/Minigames/PF_MG_{Mechanic}.prefab
  Data/Minigames/MinigameDefinition_{Name}.asset, MinigameLibrary_Main.asset, MinigameFrameStyle_{Snowman|Zig}.asset
  Art/Shaders/MinigameLens.shader, MinigameLens.hlsl
  Art/Materials/MAT_MinigameLens.mat
  Art/UI/TX_UI_Minigame_Frame_{Style}.png
Tests/EditMode/MinigameRulesTests.cs
```

## 9. 기존 코드 변경 (최소)

| 대상 | 변경 |
|---|---|
| `DialogueCommands` | 조건 타입 해석을 확장 가능하게 분리 (`flag`/`item` 외 `var`/`time`을 MinigameContext가 공급). 기존 테스트로 회귀 확인 |
| `DialogueCommands` / `DialogueUI` | `minigame:id` 액션: 대화창을 닫고 미니게임 실행, 끝나면 대화 재개 |
| `NPC` / `DialogueUI` | 미니게임 전후로 대화창을 닫았다 다시 열 때 대화 카메라 유지 |
| `PlayerInteractor` / `IInteractable` | 선택적 사용 가능 여부 검사 |
| `InputSystem_Actions` | `Minigame` 액션 맵 추가 |
| TagManager | `Minigame` 레이어 추가, 메인 카메라 컬링에서 제외 |

## 10. 단계

1. 코어: Definition/Outcome/Session/Vars/Rules/Manager, 조건 파서 분리, `MinigameRulesTests`
2. 창: MinigameWindow, 렌즈 셰이더, 눈사람 스타일, 입력 맵과 포인터 변환
3. 샘플 1개: 자판기 룰렛 (트리거 경로) - 전체 흐름 검증
4. 대화 연동: `minigame:id`, `followDialogueId`, 대화 카메라 유지 - 취조 샘플
5. 지그 스타일, 추가 메커닉, 이후 수첩/키워드 보상 연결 (수첩 시스템이 생긴 뒤)

## 11. 확인이 필요한 결정

확정 (2026-10-05):
- 월드 시간은 멈추지 않는다. 플레이어만 차단한다
- Esc는 미니게임을 닫는 데 쓰지 않는다
- 창에는 제목 줄과 닫기 버튼이 없다. 나가기는 `Minigame/Cancel`(Backspace, 패드 B)이다 (2026-10-06)
- RT 스테이지가 기본이고, 퍼즐형은 UI 모드를 허용한다
- 렌즈는 아날로그 카메라 투사 정도로만 연출하고, 왜곡은 넣지 않는다
- 대화창과 미니게임 창은 공존하지 않는다
- 창틀은 A안(반투명 테두리, 화면은 불투명)
- 창 배치는 미니게임별로 정하고, 열 때 조리개 애니메이션을 쓴다

남은 질문:

| # | 질문 | 기본안 |
|---|---|---|
| 1 | 정의 데이터 위치 | ScriptableObject (테이블 아님) |
| 2 | 결과 실패 시 보상 | 실패도 `outcomes`에 넣으면 보상 가능. 기본은 실패 보상 없음 |

## 12. 구현 현황 (2026-10-06)

### 12.1 구현된 것

| 영역 | 내용 |
|---|---|
| 데이터 | `MinigameDefinition` (SO, 상속 가능), `MinigameLibrary` (id 조회), `MinigameOutcome`, `MinigameWindowLayout`, `MinigameResult`, `MinigameDefines` |
| 규칙 | `MinigameRules` (시작 가능 여부, 결과 판정, 시작/종료 기록, 창 위치 보정), `MinigameVars`, `MinigameContext` (게임 상태 + 변수) |
| 조건/액션 | 조건 `var:key` (미니게임 정의에서만), 액션 `minigame:id` (대화에서만). 경과 시간은 변수 `time` (초, 내림)으로 읽는다: `var:time>=30` |
| 런타임 | `MinigameManager` (한 번에 한 판, 스테이지/RT 수명, 틱, 결과 적용, 중단, 정리), `MinigameSession`, `MinigameBase` / `MinigameBase<TDefinition>` |
| 입력 | `Minigame` 액션 맵 (Navigate, Submit, Alt, Point, Click, Cancel. Cancel은 Backspace와 패드 B. Esc 없음), `MinigameInput` (포인터 -> 스테이지 월드 좌표) |
| 창 | `MinigameWindow` + `PF_MinigameWindow` (A안 창틀), `MinigameLens.shader` + `MAT_MinigameLens`, DOTween 열림/닫힘 (창틀 확장 -> 사각 조리개) |
| 배치 | `layout`: Center / Anchor / Source / SourceBounds, offset, openFrom (WindowCenter / Source), displayScale. 화면 밖으로 나가면 안쪽으로 밀어 넣음 (SourceBounds는 제외) |
| 투사 배치 | `MinigameProjection` (발생원 경계 상자 -> 월드 RT 픽셀 사각형 -> 스테이지 해상도와 화면 사각형). `LowResPixelRenderer.Current`가 월드 RT 크기, 배율, 서브픽셀 위치를 알려 준다. 스테이지 해상도는 `Session.StageResolution`에 들어가고, 정의의 `MinStageResolution`(가상 속성) 이상, 월드 RT 크기 이하로 맞춘다 |
| 메카닉 훅 | `MinigameBase.OnBind()`: 세션 연결 직후, 창이 열리기 전에 부른다. 스테이지 크기에 맞춰 판을 만들 때 쓴다. `MinigameBase.CanAbort` (기본 true): false인 동안 나가기 입력(`MinigameManager.Abort`)을 무시한다. 결과가 정해져 연출 중일 때 보상을 잃지 않게 쓴다 (낙하 게이트의 착지) |
| 창틀 | 화면 둘레에 14px 여백이 네 변에 같다. 제목, 상태 문구, 닫기 버튼은 없다. `MinigameSession.Status`는 메카닉이 계속 쓰지만 지금은 표시할 곳이 없다 (12.4) |
| 진입 | `MinigameTrigger` (월드 상호작용), 대화 액션 `minigame:id` (대화를 닫고 실행, 끝나면 `followDialogueId` 또는 원래 다음 행에서 재개. onFinished(카메라 복귀)는 대화가 실제로 끝날 때 호출) |
| 상호작용 | `IInteractable.CanInteract` (기본 true). false면 `PlayerInteractor` 대상에서 빠짐. 막는 페이지(미니게임 창 등)나 시퀀스가 있는 동안 `PlayerInteractor`가 HUD 상호작용 안내를 숨기고, 풀리면 대상을 다시 찾는다 |
| 샘플 | 줄넘기: `JumpRopeMinigame` + `JumpRopeDefinition`, `PF_Minigame_JumpRope`, `MinigameDefinition_JumpRope` (5번 넘으면 성공 + `giveItem:rose`, 3번 걸리면 실패), 플라자 (39, 0, 1.2)의 `PF_JumpRopeKid` |
| 샘플 | 자판기 룰렛 (낙하 게이트 형식): `Scripts/Content/Minigame/GateDrop/`, `PF_Minigame_GateDrop`, `MinigameDefinition_VendingRoulette` (SourceBounds 배치, 음료 6종 + 꽝). 플라자 `DrinkMachine` (107.1, 0, 4.8), 레일 `RailNode_A8`에서 상호작용. 사용자가 프로토타입으로 승인했다 (2026-10-06). 기획: `Docs/Minigames/자판기_룰렛.md`, `Docs/Minigames/형식/낙하_게이트.md` |

### 12.2 새 미니게임 만드는 순서

1. 메카닉: `MinigameBase` (전용 수치가 필요하면 `MinigameBase<TDefinition>`)를 상속한 컴포넌트를 만든다. `OnTick`에서 `Session.Input`을 읽고 `Session.Vars`에 진행 상황을 쓴다. 규칙으로 표현하기 어려운 종료는 `Session.End("결과이름")`.
2. 프리팹: 루트에 메카닉 컴포넌트, 자식에 직교 카메라(`stageCamera`에 연결, 렌더러 인덱스 1)와 스프라이트를 둔다. `Prefabs/Minigames/PF_Minigame_{Name}`.
3. 정의: `Data/Minigames/`에 `LOTW/Minigame/Definition` 에셋을 만들고 id, 제목, 프리팹, outcomes(위에서부터 검사), 필요하면 시작 조건/반복 정책/창 배치/전용 화면 머티리얼을 채운다.
4. 등록: `MinigameLibrary_Main`의 definitions에 추가한다 (대화 액션 `minigame:id`가 여기서 찾음).
5. 진입: 월드 오브젝트에 `MinigameTrigger` (Interactable 레이어 + 트리거 콜라이더)를 붙이거나, Dialogue 테이블 행의 actions에 `minigame:id`를 쓴다.

전용 그래픽이 필요하면 정의의 `screenMaterial`에 머티리얼을 넣는다. 셰이더가 `_IrisX` / `_IrisY`를 받으면 같은 조리개 연출이 적용된다.

### 12.3 본문과 달라진 점

- `MinigameContext`는 `System/Minigame/`에 있다 (매니저에 의존하지 않고 `IDialogueContext`를 감싸므로).
- 조건 파서를 따로 분리하지 않았다. `DialogueCommands`에 `var` 타입 한 가지만 추가했고, 컨텍스트가 `IVariableContext`를 구현할 때만 동작한다.
- `timeLimit` 필드는 두지 않았다. 변수 `time`과 결과 조건으로 표현한다.
- 조리개는 가로세로가 함께 열리는 방식 하나만 있다 (2단 방식은 넣지 않음).
- 메카닉 프리팹 이름은 `PF_Minigame_{Name}`.
- `minigame:id` 가 있는 행은 화면에 표시되지 않는다 (행의 다른 액션은 실행됨). 분기 행(router)의 actions에 쓴 `minigame:`은 지원하지 않는다.
- 시작할 수 없는 상태(조건 불만족, 반복 정책)에서 대화가 `minigame:id` 행에 오면 미니게임 없이 그 행이 그대로 표시된다.
- 배치에 `SourceBounds`를 추가했다 (6.2.1). 스테이지 해상도가 정의 값으로 고정되지 않고 판마다 다를 수 있다. 메카닉은 `OnBind`에서 `Session.StageResolution`을 읽어 판을 만든다.
- 7장의 자판기 룰렛 사례(Roulette 메커닉, 동전 비용)는 쓰지 않았다. 낙하 게이트 형식(`GateDrop`)으로 만들었고 시작 조건이 없다. 이용 아이템은 이후 단계다 (`Docs/Minigames/자판기_룰렛.md` 8장).

### 12.4 아직 없는 것 (필요해질 때 추가)

- UI 모드 (RT 스테이지 없이 UI만으로 하는 퍼즐형)
- `MinigameFrameStyle` (지그 스타일 창틀 등 창틀 프리셋)
- `Minigame` 전용 레이어와 카메라 컬링 분리 (지금은 스테이지를 월드에서 멀리 (0, -1000, 0) 두는 것으로 분리)
- 상태 문구(`MinigameSession.Status`) 표시 위치. 제목 줄을 없앤 뒤 줄넘기 횟수와 자판기 출구 이름이 화면에 나오지 않는다
- 메카닉 -> 연출용 시그널, 결과 연출(성공/실패 표시)은 0.6초 정지뿐. 나가기 키 안내도 화면에 없다
- 미니게임 진행 중 세이브/씬 전환 처리
- 결과가 정해진 뒤 연출 중(`CanAbort`가 false)에 창이 밖에서 닫히면(`UIManager.ClearAllPages`) 중단으로 처리되어 보상이 없다. 지금은 미니게임 중에 `ClearAllPages`를 부르는 경로가 없다 (일시 정지는 미니게임 중에 열리지 않는다)
- 셰이더 공용 HLSL include

### 12.5 검증

- EditMode: `MinigameRulesTests` 26개 포함 전체 179개 통과.
- EditMode (2026-10-06): `MinigameProjectionTests`, `GateDropBoardTests`, `GateDropLayoutTests`, `PixelCanvasTests` 추가 후 전체 271개 통과.
- EditMode (2026-10-06, 리뷰 반영 후): 전체 276개 통과.
- Play 모드 (2026-10-06, 리뷰 반영 후, 스크립트 조작): 착지 단계에서 `MinigameManager.Abort`를 불러도 창이 닫히지 않았고 레몬이 지급됐다. 착지 전 `Abort`는 보상 없이 창을 닫았다 (`plays`만 1 늘었다). 착지 프레임(눌림, 색 차오름, 반짝임)을 화면 캡처로 확인했다. 카메라가 움직일 때 창이 자판기를 따라가는지는 확인하지 않았다.
- Play 모드 (2026-10-06, Plaza, 스크립트 조작): `RailNode_A8`에서 자판기 대상 탐지와 HUD 안내, 자판기 영역에 겹쳐 열리는 창, 버튼 -> 게이트 열림 -> 그 출구로 착지, 착지 연출, 음료 지급과 기록 플래그(레몬, 꽝), 중단 후 재시작, 창이 열린 동안 HUD 안내 숨김. 실제 키와 마우스 입력, 60fps에서의 손맛은 확인하지 않았다 (에디터가 뒤에서 약 9fps로 돌았다).
- Play 모드 (Plaza, 에디터 자동 조작): 열림/닫힘과 창 크기, 실제 키 입력(Space)으로 5회 넘어 성공 -> `rose` 지급과 `mg_jumprope_cleared` 기록, 3회 걸려 실패, 닫기 버튼 중단(보상 없음), 종료 후 스테이지 정리와 입력 차단 해제, 대화 `minigame:` 행 -> 미니게임 -> 대화 재개, 플라자 트리거 탐지와 실행, 화면 좌표 -> 스테이지 좌표 변환.
- 직접 해 보지 못한 것: 실제 마우스 포인터 입력(에디터가 주입한 마우스 이벤트를 받지 않아 좌표 변환 함수만 확인), 게임패드, 플레이어가 걸어가서 여는 전체 동선, Anchor/Source 배치와 `openFrom = Source` 의 화면상 모습.
