using System;
using UnityEngine;

namespace Project.Scripts.Editor.Data
{
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
    /// 폴더 이름 규칙. 이름이 일치하는(대소문자 무시) 폴더에 자동 적용.
    /// </summary>
    [Serializable]
    public struct FolderRule
    {
        public string name;
        public Color color;
        public string icon;
    }
}
