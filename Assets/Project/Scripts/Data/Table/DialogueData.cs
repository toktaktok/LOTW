using System;
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
    /// speakerName, text 는 '@키'로 Text 테이블을 참조할 수 있습니다.
    /// </summary>
    [Serializable]
    public class DialogueData : TableRowData
    {
        /// <summary>화자 내부 ID (캐릭터 식별용, 예: "npc_shopkeeper")</summary>
        public string speakerId;

        /// <summary>화면에 표시할 화자 이름</summary>
        public string speakerName;

        /// <summary>대화 텍스트</summary>
        public string text;

        /// <summary>다음 대화 DataID. -1이면 대화 종료.</summary>
        public int nextId = -1;

        /// <summary>
        /// 선택지로 분기할 때 각 선택지 대화의 DataID 목록.
        /// 값이 있으면 nextId 대신 이 목록의 ID들이 선택지로 표시됩니다.
        /// </summary>
        public int[] choiceIds;

        /// <summary>이 행을 선택지/분기 대상으로 쓸 수 있는 조건. 예) "flag:got_rose;!item:rose>=2"</summary>
        public string conditions;

        /// <summary>이 행이 표시되거나 선택될 때 실행할 액션. 예) "giveItem:rose=1;setFlag:got_rose"</summary>
        public string actions;
    }
}
