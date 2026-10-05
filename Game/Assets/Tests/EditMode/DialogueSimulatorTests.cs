using System.Collections.Generic;
using NUnit.Framework;
using Project.Scripts.Data.Table;
using Project.Scripts.Editor.Dialogue;

namespace Tests.EditMode
{
    /// <summary>꽃집 대화(10 분기 -> 1 또는 11)와 같은 구조로 시뮬레이터 진행 규칙을 확인합니다.</summary>
    public class DialogueSimulatorTests
    {
        private Dictionary<int, DialogueData> _lines;
        private DialogueSimulator _simulator;

        [SetUp]
        public void SetUp()
        {
            _lines = new Dictionary<int, DialogueData>();
            Add(1, "hello", choiceIds: new[] { 2, 3, 7 });
            Add(2, "buy", nextId: 6);
            Add(3, "leave");
            Add(6, "here", actions: "giveItem:rose=1;setFlag:got_rose");
            Add(7, "vip", conditions: "flag:vip");
            Add(10, null, choiceIds: new[] { 11, 1 });
            Add(11, "again", conditions: "flag:got_rose");
            _simulator = new DialogueSimulator(id => _lines.TryGetValue(id, out DialogueData line) ? line : null);
        }

        private void Add(int id, string text, int nextId = -1, int[] choiceIds = null, string conditions = null, string actions = null)
        {
            _lines[id] = new DialogueData { dataId = id, text = text, nextId = nextId, choiceIds = choiceIds, conditions = conditions, actions = actions };
        }

        [Test]
        public void Router_PicksBranchByStartFlag()
        {
            _simulator.Start(10);
            Assert.AreEqual(1, _simulator.Current.dataId);

            _simulator.StartFlags["got_rose"] = 1;
            _simulator.Restart();
            Assert.AreEqual(11, _simulator.Current.dataId);
        }

        [Test]
        public void Choices_ReportFailedCondition()
        {
            _simulator.Start(1);

            Assert.AreEqual(3, _simulator.Choices.Count);
            Assert.IsTrue(_simulator.Choices[0].IsAvailable);
            Assert.AreEqual("flag:vip", _simulator.Choices[2].blockedBy);

            _simulator.Choose(7);
            Assert.AreEqual(1, _simulator.Current.dataId, "숨은 선택지는 고를 수 없어야 합니다");
        }

        [Test]
        public void Choose_RunsActionsAndLogs()
        {
            _simulator.Start(1);
            _simulator.Choose(2);

            Assert.AreEqual(6, _simulator.Current.dataId);
            Assert.AreEqual(1, _simulator.Context.GetItemCount("rose"));
            Assert.AreEqual(1, _simulator.Context.GetFlag("got_rose"));
            CollectionAssert.Contains(_simulator.Context.Log, "아이템 지급 rose x1 (1)");

            _simulator.Advance();
            Assert.IsTrue(_simulator.IsFinished);
        }

        [Test]
        public void StepBack_ReplaysFromStartState()
        {
            _simulator.Start(1);
            _simulator.Choose(2);
            _simulator.StepBack();

            Assert.AreEqual(1, _simulator.Current.dataId);
            Assert.AreEqual(0, _simulator.Context.GetItemCount("rose"));
            Assert.IsFalse(_simulator.CanStepBack);
        }

        [Test]
        public void CollectConditionKeys_SplitsFlagsAndItems()
        {
            var flags = new HashSet<string>();
            var items = new HashSet<string>();
            DialogueSimulator.CollectConditionKeys(new[] { new DialogueData { conditions = "flag:a>=2;!item:rose;flag:b" } }, flags, items);

            CollectionAssert.AreEquivalent(new[] { "a", "b" }, flags);
            CollectionAssert.AreEquivalent(new[] { "rose" }, items);
        }
    }
}
