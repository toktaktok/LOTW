#ifndef VOXEL_DSS_DEPTHNORMALS_PASS_INCLUDED
#define VOXEL_DSS_DEPTHNORMALS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "VoxelDSSCommon.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionOS   : TEXCOORD0;
    float3 faceNormalOS : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings VoxelDSSDepthNormalsVertex(Attributes IN)
{
    Varyings OUT = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    OUT.positionCS   = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.positionOS   = IN.positionOS.xyz;
    OUT.faceNormalOS = IN.normalOS;
    return OUT;
}

// Output must match stock URP DepthNormals (LitDepthNormalsPass): WORLD-space normal into
// SV_Target0, oct-packed under _GBUFFER_NORMALS_OCT, else NormalizeNormalPerPixel(normalWS).
// SSAO/decals read _CameraNormalsTexture with this exact encoding.
void VoxelDSSDepthNormalsFragment(
    Varyings IN
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    float3 normalOS = DSS_DeriveNormalOS(IN.positionOS, IN.faceNormalOS);
    float3 normalWS = normalize(TransformObjectToWorldNormal(normalOS));

#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
    outNormalWS = half4(packedNormalWS, 0.0);
#else
    outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
#endif

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = EncodeMeshRenderingLayer(renderingLayers);
#endif
}

#endif // VOXEL_DSS_DEPTHNORMALS_PASS_INCLUDED
