using NUnit.Framework;
using Project.Scripts.Data;

namespace Tests.EditMode
{
    public class InventoryTests
    {
        [Test]
        public void ItemSlot_IsEmpty_WhenDefault()
        {
            var slot = new ItemSlot();
            Assert.IsTrue(slot.IsEmpty);
        }

        [Test]
        public void ItemSlot_IsNotEmpty_WhenHasItem()
        {
            var slot = new ItemSlot { itemId = "potion", count = 1 };
            Assert.IsFalse(slot.IsEmpty);
        }

        [Test]
        public void ItemSlot_IsEmpty_WhenCountZero()
        {
            var slot = new ItemSlot { itemId = "potion", count = 0 };
            Assert.IsTrue(slot.IsEmpty);
        }

        [Test]
        public void ItemSlot_IsEmpty_WhenIdNull()
        {
            var slot = new ItemSlot { itemId = null, count = 5 };
            Assert.IsTrue(slot.IsEmpty);
        }

        [Test]
        public void ItemSlot_IsEmpty_WhenIdEmpty()
        {
            var slot = new ItemSlot { itemId = "", count = 3 };
            Assert.IsTrue(slot.IsEmpty);
        }

        [Test]
        public void SaveData_DefaultValues()
        {
            var save = new SaveData();
            Assert.IsNull(save.currentScene);
            Assert.IsNull(save.entranceId);
            Assert.IsNull(save.inventory);
            Assert.IsNull(save.flags);
            Assert.IsNull(save.timestamp);
        }
    }
}
