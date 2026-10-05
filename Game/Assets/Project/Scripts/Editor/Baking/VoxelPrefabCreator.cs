// .vox 파일로 VoxelImporter 프리팹을 만든다 (에디터 전용).
// PF_SnowOffice와 같은 VoxelObject 설정을 쓰고, 생성된 메시/머티리얼/아틀라스 텍스처는 프리팹 안에 넣는다.
using System.Collections.Generic;
using System.IO;
using Project.Scripts.Editor.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoxelImporter;

namespace Project.Scripts.Editor.Baking
{
    public static class VoxelPrefabCreator
    {
        private const string ModelPrefix = "M_E_";

        [MenuItem("Assets/LOTW/Create Voxel Prefab")]
        private static void CreateFromSelection()
        {
            var created = new List<string>();
            foreach(var voxPath in GetSelectedVoxPaths())
            {
                string prefabPath = PrefabPathFor(voxPath);
                if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                    continue;
                if(CreatePrefab(voxPath, prefabPath) != null)
                    created.Add(prefabPath);
            }
            if(created.Count > 0)
                VoxelDssApplier.ApplyToPrefabs(created);
        }

        [MenuItem("Assets/LOTW/Create Voxel Prefab", true)]
        private static bool ValidateCreateFromSelection()
        {
            return GetSelectedVoxPaths().Count > 0;
        }

        /// <summary>
        /// .vox로 프리팹을 만들고 경로를 반환한다. 이미 있으면 만들지 않고 그 경로를 반환한다. 실패하면 null.
        /// VoxelImporter가 프리팹 생성 시 하는 것(ReCreate 후 생성물을 프리팹 에셋에 추가)을 인스턴스 없이 그대로 한다.
        /// </summary>
        public static string CreatePrefab(string voxAssetPath, string prefabAssetPath)
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath) != null)
                return prefabAssetPath;

            // 프리팹 편집 모드면 VoxelImporter가 생성물을 열린 프리팹에 넣어 버린다
            if(PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError("[VoxelPrefabCreator] Close Prefab Mode before creating voxel prefabs");
                return null;
            }

            var voxelFile = AssetDatabase.LoadMainAssetAtPath(voxAssetPath);
            if(voxelFile == null)
            {
                Debug.LogError($"[VoxelPrefabCreator] Voxel file not found: {voxAssetPath}");
                return null;
            }

            var go = new GameObject(Path.GetFileNameWithoutExtension(prefabAssetPath));
            try
            {
                var voxelObject = go.AddComponent<VoxelObject>();
                var core = new VoxelObjectCore(voxelObject);
                // 인스펙터가 새 컴포넌트에 하는 초기화. 파일 지정 전에 해야 dataVersion이 최신으로 찍히고 legacy 모드가 켜지지 않는다
                core.Initialize();
                Configure(voxelObject, voxelFile, voxAssetPath);

                if(!core.ReCreate())
                {
                    Debug.LogError($"[VoxelPrefabCreator] VoxelImporter failed to create mesh: {voxAssetPath}");
                    return null;
                }

                // 생성물은 아직 메모리에만 있으므로, 프리팹 파일을 만든 뒤 하위 에셋으로 넣고 다시 저장한다
                PrefabUtility.SaveAsPrefabAsset(go, prefabAssetPath, out bool success);
                if(!success)
                {
                    Debug.LogError($"[VoxelPrefabCreator] Failed to save prefab: {prefabAssetPath}");
                    return null;
                }
                AddSubAsset(voxelObject.atlasTexture, prefabAssetPath);
                if(voxelObject.materials != null)
                {
                    foreach(var material in voxelObject.materials)
                        AddSubAsset(material, prefabAssetPath);
                }
                AddSubAsset(voxelObject.mesh, prefabAssetPath);
                PrefabUtility.SaveAsPrefabAsset(go, prefabAssetPath);
                return prefabAssetPath;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // PF_SnowOffice.prefab의 VoxelObject 값과 같게 맞춘다 (기본값과 다른 항목만)
        private static void Configure(VoxelObject voxelObject, Object voxelFile, string voxAssetPath)
        {
            voxelObject.advancedMode = true;
            voxelObject.voxelFileObject = voxelFile;
            voxelObject.voxelFilePath = EditorCommon.GetProjectRelativePath2FullPath(voxAssetPath);
            voxelObject.importMode = VoxelBase.ImportMode.LowTexture;
            voxelObject.importFlags = 0;
            voxelObject.importScale = Vector3.one * ToolDefines.VoxelImportScale;
            voxelObject.ignoreCavity = true;
            voxelObject.loadFromVoxelFile = false;
        }

        private static void AddSubAsset(Object obj, string prefabAssetPath)
        {
            if(obj == null || AssetDatabase.Contains(obj))
                return;
            AssetDatabase.AddObjectToAsset(obj, prefabAssetPath);
        }

        /// <summary>.vox 경로에 대응하는 프리팹 경로 (M_E_Bench_1.vox -> PF_Bench_1.prefab).</summary>
        private static string PrefabPathFor(string voxPath)
        {
            string name = Path.GetFileNameWithoutExtension(voxPath);
            if(name.StartsWith(ModelPrefix))
                name = name.Substring(ModelPrefix.Length);
            return ToolDefines.VoxelPrefabFolder + "/PF_" + name + ".prefab";
        }

        private static List<string> GetSelectedVoxPaths()
        {
            var paths = new List<string>();
            foreach(var guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if(path.EndsWith(".vox"))
                    paths.Add(path);
            }
            return paths;
        }
    }
}
