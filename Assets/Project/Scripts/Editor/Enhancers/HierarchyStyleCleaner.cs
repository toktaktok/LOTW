#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// 이미 삭제된 오브젝트의 Hierarchy 스타일을 설정에서 지웁니다.
    /// 열려 있지 않은 씬의 오브젝트는 확인할 수 없으므로 남겨 둡니다(씬 자체가 삭제된 경우는 제거).
    /// </summary>
    public static class HierarchyStyleCleaner
    {
        private const int SceneObjectType = 2;

        public static int RemoveOrphans()
        {
            EnhancerSettings settings = EnhancerSettings.instance;
            int removed = settings.RemoveHierarchyStyles(style => IsOrphan(style.key));
            if(removed > 0)
                settings.Commit();
            return removed;
        }

        private static bool IsOrphan(string key)
        {
            if(!GlobalObjectId.TryParse(key, out GlobalObjectId id))
                return true;
            if(id.assetGUID.Empty())
                return false;

            string path = AssetDatabase.GUIDToAssetPath(id.assetGUID);
            if(string.IsNullOrEmpty(path))
                return true;

            if(id.identifierType == SceneObjectType && !SceneManager.GetSceneByPath(path).isLoaded)
                return false;

            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) == null;
        }
    }
}
#endif
