// 복셀 크기 규칙 검사 (에디터 전용).
// 규칙: VoxelImporter importScale는 xyz가 같고 (importScale * PixelsPerUnit)가 1 이상의 정수(복셀 1칸 = 정수 RT 픽셀), 복셀 오브젝트와 그 상위의 스케일은 모두 1.
// 크기를 바꾸려면 스케일이 아니라 MagicaVoxel에서 복셀 수로 조정한다. 위반 항목을 콘솔에 나열만 하고 고치지 않는다.
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelImporter;
using Project.Scripts.Data;

namespace Project.Scripts.Editor.Baking
{
    public static class VoxelScaleValidator
    {
        private const string PrefabFolder = "Assets/Project/Prefabs";
        private const float Tolerance = 0.0001f;

        [MenuItem("LOTW/Voxel/Validate Voxel Scale")]
        private static void Validate()
        {
            var report = new StringBuilder();
            int issues = 0;

            foreach(string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(VoxelObject voxel in prefab.GetComponentsInChildren<VoxelObject>(true))
                    issues += Check(voxel, path, report);
            }

            for(int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if(!scene.isLoaded)
                    continue;
                foreach(GameObject root in scene.GetRootGameObjects())
                {
                    foreach(VoxelObject voxel in root.GetComponentsInChildren<VoxelObject>(true))
                        issues += Check(voxel, scene.path, report);
                }
            }

            if(issues == 0)
                Debug.Log("[VoxelScaleValidator] OK: importScale = integer px / 25, scale 1");
            else
                Debug.LogWarning($"[VoxelScaleValidator] {issues} issue(s)\n{report}");
        }

        private static int Check(VoxelObject voxel, string location, StringBuilder report)
        {
            int issues = 0;
            string label = $"{location} > {GetPath(voxel.transform)}";

            if(!IsIntegerPixelScale(voxel.importScale))
            {
                report.AppendLine($"{label}: importScale {voxel.importScale} (expected uniform integer px / {CameraDefines.PixelsPerUnit})");
                issues++;
            }

            // DSS 머티리얼의 _ImportScale 유니폼은 importScale과 같아야 한다.
            if(voxel.materials != null)
            {
                foreach(Material material in voxel.materials)
                {
                    if(material == null || !material.HasProperty("_ImportScale"))
                        continue;
                    float uniform = material.GetVector("_ImportScale").x;
                    if(Mathf.Abs(uniform - voxel.importScale.x) > Tolerance)
                    {
                        report.AppendLine($"{label}: material '{material.name}' _ImportScale {uniform} != importScale {voxel.importScale.x}");
                        issues++;
                    }
                }
            }

            // 복셀 오브젝트부터 모든 상위 그룹까지 스케일 검사
            for(Transform t = voxel.transform; t != null; t = t.parent)
            {
                if((t.localScale - Vector3.one).sqrMagnitude > Tolerance)
                {
                    report.AppendLine($"{label}: scale {t.localScale} on '{t.name}'");
                    issues++;
                }
            }
            return issues;
        }

        private static bool IsIntegerPixelScale(Vector3 importScale)
        {
            if((importScale - Vector3.one * importScale.x).sqrMagnitude > Tolerance * Tolerance)
                return false;
            float pixels = importScale.x * CameraDefines.PixelsPerUnit;
            return pixels >= 1f - Tolerance && Mathf.Abs(pixels - Mathf.Round(pixels)) < Tolerance;
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            for(Transform p = t.parent; p != null; p = p.parent)
                path = $"{p.name}/{path}";
            return path;
        }
    }
}
