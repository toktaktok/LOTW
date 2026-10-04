#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// Hierarchy/Project 우클릭 메뉴 항목.
    /// </summary>
    public static class EnhancerMenu
    {
        private const string HierarchyStyleMenu = "GameObject/LOTW/Hierarchy Style...";
        private const string CreateHeaderMenu = "GameObject/LOTW/Create Header";
        private const string FolderStyleMenu = "Assets/LOTW/Folder Style...";

        [MenuItem(HierarchyStyleMenu, false, 0)]
        private static void OpenHierarchyStyle(MenuCommand command)
        {
            // 다중 선택 시 GameObject 메뉴는 오브젝트마다 호출되므로 첫 호출만 처리
            if(command.context != null && Selection.objects.Length > 1 && command.context != Selection.objects[0])
                return;
            HierarchyDrawer.OpenPicker(Selection.gameObjects);
        }

        [MenuItem(HierarchyStyleMenu, true)]
        private static bool ValidateHierarchyStyle()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem(CreateHeaderMenu, false, 1)]
        private static void CreateHeader(MenuCommand command)
        {
            // EditorOnly 태그라 빌드에서 제외됨
            var header = new GameObject($"{ToolDefines.HierarchyHeaderPrefix} Header {ToolDefines.HierarchyHeaderPrefix}")
            {
                tag = ToolDefines.HierarchyHeaderTag,
            };
            if(command.context is GameObject parent)
                GameObjectUtility.SetParentAndAlign(header, parent);
            Undo.RegisterCreatedObjectUndo(header, "Create Header");
            Selection.activeGameObject = header;
        }

        [MenuItem(FolderStyleMenu, false, 2000)]
        private static void OpenFolderStyle()
        {
            ProjectFolderDrawer.OpenPicker(ProjectFolderDrawer.FilterFolders(Selection.assetGUIDs));
        }

        [MenuItem(FolderStyleMenu, true)]
        private static bool ValidateFolderStyle()
        {
            return ProjectFolderDrawer.FilterFolders(Selection.assetGUIDs).Length > 0;
        }
    }
}
#endif
