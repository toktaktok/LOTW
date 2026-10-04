using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;

namespace Tests.EditMode
{
    public class InventoryManagerTests
    {
        private class FakeItemDataProvider : IItemDataProvider
        {
            private readonly Dictionary<string, ItemData> _items = new();

            public void Register(string itemId, int maxStack)
            {
                _items[itemId] = new ItemData { itemId = itemId, maxStack = maxStack };
            }

            public ItemData? GetItemData(string itemId)
            {
                if(_items.TryGetValue(itemId, out ItemData data))
                    return data;

                return null;
            }
        }

        private GameObject _go;
        private InventoryManager _inventory;
        private FakeItemDataProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("InventoryManagerTests");
            _inventory = _go.AddComponent<InventoryManager>();
            _provider = new FakeItemDataProvider();
            _inventory.SetItemDataProvider(_provider);
            _inventory.LoadFromSaveData(null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void AddItem_MaxStackZero_PlacesOnePerSlot()
        {
            _provider.Register("potion", 0);

            bool result = _inventory.AddItem("potion", 3);

            Assert.IsTrue(result);
            Assert.AreEqual(3, _inventory.GetItemCount("potion"));
            for(int i = 0; i < 3; i++)
                Assert.AreEqual(1, _inventory.Slots[i].count);
            Assert.IsTrue(_inventory.Slots[3].IsEmpty);
        }

        [Test]
        public void AddItem_StacksUpToMaxStack_ThenSpillsToNewSlot()
        {
            _provider.Register("potion", 5);

            bool result = _inventory.AddItem("potion", 7);

            Assert.IsTrue(result);
            Assert.AreEqual(5, _inventory.Slots[0].count);
            Assert.AreEqual(2, _inventory.Slots[1].count);
            Assert.IsTrue(_inventory.Slots[2].IsEmpty);
        }

        [Test]
        public void AddItem_FillsExistingStackBeforeUsingNewSlot()
        {
            _provider.Register("potion", 5);
            _inventory.AddItem("potion", 3);

            _inventory.AddItem("potion", 4);

            Assert.AreEqual(5, _inventory.Slots[0].count);
            Assert.AreEqual(2, _inventory.Slots[1].count);
        }

        [Test]
        public void AddItem_ReturnsFalse_WhenInventoryFull()
        {
            _provider.Register("potion", 1);
            Assert.IsTrue(_inventory.AddItem("potion", _inventory.MaxSlots));

            bool result = _inventory.AddItem("potion", 1);

            Assert.IsFalse(result);
            Assert.AreEqual(_inventory.MaxSlots, _inventory.GetItemCount("potion"));
        }

        [Test]
        public void RemoveItem_RemovesAcrossSlots()
        {
            _provider.Register("potion", 5);
            _inventory.AddItem("potion", 7);

            bool result = _inventory.RemoveItem("potion", 6);

            Assert.IsTrue(result);
            Assert.AreEqual(1, _inventory.GetItemCount("potion"));
        }

        [Test]
        public void RemoveItem_ReturnsFalse_WhenInsufficient()
        {
            _provider.Register("potion", 5);
            _inventory.AddItem("potion", 3);

            bool result = _inventory.RemoveItem("potion", 4);

            Assert.IsFalse(result);
            Assert.AreEqual(3, _inventory.GetItemCount("potion"));
        }
    }
}
