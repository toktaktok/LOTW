using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Project.Scripts.Data.Table;
using UnityEngine;
using Project.Scripts.Framework;

namespace Tests.EditMode
{
    /// <summary>Quest / QuestObjective / Notebook 테이블과, 대화·목표에서 이들을 가리키는 ID 가 서로 맞는지 검사.</summary>
    public class StoryTableTests
    {
        private static readonly string[] NoteCategories =
            { NotebookData.Document, NotebookData.Profile, NotebookData.Alibi, NotebookData.Question, NotebookData.Todo };

        // "startQuest:101", "!quest:101=2", "note:201" 에서 동사와 ID 를 뽑는다
        private static readonly Regex QuestRef = new(@"(?:startQuest|completeQuest|quest):(\d+)", RegexOptions.IgnoreCase);
        private static readonly Regex NoteRef = new(@"(?:addNote|strikeNote|note):(\d+)", RegexOptions.IgnoreCase);

        private List<QuestData> _quests;
        private List<QuestObjectiveData> _objectives;
        private List<NotebookData> _notes;
        private List<DialogueData> _dialogue;

        private static List<T> Load<T>(string table)
        {
            string path = Path.Combine(Application.dataPath, $"Project/Resources/Table/{table}.json");
            return JsonArrayHelper.FromJson<T>(File.ReadAllText(path));
        }

        [OneTimeSetUp]
        public void LoadTables()
        {
            _quests = Load<QuestData>("Quest");
            _objectives = Load<QuestObjectiveData>("QuestObjective");
            _notes = Load<NotebookData>("Notebook");
            _dialogue = Load<DialogueData>("Dialogue");
        }

        [Test]
        public void Quests_HaveValidType_AndObjectivesResolve()
        {
            var objectiveIds = new HashSet<int>(_objectives.Select(o => o.dataId));
            foreach(QuestData quest in _quests)
            {
                Assert.That(quest.type, Is.EqualTo(QuestData.MainType).Or.EqualTo(QuestData.SubType), $"quest {quest.dataId} type");
                foreach(int id in quest.objectiveIds ?? new int[0])
                    Assert.IsTrue(objectiveIds.Contains(id), $"quest {quest.dataId} objective {id} missing");
            }
        }

        [Test]
        public void OneMainQuestPerChapter()
        {
            foreach(var group in _quests.Where(q => q.IsMain).GroupBy(q => q.chapter))
                Assert.AreEqual(1, group.Count(), $"chapter {group.Key} has {group.Count()} main quests");
        }

        [Test]
        public void Notes_HaveValidCategory()
        {
            foreach(NotebookData note in _notes)
                CollectionAssert.Contains(NoteCategories, note.category, $"note {note.dataId} category");
        }

        [Test]
        public void DataIds_AreUnique()
        {
            Assert.AreEqual(_quests.Count, _quests.Select(q => q.dataId).Distinct().Count());
            Assert.AreEqual(_objectives.Count, _objectives.Select(o => o.dataId).Distinct().Count());
            Assert.AreEqual(_notes.Count, _notes.Select(n => n.dataId).Distinct().Count());
        }

        [Test]
        public void QuestAndNoteReferences_Resolve()
        {
            var questIds = new HashSet<int>(_quests.Select(q => q.dataId));
            var noteIds = new HashSet<int>(_notes.Select(n => n.dataId));

            var sources = _dialogue.Select(d => ($"dialogue {d.dataId}", $"{d.conditions};{d.actions}"))
                .Concat(_objectives.Select(o => ($"objective {o.dataId}", o.conditions ?? "")));

            foreach(var (owner, text) in sources)
            {
                foreach(Match m in QuestRef.Matches(text))
                    Assert.IsTrue(questIds.Contains(int.Parse(m.Groups[1].Value)), $"{owner}: quest {m.Groups[1].Value} missing");
                foreach(Match m in NoteRef.Matches(text))
                    Assert.IsTrue(noteIds.Contains(int.Parse(m.Groups[1].Value)), $"{owner}: note {m.Groups[1].Value} missing");
            }
        }
    }
}
