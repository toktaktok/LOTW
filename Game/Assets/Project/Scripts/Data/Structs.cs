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
        /// <summary>SaveDefines.Version. 0 이면 버전 도입 전 세이브.</summary>
        public int version;
        /// <summary>누적 플레이 시간(초). 슬롯 UI 표시용.</summary>
        public float playTime;
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

    /// <summary>
    /// 미니게임 결과 한 가지. when 조건(대화 조건 문법 + var:)을 만족하거나 메카닉이 이름으로 직접 끝내면 확정됩니다.
    /// </summary>
    [Serializable]
    public class MinigameOutcome
    {
        [Tooltip("결과 이름. 기록 플래그 mg_{id}_{name} 과 Session.End(name) 에 쓰임")]
        public string name;
        public MinigameOutcomeKind kind;
        [Tooltip("자동 판정 조건 (예: var:jumps>=5). 비우면 메카닉이 End(name)으로만 확정")]
        public string when;
        [Tooltip("보상/결과 액션 (예: giveItem:rose;setFlag:key)")]
        public string actions;
        [Tooltip("대화에서 시작한 경우 이어갈 Dialogue DataID. -1이면 원래 다음 행")]
        public int followDialogueId = -1;
    }

    /// <summary>
    /// 미니게임 창 배치. 창 크기는 스테이지 해상도 x 정수 배율로 정해집니다.
    /// </summary>
    [Serializable]
    public struct MinigameWindowLayout
    {
        public MinigamePlacement placement;
        [Tooltip("placement가 Anchor일 때 화면 정규화 좌표 (0,0 = 왼쪽 아래)")]
        public Vector2 anchor;
        [Tooltip("기준 위치에서의 오프셋 (1920x1080 기준 픽셀)")]
        public Vector2 offset;
        public MinigameOpenFrom openFrom;
        [Tooltip("스테이지 픽셀 표시 배율. 0이면 기본값")]
        public int displayScale;
    }

    /// <summary>
    /// 끝난 미니게임의 결과. 중단이면 kind가 Aborted.
    /// </summary>
    public struct MinigameResult
    {
        public string minigameId;
        public string outcomeName;
        public MinigameOutcomeKind kind;
        public int followDialogueId;
    }
}
