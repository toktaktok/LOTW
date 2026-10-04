#ifndef VOXEL_DSS_FORWARD_PASS_INCLUDED
#define VOXEL_DSS_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "VoxelDSSCommon.hlsl"
#include "VoxelDSSToonLighting.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 positionOS   : TEXCOORD1; // object-space pos for the voxel map
    float3 faceNormalOS : TEXCOORD2; // real mesh face normal for signing/fallback
    float2 uv           : TEXCOORD3;
    float4 shadowCoord  : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
    half  fogFactor     : TEXCOORD6;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings VoxelDSSForwardVertex(Attributes IN)
{
    Varyings OUT = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
    OUT.positionCS   = posInputs.positionCS;
    OUT.positionWS   = posInputs.positionWS;
    OUT.positionOS   = IN.positionOS.xyz;
    OUT.faceNormalOS = IN.normalOS;
    OUT.uv           = TRANSFORM_TEX(IN.uv, _BaseMap);
    OUT.shadowCoord  = GetShadowCoord(posInputs);
    OUT.fogFactor    = ComputeFogFactor(posInputs.positionCS.z);
    OUTPUT_LIGHTMAP_UV(IN.staticLightmapUV, unity_LightmapST, OUT.staticLightmapUV);
    OUTPUT_SH(0, OUT.vertexSH); // GI seeded from derived normal in fragment below
    return OUT;
}

half4 VoxelDSSForwardFragment(Varyings IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

    // Derived normal in object space, then to world.
    float3 normalOS = DSS_DeriveNormalOS(IN.positionOS, IN.faceNormalOS);
    float3 normalWS = normalize(TransformObjectToWorldNormal(normalOS));

    half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
    half3 albedo  = baseTex.rgb * _BaseColor.rgb;

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo     = albedo;
    surfaceData.metallic   = _Metallic;
    surfaceData.smoothness = _Smoothness;
    surfaceData.occlusion  = DSS_AmbientOcclusion(IN.positionOS);
    surfaceData.alpha      = 1.0;
    surfaceData.normalTS   = half3(0,0,1);

    InputData inputData = (InputData)0;
    inputData.positionWS = IN.positionWS;
    inputData.normalWS   = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
    inputData.shadowCoord = IN.shadowCoord;
    inputData.fogCoord    = IN.fogFactor;
    inputData.bakedGI     = SAMPLE_GI(IN.staticLightmapUV, IN.vertexSH, normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
    inputData.shadowMask  = SAMPLE_SHADOWMASK(IN.staticLightmapUV);

    // SSAO is applied once inside UniversalFragmentPBR (via inputData.normalizedScreenSpaceUV);
    // do not min() it into surfaceData.occlusion here or it darkens twice.
#if defined(_DSS_TOON)
    half4 color = DSS_ToonFragment(inputData, surfaceData);
#else
    half4 color = UniversalFragmentPBR(inputData, surfaceData);
#endif
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}

#endif // VOXEL_DSS_FORWARD_PASS_INCLUDED
