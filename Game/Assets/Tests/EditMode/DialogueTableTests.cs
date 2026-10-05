using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Project.Scripts.Core;
using UnityEngine;
using DialogueRow = Project.Scripts.Data.Table.DialogueData;

namespace Tests.EditMode
{
    public class DialogueTableTests
    {
        private const int MaxChoices = 4;

        // 프리팹 오버라이드 형태와 씬 직접 필드 형태
        private static readonly Regex OverrideDialogueId = new Regex(@"propertyPath: dialogueId\s+value: (-?\d+)");
        private static readonly Regex FieldDialogueId = new Regex(@"\bdialogueId: (-?\d+)");

        private Dictionary<int, DialogueRow> _rows;
        private List<DialogueRow> _list;

        [OneTimeSetUp]
        public void LoadTable()
        {
            string path = Path.Combine(Application.dataPath, "Project/Resources/Table/Dialogue.json");
            _list = JsonArrayHelper.FromJson<DialogueRow>(File.ReadAllText(path));
            _rows = new Dictionary<int, DialogueRow>();
            foreach(DialogueRow row in _list)
                _rows[row.dataId] = row;
        }

        [Test]
        public void DataIds_AreUnique()
        {
            Assert.IsNotEmpty(_list);
            Assert.AreEqual(_list.Count, _rows.Count);
        }

        [Test]
        public void NextIds_Resolve()
        {
            foreach(DialogueRow row in _list)
                if(row.nextId != -1)
                    Assert.IsTrue(_rows.ContainsKey(row.nextId), $"row {row.dataId} nextId {row.nextId} missing");
        }

        [Test]
        public void ChoiceIds_Resolve_AndAtMostFour()
        {
            foreach(DialogueRow row in _list)
            {
                if(row.choiceIds == null)
                    continue;

                Assert.LessOrEqual(row.choiceIds.Length, MaxChoices, $"row {row.dataId} has too many choices");
                foreach(int id in row.choiceIds)
                    Assert.IsTrue(_rows.ContainsKey(id), $"row {row.dataId} choiceId {id} missing");
            }
        }

        [Test]
        public void PlazaNpcDialogueIds_Exist()
        {
            string path = Path.Combine(Application.dataPath, "Project/Scenes/Plaza.unity");
            string scene = File.ReadAllText(path);

            int found = 0;
            foreach(Regex pattern in new[] { OverrideDialogueId, FieldDialogueId })
            {
                foreach(Match match in pattern.Matches(scene))
                {
                    int id = int.Parse(match.Groups[1].Value);
                    if(id == 0)
                        continue;

                    found++;
                    Assert.IsTrue(_rows.ContainsKey(id), $"dialogueId {id} missing");
                }
            }
            Assert.Greater(found, 0, "no dialogueId found in Plaza.unity");
        }
    }
}
