// 자동 생성 파일. Table/Excel 과 Table/Schema 로 ConvertTable.bat 이 만듭니다.
namespace Project.Scripts.Core.Managers
{
    public partial class DataManager
    {
        private void LoadSchemaTables()
        {
            LoadTable<Project.Scripts.Data.Table.DialogueData>("Dialogue");
            LoadTable<Project.Scripts.Data.Table.ItemTableData>("Item");
            LoadTable<Project.Scripts.Data.Table.NotebookData>("Notebook");
            LoadTable<Project.Scripts.Data.Table.QuestData>("Quest");
            LoadTable<Project.Scripts.Data.Table.QuestObjectiveData>("QuestObjective");
            LoadTable<Project.Scripts.Data.Table.SequenceData>("Sequence");
        }
    }
}
