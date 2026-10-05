# Dialogue 에디터 계획

Dialogue 테이블을 노드 그래프로 보고, 편집하고, 분기를 시뮬레이션하는 Unity 에디터 툴의 계획입니다. P1(뷰어와 시뮬레이션), P2(편집과 엑셀 저장), P3(조건과 액션 목록 UI)를 구현했습니다.

- 작성일: 2026-10-05
- 상태: P1 완료, P2와 P3 구현 (메뉴 `LOTW/Dialogue Editor`). Unity 안에서 직접 확인이 남았습니다(P2, P3 확인 결과).
- 관련 문서: `.claude/rules/table-data.md` (테이블 규칙), `Docs/GameDesign.md` 5장 대화

## 1. 목표

| 목표 | 내용 |
|---|---|
| 보기 | Dialogue 행을 노드, `NextId`와 `ChoiceIds`를 연결선으로 보여 줍니다. 노드에는 키 대신 실제 대사를 표시합니다. |
| 편집 | 노드 추가, 삭제, 연결, 대사 수정을 툴에서 하고 엑셀에 저장합니다. 엑셀이 계속 원본입니다. |
| 시뮬레이션 | 플래그와 인벤토리 상태를 정해 두고 대화를 진행합니다. 조건 때문에 숨는 선택지와 액션 실행 결과를 보여 줍니다. |
| 부수 동작 작성 | 조건과 액션을 문자열 대신 목록 UI로 작성합니다. 아이템, 플래그, 사운드는 목록에서 고릅니다. |

사용자는 작업자 전원입니다. 모두 Unity를 쓸 수 있다고 가정합니다.

## 2. 결정 사항

**Unity 에디터 툴로 만듭니다.**
- 조건과 액션 해석기(`DialogueCommands`)와 게임 상태 인터페이스(`IDialogueContext`)가 이미 있습니다. 시뮬레이션이 게임과 같은 규칙으로 돕니다.
- 사운드 키(`AudioLibrary` 에셋)와 아이템 목록을 Unity 안에서 바로 고를 수 있습니다.
- 별도 툴로 만들면 해석기를 다시 구현해야 하고, 게임과 툴의 동작이 어긋날 수 있습니다.
- 2026-10-05 Unity 밖 웹 툴과 비교한 뒤 Unity로 유지했습니다. 엑셀 저장은 어느 쪽이든 Python이 하고, 앞으로 붙을 기능(캐릭터 그림, 사운드 미리보기, 퀘스트, NPC 배치, 해당 대화부터 실행)이 대부분 Unity 리소스와 게임 규칙을 쓰기 때문입니다. Unity를 쓰지 않는 사람이 툴을 써야 하면 다시 검토합니다.
- 엑셀이 계속 원본입니다. 툴이 JSON을 원본으로 쓰는 안은 대화 테이블을 엑셀에서 고칠 수 없게 되어 채택하지 않았습니다.

**노드 UI는 GraphView를 씁니다** (`UnityEditor.Experimental.GraphView`, 에디터 내장). Unity 6의 Graph Toolkit은 실험 단계이고 설치되어 있지 않습니다.

**엑셀 쓰기: Python 연결** (C# xlsx 라이브러리 안은 엑셀 읽기와 검증이 Python과 C# 두 벌이 되어 채택하지 않음)
- 툴이 `Table/table_edit.py`를 호출해 행을 쓰고, 변환기를 다시 돌립니다. 엑셀 읽기, 쓰기, 검증이 Python 한 곳에 있습니다.
- 작업자 모두 Python과 openpyxl이 필요합니다. `ConvertTable.bat` 때문에 이미 필요합니다.

## 3. 데이터 흐름

```
읽기:  Table/Excel/*.xlsx --(convert_table.py: 파싱+검증)--> Resources/Table/*.json --> 에디터 그래프
쓰기:  에디터 편집 --(table_edit.py: 행 추가/수정/삭제)--> *.xlsx --(convert_table.py)--> JSON --> 그래프 다시 읽기
```

- 에디터는 JSON만 읽습니다. 엑셀을 직접 파싱하지 않습니다.
- 툴의 "새로고침"은 변환기를 먼저 돌립니다. 엑셀에서 직접 고친 내용도 바로 반영되고, 검증 오류는 오른쪽 패널에 표시합니다.
- 엑셀이 열려 있으면 잠금 파일(`~$*.xlsx`)이 있으므로 저장 전에 막고 "엑셀을 닫아 주세요"라고 안내합니다.
- 툴에서 읽은 뒤 엑셀 파일이 수정되었으면 저장 전에 묻습니다. 저장은 툴에서 바꾼 행만 쓰므로 엑셀에서 고친 다른 행은 남습니다.

## 4. 규칙

**대화 묶음과 DataId**
- 대화 하나는 시작 행의 100 단위 블록입니다. 예: 1100번대는 Gumman 대화
- 그래프는 블록 단위로 엽니다.
- 새 노드의 DataId는 같은 블록의 빈 번호 중 가장 작은 값입니다. 블록이 차면 경고합니다.

**대사와 키** (`table-data.md`와 같음)
- 새 노드의 대사는 `dialogue.{DataId}` 키로 `Text_Dialogue`에 저장합니다.
- 공용 키(`dialogue.common.*`, `ui.*`)를 쓰는 노드에서 대사를 고치면 "N곳에서 사용 중"이라고 경고합니다. "이 노드만 분리" 버튼을 누르면 `dialogue.{DataId}` 키를 새로 만듭니다.
- 화자 이름은 `Text_Character`에서 고릅니다.

**노드 위치**
- 엑셀에는 넣지 않습니다. 기획 데이터가 아니기 때문입니다.
- `Table/Layout/Dialogue.layout.json`(DataId → 좌표, 에디터 전용)에 저장합니다. 노드를 옮기면 바로 씁니다. 위치가 없는 노드는 자동 배치합니다.

## 5. 단계별 계획

### P1. 뷰어와 시뮬레이션 (읽기 전용)

| 파일 | 내용 |
|---|---|
| `Editor/Dialogue/DialogueEditorWindow.cs` | 메뉴 `LOTW/Dialogue Editor`. 블록 목록, 그래프, 오른쪽 시뮬레이션 패널 |
| `Editor/Dialogue/DialogueGraphView.cs` | GraphView. 노드 생성, 자동 배치 (깊이 우선, 열 = 단계) |
| `Editor/Dialogue/DialogueNodeView.cs` | 노드 표시: DataId, 화자, 대사, 조건/액션 요약. 분기 행, 선택지 행, 종료는 색으로 구분 |
| `Editor/Dialogue/DialogueTableSource.cs` | `Resources/Table/Dialogue.json`, `Text_*.json` 읽기. 키 → 대사 변환 |
| `Editor/Dialogue/SimDialogueContext.cs` | `IDialogueContext` 구현. 플래그와 인벤토리를 Dictionary로 보관하고, 실행한 액션을 기록 |
| `Editor/Dialogue/DialogueSimulator.cs` | `DialogueCommands.ResolveRoute`, `CheckConditions`, `RunActions`로 진행 |

시뮬레이션 패널:
- 시작 상태: 플래그와 아이템 개수를 직접 입력하거나, 이 블록 조건에 쓰인 키를 모아 미리 채웁니다.
- 진행: 현재 노드를 강조하고, 선택지를 버튼으로 보여 줍니다. 조건을 만족하지 않는 선택지는 회색으로 보여 주고 이유를 표시합니다.
- 기록: "giveItem rose x1", "setFlag got_rose = 1", "sfx door_open" 같은 실행 결과를 순서대로 남깁니다.
- 되돌리기: 한 단계 뒤로, 처음부터

구현 메모 (계획과 다른 점):
- 인스펙터는 따로 두지 않았습니다. 노드에 대사, 조건, 실행이 모두 보이고, P1은 읽기 전용이기 때문입니다. P2에서 편집용으로 추가합니다.
- Item.json은 읽지 않습니다. 아이템 목록은 P3 선택 목록에서 씁니다.
- 노드 우클릭 "여기서 시뮬레이션 시작"으로 아무 행에서나 시작할 수 있습니다.
- 진행 규칙은 `DialogueUI`를 따라 `DialogueSimulator`에 다시 썼습니다. `DialogueUI`의 진행 규칙을 바꾸면 시뮬레이터도 같이 바꿔야 합니다.

완료 기준 (2026-10-05 확인: EditMode 153개 통과, 실제 데이터로 블록 7개, 블록 0 노드 8개와 연결 7개, got_rose 0/1 분기와 장미 지급 기록 확인. 창 화면은 직접 확인 필요):
- Plaza 6개 대화와 꽃집 대화(1~11)가 모두 그래프로 열리고, 연결이 JSON과 일치합니다.
- 꽃집 대화에서 `got_rose`가 0일 때와 1일 때 다른 분기로 가고, 장미 지급이 기록에 나옵니다.
- EditMode 테스트 `DialogueSimulatorTests`: 분기 선택, 숨는 선택지, 액션 기록

### P2. 편집과 저장

| 파일 | 내용 |
|---|---|
| `Table/table_edit.py` | CLI. `python table_edit.py edits.json`. `[{table, key, upsert: [행], delete: [키]}]`를 받아 메모리에서 xlsx 표를 고치고, 전체 테이블을 검증해 통과하면 저장 후 JSON으로 변환합니다. 실패하면 아무 파일도 쓰지 않습니다. |
| `Table/convert_table.py` (수정) | `read_tables(grids)`로 읽기와 검증을 분리해 `table_edit.py`가 메모리의 표로 검증합니다. |
| `Table/test_table_edit.py` | 추가, 수정, 삭제, 표 범위, 텍스트 서식, 표 밖 메모 유지, 검증 실패와 잠금 파일일 때 파일이 그대로인지 |
| `Editor/Dialogue/TableWriter.cs` | Python 스크립트 실행(`System.Diagnostics.Process`), 출력 수집, 잠금 파일과 수정 시각 확인 |
| `Editor/Dialogue/DialogueTableSource.cs` (수정) | ScriptableObject로 바꿔 `Undo.RecordObject`로 되돌리기. 읽은 시점의 행을 보관해 바뀐 행을 고름 |
| `Editor/Dialogue/DialogueEdits.cs` | 행 추가, 삭제, 연결, 대사 수정, 대사 분리, 저장 요청 JSON 만들기 |
| `Editor/Dialogue/DialogueLayout.cs` | 노드 위치 파일 읽기, 쓰기 |

편집 기능:
- 노드: 추가(툴바 "대사 추가" 또는 그래프 우클릭, 블록의 빈 번호 중 가장 작은 값), 삭제(Delete 키 또는 우클릭, 들어오는 연결과 행 전용 대사도 정리)
- 분기: 툴바 "분기 추가" 또는 그래프 우클릭. 대사 없는 행을 만들고, "+ 후보"를 끌어 후보를 연결합니다. 후보 행의 조건을 만족하는 첫 후보로 넘어가고, 모두 불만족이면 "다음"으로 갑니다.
- 새 대화: 블록 메뉴 "새 대화"로 마지막 블록 다음 번호에 첫 행을 만듭니다.
- 연결: "다음" 포트는 `NextId`, "선택 N" 포트는 `ChoiceIds`, "+ 선택" 포트를 끌면 선택지를 추가합니다(최대 4개, 스키마 `maxCount`). 연결선을 지우면 끊깁니다.
- 포트에서 끈 연결을 빈 곳에 놓으면 그 자리에 새 행을 만들어 잇습니다. 출력 포트면 새 행이 그 칸의 대상, 입력 포트면 새 행의 "다음"이 그 행입니다. 대사 행의 선택지 칸에서 만든 행은 플레이어(`player`, `@character.player.name`), 나머지는 블록의 첫 NPC 화자를 씁니다.
- 화자: 인스펙터 "화자" 목록에서 이미 쓰인 화자 ID와 이름을 한 번에 고릅니다. 새 화자는 아래 칸에 직접 씁니다.
- Ctrl+S: 창에 포커스가 있으면 씬 저장 대신 대화를 저장합니다(Shortcuts의 `LOTW/Dialogue Editor/Save`).
- 인스펙터(노드 선택 시 오른쪽 패널 위): 화자, 화자 ID, 화자 이름, 대사 키, 다음, 선택지, 조건, 실행(P3 목록 UI), 대사 본문(Text_Dialogue의 Ko)
- 공용 대사를 쓰는 행은 "N개 행이 같이 씁니다" 경고와 "이 행 전용 대사로 분리" 버튼을 보여 줍니다. Text_Dialogue에 없는 문구(`@ui.*`)는 고칠 수 없고 분리만 됩니다.
- Ctrl+Z로 편집을 되돌립니다. 저장하지 않고 창을 닫으면 저장할지 묻습니다.
- 저장: 바뀐 행만 씁니다. 검증이 실패하면 오류 목록을 오른쪽 패널에 보여 주고 편집 내용은 유지합니다.

설정:
- Python 경로를 `EditorPrefs`에 둡니다(패널 아래 "설정"). 비우면 `%LOCALAPPDATA%/Programs/Python/Python3*` 설치본, 없으면 PATH의 `python`을 씁니다. Store 스텁 때문에 PATH의 `python`이 동작하지 않는 PC가 있기 때문입니다.

구현 메모 (계획과 다른 점):
- 복제는 넣지 않았습니다. 추가 후 인스펙터에서 고치는 것으로 충분하면 계속 생략합니다.
- 검증 오류는 노드가 아니라 패널에 목록으로 보여 줍니다. 오류가 엑셀 칸 위치(`Dialogue.xlsx F12`)로 나오기 때문입니다.
- `DialogueEditSession` 대신 `DialogueTableSource`(데이터, ScriptableObject)와 `DialogueEdits`(편집 동작)로 나눴습니다.

완료 기준:
- 툴에서 노드 추가, 연결, 대사 수정, 저장 → 엑셀을 열었을 때 행과 서식이 맞고, 변환 검증을 통과합니다.
- 엑셀이 열려 있으면 저장이 막히고 안내가 나옵니다.
- 저장, 툴 닫기, 다시 열기 후 노드 위치가 유지됩니다.

확인 결과 (2026-10-05):
- Python 테스트 9개 통과(`test_table_edit` 3, `test_convert_table` 6).
- 실제 엑셀 사본에 툴과 같은 형식의 요청(1번 행 선택지 추가, 12번 행과 대사 추가)을 저장: 바뀐 행은 1, 12번뿐이고 표 범위 B2:I54, 표 스타일과 자료형 행 회색 글자 유지.
- Unity EditMode 185개 통과(`DialogueEditsTests` 6개, `DialogueSimulatorTests` 5개 포함).
- 열린 Editor에서: 블록 0 노드 8개, 연결 7개 → 대사 추가 시 노드 9개, 저장 요청에 7번 행과 `dialogue.7`만 들어감 → Undo 후 노드 8개, 변경 없음.
- Unity에서 실제 저장: `dialogue.1` 대사 수정 → Python 호출 → Text_Dialogue.xlsx만 저장, JSON 차이는 그 한 줄. 툴이 다시 읽어 새 대사 표시. 확인 후 엑셀과 JSON은 백업으로 되돌림.
- 남은 확인: 창에서 직접 포트를 끌어 연결하고 엑셀을 열어 보기.

### P3. 조건과 액션 작성 UI

| 파일 | 내용 |
|---|---|
| `System/Dialogue/DialogueCommands.cs` (수정) | 문법 정보 공개: 조건 종류(flag, item), 액션 종류(setFlag 등)와 인자 형태. 문자열 ↔ 구조체 변환(`Parse`, `Format`) 추가. 게임 동작은 바꾸지 않습니다. |
| `Editor/Dialogue/CommandListField.cs` | 조건/액션 목록 편집기. 줄마다 종류 드롭다운과 인자 입력 |
| `Editor/Dialogue/CommandPickers.cs` | 아이템(Item 테이블), 플래그(전체 대화에서 쓰인 키 + 새 키), 사운드(`AudioLibrary` 클립 이름) 선택 목록 |

- 스키마 검증에 조건/액션 문법 검사를 추가합니다. 예: `item:rose`의 아이템이 Item 테이블에 있는지. Python 변환기 쪽 작업입니다.

구현 메모:
- 줄마다 [아님] 종류, 키 입력칸과 옆 목록 버튼, 연산자(조건), 값(값을 받는 액션)입니다. 값을 비운 액션은 1로 실행됩니다.
- 플래그 목록은 모든 대화의 조건과 액션에서 쓰인 키이고, 새 키는 입력칸에 직접 씁니다. 미니게임은 `MinigameLibrary`의 ID 목록입니다.
- 스토리 동사(조건 quest/note/met, 액션 startQuest/completeQuest/addNote/strikeNote/meet/sequence/save)도 목록에 있습니다. 의뢰, 수첩, 시퀀스 키는 각 테이블의 DataId에서, met/meet 는 대화에 쓰인 화자 ID에서 고릅니다.
- 조건 종류 목록에 `var`는 넣지 않았습니다(미니게임 정의 전용). 목록에 없는 종류가 이미 적혀 있으면 그대로 보이고 저장됩니다.
- 구조체는 `Data/Structs.cs`의 `DialogueCommand`. `Parse`는 쓴 그대로(연산자 `=`, 대소문자) 나눠서 왕복이 같습니다. 공백만 빠집니다.
- 아이템 검사는 스키마 규칙 `commandRefs`(`Schema/Dialogue.json`의 Conditions `item`, Actions `giveItem`/`takeItem` → `Item.ItemId`)로 넣었습니다.
- 노드 요약 아이콘은 넣지 않았습니다. 노드에 조건과 실행 문자열이 이미 보입니다.

완료 기준:
- 목록 UI로 만든 조건/액션이 `DialogueCommands`의 기존 문자열 형식과 똑같이 저장되고, 문자열 → 목록 → 문자열 왕복이 같습니다(EditMode 테스트).
- 없는 아이템 ID를 쓰면 변환할 때 오류가 납니다.

확인 결과 (2026-10-05):
- Python 테스트 11개 통과(`CommandRefTests` 2개 추가). 실제 엑셀 전체 검증 오류 없음.
- Unity 에디터가 닫혀 있어 오프라인 빌드(dotnet)로만 컴파일을 확인했습니다. EditMode 테스트(`ParseFormat_*`, `Parse_SplitsParts`, `DialogueEditsTests`의 화자/끌어 놓기 2개)는 에디터에서 돌려야 합니다. 끌어 놓기와 화자 목록 이전 단계까지는 188개 통과했습니다.
- 남은 확인: 창에서 빈 곳에 연결 놓기, Ctrl+S, 조건/실행 목록 편집 후 저장.

## 6. 나중에 고려할 것 (지금 범위 아님)

`GameDesign.md`의 대화 기획 중 아직 시스템이 없는 것들입니다. 시스템이 생기면 조건/액션 종류로 추가합니다.
- 대화 횟수에 따라 기본 대사 5줄 순환 → `talkCount` 조건
- 챕터별 대사 → `chapter` 조건
- 퀘스트 진행 중일 때만 보이는 선택지 → `quest` 조건, 퀘스트 시작/완료 액션
- 키워드를 수첩에 적기 → `addKeyword` 액션
- 미니게임 팝업 → `minigame` 액션

## 7. 위험과 확인할 것

| 항목 | 대응 |
|---|---|
| openpyxl로 저장하면 엑셀 서식 일부가 사라질 수 있음 | 2026-10-05 왕복 테스트: 지금 파일(표, 표 스타일, 글자색)은 유지됩니다. 엑셀에 그림, 차트, 도형을 넣으면 툴로 저장할 때 사라질 수 있습니다. |
| 엑셀과 툴에서 동시에 편집 | 잠금 파일로 저장을 막고, 읽은 뒤 엑셀이 수정되었으면 저장 전에 묻습니다. |
| GraphView가 Experimental API | 그래프 코드를 `DialogueGraphView`와 `DialogueNodeView`에만 두고, 데이터와 시뮬레이션은 GraphView와 무관하게 만듭니다. 나중에 Graph Toolkit으로 옮기기 쉽게 하기 위해서입니다. |
| Python 미설치 PC | P1은 Python 없이 동작합니다(JSON만 읽음). P2 저장 버튼은 Python 경로를 검사하고 안내합니다. |

## 8. 준비

- 작업할 PC에 Python 3와 `openpyxl`을 설치합니다.
