// 자동 생성 파일. Table/Schema/Notebook.json 을 고치고 ConvertTable.bat 을 실행하세요.
using System;

namespace Project.Scripts.Data.Table
{
    [Serializable]
    public partial class NotebookData : TableRowData
    {
        /// <summary>document 서류, profile 주민, alibi 알리바이, question 의문점, todo 할 일</summary>
        public string category;
        /// <summary>알리바이가 속한 사건. 0이면 없음</summary>
        public int caseId;
        /// <summary>profile, alibi 의 캐릭터 ID (예: npc_gumman)</summary>
        public string subjectId;
        /// <summary>제목. Text 키 (@키)</summary>
        public string title;
        /// <summary>본문. Text 키 (@키)</summary>
        public string body;
    }
}
