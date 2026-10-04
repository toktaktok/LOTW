#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// Hierarchy/Project 꾸미기 설정. UserSettings 폴더에 저장되는 로컬(개인) 설정입니다.
    /// </summary>
    [FilePath(ToolDefines.EnhancerSettingsPath, FilePathAttribute.Location.ProjectFolder)]
    public class EnhancerSettings : ScriptableSingleton<EnhancerSettings>
    {
        [Header("Hierarchy")]
        [SerializeField] private bool hierarchyEnabled = true;
        [SerializeField] private bool showHeaders = true;
        [SerializeField] private bool showTreeLines = true;
        [SerializeField] private bool showActiveToggle = true;
        [SerializeField] private bool showMissingScripts = true;
        [SerializeField] private bool useHierarchyRules = true;
        [SerializeField] private List<HierarchyStyle> hierarchyStyles = new List<HierarchyStyle>();
        [SerializeField] private List<FolderRule> hierarchyRules = new List<FolderRule>();

        [Header("Project")]
        [SerializeField] private bool folderEnabled = true;
        [SerializeField] private bool useFolderRules = true;
        [SerializeField] private List<FolderStyle> folderStyles = new List<FolderStyle>();
        [SerializeField] private List<FolderRule> folderRules = CreateDefaultRules();

        private Dictionary<string, HierarchyStyle> _hierarchyLookup;
        private Dictionary<string, FolderStyle> _folderLookup;

        public static event Action OnChanged;

        public bool HierarchyEnabled { get => hierarchyEnabled; set => hierarchyEnabled = value; }
        public bool ShowHeaders { get => showHeaders; set => showHeaders = value; }
        public bool ShowTreeLines { get => showTreeLines; set => showTreeLines = value; }
        public bool ShowActiveToggle { get => showActiveToggle; set => showActiveToggle = value; }
        public bool ShowMissingScripts { get => showMissingScripts; set => showMissingScripts = value; }
        public bool UseHierarchyRules { get => useHierarchyRules; set => useHierarchyRules = value; }
        public bool FolderEnabled { get => folderEnabled; set => folderEnabled = value; }
        public bool UseFolderRules { get => useFolderRules; set => useFolderRules = value; }
        public bool HasHierarchyStyles => hierarchyStyles.Count > 0 || (useHierarchyRules && hierarchyRules.Count > 0);
        public int HierarchyStyleCount => hierarchyStyles.Count;
        public List<FolderRule> FolderRules => folderRules;
        public List<FolderRule> HierarchyRules => hierarchyRules;

        public bool TryGetHierarchyStyle(string key, out HierarchyStyle style)
        {
            if(_hierarchyLookup == null)
                RebuildLookups();
            return _hierarchyLookup.TryGetValue(key, out style);
        }

        public bool TryGetFolderStyle(string guid, out FolderStyle style)
        {
            if(_folderLookup == null)
                RebuildLookups();
            return _folderLookup.TryGetValue(guid, out style);
        }

        public void SetHierarchyStyle(HierarchyStyle style)
        {
            hierarchyStyles.RemoveAll(s => s.key == style.key);
            if(style.color.a > 0f || !string.IsNullOrEmpty(style.icon))
                hierarchyStyles.Add(style);
            _hierarchyLookup = null;
        }

        public void SetFolderStyle(FolderStyle style)
        {
            folderStyles.RemoveAll(s => s.guid == style.guid);
            if(style.color.a > 0f || !string.IsNullOrEmpty(style.icon))
                folderStyles.Add(style);
            _folderLookup = null;
        }

        public int RemoveHierarchyStyles(Predicate<HierarchyStyle> match)
        {
            int removed = hierarchyStyles.RemoveAll(match);
            if(removed > 0)
                _hierarchyLookup = null;
            return removed;
        }

        public void ClearHierarchyStyles()
        {
            hierarchyStyles.Clear();
            _hierarchyLookup = null;
        }

        public void ClearFolderStyles()
        {
            folderStyles.Clear();
            _folderLookup = null;
        }

        public void ResetFolderRules()
        {
            folderRules = CreateDefaultRules();
        }

        /// <summary>
        /// 파일에 저장하고 창을 다시 그립니다.
        /// </summary>
        public void Commit()
        {
            Save(true);
            OnChanged?.Invoke();
        }

        private void RebuildLookups()
        {
            _hierarchyLookup = new Dictionary<string, HierarchyStyle>();
            foreach(HierarchyStyle style in hierarchyStyles)
                _hierarchyLookup[style.key] = style;

            _folderLookup = new Dictionary<string, FolderStyle>();
            foreach(FolderStyle style in folderStyles)
                _folderLookup[style.guid] = style;
        }

        private static List<FolderRule> CreateDefaultRules()
        {
            Color[] palette = ToolDefines.EnhancerPalette;
            return new List<FolderRule>
            {
                new FolderRule { name = "Scripts", color = palette[5], icon = "cs Script Icon" },
                new FolderRule { name = "Prefabs", color = palette[6], icon = "Prefab Icon" },
                new FolderRule { name = "Scenes", color = palette[3], icon = "SceneAsset Icon" },
                new FolderRule { name = "Materials", color = palette[1], icon = "Material Icon" },
                new FolderRule { name = "Textures", color = palette[2], icon = "Texture Icon" },
                new FolderRule { name = "Audio", color = palette[4], icon = "AudioClip Icon" },
                new FolderRule { name = "Resources", color = palette[0], icon = "" },
                new FolderRule { name = "Editor", color = palette[8], icon = "" },
            };
        }
    }
}
#endif
