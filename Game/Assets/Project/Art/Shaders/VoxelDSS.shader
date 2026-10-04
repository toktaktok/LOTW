Shader "LOTW/VoxelDSS"
{
    // Derived Surface Shading: blocky voxel geometry lit by a normal derived from the
    // occupancy field V(x,y,z) baked into a Texture3D. Geometry is untouched (NOT meshing).
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        [NoScaleOffset] _OccupancyTex ("Occupancy Volume (R)", 3D) = "white" {}
        _VoxelDims ("Voxel Dims (xyz)", Vector) = (16,16,16,0)
        _ImportScale ("Import Scale", Vector) = (1,1,1,0)
        _ImportOffset ("Import Offset", Vector) = (0,0,0,0)
        _LocalOffset ("Local Offset", Vector) = (0,0,0,0)

        _KernelRadius ("Kernel Radius (voxels)", Range(0.25,4)) = 1.0
        _StepScale ("Gradient Step Scale", Range(0.25,4)) = 1.0
        _Sigma ("Gaussian Sigma (weighted loop)", Range(0.25,4)) = 0.6
        _FallbackThreshold ("Fallback |grad| Threshold", Range(0.0001,1)) = 0.08
        _DSSStrength ("DSS Strength", Range(0,1)) = 1.0
        _AOStrength ("Derived-Surface AO Strength", Range(0,1)) = 0.0
        _AORadius ("AO Sample Radius (voxels)", Range(1,4)) = 2.0

        [Toggle(_DSS_CENTROID)] _UseCentroid ("Use Centroid Normal (A.2)", Float) = 0
        [Toggle(_DSS_WEIGHTED_LOOP)] _UseWeightedLoop ("Exact (2r+1)^3 Gaussian Loop", Float) = 0

        [Toggle(_DSS_VOXEL_LIGHTING)] _UseVoxelLighting ("Per-Voxel Lighting (snap to voxel faces)", Float) = 0

        [Header(Toon Ramp)]
        [Toggle(_DSS_TOON)] _UseToon ("Toon Ramp Lighting", Float) = 0
        [IntRange] _ToonSteps ("Toon Bands", Range(2,6)) = 3
        _ToonSoftness ("Band Edge Softness", Range(0,1)) = 0.05
        _ToonSpecular ("Hard Highlight Strength", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "UniversalMaterialType"="Lit" "Queue"="Geometry" }
        LOD 300

        // ---------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VoxelDSSForwardVertex
            #pragma fragment VoxelDSSForwardFragment

            // URP 17.3 lighting keywords (mirrors stock Lit ForwardLit; APV/decals/debug omitted)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            // DSS feature keywords
            #pragma shader_feature_local_fragment _DSS_CENTROID
            #pragma shader_feature_local_fragment _DSS_WEIGHTED_LOOP
            #pragma shader_feature_local_fragment _DSS_TOON
            #pragma shader_feature_local_fragment _DSS_VOXEL_LIGHTING

            #pragma multi_compile_instancing

            #include "VoxelDSSInput.hlsl"
            #include "VoxelDSSForwardPass.hlsl"
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "VoxelDSSInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "VoxelDSSInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Custom DepthNormals: emits the DSS-derived normal so SSAO occludes the
        // derived surface, not the cube faces. Shares VoxelDSSCommon.hlsl with ForwardLit.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VoxelDSSDepthNormalsVertex
            #pragma fragment VoxelDSSDepthNormalsFragment
            #pragma shader_feature_local_fragment _DSS_CENTROID
            #pragma shader_feature_local_fragment _DSS_WEIGHTED_LOOP

            // Match stock DepthNormals normals-texture encoding contract for SSAO/decals.
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #include "VoxelDSSInput.hlsl"
            #include "VoxelDSSDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
