#ifndef VOXEL_DSS_INPUT_INCLUDED
#define VOXEL_DSS_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// SRP-Batcher: every per-material scalar/vector/color lives in this single named block.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half   _Metallic;
    half   _Smoothness;

    float4 _VoxelDims;     // xyz = grid size in voxels
    float4 _ImportScale;   // xyz = VoxelImporter importScale
    float4 _ImportOffset;  // xyz = VoxelImporter importOffset
    float4 _LocalOffset;   // xyz = VoxelImporter localOffset

    half   _KernelRadius;
    half   _StepScale;
    half   _Sigma;
    half   _FallbackThreshold;
    half   _DSSStrength;
    half   _AOStrength;
    half   _AORadius;

    half   _ToonSteps;
    half   _ToonSoftness;
    half   _ToonSpecular;
CBUFFER_END

// Textures/samplers stay OUTSIDE the CBUFFER (SRP-Batcher requirement).
TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
TEXTURE3D(_OccupancyTex); SAMPLER(sampler_OccupancyTex);

#endif // VOXEL_DSS_INPUT_INCLUDED
