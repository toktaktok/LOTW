namespace Project.Scripts.Data
{
    /// <summary>
    /// 스토리 진행 상태를 FlagManager 에 넣을 때 쓰는 플래그 키.
    /// 플래그로 저장되므로 SaveData.flags 에 같이 실리고, 대화 조건(flag:/quest:/note:/met:)으로 바로 읽힙니다.
    ///   quest.{questId}   = QuestState
    ///   note.{noteId}     = NoteState
    ///   met.{characterId} = 1 (첫 만남 이후)
    /// </summary>
    public static class StoryKeys
    {
        public const string QuestPrefix = "quest.";
        public const string NotePrefix = "note.";
        public const string MetPrefix = "met.";

        /// <summary>현재 챕터 (1부터).</summary>
        public const string Chapter = "chapter";
        /// <summary>현재 시간대. 시계가 아니라 스토리 비트로 진행.</summary>
        public const string TimeSlot = "timeSlot";
        /// <summary>처음 만난 주민 수 (meet 액션이 올림).</summary>
        public const string MetCount = "metCount";

        public static string Quest(int questId) => QuestPrefix + questId;
        public static string Note(int noteId) => NotePrefix + noteId;
        public static string Met(string characterId) => MetPrefix + characterId;

        public static bool IsQuestKey(string key) => key != null && key.StartsWith(QuestPrefix);
        public static bool IsNoteKey(string key) => key != null && key.StartsWith(NotePrefix);

        /// <summary>"note.12" -> 12. 접두사가 다르거나 숫자가 아니면 false.</summary>
        public static bool TryGetId(string key, string prefix, out int id)
        {
            id = 0;
            return key != null && key.StartsWith(prefix) && int.TryParse(key.Substring(prefix.Length), out id);
        }
    }
}
