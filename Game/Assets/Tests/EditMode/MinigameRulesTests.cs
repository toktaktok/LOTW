using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Project.Scripts.Data;
using Project.Scripts.System.Dialogue;
using Project.Scripts.System.Minigame;

namespace Tests.EditMode
{
    public class MinigameRulesTests
    {
        private class FakeContext : IDialogueContext
        {
            public readonly Dictionary<string, int> Flags = new();
            public readonly Dictionary<string, int> Items = new();

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

            public void PlaySfx(string key)
            {
            }

            public void PlayBgm(string key)
            {
            }

            public void PlaySequence(int sequenceId)
            {
            }

            public void SaveGame()
            {
            }
        }

        private FakeContext _game;
        private MinigameVars _vars;
        private MinigameContext _context;
        private MinigameDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _game = new FakeContext();
            _vars = new MinigameVars();
            _context = new MinigameContext(_game, _vars);
            _definition = CreateDefinition("rope", MinigameRepeatPolicy.Unlimited, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_definition);
        }

        private static MinigameDefinition CreateDefinition(string id, MinigameRepeatPolicy policy, string startConditions)
        {
            var definition = ScriptableObject.CreateInstance<MinigameDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("repeatPolicy").enumValueIndex = (int)policy;
            serialized.FindProperty("startConditions").stringValue = startConditions;
            serialized.FindProperty("startActions").stringValue = "setFlag:started";
            serialized.FindProperty("abortActions").stringValue = "setFlag:gave_up";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static MinigameOutcome[] RopeOutcomes()
        {
            return new[]
            {
                new MinigameOutcome { name = "perfect", kind = MinigameOutcomeKind.Success, when = "var:jumps>=5;var:miss==0", actions = "giveItem:rose=2" },
                new MinigameOutcome { name = "success", kind = MinigameOutcomeKind.Success, when = "var:jumps>=5", actions = "giveItem:rose", followDialogueId = 40 },
                new MinigameOutcome { name = "fail", kind = MinigameOutcomeKind.Fail, when = "var:miss>=3" },
                new MinigameOutcome { name = "secret", kind = MinigameOutcomeKind.Success, when = "" },
            };
        }

        #region Vars and conditions

        [Test]
        public void Vars_UnsetIsZero_AddAccumulates()
        {
            Assert.AreEqual(0, _vars.Get("jumps"));
            _vars.Add("jumps");
            _vars.Add("jumps", 2);
            Assert.AreEqual(3, _vars.Get("jumps"));
        }

        [Test]
        public void VarCondition_ReadsMinigameVars()
        {
            _vars.Set("jumps", 5);
            Assert.IsTrue(DialogueCommands.CheckConditions("var:jumps>=5", _context));
            Assert.IsFalse(DialogueCommands.CheckConditions("var:jumps>5", _context));
            Assert.IsTrue(DialogueCommands.CheckConditions("!var:miss", _context));
        }

        [Test]
        public void VarCondition_MixesWithFlagsAndItems()
        {
            _vars.Set("jumps", 5);
            _game.Flags["met_kid"] = 1;
            Assert.IsTrue(DialogueCommands.CheckConditions("var:jumps>=5;flag:met_kid;!item:rose", _context));
        }

        [Test]
        public void VarCondition_WithoutVariableContext_WarnsAndFails()
        {
            LogAssert.Expect(LogType.Warning, new Regex("variable context"));
            Assert.IsFalse(DialogueCommands.CheckConditions("var:jumps>=0", _game));
        }

        #endregion

        #region Minigame action

        [TestCase("minigame:jumprope", "jumprope")]
        [TestCase("setFlag:a; Minigame: jumprope ;sfx:SFX_Door", "jumprope")]
        public void TryGetMinigameId_FindsId(string actions, string expected)
        {
            Assert.IsTrue(DialogueCommands.TryGetMinigameId(actions, out string id));
            Assert.AreEqual(expected, id);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("setFlag:minigame")]
        [TestCase("minigame:")]
        public void TryGetMinigameId_NoMinigame_ReturnsFalse(string actions)
        {
            Assert.IsFalse(DialogueCommands.TryGetMinigameId(actions, out string id));
            Assert.IsNull(id);
        }

        [Test]
        public void RunActions_MinigameVerb_IsSilentAndOthersStillRun()
        {
            DialogueCommands.RunActions("setFlag:a;minigame:jumprope;addFlag:a=2", _game);
            Assert.AreEqual(3, _game.GetFlag("a"));
            LogAssert.NoUnexpectedReceived();
        }

        #endregion

        #region Outcomes

        [Test]
        public void Evaluate_NothingSatisfied_ReturnsNull()
        {
            _vars.Set("jumps", 4);
            Assert.IsNull(MinigameRules.Evaluate(RopeOutcomes(), _context));
            Assert.IsNull(MinigameRules.Evaluate(null, _context));
        }

        [Test]
        public void Evaluate_PicksFirstSatisfiedFromTop()
        {
            _vars.Set("jumps", 5);
            Assert.AreEqual("perfect", MinigameRules.Evaluate(RopeOutcomes(), _context).name);

            _vars.Set("miss", 1);
            Assert.AreEqual("success", MinigameRules.Evaluate(RopeOutcomes(), _context).name);
        }

        [Test]
        public void Evaluate_SkipsOutcomesWithoutWhen()
        {
            MinigameOutcome[] outcomes = { new MinigameOutcome { name = "secret", when = " " } };
            Assert.IsNull(MinigameRules.Evaluate(outcomes, _context));
            Assert.AreEqual("secret", MinigameRules.Find(outcomes, "secret").name);
            Assert.IsNull(MinigameRules.Find(outcomes, "nope"));
        }

        [Test]
        public void ApplyStart_RunsActionsAndCountsPlays()
        {
            MinigameRules.ApplyStart(_definition, _context);
            MinigameRules.ApplyStart(_definition, _context);
            Assert.AreEqual(1, _game.GetFlag("started"));
            Assert.AreEqual(2, _game.GetFlag("mg_rope_plays"));
        }

        [Test]
        public void ApplyEnd_Success_GivesRewardAndRecords()
        {
            MinigameOutcome success = RopeOutcomes()[1];
            MinigameResult result = MinigameRules.ApplyEnd(_definition, success, _context);

            Assert.AreEqual(1, _game.GetItemCount("rose"));
            Assert.AreEqual(1, _game.GetFlag("mg_rope_cleared"));
            Assert.AreEqual(1, _game.GetFlag("mg_rope_success"));
            Assert.AreEqual("rope", result.minigameId);
            Assert.AreEqual("success", result.outcomeName);
            Assert.AreEqual(MinigameOutcomeKind.Success, result.kind);
            Assert.AreEqual(40, result.followDialogueId);
        }

        [Test]
        public void ApplyEnd_Fail_RecordsWithoutClearing()
        {
            MinigameResult result = MinigameRules.ApplyEnd(_definition, RopeOutcomes()[2], _context);

            Assert.AreEqual(0, _game.GetFlag("mg_rope_cleared"));
            Assert.AreEqual(1, _game.GetFlag("mg_rope_fail"));
            Assert.AreEqual(0, _game.GetItemCount("rose"));
            Assert.AreEqual(MinigameOutcomeKind.Fail, result.kind);
            Assert.AreEqual(-1, result.followDialogueId);
        }

        [Test]
        public void ApplyEnd_Null_IsAbort()
        {
            MinigameResult result = MinigameRules.ApplyEnd(_definition, null, _context);

            Assert.AreEqual(1, _game.GetFlag("gave_up"));
            Assert.AreEqual(0, _game.GetFlag("mg_rope_cleared"));
            Assert.AreEqual(MinigameOutcomeKind.Aborted, result.kind);
            Assert.AreEqual(MinigameDefines.AbortedOutcomeName, result.outcomeName);
            Assert.AreEqual(-1, result.followDialogueId);
        }

        #endregion

        #region Start rules

        [Test]
        public void CanStart_NullDefinition_False()
        {
            Assert.IsFalse(MinigameRules.CanStart(null, _game));
        }

        [Test]
        public void CanStart_Unlimited_AlwaysWhileConditionsPass()
        {
            _game.Flags["mg_rope_plays"] = 3;
            _game.Flags["mg_rope_cleared"] = 1;
            Assert.IsTrue(MinigameRules.CanStart(_definition, _game));
        }

        [Test]
        public void CanStart_Once_BlockedAfterFirstPlay()
        {
            MinigameDefinition once = CreateDefinition("once", MinigameRepeatPolicy.Once, null);
            Assert.IsTrue(MinigameRules.CanStart(once, _game));
            MinigameRules.ApplyStart(once, _game);
            Assert.IsFalse(MinigameRules.CanStart(once, _game));
            Object.DestroyImmediate(once);
        }

        [Test]
        public void CanStart_UntilSuccess_BlockedOnlyAfterSuccess()
        {
            MinigameDefinition retry = CreateDefinition("retry", MinigameRepeatPolicy.UntilSuccess, null);
            MinigameRules.ApplyStart(retry, _game);
            MinigameRules.ApplyEnd(retry, RopeOutcomes()[2], _game);
            Assert.IsTrue(MinigameRules.CanStart(retry, _game));

            MinigameRules.ApplyEnd(retry, RopeOutcomes()[1], _game);
            Assert.IsFalse(MinigameRules.CanStart(retry, _game));
            Object.DestroyImmediate(retry);
        }

        [Test]
        public void CanStart_ChecksStartConditions()
        {
            MinigameDefinition gated = CreateDefinition("gated", MinigameRepeatPolicy.Unlimited, "flag:met_kid;!item:rose");
            Assert.IsFalse(MinigameRules.CanStart(gated, _game));
            _game.Flags["met_kid"] = 1;
            Assert.IsTrue(MinigameRules.CanStart(gated, _game));
            _game.Items["rose"] = 1;
            Assert.IsFalse(MinigameRules.CanStart(gated, _game));
            Object.DestroyImmediate(gated);
        }

        #endregion

        #region Layout

        [Test]
        public void ClampToScreen_InsideStaysPut()
        {
            Vector2 result = MinigameRules.ClampToScreen(new Vector2(100f, -50f), new Vector2(988f, 672f), new Vector2(1920f, 1080f));
            Assert.AreEqual(new Vector2(100f, -50f), result);
        }

        [Test]
        public void ClampToScreen_PushesBackInside()
        {
            Vector2 result = MinigameRules.ClampToScreen(new Vector2(900f, -500f), new Vector2(988f, 672f), new Vector2(1920f, 1080f));
            Assert.AreEqual(new Vector2(466f, -204f), result);
        }

        [Test]
        public void ClampToScreen_WindowLargerThanScreen_Centers()
        {
            Vector2 result = MinigameRules.ClampToScreen(new Vector2(300f, 300f), new Vector2(2400f, 1500f), new Vector2(1920f, 1080f));
            Assert.AreEqual(Vector2.zero, result);
        }

        #endregion
    }
}
