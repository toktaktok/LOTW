using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Project.Scripts.Data.Table;
using Project.Scripts.Editor.Dialogue;
using Project.Scripts.System.Dialogue;

namespace Tests.EditMode
{
    /// <summary>Dialogue 에디터 편집과 저장 요청(table_edit.py 입력) 만들기.</summary>
    public class DialogueEditsTests
    {
        private DialogueTableSource _source;

        [SetUp]
        public void SetUp()
        {
            _source = DialogueTableSource.Create(
                new[]
                {
                    new DialogueData { dataId = 1, text = "@dialogue.1", choiceIds = new[] { 2, 3 } },
                    new DialogueData { dataId = 2, text = "@dialogue.2", nextId = 3 },
                    new DialogueData { dataId = 3, text = "@dialogue.common.bye" },
                },
                new[]
                {
                    new TextData { dataId = 1, key = "dialogue.1", ko = "hi" },
                    new TextData { dataId = 2, key = "dialogue.2", ko = "buy" },
                    new TextData { dataId = 3, key = "dialogue.common.bye", ko = "bye" },
                },
                new TextData[0]);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_source);
        }

        [Test]
        public void Unchanged_BuildsNothing()
        {
            Assert.IsFalse(_source.IsDirty);
            Assert.IsNull(DialogueEdits.BuildSaveJson(_source));
        }

        [Test]
        public void AddLine_UsesFirstFreeIdAndCreatesText()
        {
            int id = DialogueEdits.AddLine(_source, 0);

            Assert.AreEqual(4, id);
            Assert.AreEqual("@dialogue.4", _source.GetLine(4).text);
            Assert.IsNotNull(_source.GetDialogueText("dialogue.4"));
            Assert.IsTrue(_source.IsDirty);
        }

        [Test]
        public void AddLine_CopiesNpcSpeakerOrUsesPlayer()
        {
            _source.GetLine(1).speakerId = "player";
            _source.GetLine(2).speakerId = "npc_a";
            _source.GetLine(2).speakerName = "@character.a.name";

            DialogueData npc = _source.GetLine(DialogueEdits.AddLine(_source, 0));
            DialogueData player = _source.GetLine(DialogueEdits.AddLine(_source, 0, false, true));

            Assert.AreEqual("npc_a", npc.speakerId);
            Assert.AreEqual("@character.a.name", npc.speakerName);
            Assert.AreEqual("player", player.speakerId);
            Assert.AreEqual("@character.player.name", player.speakerName);
        }

        [Test]
        public void DropOutsidePort_CreatesConnectedLine()
        {
            var graph = new DialogueGraphView();
            graph.Show(_source, 0, false);
            Port choiceSlot = graph.nodes.ToList().ConvertAll(n => (DialogueNodeView)n).Find(n => n.Line.dataId == 1).GetOutputPort(2);

            graph.OnDropOutsidePort(new Edge { output = choiceSlot }, Vector2.zero);

            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, _source.GetLine(1).choiceIds);
            Assert.AreEqual("player", _source.GetLine(4).speakerId, "선택지 칸에서 만든 행은 플레이어");

            graph.OnDropOutsidePort(new Edge { input = graph.nodes.ToList().ConvertAll(n => (DialogueNodeView)n).Find(n => n.Line.dataId == 3).InputPort }, Vector2.zero);
            Assert.AreEqual(3, _source.GetLine(5).nextId);
        }

        [Test]
        public void AddLine_RouterHasNoTextAndRoutesAfterConnect()
        {
            int id = DialogueEdits.AddLine(_source, 0, true);
            DialogueEdits.Connect(_source, id, 0, 2);

            Assert.IsTrue(string.IsNullOrEmpty(_source.GetLine(id).text));
            Assert.IsNull(_source.GetDialogueText($"dialogue.{id}"));
            Assert.IsTrue(DialogueCommands.IsRouter(_source.GetLine(id)));
        }

        [Test]
        public void DeleteLines_ClearsReferencesAndOwnText()
        {
            DialogueEdits.DeleteLines(_source, new[] { 2 });

            Assert.IsNull(_source.GetLine(2));
            CollectionAssert.AreEqual(new[] { 3 }, _source.GetLine(1).choiceIds);
            Assert.IsNull(_source.GetDialogueText("dialogue.2"));

            DialogueEdits.DeleteLines(_source, new[] { 3 });
            Assert.IsNotNull(_source.GetDialogueText("dialogue.common.bye"), "공용 키는 지우지 않습니다");
        }

        [Test]
        public void ConnectAndDisconnect_EditNextAndChoices()
        {
            DialogueEdits.Connect(_source, 3, DialogueEdits.NextSlot, 1);
            DialogueEdits.Connect(_source, 1, 2, 1);
            DialogueEdits.Connect(_source, 1, 0, 3);
            Assert.AreEqual(1, _source.GetLine(3).nextId);
            CollectionAssert.AreEqual(new[] { 3, 3, 1 }, _source.GetLine(1).choiceIds);

            DialogueEdits.Disconnect(_source, 1, 0, 3);
            DialogueEdits.Disconnect(_source, 3, DialogueEdits.NextSlot, 1);
            CollectionAssert.AreEqual(new[] { 3, 1 }, _source.GetLine(1).choiceIds);
            Assert.AreEqual(-1, _source.GetLine(3).nextId);
        }

        [Test]
        public void SplitText_GivesRowOwnKey()
        {
            DialogueEdits.SplitText(_source, _source.GetLine(3));

            Assert.AreEqual("@dialogue.3", _source.GetLine(3).text);
            Assert.AreEqual("bye", _source.GetDialogueText("dialogue.3").ko);
        }

        [Test]
        public void BuildSaveJson_HasOnlyChangedRows()
        {
            DialogueEdits.SetText(_source, "dialogue.2", "buy!");
            DialogueEdits.DeleteLines(_source, new[] { 3 });

            string json = DialogueEdits.BuildSaveJson(_source);

            StringAssert.Contains("\"table\":\"Dialogue\"", json);
            StringAssert.Contains("\"delete\":[3]", json);
            StringAssert.Contains("\"ko\":\"buy!\"", json);
            StringAssert.DoesNotContain("\"ko\":\"hi\"", json);
            StringAssert.Contains("\"delete\":[]", json);
        }
    }
}
