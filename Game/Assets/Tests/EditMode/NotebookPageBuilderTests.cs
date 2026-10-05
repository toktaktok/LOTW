using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Project.Scripts.Content.Story;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;

namespace Tests.EditMode
{
    public class NotebookPageBuilderTests
    {
        [Test]
        public void QuestList_ActiveMainFirst_ThenDone_SkipsLockedAndNone()
        {
            var quests = new List<QuestData>
            {
                new QuestData { dataId = 102, type = QuestData.SubType, chapter = 1, title = "b" },
                new QuestData { dataId = 101, type = QuestData.SubType, chapter = 1, title = "a" },
                new QuestData { dataId = 1, type = QuestData.MainType, chapter = 1, title = "main" },
                new QuestData { dataId = 103, type = QuestData.SubType, chapter = 1, title = "locked" },
                new QuestData { dataId = 104, type = QuestData.SubType, chapter = 1, title = "none" },
            };
            var states = new Dictionary<int, QuestState>
            {
                { 1, QuestState.Active }, { 101, QuestState.Done }, { 102, QuestState.Active }, { 103, QuestState.Locked }
            };

            List<NotebookEntryView> list = NotebookPageBuilder.BuildQuestList(quests, id => states.TryGetValue(id, out QuestState s) ? s : QuestState.None);

            CollectionAssert.AreEqual(new[] { 1, 102, 101 }, list.Select(e => e.id));
            CollectionAssert.AreEqual(new[] { false, false, true }, list.Select(e => e.isDone));
        }

        [Test]
        public void NoteList_InsertsCaseHeaders_AndFlags()
        {
            var notes = new List<NotebookData>
            {
                new NotebookData { dataId = 101, caseId = 0, title = "profile" },
                new NotebookData { dataId = 201, caseId = 1, title = "alibi a" },
                new NotebookData { dataId = 202, caseId = 1, title = "alibi b" },
                new NotebookData { dataId = 301, caseId = 2, title = "q" },
                new NotebookData { dataId = 999, caseId = 2, title = "hidden" },
            };
            var states = new Dictionary<int, NoteState>
            {
                { 101, NoteState.Read }, { 201, NoteState.Struck }, { 202, NoteState.Unread }, { 301, NoteState.Read }
            };

            List<NotebookEntryView> list = NotebookPageBuilder.BuildNoteList(notes, id => states.TryGetValue(id, out NoteState s) ? s : NoteState.None, "case {0}");

            CollectionAssert.AreEqual(new[] { 101, -1, 202, 201, -2, 301 }, list.Select(e => e.id));
            Assert.IsTrue(list[1].isHeader);
            Assert.AreEqual("case 1", list[1].label);
            Assert.IsTrue(list[2].isUnread);
            Assert.IsTrue(list[3].isStruck);
        }

        [Test]
        public void QuestDetail_MarksDoneObjectives()
        {
            string text = NotebookPageBuilder.BuildQuestDetail("sum", new[] { "a", "b" }, new[] { true, false }, 0.5f, "{0}%");

            Assert.AreEqual("sum\n\n50%\n● <s>a</s>\n○ b", text);
        }

        [Test]
        public void StickyNotes_FirstUndoneObjective_FallbackTitle_Capped()
        {
            var main = new QuestData { dataId = 1, type = QuestData.MainType, title = "main", objectiveIds = new[] { 10, 11 } };
            var sub = new QuestData { dataId = 2, type = QuestData.SubType, title = "sub", objectiveIds = new[] { 20 } };
            var extra = new QuestData { dataId = 3, type = QuestData.SubType, title = "extra" };
            var objectives = new Dictionary<int, QuestObjectiveData>
            {
                { 10, new QuestObjectiveData { dataId = 10, text = "o10" } },
                { 11, new QuestObjectiveData { dataId = 11, text = "o11" } },
                { 20, new QuestObjectiveData { dataId = 20, text = "o20" } },
            };
            var done = new HashSet<int> { 10, 20 };

            var notes = NotebookPageBuilder.BuildStickyNotes(new[] { main, sub, extra },
                q => (q.objectiveIds ?? new int[0]).Select(id => objectives[id]).ToList(),
                o => done.Contains(o.dataId), 2);

            Assert.AreEqual(2, notes.Count);
            Assert.AreEqual(("o11", true), notes[0]);
            Assert.AreEqual(("sub", false), notes[1]);
        }
    }
}
