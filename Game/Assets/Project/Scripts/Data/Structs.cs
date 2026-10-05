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

    /// <summary>
    /// 수첩 왼쪽 목록의 한 줄. NotebookPageBuilder 가 만들고 NotebookUI 가 그립니다.
    /// isHeader 행(사건 인덱스 등)은 선택할 수 없습니다.
    /// </summary>
    public struct NotebookEntryView
    {
        public int id;
        public string label;
        public bool isHeader;
        public bool isUnread;
        public bool isStruck;
        public bool isDone;
    }
}
