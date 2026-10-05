using NUnit.Framework;
using Project.Scripts.Content.Title;
using Project.Scripts.Data;

namespace Tests.EditMode
{
    public class SaveSlotSelectorTests
    {
        [Test]
        public void FindLatest_PicksNewestTimestamp_SkipsEmpty()
        {
            var slots = new SaveData?[]
            {
                new SaveData { timestamp = "2026-10-01 09:00:00" },
                null,
                new SaveData { timestamp = "2026-10-05 08:30:00" },
            };

            Assert.AreEqual(2, SaveSlotSelector.FindLatest(slots));
        }

        [Test]
        public void FindLatest_NoSaves_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, SaveSlotSelector.FindLatest(new SaveData?[] { null, null }));
        }

        [TestCase(-1, 0)]
        [TestCase(2, 2)]
        [TestCase(5, 0)]
        public void NewGameSlot_UsesCurrentOrZero(int current, int expected)
        {
            Assert.AreEqual(expected, SaveSlotSelector.GetNewGameSlot(current, 3));
        }

        [TestCase(0f, "0:00")]
        [TestCase(59f, "0:00")]
        [TestCase(3725f, "1:02")]
        [TestCase(-5f, "0:00")]
        public void FormatPlayTime(float seconds, string expected)
        {
            Assert.AreEqual(expected, SaveSlotSelector.FormatPlayTime(seconds));
        }
    }
}
