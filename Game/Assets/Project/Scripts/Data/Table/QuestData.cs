using System;

namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 의뢰 테이블 한 행. 상태는 플래그 quest.{dataId} 에 저장됩니다 (StoryKeys).
    /// Excel: Table/Excel/Quest.xlsx  (DataId, Type, Chapter, GiverId, Title, Summary, ObjectiveIds)
    /// 챕터당 메인 1개 + 서브 최대 3개. 같은 주민(giverId)은 한 번에 의뢰 1개만 맡깁니다.
    /// title, summary 는 '@키' 가능.
    /// </summary>
    [Serializable]
    public class QuestData : TableRowData
    {
        public const string MainType = "main";
        public const string SubType = "sub";

        /// <summary>"main" 또는 "sub"</summary>
        public string type;
        public int chapter;
        /// <summary>의뢰한 캐릭터 ID (Dialogue speakerId 와 같은 규칙, 예: npc_gumman)</summary>
        public string giverId;
        public string title;
        public string summary;
        /// <summary>QuestObjective 테이블 dataId 목록. 수첩 의뢰 상세의 진행도가 됩니다.</summary>
        public int[] objectiveIds;

        public bool IsMain => type == MainType;
    }
}
