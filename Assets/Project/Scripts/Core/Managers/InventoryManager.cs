using System;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 플레이어 인벤토리를 관리합니다.
    /// ItemData는 Resources/Items/에서 로드합니다.
    /// </summary>
    public class InventoryManager : Singleton<InventoryManager>
    {
        #region Events

        public static event Action OnInventoryChanged;

        #endregion

        #region Fields

        [SerializeField] private int maxSlots = 20;

        private readonly List<ItemSlot> _slots = new();
        private readonly Dictionary<string, ItemData> _itemDatabase = new();

        #endregion

        #region Properties

        public IReadOnlyList<ItemSlot> Slots => _slots;
        public int MaxSlots => maxSlots;

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            _slots.Clear();
            for (int i = 0; i < maxSlots; i++)
                _slots.Add(new ItemSlot());
        }

        #endregion

        #region Item Database

        public ItemData? GetItemData(string itemId)
        {
            if (_itemDatabase.TryGetValue(itemId, out ItemData data))
                return data;

            var loaded = Resources.Load<ItemDatabase>($"Items/{itemId}");
            if (loaded != null)
            {
                _itemDatabase[itemId] = loaded.Data;
                return loaded.Data;
            }

            return null;
        }

        #endregion

        #region Add / Remove

        public bool AddItem(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;

            ItemData? data = GetItemData(itemId);
            int maxStack = data?.maxStack ?? 99;

            // Try stacking on existing slots first
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].itemId == itemId && _slots[i].count < maxStack)
                {
                    int canAdd = Mathf.Min(amount, maxStack - _slots[i].count);
                    _slots[i] = new ItemSlot { itemId = itemId, count = _slots[i].count + canAdd };
                    amount -= canAdd;
                    if (amount <= 0) break;
                }
            }

            // Place remainder in empty slots
            while (amount > 0)
            {
                int emptyIndex = FindEmptySlot();
                if (emptyIndex < 0)
                {
                    Debug.LogWarning("[InventoryManager] Inventory full.");
                    OnInventoryChanged?.Invoke();
                    return false;
                }

                int toPlace = Mathf.Min(amount, maxStack);
                _slots[emptyIndex] = new ItemSlot { itemId = itemId, count = toPlace };
                amount -= toPlace;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;

            int totalOwned = GetItemCount(itemId);
            if (totalOwned < amount) return false;

            for (int i = _slots.Count - 1; i >= 0 && amount > 0; i--)
            {
                if (_slots[i].itemId != itemId) continue;

                int toRemove = Mathf.Min(amount, _slots[i].count);
                int remaining = _slots[i].count - toRemove;

                _slots[i] = remaining > 0
                    ? new ItemSlot { itemId = itemId, count = remaining }
                    : new ItemSlot();

                amount -= toRemove;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool HasItem(string itemId, int amount = 1)
        {
            return GetItemCount(itemId) >= amount;
        }

        public int GetItemCount(string itemId)
        {
            int total = 0;
            foreach (var slot in _slots)
            {
                if (slot.itemId == itemId)
                    total += slot.count;
            }
            return total;
        }

        #endregion

        #region Save/Load Integration

        public ItemSlot[] ToSaveData()
        {
            return _slots.ToArray();
        }

        public void LoadFromSaveData(ItemSlot[] data)
        {
            _slots.Clear();
            if (data != null)
            {
                foreach (var slot in data)
                    _slots.Add(slot);
            }

            while (_slots.Count < maxSlots)
                _slots.Add(new ItemSlot());

            OnInventoryChanged?.Invoke();
        }

        #endregion

        #region Helpers

        private int FindEmptySlot()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty) return i;
            }
            return -1;
        }

        #endregion
    }
}
