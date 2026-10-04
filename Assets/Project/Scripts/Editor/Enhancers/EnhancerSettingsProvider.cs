#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// Edit > Preferences > LOTW > Editor Enhancers 페이지.
    /// </summary>
    public static class EnhancerSettingsProvider
    {
        private static string[] _iconOptions;

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(ToolDefines.EnhancerPreferencesPath, SettingsScope.User)
            {
                guiHandler = _ => DrawGUI(),
                keywords = new HashSet<string> { "Hierarchy", "Folder", "Icon", "Color", "Header" },
            };
        }

        private static void DrawGUI()
        {
            EnhancerSettings settings = EnhancerSettings.instance;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Hierarchy", EditorStyles.boldLabel);
            settings.HierarchyEnabled = EditorGUILayout.Toggle("Enabled", settings.HierarchyEnabled);
            using(new EditorGUI.DisabledScope(!settings.HierarchyEnabled))
            {
                settings.ShowHeaders = EditorGUILayout.Toggle($"Headers (\"{ToolDefines.HierarchyHeaderPrefix}\" prefix)", settings.ShowHeaders);
                settings.ShowTreeLines = EditorGUILayout.Toggle("Tree Lines", settings.ShowTreeLines);
                settings.ShowComponentIcons = EditorGUILayout.Toggle("Component Icons", settings.ShowComponentIcons);
                settings.ShowActiveToggle = EditorGUILayout.Toggle("Active Toggle", settings.ShowActiveToggle);
                if(GUILayout.Button("Clear All Object Styles", GUILayout.Width(200f)))
                    settings.ClearHierarchyStyles();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Project", EditorStyles.boldLabel);
            settings.FolderEnabled = EditorGUILayout.Toggle("Enabled", settings.FolderEnabled);
            using(new EditorGUI.DisabledScope(!settings.FolderEnabled))
            {
                if(GUILayout.Button("Clear All Folder Styles", GUILayout.Width(200f)))
                    settings.ClearFolderStyles();

                EditorGUILayout.Space();
                settings.UseFolderRules = EditorGUILayout.Toggle("Folder Name Rules", settings.UseFolderRules);
                using(new EditorGUI.DisabledScope(!settings.UseFolderRules))
                    DrawRules(settings);
            }

            if(EditorGUI.EndChangeCheck())
                settings.Commit();
        }

        private static void DrawRules(EnhancerSettings settings)
        {
            List<FolderRule> rules = settings.FolderRules;
            string[] options = GetIconOptions();

            for(int i = 0; i < rules.Count; i++)
            {
                FolderRule rule = rules[i];
                EditorGUILayout.BeginHorizontal();
                rule.name = EditorGUILayout.TextField(rule.name);
                rule.color = EditorGUILayout.ColorField(GUIContent.none, rule.color, true, true, false, GUILayout.Width(60f));
                int current = Mathf.Max(0, Array.IndexOf(options, rule.icon));
                int picked = EditorGUILayout.Popup(current, options, GUILayout.Width(160f));
                rule.icon = picked == 0 ? string.Empty : options[picked];
                bool remove = GUILayout.Button("-", GUILayout.Width(24f));
                EditorGUILayout.EndHorizontal();

                if(remove)
                {
                    rules.RemoveAt(i);
                    GUI.changed = true;
                    break;
                }
                rules[i] = rule;
            }

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Add Rule", GUILayout.Width(100f)))
                rules.Add(new FolderRule { name = "NewFolder", color = ToolDefines.EnhancerPalette[0], icon = string.Empty });
            if(GUILayout.Button("Reset To Defaults", GUILayout.Width(140f)))
                settings.ResetFolderRules();
            EditorGUILayout.EndHorizontal();
        }

        private static string[] GetIconOptions()
        {
            if(_iconOptions != null)
                return _iconOptions;

            string[] icons = ToolDefines.EnhancerIcons;
            _iconOptions = new string[icons.Length + 1];
            _iconOptions[0] = "(None)";
            icons.CopyTo(_iconOptions, 1);
            return _iconOptions;
        }
    }
}
#endif
