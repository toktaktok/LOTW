using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Project.Scripts.Core;
using Project.Scripts.Editor.Plaza;
using UnityEngine;
using DialogueRow = Project.Scripts.Data.Table.DialogueData;

namespace Tests.EditMode
{
    public class DialogueTableTests
    {
        private const int MaxChoices = 3;

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
        public void ChoiceIds_Resolve_AndAtMostThree()
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
            foreach(var npc in PlazaLayout.Npcs)
                if(npc.dialogueId != 0)
                    Assert.IsTrue(_rows.ContainsKey(npc.dialogueId), $"dialogueId {npc.dialogueId} missing");
        }
    }
}
