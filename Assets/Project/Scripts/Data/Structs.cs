using System;
using UnityEngine;

namespace Project.Scripts.Data
{
    /// <summary>
    /// 대화 한 줄을 나타냅니다.
    /// </summary>
    [Serializable]
    public struct DialogueLine
    {
        [Tooltip("화자 이름 (비워두면 내레이션)")]
        public string speaker;

        [TextArea(2, 5)]
        public string text;
    }

    /// <summary>
    /// NPC 등이 가지는 대화 데이터 세트.
    /// ScriptableObject로 만들어 재사용할 수도 있지만,
    /// 인라인 직렬화도 지원합니다.
    /// </summary>
    [Serializable]
    public struct DialogueData
    {
        public DialogueLine[] lines;
    }

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
