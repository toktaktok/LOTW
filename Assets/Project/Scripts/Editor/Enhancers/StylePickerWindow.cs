#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// 색상/아이콘 선택 창. 변경 즉시 onApply로 전달합니다.
    /// </summary>
    public class StylePickerWindow : EditorWindow
    {
        private Color _color;
        private string _icon;
        private bool _inherit;
        private bool _showInherit;
        private Action<Color, string, bool> _onApply;
        private Vector2 _scroll;

        public static void Open(string title, Color color, string icon, bool? inherit, Action<Color, string, bool> onApply)
        {
            var window = CreateInstance<StylePickerWindow>();
            window.titleContent = new GUIContent(title);
            window._color = color;
            window._icon = icon ?? string.Empty;
            window._showInherit = inherit.HasValue;
            window._inherit = inherit ?? false;
            window._onApply = onApply;

            Vector2 size = ToolDefines.PickerWindowSize;
            Vector2 anchor = Event.current != null
                ? GUIUtility.GUIToScreenPoint(Event.current.mousePosition)
                : (focusedWindow != null ? focusedWindow.position.center : new Vector2(200f, 200f));
            window.position = new Rect(anchor, size);
            window.ShowAuxWindow();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
            DrawPalette();
            EditorGUI.BeginChangeCheck();
            Color custom = EditorGUILayout.ColorField("Custom", _color);
            if(EditorGUI.EndChangeCheck())
                SetColor(custom);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Icon", EditorStyles.boldLabel);
            DrawIconGrid();

            if(_showInherit)
            {
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                _inherit = EditorGUILayout.Toggle("Apply To Subfolders", _inherit);
                if(EditorGUI.EndChangeCheck())
                    Apply();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Clear"))
            {
                _color = Color.clear;
                _icon = string.Empty;
                _inherit = false;
                Apply();
            }
            if(GUILayout.Button("Close"))
                Close();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPalette()
        {
            float size = ToolDefines.PickerSwatchSize;
            Rect row = GUILayoutUtility.GetRect(0f, size, GUILayout.ExpandWidth(true));
            Rect swatch = new Rect(row.x, row.y, size, size);

            // 첫 칸은 "색상 없음"
            if(DrawSwatch(swatch, Color.clear, _color.a <= 0f))
                SetColor(Color.clear);

            foreach(Color color in ToolDefines.EnhancerPalette)
            {
                swatch.x += size + 2f;
                if(DrawSwatch(swatch, color, _color.a > 0f && ApproximatelyRgb(_color, color)))
                    SetColor(color);
            }
        }

        private bool DrawSwatch(Rect rect, Color color, bool selected)
        {
            if(selected)
                EditorGUI.DrawRect(rect, Color.white);
            Rect inner = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
            if(color.a > 0f)
                EditorGUI.DrawRect(inner, color);
            else
                GUI.Label(inner, "x", EditorStyles.centeredGreyMiniLabel);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void DrawIconGrid()
        {
            string[] icons = ToolDefines.EnhancerIcons;
            int columns = ToolDefines.PickerIconColumns;
            float size = ToolDefines.PickerSwatchSize + 4f;

            // 0번 칸은 "아이콘 없음"
            int total = icons.Length + 1;
            int rows = (total + columns - 1) / columns;
            for(int r = 0; r < rows; r++)
            {
                Rect row = GUILayoutUtility.GetRect(0f, size, GUILayout.ExpandWidth(true));
                for(int c = 0; c < columns; c++)
                {
                    int index = r * columns + c;
                    if(index >= total)
                        break;

                    Rect cell = new Rect(row.x + c * (size + 2f), row.y, size, size);
                    string name = index == 0 ? string.Empty : icons[index - 1];
                    bool selected = name == _icon;
                    if(selected)
                        EditorGUI.DrawRect(cell, new Color(1f, 1f, 1f, 0.25f));

                    GUIContent content = index == 0
                        ? new GUIContent("x", "None")
                        : new GUIContent(EnhancerIcons.Get(name), name);
                    if(GUI.Button(cell, content, EditorStyles.iconButton))
                    {
                        _icon = name;
                        Apply();
                    }
                }
            }
        }

        private void SetColor(Color color)
        {
            _color = color;
            Apply();
        }

        private void Apply()
        {
            _onApply?.Invoke(_color, _icon, _inherit);
        }

        private static bool ApproximatelyRgb(Color a, Color b)
        {
            return Mathf.Approximately(a.r, b.r) && Mathf.Approximately(a.g, b.g) && Mathf.Approximately(a.b, b.b);
        }
    }
}
#endif
