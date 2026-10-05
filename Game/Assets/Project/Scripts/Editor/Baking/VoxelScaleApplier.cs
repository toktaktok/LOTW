// 복셀 크기 규칙 일괄 적용 (에디터 전용).
// CameraDefines.VoxelScale이 바뀌었을 때 프리팹의 importScale을 새 값으로 맞춘다.
using System.Collections.Generic;
using System.Linq;
using Project.Scripts.Data;
using UnityEditor;
using UnityEngine;
using VoxelImporter;

namespace Project.Scripts.Editor.Baking
{
    /// <summary>
    /// Assets/Project/Prefabs 아래 복셀 프리팹의 importScale을 CameraDefines.VoxelScale로 바꾸고 메시를 다시 만든다.
    /// - DSS 머티리얼의 _ImportScale 유니폼도 같이 갱신한다 (점유 볼륨은 복셀 수 기준이라 다시 굽지 않는다).
    /// - 복셀 오브젝트 바로 아래 비복셀 자식(스프라이트, 조명)의 로컬 위치는 새/옛 배율만큼 곱한다.
    /// - 중첩 프리팹 인스턴스 안의 복셀은 건너뛴다. 원본 프리팹에서 처리한다.
    /// </summary>
    public static class VoxelScaleApplier
    {
        private const string PrefabFolder = "Assets/Project/Prefabs";
        private const float Tolerance = 0.0001f;

        [MenuItem("LOTW/Voxel/Apply Voxel Scale To Prefabs")]
        private static void ApplyToAll()
        {
            Apply();
        }

        /// <summary>정렬된 복셀 프리팹 목록에서 start부터 count개를 처리하고 바뀐 프리팹 수를 반환한다.</summary>
        public static int Apply(int start = 0, int count = int.MaxValue)
        {
            if(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError("[VoxelScaleApplier] Close Prefab Mode before applying voxel scale");
                return 0;
            }

            var paths = FindVoxelPrefabPaths();
            int updated = 0;
            foreach(var path in paths.Skip(start).Take(count))
            {
                if(ApplyToPrefab(path))
                    updated++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[VoxelScaleApplier] Updated {updated} voxel prefab(s) (range {start}+{count} of {paths.Count})");
            return updated;
        }

        private static List<string> FindVoxelPrefabPaths()
        {
            var paths = new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(root != null && root.GetComponentInChildren<VoxelObject>(true) != null)
                    paths.Add(path);
            }
            paths.Sort();
            return paths;
        }

        private static bool ApplyToPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach(var voxelObject in root.GetComponentsInChildren<VoxelObject>(true))
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(voxelObject.gameObject))
                        continue;
                    changed |= ApplyToVoxelObject(voxelObject);
                }

                if(changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool ApplyToVoxelObject(VoxelObject voxelObject)
        {
            // 머티리얼은 프리팹이 이미 새 배율이어도 어긋나 있을 수 있어 매번 맞춘다.
            bool changed = ApplyToMaterials(voxelObject);

            float oldScale = voxelObject.importScale.x;
            if(Mathf.Abs(oldScale - CameraDefines.VoxelScale) < Tolerance)
                return changed;

            float ratio = CameraDefines.VoxelScale / oldScale;
            voxelObject.importScale = Vector3.one * CameraDefines.VoxelScale;
            if(!new VoxelObjectCore(voxelObject).ReCreate())
            {
                Debug.LogError($"[VoxelScaleApplier] VoxelImporter failed to recreate mesh: {voxelObject.name}");
                return changed;
            }

            foreach(Transform child in voxelObject.transform)
            {
                if(child.GetComponentInChildren<VoxelObject>(true) == null)
                    child.localPosition *= ratio;
            }
            return true;
        }

        private static bool ApplyToMaterials(VoxelObject voxelObject)
        {
            bool changed = false;
            var value = Vector4.one * CameraDefines.VoxelScale;
            value.w = 0;
            foreach(var material in voxelObject.materials.Where(m => m != null && m.HasProperty("_ImportScale")))
            {
                if(Mathf.Abs(material.GetVector("_ImportScale").x - CameraDefines.VoxelScale) < Tolerance)
                    continue;
                material.SetVector("_ImportScale", value);
                EditorUtility.SetDirty(material);
                changed = true;
            }
            return changed;
        }
    }
}
