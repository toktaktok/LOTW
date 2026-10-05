using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Project.Scripts.Content.Story;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;

namespace Tests.EditMode
{
    public class StoryLogTests
    {
        [Test]
        public void QuestSort_MainFirst_ThenChapter_ThenId()
        {
            var quests = new List<QuestData>
            {
                new QuestData { dataId = 103, type = QuestData.SubType, chapter = 1 },
                new QuestData { dataId = 201, type = QuestData.SubType, chapter = 2 },
                new QuestData { dataId = 101, type = QuestData.SubType, chapter = 1 },
                new QuestData { dataId = 1, type = QuestData.MainType, chapter = 1 },
            };

            QuestLog.SortForDisplay(quests);

            CollectionAssert.AreEqual(new[] { 1, 101, 103, 201 }, quests.Select(q => q.dataId));
        }

        [Test]
        public void NoteSort_ByCase_StruckLast_ThenId()
        {
            var notes = new List<NotebookData>
            {
                new NotebookData { dataId = 203, caseId = 1 },
                new NotebookData { dataId = 201, caseId = 1 },
                new NotebookData { dataId = 202, caseId = 1 },
                new NotebookData { dataId = 301, caseId = 2 },
            };
            var states = new Dictionary<int, NoteState> { { 201, NoteState.Struck } };

            NotebookLog.SortForDisplay(notes, id => states.TryGetValue(id, out NoteState s) ? s : NoteState.Read);

            CollectionAssert.AreEqual(new[] { 202, 203, 201, 301 }, notes.Select(n => n.dataId));
        }

        [Test]
        public void ObjectiveWithoutConditions_IsNeverAutoDone()
        {
            var objective = new QuestObjectiveData { dataId = 1, text = "manual" };

            Assert.IsFalse(QuestLog.IsObjectiveDone(objective, null));
        }

        [TestCase("note.12", 12)]
        [TestCase("note.x", -1)]
        [TestCase("quest.12", -1)]
        public void StoryKeys_TryGetId(string key, int expected)
        {
            bool ok = StoryKeys.TryGetId(key, StoryKeys.NotePrefix, out int id);

            Assert.AreEqual(expected >= 0, ok);
            if(ok)
                Assert.AreEqual(expected, id);
        }
    }
}
