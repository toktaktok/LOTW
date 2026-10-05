using System;
using System.Collections.Generic;
using System.Text;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;

namespace Project.Scripts.Content.Story
{
    /// <summary>
    /// 테이블 행과 플래그 상태를 수첩 화면 내용(목록 줄, 상세 본문, 포스트잇)으로 바꿉니다.
    /// 매니저를 직접 부르지 않으므로 테스트에서 그대로 씁니다. 문자열 '@키' 변환은 NotebookUI 가 합니다.
    /// </summary>
    public static class NotebookPageBuilder
    {
        private const string DoneMark = "●";
        private const string TodoMark = "○";

        /// <summary>진행 중(메인 먼저) 다음 완료 의뢰. 시작 전/잠긴 의뢰는 빠집니다.</summary>
        public static List<NotebookEntryView> BuildQuestList(IEnumerable<QuestData> quests, Func<int, QuestState> getState)
        {
            var active = new List<QuestData>();
            var done = new List<QuestData>();
            foreach(QuestData quest in quests)
            {
                QuestState state = getState(quest.dataId);
                if(state == QuestState.Active)
                    active.Add(quest);
                else if(state == QuestState.Done)
                    done.Add(quest);
            }
            QuestLog.SortForDisplay(active);
            QuestLog.SortForDisplay(done);

            var result = new List<NotebookEntryView>(active.Count + done.Count);
            foreach(QuestData quest in active)
                result.Add(new NotebookEntryView { id = quest.dataId, label = quest.title });
            foreach(QuestData quest in done)
                result.Add(new NotebookEntryView { id = quest.dataId, label = quest.title, isDone = true });
            return result;
        }

        /// <summary>
        /// 해금된 수첩 항목. 사건(caseId &gt; 0)이 바뀔 때마다 caseLabelFormat("사건 {0}") 머리줄을 넣고,
        /// 같은 사건 안에서 취소선 항목은 아래로 보냅니다.
        /// </summary>
        public static List<NotebookEntryView> BuildNoteList(IEnumerable<NotebookData> notes, Func<int, NoteState> getState, string caseLabelFormat)
        {
            var unlocked = new List<NotebookData>();
            foreach(NotebookData note in notes)
            {
                if(getState(note.dataId) != NoteState.None)
                    unlocked.Add(note);
            }
            NotebookLog.SortForDisplay(unlocked, getState);

            var result = new List<NotebookEntryView>(unlocked.Count);
            int currentCase = 0;
            foreach(NotebookData note in unlocked)
            {
                if(note.caseId > 0 && note.caseId != currentCase)
                {
                    currentCase = note.caseId;
                    result.Add(new NotebookEntryView { id = -note.caseId, label = string.Format(caseLabelFormat, note.caseId), isHeader = true });
                }

                NoteState state = getState(note.dataId);
                result.Add(new NotebookEntryView
                {
                    id = note.dataId,
                    label = note.title,
                    isUnread = state == NoteState.Unread,
                    isStruck = state == NoteState.Struck
                });
            }
            return result;
        }

        /// <summary>의뢰 상세 본문: 요약, 진행도, 목표 체크 목록. 완료 목표는 취소선.</summary>
        public static string BuildQuestDetail(string summary, IReadOnlyList<string> objectiveTexts, IReadOnlyList<bool> objectiveDone, float progress, string progressFormat)
        {
            var sb = new StringBuilder();
            if(!string.IsNullOrEmpty(summary))
                sb.Append(summary).Append("\n\n");

            sb.AppendFormat(progressFormat, (int)Math.Round(progress * 100f));
            for(int i = 0; i < objectiveTexts.Count; i++)
            {
                bool done = i < objectiveDone.Count && objectiveDone[i];
                sb.Append('\n').Append(done ? DoneMark : TodoMark).Append(' ');
                if(done)
                    sb.Append("<s>").Append(objectiveTexts[i]).Append("</s>");
                else
                    sb.Append(objectiveTexts[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 표지 포스트잇: 진행 중 의뢰마다 아직 안 끝난 첫 목표 하나. 목표가 모두 끝났거나 없으면 의뢰 제목.
        /// activeQuests 는 표시 순서(메인 먼저)로 넘깁니다.
        /// </summary>
        public static List<(string text, bool isMain)> BuildStickyNotes(IReadOnlyList<QuestData> activeQuests,
            Func<QuestData, IReadOnlyList<QuestObjectiveData>> getObjectives, Func<QuestObjectiveData, bool> isDone, int max)
        {
            var result = new List<(string, bool)>();
            foreach(QuestData quest in activeQuests)
            {
                if(result.Count >= max)
                    break;

                string text = quest.title;
                foreach(QuestObjectiveData objective in getObjectives(quest))
                {
                    if(!isDone(objective))
                    {
                        text = objective.text;
                        break;
                    }
                }
                result.Add((text, quest.IsMain));
            }
            return result;
        }
    }
}
