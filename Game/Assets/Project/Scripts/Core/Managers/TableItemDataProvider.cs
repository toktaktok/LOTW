using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// Item 테이블(DataManager)에서 아이템 데이터를 조회하는 기본 구현.
    /// 아이콘은 Resources/Items/{icon} 스프라이트를 처음 조회할 때 로드해 캐싱합니다.
    /// 새 아이템 추가: Item.xlsx 에 행 추가 + 아이콘 스프라이트를 Resources/Items/ 에 배치.
    /// </summary>
    public class TableItemDataProvider : IItemDataProvider
    {
        private const string IconFolder = "Items";

        private Dictionary<string, ItemData> _items;

        public ItemData? GetItemData(string itemId)
        {
            if(string.IsNullOrEmpty(itemId))
                return null;

            Dictionary<string, ItemData> items = GetItems();
            if(items != null && items.TryGetValue(itemId, out ItemData data))
                return data;
            return null;
        }

        /// <summary>DataManager 로드가 끝난 뒤에만 캐싱합니다. 로드 전 호출은 null.</summary>
        private Dictionary<string, ItemData> GetItems()
        {
            if(_items != null)
                return _items;
            if(!DataManager.HasInstance || !DataManager.Instance.IsLoaded)
                return null;

            _items = new Dictionary<string, ItemData>();
            foreach(ItemTableData row in DataManager.Instance.GetRows<ItemTableData>())
            {
                if(string.IsNullOrEmpty(row.itemId))
                    continue;
                if(_items.ContainsKey(row.itemId))
                    Debug.LogWarning($"[TableItemDataProvider] Duplicate itemId '{row.itemId}' (dataId {row.dataId}); later row wins.");
                _items[row.itemId] = ToItemData(row);
            }
            return _items;
        }

        private static ItemData ToItemData(ItemTableData row)
        {
            Sprite icon = null;
            if(!string.IsNullOrEmpty(row.icon))
            {
                icon = Resources.Load<Sprite>($"{IconFolder}/{row.icon}");
                if(icon == null)
                    Debug.LogWarning($"[TableItemDataProvider] Missing icon Resources/{IconFolder}/{row.icon} for item '{row.itemId}'.");
            }

            return new ItemData
            {
                itemId = row.itemId,
                displayName = row.displayName,
                description = row.description,
                icon = icon,
                maxStack = row.maxStack,
            };
        }
    }
}
