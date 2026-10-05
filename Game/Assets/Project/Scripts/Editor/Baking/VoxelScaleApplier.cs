// 건물별 복셀 밀도 적용 (에디터 전용).
// 선택한 복셀 프리팹의 importScale을 "복셀 1칸 = N픽셀" 값으로 바꾼다.
using System.Linq;
using Project.Scripts.Data;
using UnityEditor;
using UnityEngine;
using VoxelImporter;

namespace Project.Scripts.Editor.Baking
{
    /// <summary>
    /// 프로젝트 창에서 선택한 프리팹의 importScale을 pixels / PixelsPerUnit으로 바꾸고 메시를 다시 만든다.
    /// 문 높이가 캐릭터 키의 약 1.6배가 되도록 건물마다 값을 따로 정한다.
    /// - DSS 머티리얼의 _ImportScale 유니폼도 같이 갱신한다 (점유 볼륨은 복셀 수 기준이라 다시 굽지 않는다).
    /// - 복셀 오브젝트 바로 아래 비복셀 자식(스프라이트, 조명)의 로컬 위치는 새/옛 배율만큼 곱한다.
    /// - 중첩 프리팹 인스턴스 안의 복셀은 건너뛴다. 원본 프리팹에서 처리한다.
    /// </summary>
    public class VoxelScaleApplier : ScriptableWizard
    {
        private const float Tolerance = 0.0001f;

        [Tooltip("복셀 한 칸이 차지하는 저해상도 RT 픽셀 수 (정수)")]
        public int pixels = CameraDefines.VoxelPixels;

        [MenuItem("LOTW/Voxel/Set Voxel Pixels...")]
        private static void Open()
        {
            DisplayWizard<VoxelScaleApplier>("Set Voxel Pixels", "Apply");
        }

        private void OnWizardUpdate()
        {
            helpString = "프로젝트 창에서 선택한 복셀 프리팹에 적용한다. 문 높이 = 캐릭터 키 x 1.6 (192px / 문 복셀 수).";
            isValid = pixels >= 1;
        }

        private void OnWizardCreate()
        {
            int changed = 0;
            int total = 0;
            foreach(var obj in Selection.GetFiltered<GameObject>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if(!path.EndsWith(".prefab"))
                    continue;
                total++;
                changed += ApplyPixels(path, pixels);
            }
            Debug.Log($"[VoxelScaleApplier] {pixels}px: {changed} voxel object(s) changed in {total} selected prefab(s)");
        }

        /// <summary>프리팹의 복셀 오브젝트를 pixels 밀도로 바꾸고 바뀐 복셀 오브젝트 수를 반환한다.</summary>
        public static int ApplyPixels(string prefabPath, int pixels)
        {
            if(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError("[VoxelScaleApplier] Close Prefab Mode before applying voxel scale");
                return 0;
            }
            if(pixels < 1)
            {
                Debug.LogError($"[VoxelScaleApplier] pixels must be 1 or more: {pixels}");
                return 0;
            }

            float scale = pixels / CameraDefines.PixelsPerUnit;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            int changed = 0;
            try
            {
                foreach(var voxelObject in root.GetComponentsInChildren<VoxelObject>(true))
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(voxelObject.gameObject))
                        continue;
                    if(ApplyToVoxelObject(voxelObject, scale))
                        changed++;
                }

                if(changed > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            return changed;
        }

        private static bool ApplyToVoxelObject(VoxelObject voxelObject, float scale)
        {
            Vector3 oldImportScale = voxelObject.importScale;
            Vector3 newImportScale = Vector3.one * scale;
            // 머티리얼은 프리팹이 이미 새 배율이어도 어긋나 있을 수 있어 매번 맞춘다.
            if((oldImportScale - newImportScale).sqrMagnitude < Tolerance * Tolerance)
                return ApplyToMaterials(voxelObject, scale);

            float ratio = scale / oldImportScale.x;
            voxelObject.importScale = newImportScale;
            if(!new VoxelObjectCore(voxelObject).ReCreate())
            {
                voxelObject.importScale = oldImportScale;
                Debug.LogError($"[VoxelScaleApplier] VoxelImporter failed to recreate mesh: {voxelObject.name}");
                return false;
            }

            // 메시 재생성이 성공한 뒤에만 머티리얼 유니폼을 쓴다.
            ApplyToMaterials(voxelObject, scale);

            foreach(Transform child in voxelObject.transform)
            {
                if(child.GetComponentInChildren<VoxelObject>(true) == null)
                    child.localPosition *= ratio;
            }
            return true;
        }

        private static bool ApplyToMaterials(VoxelObject voxelObject, float scale)
        {
            if(voxelObject.materials == null)
                return false;

            bool changed = false;
            var value = Vector4.one * scale;
            value.w = 0;
            foreach(var material in voxelObject.materials.Where(m => m != null && m.HasProperty("_ImportScale")))
            {
                if(Mathf.Abs(material.GetVector("_ImportScale").x - scale) < Tolerance)
                    continue;
                material.SetVector("_ImportScale", value);
                EditorUtility.SetDirty(material);
                changed = true;
            }
            return changed;
        }
    }
}
