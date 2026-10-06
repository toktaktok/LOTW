namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 탐정 수첩 항목 한 행. 해금/읽음/취소선 상태는 플래그 note.{dataId} 에 저장됩니다 (StoryKeys, NoteState).
    /// Excel: Table/Excel/Notebook.xlsx  (DataId, Category, CaseId, SubjectId, Title, Body)
    /// 대화 액션 addNote:id 로 해금, strikeNote:id 로 무관 판정. title, body 는 Text 키(@키).
    /// 필드는 Generated/NotebookData.cs.
    /// </summary>
    public partial class NotebookData
    {
        /// <summary>책상 서류, 팸플릿 등 (마을 소개, 지도, 신분증)</summary>
        public const string Document = "document";
        /// <summary>주민 프로필. subjectId = 캐릭터 ID</summary>
        public const string Profile = "profile";
        /// <summary>알리바이. caseId 로 사건별 인덱스, subjectId = 진술한 캐릭터</summary>
        public const string Alibi = "alibi";
        /// <summary>의문점</summary>
        public const string Question = "question";
        /// <summary>할 일 체크리스트 (예: 바에 가 보기)</summary>
        public const string Todo = "todo";
    }
}
