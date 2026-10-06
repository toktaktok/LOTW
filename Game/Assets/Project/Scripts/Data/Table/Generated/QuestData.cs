// 자동 생성 파일. Table/Schema/Quest.json 을 고치고 ConvertTable.bat 을 실행하세요.
using System;

namespace Project.Scripts.Data.Table
{
    [Serializable]
    public partial class QuestData : TableRowData
    {
        /// <summary>main 메인 의뢰, sub 서브 의뢰</summary>
        public string type;
        /// <summary>챕터 번호</summary>
        public int chapter;
        /// <summary>의뢰한 캐릭터 ID (Dialogue SpeakerId 와 같은 규칙)</summary>
        public string giverId;
        /// <summary>제목. Text 키 (@키)</summary>
        public string title;
        /// <summary>요약. Text 키 (@키)</summary>
        public string summary;
        /// <summary>목표 행 DataId 목록 (쉼표 구분)</summary>
        public int[] objectiveIds;
    }
}
