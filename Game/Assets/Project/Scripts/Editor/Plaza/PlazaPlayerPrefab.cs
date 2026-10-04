// Character.unity의 SnowMan 계층을 PF_SnowMan 프리팹으로 복사 (에디터 전용).
// Character.unity는 추가(Additive)로 열고 저장하지 않은 채 닫는다. 활성 씬은 건드리지 않는다.
using Project.Scripts.Editor.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Scripts.Editor.Plaza
{
    public static class PlazaPlayerPrefab
    {
        private const string SnowManName = "SnowMan";

        [MenuItem("LOTW/Plaza/4 Create Player Prefab")]
        private static void CreateMenu()
        {
            Create();
        }

        /// <summary>PF_SnowMan 프리팹이 없으면 만든다. 프리팹 경로를 반환하고 실패하면 null.</summary>
        public static string Create()
        {
            string prefabPath = PlazaLayout.PlayerPrefabPath;
            if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return prefabPath;

            Scene scene = SceneManager.GetSceneByPath(ToolDefines.CharacterScenePath);
            bool wasLoaded = scene.isLoaded;
            if(!wasLoaded)
                scene = EditorSceneManager.OpenScene(ToolDefines.CharacterScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject snowMan = FindSnowMan(scene);
                if(snowMan == null)
                {
                    Debug.LogError($"[PlazaPlayerPrefab] '{SnowManName}' not found in {ToolDefines.CharacterScenePath}");
                    return null;
                }

                PrefabUtility.SaveAsPrefabAsset(snowMan, prefabPath, out bool success);
                if(!success)
                {
                    Debug.LogError($"[PlazaPlayerPrefab] Failed to save {prefabPath}");
                    return null;
                }
                return prefabPath;
            }
            finally
            {
                if(!wasLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject FindSnowMan(Scene scene)
        {
            foreach(GameObject root in scene.GetRootGameObjects())
            {
                if(root.name == SnowManName)
                    return root;
            }
            return null;
        }
    }
}
