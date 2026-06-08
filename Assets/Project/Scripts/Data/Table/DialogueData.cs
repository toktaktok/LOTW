using System;
using UnityEngine;

namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 대화 테이블 한 행의 데이터.
    /// Excel: Table/Excel/DialogueTable.xml
    /// JSON:  Assets/Project/Resources/Table/DialogueTable.json
    ///
    /// 대화 흐름:
    ///   - nextId == -1      : 대화 종료
    ///   - choiceIds.Length > 0 : 선택지 분기 (각 ID가 선택 텍스트 대화 행을 가리킴)
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
    }
}
