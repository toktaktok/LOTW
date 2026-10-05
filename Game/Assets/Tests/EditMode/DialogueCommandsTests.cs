using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Project.Scripts.Data;
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

        [TestCase("clue:main")]
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

        #region Story

        [Test]
        public void StartQuest_SetsActive_OnlyWhenUnset()
        {
            DialogueCommands.RunActions("startQuest:101", _context);
            Assert.AreEqual((int)QuestState.Active, _context.GetFlag("quest.101"));

            _context.Flags["quest.101"] = (int)QuestState.Done;
            DialogueCommands.RunActions("startQuest:101", _context);

            Assert.AreEqual((int)QuestState.Done, _context.GetFlag("quest.101"));
        }

        [Test]
        public void CompleteQuest_SetsDone()
        {
            DialogueCommands.RunActions("startQuest:1;completeQuest:1", _context);

            Assert.AreEqual((int)QuestState.Done, _context.GetFlag("quest.1"));
        }

        [Test]
        public void AddNote_DoesNotResetReadOrStruck()
        {
            DialogueCommands.RunActions("addNote:201", _context);
            Assert.AreEqual((int)NoteState.Unread, _context.GetFlag("note.201"));

            _context.Flags["note.201"] = (int)NoteState.Read;
            DialogueCommands.RunActions("addNote:201", _context);
            Assert.AreEqual((int)NoteState.Read, _context.GetFlag("note.201"));

            DialogueCommands.RunActions("strikeNote:201;addNote:201", _context);
            Assert.AreEqual((int)NoteState.Struck, _context.GetFlag("note.201"));
        }

        [Test]
        public void Meet_CountsFirstMeetingOnce()
        {
            DialogueCommands.RunActions("meet:npc_gumman", _context);
            DialogueCommands.RunActions("meet:npc_gumman;meet:npc_mayor", _context);

            Assert.AreEqual(1, _context.GetFlag("met.npc_gumman"));
            Assert.AreEqual(2, _context.GetFlag(StoryKeys.MetCount));
        }

        [Test]
        public void StoryConditions_ReadPrefixedFlags()
        {
            Assert.IsFalse(DialogueCommands.CheckConditions("quest:101", _context));
            Assert.IsFalse(DialogueCommands.CheckConditions("met:npc_gumman", _context));

            DialogueCommands.RunActions("startQuest:101;addNote:201;meet:npc_gumman", _context);

            Assert.IsTrue(DialogueCommands.CheckConditions("quest:101=1;note:201;met:npc_gumman", _context));
            Assert.IsFalse(DialogueCommands.CheckConditions("quest:101=2", _context));
            Assert.IsTrue(DialogueCommands.CheckConditions("!note:202", _context));
        }

        #endregion
    }
}
