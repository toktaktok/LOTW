using UnityEngine;

namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 대화 테이블 한 행의 데이터.
    /// Excel: Table/Excel/Dialogue.xlsx
    /// JSON:  Assets/Project/Resources/Table/Dialogue.json
    ///
    /// 대화 흐름:
    ///   - nextId == -1      : 대화 종료
    ///   - choiceIds.Length > 0 : 선택지 분기 (각 ID가 선택 텍스트 대화 행을 가리킴)
    ///   - text 비어 있음 + choiceIds : 분기 행. conditions를 만족하는 첫 choiceIds 행을 바로 표시
    ///     (모두 불만족이면 nextId). 문법은 DialogueCommands 참고.
    /// speakerName, text 는 Text 키(@키)입니다. 필드는 Generated/DialogueData.cs (스키마에서 생성).
    /// </summary>
    public partial class DialogueData
    {
    }
}
