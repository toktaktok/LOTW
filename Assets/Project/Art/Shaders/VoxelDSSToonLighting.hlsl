#ifndef VOXEL_DSS_TOON_LIGHTING_INCLUDED
#define VOXEL_DSS_TOON_LIGHTING_INCLUDED

// Stepped (toon ramp) lighting for the DSS-derived normal, used instead of UniversalFragmentPBR
// when _DSS_TOON is on. Direct light is quantized into _ToonSteps bands (lowest band = ambient
// only), plus an optional single hard highlight. Shadows are hard-stepped. Ambient GI and light
// layers follow the PBR path; SSAO, metallic and reflection probes are ignored.

// Quantizes 0..1 light into _ToonSteps levels from 0 to 1. _ToonSoftness widens each band edge.
half DSS_ToonBand(half light)
{
    half steps = max(_ToonSteps, 2.0h);
    half f = saturate(light) * steps;
    half band = floor(f);
    band += smoothstep(1.0h - max(_ToonSoftness, 0.001h), 1.0h, f - band);
    return saturate(band / (steps - 1.0h));
}

half3 DSS_ToonLight(Light light, half shadowFloor, half3 albedo, half3 normalWS, half3 viewDirWS)
{
    half NdotL = saturate(dot(normalWS, light.direction));
    // Hard shadow edge: soft-shadow PCF fractions would otherwise land in random bands.
    // shadowFloor = attenuation when fully shadowed (1 - shadow strength).
    half shadow = lerp(shadowFloor, 1.0h, step((1.0h + shadowFloor) * 0.5h, light.shadowAttenuation));
    half band = DSS_ToonBand(NdotL * light.distanceAttenuation * shadow);
    half3 color = albedo * band;

    // One hard highlight; _Smoothness sets its size like the PBR lobe.
    half3 halfDir = SafeNormalize(float3(light.direction) + float3(viewDirWS));
    half spec = pow(saturate(dot(normalWS, halfDir)), exp2(10.0h * _Smoothness + 1.0h));
    color += step(0.5h, spec) * band * _ToonSpecular;

    return color * light.color;
}

half4 DSS_ToonFragment(InputData inputData, SurfaceData surfaceData)
{
    // SSAO is skipped: it is screen-space, so at low resolution it adds per-pixel noise that
    // changes with the view. Use the voxel-space _AOStrength (surfaceData.occlusion) instead.
    half4 shadowMask = CalculateShadowMask(inputData);
    uint meshRenderingLayers = GetMeshRenderingLayer();
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    half3 albedo = surfaceData.albedo;
    half3 color = inputData.bakedGI * albedo * surfaceData.occlusion;

#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        color += DSS_ToonLight(mainLight, 1.0h - GetMainLightShadowStrength(), albedo, inputData.normalWS, inputData.viewDirectionWS);
    }

    // Additional lights assume shadow strength 1 for the hard shadow step.
    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            color += DSS_ToonLight(light, 0.0h, albedo, inputData.normalWS, inputData.viewDirectionWS);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            color += DSS_ToonLight(light, 0.0h, albedo, inputData.normalWS, inputData.viewDirectionWS);
        }
    LIGHT_LOOP_END
    #endif

    return half4(min(color, HALF_MAX), surfaceData.alpha);
}

#endif // VOXEL_DSS_TOON_LIGHTING_INCLUDED
