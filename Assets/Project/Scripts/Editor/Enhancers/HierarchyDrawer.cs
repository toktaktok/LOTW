#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// Hierarchy 행 꾸미기: 헤더, 트리 라인, 배경색/커스텀 아이콘, 컴포넌트 아이콘, 활성 토글.
    /// Alt+클릭(아이콘 영역)으로 스타일 선택 창을 엽니다.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyDrawer
    {
        private static readonly Dictionary<int, string> _keyCache = new Dictionary<int, string>();
        private static readonly List<Component> _components = new List<Component>();
        private static GUIStyle _headerStyle;

        static HierarchyDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnItemGUI;
            EditorSceneManager.sceneOpened += (_, _) => _keyCache.Clear();
            EnhancerSettings.OnChanged += EditorApplication.RepaintHierarchyWindow;
        }

        /// <summary>
        /// 오브젝트의 영구 키(GlobalObjectId). 씬을 다시 열어도 유지됩니다.
        /// </summary>
        public static string GetKey(GameObject go)
        {
            int id = go.GetInstanceID();
            if(!_keyCache.TryGetValue(id, out string key))
            {
                key = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
                _keyCache[id] = key;
            }
            return key;
        }

        public static void OpenPicker(GameObject[] targets)
        {
            if(targets == null || targets.Length == 0)
                return;

            EnhancerSettings settings = EnhancerSettings.instance;
            settings.TryGetHierarchyStyle(GetKey(targets[0]), out HierarchyStyle current);
            StylePickerWindow.Open("Hierarchy Style", current.color, current.icon, null, (color, icon, _) =>
            {
                foreach(GameObject go in targets)
                {
                    if(go != null)
                        settings.SetHierarchyStyle(new HierarchyStyle { key = GetKey(go), color = color, icon = icon });
                }
                settings.Commit();
            });
        }

        private static void OnItemGUI(int instanceID, Rect rect)
        {
            EnhancerSettings settings = EnhancerSettings.instance;
            if(!settings.HierarchyEnabled)
                return;

            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if(go == null)
                return;

            if(settings.ShowHeaders && go.name.StartsWith(ToolDefines.HierarchyHeaderPrefix))
            {
                DrawHeader(go, rect);
                return;
            }

            if(settings.ShowTreeLines)
                DrawTreeLines(go.transform, rect);

            Rect iconRect = new Rect(rect.x, rect.y, ToolDefines.HierarchyIconSize, ToolDefines.HierarchyIconSize);
            if(settings.HasHierarchyStyles && settings.TryGetHierarchyStyle(GetKey(go), out HierarchyStyle style))
                DrawStyle(style, rect, iconRect, Selection.Contains(instanceID));

            float right = rect.xMax;
            if(settings.ShowActiveToggle)
                right = DrawActiveToggle(go, rect, right);
            if(settings.ShowComponentIcons)
                DrawComponentIcons(go, rect, right);

            HandleAltClick(go, iconRect);
        }

        private static void DrawHeader(GameObject go, Rect rect)
        {
            if(_headerStyle == null)
                _headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };

            // 왼쪽 폴드아웃 화살표는 가리지 않도록 아이콘 위치부터 덮음
            Color background = EditorGUIUtility.isProSkin ? ToolDefines.HierarchyHeaderColor : ToolDefines.HierarchyHeaderColorLight;
            EditorGUI.DrawRect(rect, background);

            if(EnhancerSettings.instance.TryGetHierarchyStyle(GetKey(go), out HierarchyStyle style) && style.color.a > 0f)
                DrawGradient(rect, style.color, ToolDefines.HierarchyHeaderGradientAlpha);

            string title = go.name.Trim('-', ' ').ToUpperInvariant();
            EditorGUI.LabelField(rect, title, _headerStyle);
        }

        private static void DrawTreeLines(Transform node, Rect rect)
        {
            if(node.parent == null)
                return;

            float indent = ToolDefines.HierarchyIndent;
            Color color = ToolDefines.HierarchyTreeLineColor;
            float midY = rect.y + rect.height * 0.5f;

            // 바로 위 부모와의 연결선: 마지막 자식이면 절반 높이
            float x = rect.x - indent - indent * 0.5f;
            bool last = IsLastSibling(node);
            EditorGUI.DrawRect(new Rect(x, rect.y, 1f, last ? rect.height * 0.5f : rect.height), color);
            float stubEnd = node.childCount > 0 ? rect.x - indent : rect.x - 2f;
            EditorGUI.DrawRect(new Rect(x, midY, stubEnd - x, 1f), color);

            // 상위 조상들: 그 조상 아래에 형제가 더 남아 있으면 세로선 유지
            Transform current = node.parent;
            int level = 2;
            while(current.parent != null)
            {
                if(!IsLastSibling(current))
                {
                    float ax = rect.x - indent * level - indent * 0.5f;
                    EditorGUI.DrawRect(new Rect(ax, rect.y, 1f, rect.height), color);
                }
                current = current.parent;
                level++;
            }
        }

        private static bool IsLastSibling(Transform t)
        {
            return t.GetSiblingIndex() == t.parent.childCount - 1;
        }

        private static void DrawStyle(HierarchyStyle style, Rect rect, Rect iconRect, bool selected)
        {
            if(style.color.a > 0f && !selected)
                DrawGradient(rect, style.color, ToolDefines.HierarchyBackgroundAlpha);

            Texture2D icon = EnhancerIcons.Get(style.icon);
            if(icon == null)
                return;

            // 기본 아이콘을 행 배경색으로 가린 뒤 커스텀 아이콘을 그림
            EditorGUI.DrawRect(iconRect, GetRowBackground(selected));
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        /// <summary>
        /// 왼쪽은 startAlpha, 오른쪽 끝으로 갈수록 투명해지는 색 띠.
        /// </summary>
        private static void DrawGradient(Rect rect, Color color, float startAlpha)
        {
            if(Event.current.type != EventType.Repaint)
                return;

            Color previous = GUI.color;
            color.a = startAlpha;
            GUI.color = color;
            GUI.DrawTexture(rect, EnhancerIcons.GetGradient(), ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        private static Color GetRowBackground(bool selected)
        {
            bool pro = EditorGUIUtility.isProSkin;
            if(selected)
                return pro ? ToolDefines.HierarchySelectedColor : ToolDefines.HierarchySelectedColorLight;
            return pro ? ToolDefines.HierarchyRowColor : ToolDefines.HierarchyRowColorLight;
        }

        private static float DrawActiveToggle(GameObject go, Rect rect, float right)
        {
            float size = ToolDefines.HierarchyIconSize;
            Rect toggleRect = new Rect(right - size, rect.y, size, rect.height);

            EditorGUI.BeginChangeCheck();
            bool active = GUI.Toggle(toggleRect, go.activeSelf, GUIContent.none);
            if(EditorGUI.EndChangeCheck())
            {
                // 선택된 오브젝트를 토글하면 선택 전체에 적용
                GameObject[] targets = Selection.Contains(go) ? Selection.gameObjects : new[] { go };
                Undo.RecordObjects(targets, "Toggle Active");
                foreach(GameObject target in targets)
                    target.SetActive(active);
            }
            return toggleRect.x - 2f;
        }

        private static void DrawComponentIcons(GameObject go, Rect rect, float right)
        {
            float size = ToolDefines.HierarchyComponentIconSize;
            float y = rect.y + (rect.height - size) * 0.5f;
            int drawn = 0;

            go.GetComponents(_components);
            for(int i = _components.Count - 1; i >= 0 && drawn < ToolDefines.HierarchyMaxComponentIcons; i--)
            {
                Component component = _components[i];
                if(component is Transform)
                    continue;

                Texture image = component == null
                    ? EnhancerIcons.Get("console.warnicon.sml")
                    : EditorGUIUtility.ObjectContent(component, component.GetType()).image;
                if(image == null)
                    continue;

                right -= size;
                Rect iconRect = new Rect(right, y, size, size);
                bool enabled = !(component is Behaviour behaviour) || behaviour.enabled;
                Color previous = GUI.color;
                if(!enabled)
                    GUI.color = new Color(1f, 1f, 1f, ToolDefines.HierarchyDisabledIconAlpha);
                GUI.DrawTexture(iconRect, image, ScaleMode.ScaleToFit);
                GUI.color = previous;
                drawn++;
            }
            _components.Clear();
        }

        private static void HandleAltClick(GameObject go, Rect iconRect)
        {
            Event e = Event.current;
            if(e.type != EventType.MouseDown || e.button != 0 || !e.alt || !iconRect.Contains(e.mousePosition))
                return;

            GameObject[] targets = Selection.Contains(go) ? Selection.gameObjects : new[] { go };
            OpenPicker(targets);
            e.Use();
        }
    }
}
#endif
