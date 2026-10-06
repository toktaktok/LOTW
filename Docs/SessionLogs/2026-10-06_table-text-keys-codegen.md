# 테이블 텍스트 키 정리와 RowData 코드 생성

## 2026-10-06 18:00 | refactor/table-text-keys-codegen
- 작업: 원문 64개를 Text 테이블로 옮기고 셀을 `@키`로 바꿨다. 대상은 Notebook Title/Body, Quest Title/Summary, QuestObjective Text, Dialogue 9010-9013 SpeakerName이다.
- 작업: `Text_Notebook.xlsx` 분류를 새로 만들었다. 키는 `notebook.{DataId}.title`, `notebook.{DataId}.body`이다.
- 작업: Quest 키는 `quest.{DataId}.title`, `quest.{DataId}.summary`, `quest.objective.{DataId}`이다. 촌장 이름은 `character.mayor.name`이다.
- 작업: `table_schema.py` 검증기가 textKey 컬럼의 원문을 오류로 막는다.
- 작업: `convert_table.py`가 스키마로 `Scripts/Data/Table/Generated/{rowClass}.cs` 필드 클래스를 만든다. 손으로 쓴 RowData 파일은 partial이고 상수와 메서드만 가진다.
- 작업: 스키마 규칙 `default`를 추가했다. Dialogue NextId는 `default: -1`이다.
- 결과: Python 테스트 13개가 통과했다. `dotnet build EditModeTests.csproj`가 오류 0개로 끝났다.
- 결과: Fable 리뷰 1회를 받았다. 테스트의 rowClass 누락 처리와 desc 줄바꿈 처리를 고쳤다.
- 확인 안 함: Unity EditMode 테스트(`TableSchemaTests`, `LocalizationTests`). 에디터가 main 체크아웃을 열고 있다.
- 확인 안 함: 게임 화면의 수첩과 의뢰 문구. Play 모드를 실행하지 않았다.
- 다음: main에 병합한 뒤 에디터에서 EditMode 테스트를 실행한다.

## 2026-10-06 18:40 | main
- 작업: main의 미커밋 변경을 먼저 커밋했다. 커밋 b76d83d는 xlsx 6개를 다시 저장한 것이고, 셀 값은 같다. 커밋 a9714f0은 TMP 폰트 아틀라스 2개다.
- 작업: `refactor/table-text-keys-codegen`을 main에 병합했다. 충돌한 xlsx 3개(Notebook, Quest, QuestObjective)는 브랜치 버전을 썼다.
- 결과: `convert_table.py`를 다시 실행했고, JSON과 생성 코드에 변경이 없었다. Python 테스트 13개가 통과했다.
- 결과: Unity EditMode 테스트 276개가 모두 통과했다.
- 확인 안 함: 게임 화면의 수첩과 의뢰 문구. Play 모드를 실행하지 않았다.
- 다음: Play 모드에서 수첩과 HUD 의뢰 문구를 확인한다.
