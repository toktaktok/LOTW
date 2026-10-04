// Derived Surface Shading - Texture3D 점유 베이커 (에디터 전용).
// 점유 필드 V(x,y,z)를 단일 채널 Texture3D(R8, Linear)로 굽고
// 대상 머티리얼의 _OccupancyTex / _VoxelDims / 매핑 유니폼을 설정한다.
// 소스: VoxelImporter VoxelObject 또는 즉시 데모용 절차적 구(sphere).
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using VoxelImporter;

namespace Project.Scripts.Editor.Baking
{
    /// <summary>
    /// DSS용 점유 Texture3D를 굽는 에디터 윈도우.
    /// VoxelImporter 데이터는 에디트 타임에만 접근 가능하므로(에셋은 #if UNITY_EDITOR)
    /// 결과는 반드시 직렬화된 Texture3D 에셋으로 저장한다.
    /// </summary>
    public sealed class VoxelOccupancyBaker : EditorWindow
    {
        private const string OutputFolder = "Assets/Project/Art/Textures/Voxel";
        // 빈 테두리(복셀 단위). Clamp 샘플링이 경계 밖을 "채워짐"으로 읽어 경계 면 노멀이 틀어지는 것을 막는다.
        private const int Padding = 1;

        private VoxelObject _voxelObject;
        private Material _targetMaterial;
        private int _sphereResolution = 32;

        [MenuItem("LOTW/Bake Voxel Occupancy (DSS)")]
        private static void Open()
        {
            GetWindow<VoxelOccupancyBaker>(true, "Voxel DSS Occupancy Baker");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VoxelImporter Source", EditorStyles.boldLabel);
            _voxelObject = (VoxelObject)EditorGUILayout.ObjectField(
                "Voxel Object", _voxelObject, typeof(VoxelObject), true);
            _targetMaterial = (Material)EditorGUILayout.ObjectField(
                "Target Material", _targetMaterial, typeof(Material), false);

            using(new EditorGUI.DisabledScope(_voxelObject == null))
            {
                if(GUILayout.Button("Bake From Voxel Object"))
                {
                    BakeFromVoxelObject(_voxelObject, _targetMaterial);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Procedural Test Sphere", EditorStyles.boldLabel);
            _sphereResolution = EditorGUILayout.IntSlider("Resolution", _sphereResolution, 8, 128);
            if(GUILayout.Button("Bake Test Sphere"))
            {
                BakeTestSphere(_sphereResolution, _targetMaterial);
            }
        }

        /// <summary>VoxelImporter VoxelObject에서 점유 그리드를 읽어 굽고, 지정한 머티리얼들에 매핑을 적용한다.</summary>
        public static void BakeFromVoxelObject(VoxelObject voxelObject, params Material[] materials)
        {
            if(voxelObject == null)
            {
                Debug.LogError("[VoxelDSS] VoxelObject가 지정되지 않았습니다.");
                return;
            }

            // 에디트 타임 코어 구성 후 voxelData 로드.
            var core = new VoxelObjectCore(voxelObject);
            if(!core.ReadyVoxelData())
            {
                Debug.LogError("[VoxelDSS] ReadyVoxelData 실패. 소스 .vox/.qb/.png 파일이 존재하는지 확인하세요.");
                return;
            }

            var data = core.voxelData;
            if(data == null || data.voxels == null)
            {
                Debug.LogError("[VoxelDSS] voxelData가 비어 있습니다.");
                return;
            }

            IntVector3 dims = data.voxelSize;
            var grid = new VoxelOccupancyGrid(dims.x + 2 * Padding, dims.y + 2 * Padding, dims.z + 2 * Padding);
            for(int i = 0; i < data.voxels.Length; ++i)
            {
                var v = data.voxels[i];
                grid.SetFilled(v.x + Padding, v.y + Padding, v.z + Padding); // 점유 판정 = 존재 여부 (visible 플래그 아님)
            }

            var tex = CreateVolume(grid, voxelObject.name);
            string assetPath = SaveVolume(tex, voxelObject.name);

            var volume = AssetDatabase.LoadAssetAtPath<Texture3D>(assetPath);
            foreach(var material in materials)
            {
                if(material == null)
                    continue;
                // 패딩만큼 그리드가 +Padding 이동했으므로 importOffset에서 빼서 셰이더 매핑을 맞춘다.
                ApplyToMaterial(material, volume,
                    new Vector3(grid.Width, grid.Height, grid.Depth),
                    voxelObject.importScale, voxelObject.importOffset - Vector3.one * Padding, voxelObject.localOffset);
            }

            Debug.Log($"[VoxelDSS] Baked occupancy {dims.x}x{dims.y}x{dims.z} -> {assetPath}");
        }

        /// <summary>즉시 데모용 절차적 구를 굽는다. 항등(identity) 매핑을 사용한다.</summary>
        public static void BakeTestSphere(int resolution, Material material)
        {
            int n = Mathf.Max(8, resolution);
            int size = n + 2 * Padding;
            var grid = new VoxelOccupancyGrid(size, size, size);
            float c = (n - 1) * 0.5f;
            float radius = c; // inscribed: shell meets the cube face centers so faces show rounding
            float r2 = radius * radius;
            for(int z = 0; z < n; ++z)
            for(int y = 0; y < n; ++y)
            for(int x = 0; x < n; ++x)
            {
                float dx = x - c, dy = y - c, dz = z - c;
                if(dx * dx + dy * dy + dz * dz <= r2)
                    grid.SetFilled(x + Padding, y + Padding, z + Padding);
            }

            var tex = CreateVolume(grid, "Sphere");
            string assetPath = SaveVolume(tex, "Sphere");

            if(material != null)
            {
                // 데모 큐브: 오브젝트 공간 [-0.5,0.5] 정육면체를 그리드에 매핑.
                // grid = (objectPos / importScale) - (localOffset + importOffset).
                // [-0.5,0.5] -> grid[Padding,n+Padding] 정렬: importScale = 1/n, localOffset = 0, importOffset = -0.5n - Padding.
                float s = 1.0f / n;
                var importScale = new Vector3(s, s, s);
                var importOffset = Vector3.one * (-0.5f * n - Padding);
                var localOffset = Vector3.zero;
                ApplyToMaterial(material, AssetDatabase.LoadAssetAtPath<Texture3D>(assetPath),
                    new Vector3(size, size, size), importScale, importOffset, localOffset);
            }

            Debug.Log($"[VoxelDSS] Baked test sphere {n}^3 -> {assetPath}");
        }

        // 셰이더는 항상 LOD 0만 샘플링하므로 밉 체인은 만들지 않는다.
        private static Texture3D CreateVolume(VoxelOccupancyGrid grid, string label)
        {
            var tex = new Texture3D(grid.Width, grid.Height, grid.Depth,
                GraphicsFormat.R8_UNorm, TextureCreationFlags.None)
            {
                name = $"TEX3D_{label}_Occupancy",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };
            tex.SetPixelData(grid.ToR8Bytes(), 0);
            tex.Apply(false, false); // 데이터 텍스처: 점유는 색이 아니므로 sRGB 변환 없음(R8_UNorm은 선형)
            return tex;
        }

        private static string SaveVolume(Texture3D tex, string label)
        {
            if(!AssetDatabase.IsValidFolder(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
                AssetDatabase.Refresh();
            }
            string path = $"{OutputFolder}/TEX3D_{label}_Occupancy.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
            if(existing != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(tex, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return path;
        }

        private static void ApplyToMaterial(Material material, Texture3D tex,
            Vector3 dims, Vector3 importScale, Vector3 importOffset, Vector3 localOffset)
        {
            Undo.RecordObject(material, "Apply Voxel DSS Occupancy");
            material.SetTexture("_OccupancyTex", tex);
            material.SetVector("_VoxelDims", new Vector4(dims.x, dims.y, dims.z, 0));
            material.SetVector("_ImportScale", new Vector4(importScale.x, importScale.y, importScale.z, 0));
            material.SetVector("_ImportOffset", new Vector4(importOffset.x, importOffset.y, importOffset.z, 0));
            material.SetVector("_LocalOffset", new Vector4(localOffset.x, localOffset.y, localOffset.z, 0));
            EditorUtility.SetDirty(material);
        }
    }
}
