using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Project.Scripts.Data.Table;
using Project.Scripts.System.Dialogue;

namespace Tests.EditMode
{
    public class DialogueCommandsTests
    {
        private class FakeContext : IDialogueContext
        {
            public readonly Dictionary<string, int> Flags = new();
            public readonly Dictionary<string, int> Items = new();
            public readonly List<string> Sounds = new();

            public int GetFlag(string key) => Flags.TryGetValue(key, out int v) ? v : 0;
            public void SetFlag(string key, int value) => Flags[key] = value;
            public void AddFlag(string key, int amount) => Flags[key] = GetFlag(key) + amount;

            public int GetItemCount(string itemId) => Items.TryGetValue(itemId, out int v) ? v : 0;

            public bool AddItem(string itemId, int amount)
            {
                Items[itemId] = GetItemCount(itemId) + amount;
                return true;
            }

            public bool RemoveItem(string itemId, int amount)
            {
                Items[itemId] = GetItemCount(itemId) - amount;
                return true;
            }

            public void PlaySfx(string key) => Sounds.Add("sfx:" + key);
            public void PlayBgm(string key) => Sounds.Add("bgm:" + key);
        }

        private FakeContext _context;
        private Dictionary<int, DialogueData> _lines;

        [SetUp]
        public void SetUp()
        {
            _context = new FakeContext();
            _lines = new Dictionary<int, DialogueData>();
        }

        private DialogueData GetLine(int id) => _lines.TryGetValue(id, out DialogueData line) ? line : null;

        private DialogueData AddLine(int id, string text, int nextId = -1, int[] choiceIds = null, string conditions = null, string actions = null)
        {
            var line = new DialogueData { dataId = id, text = text, nextId = nextId, choiceIds = choiceIds, conditions = conditions, actions = actions };
            _lines[id] = line;
            return line;
        }

        #region Conditions

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void CheckConditions_Empty_Passes(string conditions)
        {
            Assert.IsTrue(DialogueCommands.CheckConditions(conditions, _context));
        }

        [Test]
        public void CheckConditions_FlagWithoutOperator_ChecksNonZero()
        {
            Assert.IsFalse(DialogueCommands.CheckConditions("flag:met", _context));

            _context.Flags["met"] = 1;

            Assert.IsTrue(DialogueCommands.CheckConditions("flag:met", _context));
        }

        [Test]
        public void CheckConditions_Negation_Inverts()
        {
            Assert.IsTrue(DialogueCommands.CheckConditions("!flag:met", _context));

            _context.Flags["met"] = 1;

            Assert.IsFalse(DialogueCommands.CheckConditions("!flag:met", _context));
        }

        [TestCase("flag:count>=2", true)]
        [TestCase("flag:count>2", false)]
        [TestCase("flag:count<=1", false)]
        [TestCase("flag:count<3", true)]
        [TestCase("flag:count==2", true)]
        [TestCase("flag:count=2", true)]
        [TestCase("flag:count!=2", false)]
        [TestCase(" flag : count >= 2 ", true)]
        public void CheckConditions_Comparison(string condition, bool expected)
        {
            _context.Flags["count"] = 2;

            Assert.AreEqual(expected, DialogueCommands.CheckConditions(condition, _context));
        }

        [Test]
        public void CheckConditions_Item_UsesCount()
        {
            _context.Items["rose"] = 2;

            Assert.IsTrue(DialogueCommands.CheckConditions("item:rose", _context));
            Assert.IsTrue(DialogueCommands.CheckConditions("item:rose>=2", _context));
            Assert.IsFalse(DialogueCommands.CheckConditions("item:rose>=3", _context));
            Assert.IsFalse(DialogueCommands.CheckConditions("item:tulip", _context));
        }

        [Test]
        public void CheckConditions_Multiple_RequiresAll()
        {
            _context.Flags["a"] = 1;

            Assert.IsFalse(DialogueCommands.CheckConditions("flag:a;flag:b", _context));

            _context.Flags["b"] = 1;

            Assert.IsTrue(DialogueCommands.CheckConditions("flag:a; flag:b;", _context));
        }

        [TestCase("quest:main")]
        [TestCase("flag")]
        [TestCase("flag:count>=x")]
        [TestCase("flag:count=>2")]
        public void CheckConditions_Invalid_FailsWithWarning(string condition)
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[DialogueCommands\]"));

            Assert.IsFalse(DialogueCommands.CheckConditions(condition, _context));
        }

        #endregion

        #region Actions

        [Test]
        public void RunActions_Flags()
        {
            DialogueCommands.RunActions("setFlag:a;setFlag:b=5;addFlag:b=2;addFlag:c", _context);

            Assert.AreEqual(1, _context.GetFlag("a"));
            Assert.AreEqual(7, _context.GetFlag("b"));
            Assert.AreEqual(1, _context.GetFlag("c"));

            DialogueCommands.RunActions("clearFlag:a", _context);

            Assert.AreEqual(0, _context.GetFlag("a"));
        }

        [Test]
        public void RunActions_Items()
        {
            DialogueCommands.RunActions("giveItem:rose=3;takeItem:rose", _context);

            Assert.AreEqual(2, _context.GetItemCount("rose"));
        }

        [Test]
        public void RunActions_VerbIsCaseInsensitive()
        {
            DialogueCommands.RunActions("SETFLAG:a;GiveItem:rose", _context);

            Assert.AreEqual(1, _context.GetFlag("a"));
            Assert.AreEqual(1, _context.GetItemCount("rose"));
        }

        [Test]
        public void RunActions_Sounds()
        {
            DialogueCommands.RunActions("sfx:SFX_Door;bgm:BGM_Village", _context);

            CollectionAssert.AreEqual(new[] { "sfx:SFX_Door", "bgm:BGM_Village" }, _context.Sounds);
        }

        [Test]
        public void RunActions_UnknownVerb_SkipsAndContinues()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Unknown action"));

            DialogueCommands.RunActions("teleport:home;setFlag:a", _context);

            Assert.AreEqual(1, _context.GetFlag("a"));
        }

        [Test]
        public void RunActions_InvalidValue_Skips()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Invalid value"));

            DialogueCommands.RunActions("giveItem:rose=many", _context);

            Assert.AreEqual(0, _context.GetItemCount("rose"));
        }

        #endregion

        #region Routing

        [Test]
        public void ResolveRoute_NormalLine_ReturnsItself()
        {
            DialogueData line = AddLine(1, "hello", choiceIds: new[] { 2 });

            Assert.AreSame(line, DialogueCommands.ResolveRoute(line, GetLine, _context));
        }

        [Test]
        public void ResolveRoute_PicksFirstPassingChoice()
        {
            DialogueData router = AddLine(10, null, choiceIds: new[] { 11, 12 }, actions: "addFlag:visits");
            AddLine(11, "again", conditions: "flag:got_rose");
            DialogueData first = AddLine(12, "first visit");

            Assert.AreSame(first, DialogueCommands.ResolveRoute(router, GetLine, _context));
            Assert.AreEqual(1, _context.GetFlag("visits"));

            _context.Flags["got_rose"] = 1;

            Assert.AreEqual(11, DialogueCommands.ResolveRoute(router, GetLine, _context).dataId);
        }

        [Test]
        public void ResolveRoute_NoPassingChoice_FallsBackToNextId()
        {
            DialogueData router = AddLine(10, "", nextId: 20, choiceIds: new[] { 11 });
            AddLine(11, "locked", conditions: "flag:never");
            DialogueData fallback = AddLine(20, "fallback");

            Assert.AreSame(fallback, DialogueCommands.ResolveRoute(router, GetLine, _context));
        }

        [Test]
        public void ResolveRoute_NoPassingChoiceAndNoNext_Ends()
        {
            DialogueData router = AddLine(10, null, choiceIds: new[] { 11 });
            AddLine(11, "locked", conditions: "flag:never");

            Assert.IsNull(DialogueCommands.ResolveRoute(router, GetLine, _context));
        }

        [Test]
        public void ResolveRoute_ChainedRouters_Followed()
        {
            DialogueData a = AddLine(10, null, choiceIds: new[] { 11 });
            AddLine(11, null, choiceIds: new[] { 12 });
            DialogueData end = AddLine(12, "end");

            Assert.AreSame(end, DialogueCommands.ResolveRoute(a, GetLine, _context));
        }

        [Test]
        public void ResolveRoute_RouterLoop_StopsWithWarning()
        {
            DialogueData a = AddLine(10, null, choiceIds: new[] { 11 });
            AddLine(11, null, choiceIds: new[] { 10 });
            LogAssert.Expect(LogType.Warning, new Regex("Route depth exceeded"));

            Assert.IsNull(DialogueCommands.ResolveRoute(a, GetLine, _context));
        }

        #endregion

        #region Edit

        [TestCase("flag:got_rose")]
        [TestCase("!item:rose>=2;flag:met=1;var:jumps<5")]
        [TestCase("giveItem:rose=1;setFlag:got_rose;sfx:SFX_Door;minigame:jump_rope")]
        public void ParseFormat_RoundTrips(string text)
        {
            Assert.AreEqual(text, DialogueCommands.Format(DialogueCommands.Parse(text)));
        }

        [Test]
        public void ParseFormat_RoundTripsAllTableRows()
        {
            var asset = Resources.Load<TextAsset>("Table/Dialogue");
            foreach(DialogueData line in Project.Scripts.Core.JsonArrayHelper.FromJson<DialogueData>(asset.text))
            {
                foreach(string text in new[] { line.conditions, line.actions })
                {
                    if(!string.IsNullOrEmpty(text))
                        Assert.AreEqual(text, DialogueCommands.Format(DialogueCommands.Parse(text)), $"dataId {line.dataId}");
                }
            }
        }

        [Test]
        public void Parse_SplitsParts()
        {
            Project.Scripts.Data.DialogueCommand command = DialogueCommands.Parse(" !item : rose >= 2 ")[0];

            Assert.IsTrue(command.negate);
            Assert.AreEqual("item", command.verb);
            Assert.AreEqual("rose", command.key);
            Assert.AreEqual(">=", command.op);
            Assert.AreEqual("2", command.value);
        }

        #endregion
    }
}
