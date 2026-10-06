namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 의뢰 테이블 한 행. 상태는 플래그 quest.{dataId} 에 저장됩니다 (StoryKeys).
    /// Excel: Table/Excel/Quest.xlsx  (DataId, Type, Chapter, GiverId, Title, Summary, ObjectiveIds)
    /// 챕터당 메인 1개 + 서브 최대 3개. 같은 주민(giverId)은 한 번에 의뢰 1개만 맡깁니다.
    /// title, summary 는 Text 키(@키). 필드는 Generated/QuestData.cs.
    /// </summary>
    public partial class QuestData
    {
        public const string MainType = "main";
        public const string SubType = "sub";

        public bool IsMain => type == MainType;
    }
}
