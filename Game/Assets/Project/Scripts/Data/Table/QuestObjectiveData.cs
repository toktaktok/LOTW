namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 의뢰 목표 한 행. 완료 여부는 저장하지 않고 conditions 를 매번 평가합니다 (문법은 DialogueCommands).
    /// Excel: Table/Excel/QuestObjective.xlsx  (DataId, Text, Conditions)
    /// conditions 가 비어 있으면 자동 완료되지 않습니다. text 는 Text 키(@키).
    /// 필드는 Generated/QuestObjectiveData.cs.
    /// </summary>
    public partial class QuestObjectiveData
    {
    }
}
