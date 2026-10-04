#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// Project 창 폴더 꾸미기: 색상 틴트 + 커스텀 아이콘 배지. 리스트/트리/그리드 뷰 지원.
    /// Alt+클릭(폴더 아이콘)으로 스타일 선택 창을 엽니다.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectFolderDrawer
    {
        private struct Resolved
        {
            public bool has;
            public FolderStyle style;
            public bool empty;
        }

        private static readonly Dictionary<string, Resolved> _cache = new Dictionary<string, Resolved>();
        private static readonly FolderStyleResolver.StyleLookup _lookup = LookupByPath;

        static ProjectFolderDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += OnItemGUI;
            EditorApplication.projectChanged += _cache.Clear;
            EnhancerSettings.OnChanged += OnSettingsChanged;
        }

        public static void OpenPicker(string[] folderGuids)
        {
            if(folderGuids == null || folderGuids.Length == 0)
                return;

            EnhancerSettings settings = EnhancerSettings.instance;
            settings.TryGetFolderStyle(folderGuids[0], out FolderStyle current);
            StylePickerWindow.Open("Folder Style", current.color, current.icon, current.inherit, (color, icon, inherit) =>
            {
                foreach(string guid in folderGuids)
                    settings.SetFolderStyle(new FolderStyle { guid = guid, color = color, icon = icon, inherit = inherit });
                settings.Commit();
            });
        }

        private static void OnSettingsChanged()
        {
            _cache.Clear();
            EditorApplication.RepaintProjectWindow();
        }

        private static void OnItemGUI(string guid, Rect rect)
        {
            if(!EnhancerSettings.instance.FolderEnabled || string.IsNullOrEmpty(guid))
                return;

            if(!_cache.TryGetValue(guid, out Resolved resolved))
            {
                resolved = Resolve(guid);
                _cache[guid] = resolved;
            }
            if(!resolved.has)
                return;

            Rect iconRect = GetIconRect(rect);
            HandleAltClick(guid, iconRect);
            if(Event.current.type != EventType.Repaint)
                return;

            FolderStyle style = resolved.style;
            if(style.color.a > 0f)
            {
                Texture2D folder = EnhancerIcons.Get(resolved.empty ? "FolderEmpty Icon" : "Folder Icon");
                if(folder != null)
                {
                    Color previous = GUI.color;
                    GUI.color = style.color;
                    GUI.DrawTexture(iconRect, folder, ScaleMode.ScaleToFit);
                    GUI.color = previous;
                }
            }

            Texture2D badge = EnhancerIcons.Get(style.icon);
            if(badge != null)
            {
                float size = iconRect.width * ToolDefines.FolderBadgeScale;
                Rect badgeRect = new Rect(iconRect.xMax - size, iconRect.yMax - size, size, size);
                GUI.DrawTexture(badgeRect, badge, ScaleMode.ScaleToFit);
            }
        }

        private static Resolved Resolve(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if(!AssetDatabase.IsValidFolder(path))
                return default;

            EnhancerSettings settings = EnhancerSettings.instance;
            IReadOnlyList<FolderRule> rules = settings.UseFolderRules ? settings.FolderRules : null;
            if(!FolderStyleResolver.TryResolve(path, _lookup, rules, out FolderStyle style))
                return default;

            return new Resolved
            {
                has = true,
                style = style,
                empty = !Directory.EnumerateFileSystemEntries(path).GetEnumerator().MoveNext(),
            };
        }

        private static bool LookupByPath(string folderPath, out FolderStyle style)
        {
            return EnhancerSettings.instance.TryGetFolderStyle(AssetDatabase.AssetPathToGUID(folderPath), out style);
        }

        private static Rect GetIconRect(Rect rect)
        {
            // 그리드 뷰: 위쪽 정사각형이 아이콘
            if(rect.height > EditorGUIUtility.singleLineHeight + 4f)
                return new Rect(rect.x, rect.y, rect.width, rect.width);
            // 리스트/트리 뷰: 왼쪽 행 높이 정사각형
            return new Rect(rect.x, rect.y, rect.height, rect.height);
        }

        private static void HandleAltClick(string guid, Rect iconRect)
        {
            Event e = Event.current;
            if(e.type != EventType.MouseDown || e.button != 0 || !e.alt || !iconRect.Contains(e.mousePosition))
                return;

            string[] selected = Selection.assetGUIDs;
            string[] targets = Array.IndexOf(selected, guid) >= 0 ? FilterFolders(selected) : new[] { guid };
            OpenPicker(targets);
            e.Use();
        }

        public static string[] FilterFolders(string[] guids)
        {
            var folders = new List<string>();
            foreach(string guid in guids)
            {
                if(AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(guid)))
                    folders.Add(guid);
            }
            return folders.ToArray();
        }
    }
}
#endif
