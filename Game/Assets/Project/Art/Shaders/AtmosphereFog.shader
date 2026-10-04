Shader "Hidden/LOTW/AtmosphereFog"
{
    // Full-screen pass (URP Full Screen Pass Renderer Feature, before transparents, after
    // PixelOutline, requires Depth). Hazes geometry by its reconstructed WORLD-space Z (distance
    // behind the walking lane), not eye depth: with the pitched camera eye depth shrinks with
    // height, which would leave tall facades un-hazed. Background/sky pixels are left untouched.
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.847, 0.824, 0.941, 1)
        _FogStartZ ("Fog Start (world Z)", Float) = 2
        _FogEndZ ("Fog End (world Z)", Float) = 60
        _FogMaxOpacity ("Fog Max Opacity", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "AtmosphereFog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            half4 _FogColor;
            float _FogStartZ;
            float _FogEndZ;
            half _FogMaxOpacity;

            bool IsBackground(float rawDepth)
            {
            #if UNITY_REVERSED_Z
                return rawDepth <= 0.0;
            #else
                return rawDepth >= 1.0;
            #endif
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);

                float raw = LoadSceneDepth(int2(input.positionCS.xy));
                if (IsBackground(raw))
                    return color;

                // Device depth -> clip-space Z expected by the inverse view-projection matrix.
                // Same convention as URP ScreenSpaceShadows; the matrix inverse covers both
                // orthographic (w = 1) and perspective cameras.
                float deviceDepth = raw;
            #if !UNITY_REVERSED_Z
                deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, raw);
            #endif
                float3 positionWS = ComputeWorldSpacePosition(input.texcoord, deviceDepth, UNITY_MATRIX_I_VP);

                // Linear ramp so the first lanes (Z 3-11) already get a low, visible haze
                half fog = _FogMaxOpacity * saturate((positionWS.z - _FogStartZ) / (_FogEndZ - _FogStartZ));
                color.rgb = lerp(color.rgb, _FogColor.rgb, fog);
                return color;
            }
            ENDHLSL
        }
    }
}
