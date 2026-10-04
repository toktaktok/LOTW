// Voxel DSS 일괄 적용 (에디터 전용).
// VoxelImporter 프리팹의 머티리얼을 .mat 에셋으로 내보내고 LOTW/VoxelDSS로 바꾼 뒤 점유 볼륨을 굽는다.
using System.Collections.Generic;
using System.IO;
using Project.Scripts.Editor.Data;
using UnityEditor;
using UnityEngine;
using VoxelImporter;

namespace Project.Scripts.Editor.Baking
{
    /// <summary>
    /// 복셀 프리팹에 Voxel DSS 셰이더를 적용한다.
    /// - 프리팹에 내장된 머티리얼은 프리팹 옆에 {이름}_mat{번호}.mat 로 내보낸다 (VoxelImporter Save와 같은 방식).
    /// - 새로 변환한 머티리얼만 toon + 복셀 단위 조명 기본값을 넣는다. 이미 DSS인 머티리얼의 값은 그대로 둔다.
    /// - 점유 볼륨이 없는 머티리얼만 굽는다. 다시 구우려면 LOTW/Bake Voxel Occupancy (DSS)를 쓴다.
    /// - 투명 머티리얼은 DSS(불투명 전용) 대상이 아니므로 건너뛴다.
    /// </summary>
    public static class VoxelDssApplier
    {
        [MenuItem("LOTW/Voxel DSS/Apply To Selected Prefabs")]
        private static void ApplyToSelected()
        {
            ApplyToPrefabs(GetSelectedPrefabPaths());
        }

        [MenuItem("LOTW/Voxel DSS/Apply To Selected Prefabs", true)]
        private static bool ValidateApplyToSelected()
        {
            return GetSelectedPrefabPaths().Count > 0;
        }

        [MenuItem("LOTW/Voxel DSS/Apply To All Voxel Prefabs")]
        private static void ApplyToAll()
        {
            var paths = new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ToolDefines.VoxelPrefabSearchFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(root != null && root.GetComponentInChildren<VoxelObject>(true) != null)
                    paths.Add(path);
            }
            ApplyToPrefabs(paths);
        }

        /// <summary>프리팹 경로 목록에 DSS를 적용하고 바뀐 프리팹 수를 반환한다.</summary>
        public static int ApplyToPrefabs(IReadOnlyList<string> prefabPaths)
        {
            var shader = Shader.Find(ToolDefines.VoxelDssShaderName);
            if(shader == null)
            {
                Debug.LogError($"[VoxelDssApplier] Shader '{ToolDefines.VoxelDssShaderName}' not found");
                return 0;
            }

            int updated = 0;
            foreach(var path in prefabPaths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = false;
                    string folder = Path.GetDirectoryName(path).Replace('\\', '/');
                    foreach(var voxelObject in root.GetComponentsInChildren<VoxelObject>(true))
                        changed |= ApplyToVoxelObject(voxelObject, shader, folder);

                    if(changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        updated++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                RemoveOrphanMaterials(path);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[VoxelDssApplier] Updated {updated}/{prefabPaths.Count} voxel prefab(s)");
            return updated;
        }

        private static bool ApplyToVoxelObject(VoxelObject voxelObject, Shader shader, string folder)
        {
            if(voxelObject.materials == null)
                return false;

            bool materialsChanged = false;
            var needsBake = new List<Material>();
            for(int i = 0; i < voxelObject.materials.Count; i++)
            {
                var material = voxelObject.materials[i];
                if(material == null || IsTransparent(voxelObject, i))
                    continue;

                if(!AssetDatabase.IsMainAsset(material))
                {
                    string materialPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{voxelObject.gameObject.name}_mat{i}.mat");
                    AssetDatabase.CreateAsset(Object.Instantiate(material), materialPath);
                    material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    voxelObject.materials[i] = material;
                    materialsChanged = true;
                }

                if(material.shader != shader)
                {
                    material.shader = shader;
                    SetToonDefaults(material);
                    EditorUtility.SetDirty(material);
                    materialsChanged = true;
                }

                if(material.GetTexture("_OccupancyTex") == null)
                    needsBake.Add(material);
            }

            if(needsBake.Count > 0)
                VoxelOccupancyBaker.BakeFromVoxelObject(voxelObject, needsBake.ToArray());

            // VoxelImporter가 렌더러 머티리얼을 새 에셋으로 갱신하도록 다시 생성
            if(materialsChanged)
                new VoxelObjectCore(voxelObject).ReCreate();

            return materialsChanged || needsBake.Count > 0;
        }

        /// <summary>내보낸 뒤 프리팹 안에 남은, 어디서도 참조하지 않는 내장 머티리얼을 지운다.</summary>
        private static void RemoveOrphanMaterials(string prefabPath)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var used = new HashSet<Object>(EditorUtility.CollectDependencies(new Object[] { root }));
            bool removed = false;
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(prefabPath))
            {
                if(asset is Material && !used.Contains(asset))
                {
                    AssetDatabase.RemoveObjectFromAsset(asset);
                    Object.DestroyImmediate(asset, true);
                    removed = true;
                }
            }
            if(removed)
                AssetDatabase.SaveAssets();
        }

        private static bool IsTransparent(VoxelObject voxelObject, int index)
        {
            return voxelObject.materialData != null
                && index < voxelObject.materialData.Count
                && voxelObject.materialData[index].transparent;
        }

        private static void SetToonDefaults(Material material)
        {
            material.SetFloat("_UseToon", 1f);
            material.EnableKeyword("_DSS_TOON");
            material.SetFloat("_UseVoxelLighting", 1f);
            material.EnableKeyword("_DSS_VOXEL_LIGHTING");
            material.SetFloat("_ToonSteps", ToolDefines.VoxelDssDefaultToonSteps);
            material.SetFloat("_ToonSoftness", 0f);
            material.SetFloat("_ToonSpecular", 0f);
        }

        private static List<string> GetSelectedPrefabPaths()
        {
            var paths = new List<string>();
            foreach(var guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if(path.EndsWith(".prefab"))
                    paths.Add(path);
            }
            return paths;
        }
    }
}
