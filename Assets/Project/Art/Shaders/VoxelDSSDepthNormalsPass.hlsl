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

half4 VoxelDSSDepthNormalsFragment(Varyings IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    float3 normalOS = DSS_DeriveNormalOS(IN.positionOS, IN.faceNormalOS);
    float3 normalWS = normalize(TransformObjectToWorldNormal(normalOS));
    float3 normalVS = normalize(TransformWorldToViewNormal(normalWS, true));

    return half4(normalVS, 0.0);
}

#endif // VOXEL_DSS_DEPTHNORMALS_PASS_INCLUDED
