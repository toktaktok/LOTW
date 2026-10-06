// 자동 생성 파일. Table/Schema/Dialogue.json 을 고치고 ConvertTable.bat 을 실행하세요.
using System;

namespace Project.Scripts.Data.Table
{
    [Serializable]
    public partial class DialogueData : TableRowData
    {
        /// <summary>화자 내부 ID (예: npc_shopkeeper)</summary>
        public string speakerId;
        /// <summary>표시 이름. Text 키 (@키)</summary>
        public string speakerName;
        /// <summary>대사 키. 이 행 전용은 @dialogue.{DataId}, 공용 대사는 @dialogue.common.{이름}, 공용 메뉴는 @ui.*. 비우고 ChoiceIds를 채우면 분기 행</summary>
        public string text;
        /// <summary>다음 행 DataId. 비우거나 -1이면 종료</summary>
        public int nextId = -1;
        /// <summary>선택지 행 DataId 목록 (쉼표 구분, 최대 4개)</summary>
        public int[] choiceIds;
        /// <summary>표시/선택 조건 (예: flag:got_rose;!item:rose&gt;=2)</summary>
        public string conditions;
        /// <summary>표시/선택 시 실행 (예: giveItem:rose=1;setFlag:got_rose)</summary>
        public string actions;
    }
}
