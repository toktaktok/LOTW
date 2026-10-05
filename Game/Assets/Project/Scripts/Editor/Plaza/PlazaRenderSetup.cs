// Plaza 렌더 설정 (에디터 전용). 머티리얼, PC_Renderer 풀스크린 패스, 볼륨 프로파일, 타일셋 임포트를 만든다.
// 여러 번 실행해도 이미 있는 에셋은 건드리지 않는다.
using System;
using Project.Scripts.Editor.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ParticleGUI = UnityEditor.Rendering.Universal.ShaderGUI.ParticleGUI;

namespace Project.Scripts.Editor.Plaza
{
    public static class PlazaRenderSetup
    {
        private const string RendererPath = "Assets/Project/Settings/PC_Renderer.asset";
        private const string WallTexturePath = "Assets/Project/Art/Environments/Tilesets/TX_E_CentralGround.png";
        private const string GroundSourceMaterialPath = "Assets/Project/Art/Materials/MAT_Ground.mat";
        private const string OutlineFeatureName = "PixelOutline";
        private const string FogFeatureName = "AtmosphereFog";
        private const string TiltShiftFeatureName = "TiltShift";

        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";
        private const string ParticlesUnlitShader = "Universal Render Pipeline/Particles/Unlit";

        // URP 머티리얼 Surface Type (0 = Opaque, 1 = Transparent), Blend (0 = Alpha)
        private const float SurfaceTransparent = 1f;
        private const float BlendAlpha = 0f;

        private const float WallPixelsPerUnit = 25f;

        private const float SnowAlpha = 0.8f;
        private static readonly Color LampGlowColor = new Color(4f, 3f, 1.6f, 1f);

        // D5 볼륨 값
        private const float BloomThreshold = 1f;
        private const float BloomIntensity = 0.5f;
        private const float BloomScatter = 0.65f;
        private const float VignetteIntensity = 0.3f;
        private const float VignetteSmoothness = 0.4f;
        private const float ColorContrast = 10f;
        private const float ColorSaturation = 10f;
        private const float WhiteBalanceTemperature = -5f;

        [MenuItem("LOTW/Plaza/2 Setup Render")]
        public static void Run()
        {
            SetupWallTextureImport();
            CreateMaterials();
            SetupRendererFeatures();
            CreateVolumeProfile();
            AssetDatabase.SaveAssets();
        }

        private static void SetupWallTextureImport()
        {
            var importer = AssetImporter.GetAtPath(WallTexturePath) as TextureImporter;
            if(importer == null)
            {
                Debug.LogError($"[PlazaRenderSetup] Texture not found: {WallTexturePath}");
                return;
            }

            if(importer.filterMode == FilterMode.Point && importer.wrapMode == TextureWrapMode.Repeat
               && Mathf.Approximately(importer.spritePixelsPerUnit, WallPixelsPerUnit)
               && importer.textureCompression == TextureImporterCompression.Uncompressed)
                return;

            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.spritePixelsPerUnit = WallPixelsPerUnit;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void CreateMaterials()
        {
            CreateMaterial("MAT_Sky_Plaza", "Skybox/LOTW Gradient", null);
            CreateMaterial("MAT_TiltShift", "Hidden/LOTW/TiltShift", null);
            // 안개 값(보정 7)은 셰이더 기본값이 기준이다
            CreateMaterial("MAT_AtmosphereFog", "Hidden/LOTW/AtmosphereFog", null);
            CreateMaterial("MAT_Snow", ParticlesUnlitShader, m =>
            {
                m.SetColor("_BaseColor", new Color(1f, 1f, 1f, SnowAlpha));
                SetTransparent(m);
                BaseShaderGUI.SetMaterialKeywords(m, null, ParticleGUI.SetMaterialKeywords);
            });
            CreateMaterial("MAT_LampGlow", UnlitShader, m => m.SetColor("_BaseColor", LampGlowColor));
            CreateMaterial("MAT_PlazaWall", LitShader, m =>
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WallTexturePath);
                if(texture == null)
                    Debug.LogError($"[PlazaRenderSetup] Texture not found: {WallTexturePath}");
                m.SetTexture("_BaseMap", texture);
            });
            CreateMaterial("MAT_PlazaGround", LitShader, m =>
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(GroundSourceMaterialPath);
                if(source == null)
                    Debug.LogWarning($"[PlazaRenderSetup] {GroundSourceMaterialPath} not found, ground stays white");
                else
                    m.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            });
        }

        private static void CreateMaterial(string name, string shaderName, Action<Material> setup)
        {
            string path = $"{ToolDefines.PlazaMaterialFolder}/{name}.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                return;

            Shader shader = Shader.Find(shaderName);
            if(shader == null)
            {
                Debug.LogError($"[PlazaRenderSetup] Shader not found: {shaderName} ({name} skipped)");
                return;
            }

            var material = new Material(shader);
            setup?.Invoke(material);
            AssetDatabase.CreateAsset(material, path);
        }

        private static void SetTransparent(Material material)
        {
            material.SetFloat("_Surface", SurfaceTransparent);
            material.SetFloat("_Blend", BlendAlpha);
        }

        private static void SetupRendererFeatures()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            if(data == null)
            {
                Debug.LogError($"[PlazaRenderSetup] Renderer not found: {RendererPath}");
                return;
            }

            AddFullScreenFeature(data, FogFeatureName, "MAT_AtmosphereFog",
                FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents, ScriptableRenderPassInput.Depth);
            AddFullScreenFeature(data, TiltShiftFeatureName, "MAT_TiltShift",
                FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing, ScriptableRenderPassInput.None);
        }

        // ScriptableRendererDataEditor.AddComponent와 같은 방식: 서브 에셋 추가 + 기능 목록과 localId 맵을 함께 늘린다.
        private static void AddFullScreenFeature(ScriptableRendererData data, string featureName, string materialName,
            FullScreenPassRendererFeature.InjectionPoint injectionPoint, ScriptableRenderPassInput requirements)
        {
            if(data.rendererFeatures.Exists(f => f != null && f.name == featureName))
                return;

            var material = AssetDatabase.LoadAssetAtPath<Material>($"{ToolDefines.PlazaMaterialFolder}/{materialName}.mat");
            if(material == null)
            {
                Debug.LogError($"[PlazaRenderSetup] {materialName} missing, feature {featureName} skipped");
                return;
            }

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = featureName;
            feature.passMaterial = material;
            feature.injectionPoint = injectionPoint;
            feature.requirements = requirements;
            feature.fetchColorBuffer = true;
            AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

            // 안개는 외곽선 바로 뒤(같은 주입 지점에서 외곽선 결과 위에 덮도록), 나머지는 맨 뒤
            int insertIndex = data.rendererFeatures.Count;
            if(featureName == FogFeatureName)
            {
                int outlineIndex = data.rendererFeatures.FindIndex(f => f != null && f.name == OutlineFeatureName);
                if(outlineIndex >= 0)
                    insertIndex = outlineIndex + 1;
            }

            var serialized = new SerializedObject(data);
            SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
            SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
            int last = features.arraySize;
            features.arraySize++;
            map.arraySize++;
            features.GetArrayElementAtIndex(last).objectReferenceValue = feature;
            map.GetArrayElementAtIndex(last).longValue = localId;
            features.MoveArrayElement(last, insertIndex);
            map.MoveArrayElement(last, insertIndex);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            data.SetDirty();
            EditorUtility.SetDirty(data);
        }

        private static void CreateVolumeProfile()
        {
            string path = ToolDefines.PlazaVolumeProfilePath;
            if(AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null)
                return;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(BloomThreshold);
            bloom.intensity.Override(BloomIntensity);
            bloom.scatter.Override(BloomScatter);
            bloom.highQualityFiltering.Override(true);

            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(VignetteIntensity);
            vignette.smoothness.Override(VignetteSmoothness);

            var colorAdjustments = profile.Add<ColorAdjustments>();
            colorAdjustments.contrast.Override(ColorContrast);
            colorAdjustments.saturation.Override(ColorSaturation);

            var whiteBalance = profile.Add<WhiteBalance>();
            whiteBalance.temperature.Override(WhiteBalanceTemperature);

            // VolumeProfileFactory와 같이 컴포넌트를 서브 에셋으로 저장
            foreach(VolumeComponent component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }

            EditorUtility.SetDirty(profile);
        }
    }
}
