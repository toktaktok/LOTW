#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// Hierarchy 행 꾸미기: 헤더, 트리 라인, 배경색/커스텀 아이콘(직접 지정 또는 이름 규칙),
    /// 활성 토글(마우스 오버 시), Missing Script 경고.
    /// Alt+클릭(아이콘 영역)으로 스타일 선택 창을 엽니다.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyDrawer
    {
        private static readonly Dictionary<int, string> _keyCache = new Dictionary<int, string>();
        private static int _hoveredId;
        private static GUIContent _missingScriptContent;
        private static GUIStyle _headerStyle;
        private static GUIStyle _labelStyle;

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

            if(settings.ShowActiveToggle)
                TrackHover(instanceID, rect);

            if(settings.ShowHeaders && go.name.StartsWith(ToolDefines.HierarchyHeaderPrefix))
            {
                DrawHeader(go, rect);
                return;
            }

            if(settings.ShowTreeLines)
                DrawTreeLines(go.transform, rect);

            Rect iconRect = new Rect(rect.x, rect.y, ToolDefines.HierarchyIconSize, ToolDefines.HierarchyIconSize);
            if(TryGetStyle(go, out HierarchyStyle style))
                DrawStyle(go, style, rect, iconRect, Selection.Contains(instanceID));

            float right = rect.xMax;
            if(settings.ShowActiveToggle && _hoveredId == instanceID)
                right = DrawActiveToggle(go, rect);
            if(settings.ShowMissingScripts)
                DrawMissingScriptWarning(go, rect, right);

            HandleAltClick(go, iconRect);
        }

        /// <summary>
        /// 직접 지정한 스타일이 우선이고, 없으면 이름 규칙을 적용합니다.
        /// </summary>
        private static bool TryGetStyle(GameObject go, out HierarchyStyle style)
        {
            EnhancerSettings settings = EnhancerSettings.instance;
            style = default;
            if(!settings.HasHierarchyStyles)
                return false;
            if(settings.TryGetHierarchyStyle(GetKey(go), out style))
                return true;

            if(settings.UseHierarchyRules && HierarchyStyleResolver.TryMatchRule(go.name, settings.HierarchyRules, out FolderRule rule))
            {
                style = new HierarchyStyle { color = rule.color, icon = rule.icon };
                return true;
            }
            return false;
        }

        /// <summary>
        /// 마우스가 올라간 행을 기록합니다. 기본 Hierarchy 창은 마우스 이동 이벤트를 받지 않으므로 켜 줍니다.
        /// </summary>
        private static void TrackHover(int instanceID, Rect rect)
        {
            Event e = Event.current;
            EditorWindow window = EditorWindow.mouseOverWindow;
            if(window != null && !window.wantsMouseMove && window.GetType().Name == ToolDefines.HierarchyWindowTypeName)
            {
                window.wantsMouseMove = true;
                window.wantsMouseEnterLeaveWindow = true;
            }

            if(e.type == EventType.MouseLeaveWindow && _hoveredId != 0)
            {
                _hoveredId = 0;
                EditorApplication.RepaintHierarchyWindow();
                return;
            }

            if(e.type != EventType.MouseMove && e.type != EventType.MouseDrag)
                return;

            // rect는 들여쓰기 이후부터 시작하므로 행 전체 폭으로 넓혀서 판정
            Rect row = new Rect(0f, rect.y, rect.xMax, rect.height);
            if(row.Contains(e.mousePosition) && _hoveredId != instanceID)
            {
                _hoveredId = instanceID;
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        private static void DrawHeader(GameObject go, Rect rect)
        {
            if(_headerStyle == null)
                _headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };

            // 원래 아이콘/이름을 행 배경색으로 지움. 왼쪽 폴드아웃 화살표는 가리지 않도록 아이콘 위치부터.
            // 그라데이션이 오른쪽 끝에서 일반 행 배경과 자연스럽게 이어지도록 어두운 단색 바탕은 깔지 않음.
            EditorGUI.DrawRect(rect, GetRowBackground(Selection.Contains(go)));

            if(TryGetStyle(go, out HierarchyStyle style) && style.color.a > 0f)
            {
                DrawGradient(rect, style.color, ToolDefines.HierarchyHeaderGradientAlpha);
            }
            else
            {
                Color neutral = EditorGUIUtility.isProSkin ? ToolDefines.HierarchyHeaderColor : ToolDefines.HierarchyHeaderColorLight;
                DrawGradient(rect, neutral, neutral.a);
            }

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

        /// <summary>
        /// 콜백은 Unity가 행을 그린 뒤 호출되므로, 아이콘/이름 영역을 행 배경색으로 지우고
        /// 색 띠를 깐 다음 아이콘과 이름을 다시 그려 색이 글자 뒤에 깔리게 합니다.
        /// </summary>
        private static void DrawStyle(GameObject go, HierarchyStyle style, Rect rect, Rect iconRect, bool selected)
        {
            if(Event.current.type != EventType.Repaint)
                return;

            bool hasColor = style.color.a > 0f && !selected;
            Texture2D customIcon = EnhancerIcons.Get(style.icon);
            if(!hasColor && customIcon == null)
                return;

            GUIStyle labelStyle = GetLabelStyle(go, selected);
            var content = new GUIContent(go.name);
            float labelX = rect.x + ToolDefines.HierarchyLabelOffset;
            float labelWidth = labelStyle.CalcSize(content).x;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, labelX - rect.x + labelWidth, rect.height), GetRowBackground(selected));

            if(hasColor)
                DrawGradient(rect, style.color, ToolDefines.HierarchyBackgroundAlpha);

            Texture icon = customIcon != null ? customIcon : AssetPreview.GetMiniThumbnail(go);
            if(icon != null)
            {
                Color previous = GUI.color;
                if(!go.activeInHierarchy)
                    GUI.color = new Color(1f, 1f, 1f, ToolDefines.HierarchyInactiveAlpha);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                GUI.color = previous;
            }

            GUI.Label(new Rect(labelX, rect.y, rect.xMax - labelX, rect.height), content, labelStyle);
        }

        private static GUIStyle GetLabelStyle(GameObject go, bool selected)
        {
            if(_labelStyle == null)
                _labelStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset() };

            bool pro = EditorGUIUtility.isProSkin;
            Color text;
            if(selected)
                text = Color.white;
            else if(PrefabUtility.IsPrefabAssetMissing(go))
                text = pro ? ToolDefines.HierarchyMissingPrefabTextColor : ToolDefines.HierarchyMissingPrefabTextColorLight;
            else if(PrefabUtility.IsPartOfPrefabInstance(go))
                text = pro ? ToolDefines.HierarchyPrefabTextColor : ToolDefines.HierarchyPrefabTextColorLight;
            else
                text = EditorStyles.label.normal.textColor;

            if(!go.activeInHierarchy)
                text.a *= ToolDefines.HierarchyInactiveAlpha;

            _labelStyle.normal.textColor = text;
            return _labelStyle;
        }

        /// <summary>
        /// 왼쪽은 startAlpha, 오른쪽 끝으로 갈수록 투명해지는 색 띠.
        /// 세로 띠 여러 개를 겹치지 않게 이어 붙여 그립니다.
        /// </summary>
        private static void DrawGradient(Rect rect, Color color, float startAlpha)
        {
            if(Event.current.type != EventType.Repaint)
                return;

            int steps = ToolDefines.HierarchyGradientSteps;
            float step = rect.width / steps;
            for(int i = 0; i < steps; i++)
            {
                float x0 = Mathf.Round(rect.x + i * step);
                float x1 = Mathf.Round(rect.x + (i + 1) * step);
                color.a = startAlpha * (1f - (i + 0.5f) / steps);
                EditorGUI.DrawRect(new Rect(x0, rect.y, x1 - x0, rect.height), color);
            }
        }

        private static Color GetRowBackground(bool selected)
        {
            bool pro = EditorGUIUtility.isProSkin;
            if(selected)
                return pro ? ToolDefines.HierarchySelectedColor : ToolDefines.HierarchySelectedColorLight;
            return pro ? ToolDefines.HierarchyRowColor : ToolDefines.HierarchyRowColorLight;
        }

        private static float DrawActiveToggle(GameObject go, Rect rect)
        {
            float size = ToolDefines.HierarchyIconSize;
            Rect toggleRect = new Rect(rect.xMax - size, rect.y, size, rect.height);

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

        private static void DrawMissingScriptWarning(GameObject go, Rect rect, float right)
        {
            if(Event.current.type != EventType.Repaint)
                return;
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0)
                return;

            if(_missingScriptContent == null)
                _missingScriptContent = new GUIContent(EnhancerIcons.Get(ToolDefines.HierarchyMissingScriptIcon), "Missing Script");

            float size = ToolDefines.HierarchyIconSize;
            GUI.Label(new Rect(right - size, rect.y, size, rect.height), _missingScriptContent);
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
