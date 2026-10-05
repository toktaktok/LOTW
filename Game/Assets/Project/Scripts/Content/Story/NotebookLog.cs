using System;
using System.Collections.Generic;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;

namespace Project.Scripts.Content.Story
{
    /// <summary>
    /// 탐정 수첩 항목 조회. 상태는 플래그 note.{id} 에 있고 해금/취소선은 대화 액션(addNote/strikeNote)이 합니다.
    /// 새 항목 알림은 FlagManager.OnFlagChanged 에서 StoryKeys.IsNoteKey 이고 상태가 Unread 인 경우입니다.
    /// </summary>
    public static class NotebookLog
    {
        public static NoteState GetState(int noteId) => (NoteState)FlagManager.Instance.Get(StoryKeys.Note(noteId));

        public static bool IsUnlocked(int noteId) => GetState(noteId) != NoteState.None;

        /// <summary>해금된 항목. category 가 null 이면 전체. 취소선 항목은 맨 아래.</summary>
        public static List<NotebookData> GetUnlocked(string category)
        {
            var result = new List<NotebookData>();
            foreach(NotebookData note in DataManager.Instance.GetRows<NotebookData>())
            {
                if((category == null || note.category == category) && IsUnlocked(note.dataId))
                    result.Add(note);
            }
            SortForDisplay(result, GetState);
            return result;
        }

        public static int GetUnreadCount()
        {
            int count = 0;
            foreach(NotebookData note in DataManager.Instance.GetRows<NotebookData>())
            {
                if(GetState(note.dataId) == NoteState.Unread)
                    count++;
            }
            return count;
        }

        /// <summary>안 읽은 항목만 읽음으로. 취소선은 유지.</summary>
        public static void MarkRead(int noteId)
        {
            if(GetState(noteId) == NoteState.Unread)
                FlagManager.Instance.Set(StoryKeys.Note(noteId), (int)NoteState.Read);
        }

        /// <summary>사건(caseId) 순, 같은 사건 안에서 취소선 항목은 아래로, 그다음 dataId 순.</summary>
        public static void SortForDisplay(List<NotebookData> notes, Func<int, NoteState> getState)
        {
            notes.Sort((a, b) =>
            {
                if(a.caseId != b.caseId)
                    return a.caseId.CompareTo(b.caseId);

                bool aStruck = getState(a.dataId) == NoteState.Struck;
                bool bStruck = getState(b.dataId) == NoteState.Struck;
                if(aStruck != bStruck)
                    return aStruck ? 1 : -1;
                return a.dataId.CompareTo(b.dataId);
            });
        }
    }
}
