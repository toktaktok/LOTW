#ifndef VOXEL_DSS_COMMON_INCLUDED
#define VOXEL_DSS_COMMON_INCLUDED

// All math runs in OBJECT space. Mapping mirrors VoxelImporter GetVoxelPosition
// (VoxelBaseCore.cs:5226-5228): gridPos = (objectPos / importScale) - (localOffset + importOffset).
// Cell center is at +0.5 (GetVoxelCenterPosition), so texel center == cell center.

float3 DSS_ObjectToUVW(float3 positionOS)
{
    float3 grid = (positionOS / _ImportScale.xyz) - (_LocalOffset.xyz + _ImportOffset.xyz);
    return (grid + 0.5) / max(_VoxelDims.xyz, 1.0);
}

// Single smoothed occupancy sample. Hardware trilinear on the volume already gives a
// box/Gaussian-ish smoothing; this is the cheap default path.
float DSS_SampleRho(float3 uvw)
{
    return SAMPLE_TEXTURE3D_LOD(_OccupancyTex, sampler_OccupancyTex, uvw, 0).r;
}

// Exact (2r+1)^3 Gaussian-weighted occupancy average around uvw, radius in voxels.
float DSS_SampleRhoWeighted(float3 uvw, int r, float sigma)
{
    float3 texel = 1.0 / max(_VoxelDims.xyz, 1.0);
    float invTwoSigmaSq = 1.0 / (2.0 * sigma * sigma);
    float wsum = 0.0;
    float vsum = 0.0;
    [loop] for (int z = -r; z <= r; ++z)
    [loop] for (int y = -r; y <= r; ++y)
    [loop] for (int x = -r; x <= r; ++x)
    {
        float d2 = (float)(x*x + y*y + z*z);
        float w  = exp(-d2 * invTwoSigmaSq);
        float v  = SAMPLE_TEXTURE3D_LOD(_OccupancyTex, sampler_OccupancyTex,
                                        uvw + float3(x,y,z) * texel, 0).r;
        vsum += v * w;
        wsum += w;
    }
    return vsum / max(wsum, 1e-5);
}

// Density gradient (whitepaper A.1) via central differences. Returns RAW (un-normalized)
// object-space gradient; magnitude encodes local thickness/definition.
float3 DSS_GradientNormalOS(float3 uvw)
{
    float3 texel = 1.0 / max(_VoxelDims.xyz, 1.0);
    float3 du = texel * (_KernelRadius * _StepScale);

#if defined(_DSS_WEIGHTED_LOOP)
    int ri = (int)max(1.0, round(_KernelRadius));
    float s = _Sigma;
    float nx = DSS_SampleRhoWeighted(uvw - float3(du.x,0,0), ri, s) - DSS_SampleRhoWeighted(uvw + float3(du.x,0,0), ri, s);
    float ny = DSS_SampleRhoWeighted(uvw - float3(0,du.y,0), ri, s) - DSS_SampleRhoWeighted(uvw + float3(0,du.y,0), ri, s);
    float nz = DSS_SampleRhoWeighted(uvw - float3(0,0,du.z), ri, s) - DSS_SampleRhoWeighted(uvw + float3(0,0,du.z), ri, s);
#else
    // 6-tap central difference on the trilinear-filtered volume (cheap default).
    float nx = DSS_SampleRho(uvw - float3(du.x,0,0)) - DSS_SampleRho(uvw + float3(du.x,0,0));
    float ny = DSS_SampleRho(uvw - float3(0,du.y,0)) - DSS_SampleRho(uvw + float3(0,du.y,0));
    float nz = DSS_SampleRho(uvw - float3(0,0,du.z)) - DSS_SampleRho(uvw + float3(0,0,du.z));
#endif
    // Gradient points toward MORE occupancy; surface normal points OUT, so negate.
    return -float3(nx, ny, nz);
}

// Occupancy centroid (whitepaper A.2): N = normalize(p - C). Empirically Centroid(r) ~= Gradient(r-1).
float3 DSS_CentroidNormalOS(float3 uvw)
{
    float3 texel = 1.0 / max(_VoxelDims.xyz, 1.0);
    int ri = (int)max(1.0, round(_KernelRadius));
    float sigma = _Sigma;
    float invTwoSigmaSq = 1.0 / (2.0 * sigma * sigma);
    float3 csum = 0;
    float  wsum = 0;
    [loop] for (int z = -ri; z <= ri; ++z)
    [loop] for (int y = -ri; y <= ri; ++y)
    [loop] for (int x = -ri; x <= ri; ++x)
    {
        float3 o = float3(x,y,z);
        float v = SAMPLE_TEXTURE3D_LOD(_OccupancyTex, sampler_OccupancyTex, uvw + o * texel, 0).r;
        float w = v * exp(-dot(o,o) * invTwoSigmaSq);
        csum += w * o;
        wsum += w;
    }
    // p is the local origin in offset coords; centroid C = csum/wsum; N = p - C = -csum/wsum.
    return -csum / max(wsum, 1e-5);
}

// Full DSS normal in OBJECT space. faceNormalOS is the REAL mesh face normal (NORMAL semantic).
// Per-face hemisphere signing + thin-feature fallback blend + DSS strength blend.
float3 DSS_DeriveNormalOS(float3 positionOS, float3 faceNormalOS)
{
    float3 uvw = DSS_ObjectToUVW(positionOS);

#if defined(_DSS_CENTROID)
    float3 g = DSS_CentroidNormalOS(uvw);
#else
    float3 g = DSS_GradientNormalOS(uvw);
#endif

    float mag = length(g);
    float3 faceN = normalize(faceNormalOS);

    // Per-face signing: flip derived normal into the same hemisphere as the geometric face
    // so opposing faces of a thin sheet stay correct.
    float3 dssN = (mag > 1e-5) ? (g / mag) : faceN;
    if (dot(dssN, faceN) < 0.0) dssN = -dssN;

    // Thin-feature fallback: |grad| ~ 0 (isolated voxel / 1-voxel sheet) -> use face normal,
    // ease into DSS as local thickness rises. Smooth blend, no popping, no NaN at mag==0.
    float t = smoothstep(_FallbackThreshold * 0.5, _FallbackThreshold, mag);
    float3 n = normalize(lerp(faceN, dssN, t));

    // Global DSS strength (0 = pure cube faces, 1 = full DSS).
    return normalize(lerp(faceN, n, _DSSStrength));
}

// Derived-Surface AO: more nearby occupied voxels => more occluded. Returns 0..1 (1 = unoccluded).
float DSS_AmbientOcclusion(float3 positionOS)
{
    if (_AOStrength <= 0.0) return 1.0;
    float3 uvw = DSS_ObjectToUVW(positionOS);
    float3 texel = 1.0 / max(_VoxelDims.xyz, 1.0);
    int ri = (int)max(1.0, round(_AORadius));
    float occ = 0.0;
    float cnt = 0.0;
    [loop] for (int z = -ri; z <= ri; ++z)
    [loop] for (int y = -ri; y <= ri; ++y)
    [loop] for (int x = -ri; x <= ri; ++x)
    {
        if (x == 0 && y == 0 && z == 0) continue;
        float w = 1.0 / (1.0 + dot(float3(x,y,z), float3(x,y,z)));
        occ += SAMPLE_TEXTURE3D_LOD(_OccupancyTex, sampler_OccupancyTex, uvw + float3(x,y,z) * texel, 0).r * w;
        cnt += w;
    }
    float ratio = occ / max(cnt, 1e-5);
    return saturate(1.0 - ratio * _AOStrength);
}

#endif // VOXEL_DSS_COMMON_INCLUDED
