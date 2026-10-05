using System;
using UnityEngine;
using Project.Scripts.Data.Table;

namespace Project.Scripts.Editor.Data
{
    /// <summary>
    /// 대화 시뮬레이션에서 현재 행의 선택지 하나. blockedBy 가 비어 있으면 고를 수 있고,
    /// 아니면 만족하지 못한 첫 조건(또는 사유)이 들어 있습니다.
    /// </summary>
    public readonly struct DialogueSimChoice
    {
        public readonly int dataId;
        public readonly DialogueData line;
        public readonly string blockedBy;

        public DialogueSimChoice(int dataId, DialogueData line, string blockedBy)
        {
            this.dataId = dataId;
            this.line = line;
            this.blockedBy = blockedBy;
        }

        public bool IsAvailable => string.IsNullOrEmpty(blockedBy);
    }

    /// <summary>Dialogue 그래프 노드 하나의 위치 (Table/Layout/Dialogue.layout.json).</summary>
    [Serializable]
    public struct DialogueNodePosition
    {
        public int dataId;
        public Vector2 position;
    }

    [Serializable]
    public struct DialogueLayoutFile
    {
        public DialogueNodePosition[] nodes;
    }

    /// <summary>
    /// Hierarchy 오브젝트 하나의 표시 스타일. key는 GlobalObjectId 문자열.
    /// color.a == 0이면 색상 없음, icon이 비어 있으면 아이콘 없음.
    /// </summary>
    [Serializable]
    public struct HierarchyStyle
    {
        public string key;
        public Color color;
        public string icon;
    }

    /// <summary>
    /// Project 폴더 하나의 표시 스타일. inherit이면 하위 폴더에도 적용.
    /// </summary>
    [Serializable]
    public struct FolderStyle
    {
        public string guid;
        public Color color;
        public string icon;
        public bool inherit;
    }

    /// <summary>
    /// 이름 규칙. 이름이 일치하는(대소문자 무시) 폴더에 자동 적용.
    /// Hierarchy 규칙에도 쓰이며, 그때 name은 '*' 와일드카드 패턴입니다.
    /// </summary>
    [Serializable]
    public struct FolderRule
    {
        public string name;
        public Color color;
        public string icon;
    }
}
