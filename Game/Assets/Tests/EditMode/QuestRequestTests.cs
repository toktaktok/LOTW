using System.Collections.Generic;
using NUnit.Framework;
using Project.Scripts.Content.Story;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;

namespace Tests.EditMode
{
    /// <summary>NPC 머리 위 땀 표시 판정 (QuestLog.HasRequestFrom).</summary>
    public class QuestRequestTests
    {
        private readonly List<QuestData> _quests = new()
        {
            new QuestData { dataId = 1, giverId = "npc_mayor", chapter = 1 },
            new QuestData { dataId = 101, giverId = "npc_gumman", chapter = 1 },
            new QuestData { dataId = 201, giverId = "npc_gumman", chapter = 2 }
        };

        private readonly Dictionary<int, QuestState> _states = new();

        private QuestState GetState(int id) => _states.TryGetValue(id, out QuestState state) ? state : QuestState.None;

        [Test]
        public void NotStarted_InCurrentChapter_HasRequest()
        {
            Assert.IsTrue(QuestLog.HasRequestFrom("npc_gumman", 1, _quests, GetState));
        }

        [TestCase(QuestState.Active)]
        [TestCase(QuestState.Done)]
        [TestCase(QuestState.Locked)]
        public void StartedOrLocked_NoRequest(QuestState state)
        {
            _states[101] = state;
            Assert.IsFalse(QuestLog.HasRequestFrom("npc_gumman", 1, _quests, GetState));
        }

        [Test]
        public void OtherChapter_NoRequest()
        {
            Assert.IsFalse(QuestLog.HasRequestFrom("npc_mayor", 0, _quests, GetState));
            Assert.IsTrue(QuestLog.HasRequestFrom("npc_gumman", 2, _quests, GetState));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("npc_nobody")]
        public void UnknownGiver_NoRequest(string giverId)
        {
            Assert.IsFalse(QuestLog.HasRequestFrom(giverId, 1, _quests, GetState));
        }
    }
}
