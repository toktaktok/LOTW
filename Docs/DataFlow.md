# 데이터 참조/쓰기 구조

경로는 `Game/Assets/Project/Scripts/` 기준. 시스템을 추가하거나 바꾸면 이 표를 같이 고친다.

## 원천별 흐름

| 원천 | 읽는 곳 | 쓰는 곳 |
|---|---|---|
| 테이블 JSON (`Resources/Table/*.json`) | `DataManager.GetRow<T>(dataId)` / `GetRows<T>()` 만 거친다. 소비자: `NPC.Interact`, `DialogueUI.GetLine`, `Localization` (Text, key 사전), `TableItemDataProvider` (Item, itemId 사전) | Excel `Table/Excel/*.xlsx` -> `Table/ConvertTable.bat` (`convert_table.py`) |
| `Map.json` | `DataManager` 가 로드만 함. 런타임 사용처 없음. 맵 에디터는 `MapSerializer` 로 persistentDataPath 를 읽음 | 에디터 `MapDefaultPromoter` |
| `Resources/Items/{icon}` | `TableItemDataProvider` (직접 `Resources.Load`) | - |
| `Resources/MapPrefabs/{prefabId}` | `MapBuilder` (직접 `Resources.Load`) | - |
| ScriptableObject | `AudioLibrary` -> `AudioManager`, `CharacterProfile` -> `NPC.ApplyProfile`, `BootstrapConfig` -> `Bootstrapper` | 에디터에서만 |
| PlayerPrefs (`Settings_*`, 마지막 슬롯) | `GameInstance.LoadSettings` | `GameInstance.SaveSettings`, `SelectSaveSlot` |
| 세이브 파일 (`persistentDataPath/Saves/save_{n}.json`) | `SaveManager.Load/PeekSave/LoadAndApply` | `SaveManager.Save/SaveCurrent` |

## 런타임 상태 소유자

| 상태 | 소유자 | 저장 여부 |
|---|---|---|
| 스토리 플래그 (key -> int) | `FlagManager` (`FlagStore`) | O (`SaveData.flags`) |
| 인벤토리 | `InventoryManager` | O (`SaveData.inventory`) |
| 현재 씬 / 입구 | `SceneTransitionManager.CurrentEntranceId` + 활성 씬 | O |
| 설정 (볼륨, 언어) | `GameInstance` | PlayerPrefs |
| 의뢰 / 수첩 / 챕터 / 시간대 | 없음 (로드맵 1~2단계) | X |

## 키 규칙

- 테이블 행은 항상 int `dataId` 로 조회한다. 문자열 키(아이템 `itemId`, Text `key`, 오디오 클립 이름)는 소비자가 자체 사전을 만든다.
- 플래그 키는 지금 JSON 안에만 있다(`got_rose`). 스토리 진행용 키는 로드맵 1단계에서 상수로 모은다.
- 대화 조건/액션 문법은 `System/Dialogue/DialogueCommands.cs` 가 유일한 파서다.

## 알려진 문제

- 테이블마다 문자열 조회 사전이 소비자별로 따로 만들어지고 갱신되지 않는다.
- 오디오 재생 경로가 둘이다: `PlayBGM(AudioClip)` (SceneBgm) / `PlayBGM(string)` (대화 액션).
- `GameInstance`, `FlagManager`, `SceneTransitionManager`, `SignalManager` 는 SubSystemCollection 프리팹에 없고 처음 `Instance` 접근 때 생성된다.
