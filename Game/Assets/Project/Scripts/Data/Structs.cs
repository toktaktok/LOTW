using System;
using UnityEngine;

namespace Project.Scripts.Data
{
    /// <summary>
    /// 아이템 기본 정보.
    /// </summary>
    [Serializable]
    public struct ItemData
    {
        public string itemId;
        public string displayName;
        [TextArea(1, 3)]
        public string description;
        public Sprite icon;
        public int maxStack;
    }

    /// <summary>
    /// 인벤토리 내 아이템 슬롯.
    /// </summary>
    [Serializable]
    public struct ItemSlot
    {
        public string itemId;
        public int count;

        public bool IsEmpty => string.IsNullOrEmpty(itemId) || count <= 0;
    }

    /// <summary>
    /// 세이브 데이터 구조.
    /// </summary>
    [Serializable]
    public struct SaveData
    {
        public string currentScene;
        public string entranceId;
        public ItemSlot[] inventory;
        public string[] flags;
        public string timestamp;
    }
}
