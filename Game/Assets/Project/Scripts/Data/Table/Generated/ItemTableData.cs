// 자동 생성 파일. Table/Schema/Item.json 을 고치고 ConvertTable.bat 을 실행하세요.
using System;

namespace Project.Scripts.Data.Table
{
    [Serializable]
    public partial class ItemTableData : TableRowData
    {
        /// <summary>세이브/인벤토리에서 쓰는 문자열 ID</summary>
        public string itemId;
        /// <summary>표시 이름. Text 키 (@키)</summary>
        public string displayName;
        /// <summary>설명. Text 키 (@키)</summary>
        public string description;
        /// <summary>Resources/Items/ 아래 스프라이트 파일 이름</summary>
        public string icon;
        /// <summary>한 칸 최대 개수</summary>
        public int maxStack;
    }
}
