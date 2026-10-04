using System;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 아이템 ID로부터 아이템 데이터를 조회하는 인터페이스.
    /// 기본 구현(TableItemDataProvider) 외에 다른 데이터 소스로 교체 가능합니다.
    /// </summary>
    public interface IItemDataProvider
    {
        ItemData? GetItemData(string itemId);
    }

    /// <summary>
    /// 플레이어 인벤토리를 관리합니다.
    /// IItemDataProvider를 통해 아이템 데이터를 조회합니다.
    /// </summary>
    public class InventoryManager : Singleton<InventoryManager>
    {
        #region Events

        public static event Action OnInventoryChanged;

        #endregion

        #region Fields

        [SerializeField] private int maxSlots = 20;

        private readonly List<ItemSlot> _slots = new();
        private IItemDataProvider _itemDataProvider;

        #endregion

        #region Properties

        public IReadOnlyList<ItemSlot> Slots => _slots;
        public int MaxSlots => maxSlots;

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _itemDataProvider = new TableItemDataProvider();
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            _slots.Clear();
            for(int i = 0; i < maxSlots; i++)
                _slots.Add(new ItemSlot());
        }

        #endregion

        #region Item Data Provider

        /// <summary>
        /// 아이템 데이터 제공자를 교체합니다. 기본값은 TableItemDataProvider입니다.
        /// </summary>
        public void SetItemDataProvider(IItemDataProvider provider)
        {
            _itemDataProvider = provider;
        }

        public ItemData? GetItemData(string itemId)
        {
            return _itemDataProvider?.GetItemData(itemId);
        }

        #endregion

        #region Add / Remove

        public bool AddItem(string itemId, int amount = 1)
        {
            if(string.IsNullOrEmpty(itemId) || amount <= 0)
                return false;

            ItemData? data = GetItemData(itemId);
            int maxStack = Mathf.Max(1, data?.maxStack ?? 99);

            for(int i = 0; i < _slots.Count; i++)
            {
                if(_slots[i].itemId == itemId && _slots[i].count < maxStack)
                {
                    int canAdd = Mathf.Min(amount, maxStack - _slots[i].count);
                    _slots[i] = new ItemSlot { itemId = itemId, count = _slots[i].count + canAdd };
                    amount -= canAdd;
                    if(amount <= 0)
                        break;
                }
            }

            while(amount > 0)
            {
                int emptyIndex = FindEmptySlot();
                if(emptyIndex < 0)
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
            if(string.IsNullOrEmpty(itemId) || amount <= 0)
                return false;

            int totalOwned = GetItemCount(itemId);
            if(totalOwned < amount)
                return false;

            for(int i = _slots.Count - 1; i >= 0 && amount > 0; i--)
            {
                if(_slots[i].itemId != itemId)
                    continue;

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
            foreach(var slot in _slots)
            {
                if(slot.itemId == itemId)
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
            if(data != null)
            {
                foreach(var slot in data)
                    _slots.Add(slot);
            }

            while(_slots.Count < maxSlots)
                _slots.Add(new ItemSlot());

            OnInventoryChanged?.Invoke();
        }

        #endregion

        #region Helpers

        private int FindEmptySlot()
        {
            for(int i = 0; i < _slots.Count; i++)
            {
                if(_slots[i].IsEmpty)
                    return i;
            }
            return -1;
        }

        #endregion
    }
}
