// 자동 생성 파일. Table/Schema/QuestObjective.json 을 고치고 ConvertTable.bat 을 실행하세요.
using System;

namespace Project.Scripts.Data.Table
{
    [Serializable]
    public partial class QuestObjectiveData : TableRowData
    {
        /// <summary>목표 문구. Text 키 (@키)</summary>
        public string text;
        /// <summary>완료 조건 (Dialogue 조건 문법). 비우면 자동 완료되지 않음</summary>
        public string conditions;
    }
}
