Shader "Hidden/LOTW/TiltShift"
{
    // Full-screen pass (URP Full Screen Pass Renderer Feature, before post-processing, fetch
    // color buffer). Screen-space tilt-shift: a golden-angle disk blur whose radius grows with
    // distance from a horizontal focus band, plus a saturation boost inside the band.
    // Runs at the camera target resolution, so with LowResPixelRenderer the blur is in RT pixels.
    // Values are globals set every frame by TiltShiftDriver from the TiltShiftVolume volume component;
    // the focus band center follows the player's screen height.
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "TiltShift"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define MAX_SAMPLES 32
            #define GOLDEN_ANGLE 2.39996323

            float _TiltShiftFocusCenter;
            float _TiltShiftFocusWidth;
            float _TiltShiftFalloff;
            float _TiltShiftMaxRadius;
            float _TiltShiftSampleCount;
            half _TiltShiftFocusSaturation;
            float3 _TiltShiftCameraPos;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                float blur = smoothstep(_TiltShiftFocusWidth, _TiltShiftFocusWidth + _TiltShiftFalloff, abs(uv.y - _TiltShiftFocusCenter));
                float radius = _TiltShiftMaxRadius * blur;

                // Only the play-mode main camera (whose position the driver publishes) gets the effect;
                // Scene view and other cameras pass through untouched.
                float3 camDelta = _WorldSpaceCameraPos - _TiltShiftCameraPos;
                if (dot(camDelta, camDelta) > 1e-4)
                {
                    radius = 0;
                    blur = 1;
                }

                half4 color;
                if (radius < 0.5)
                {
                    color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
                }
                else
                {
                    // _BlitTexture_TexelSize is not set by the full-screen pass; the source matches
                    // the camera target, so use its pixel size.
                    float2 texel = _ScreenParams.zw - 1.0;
                    int count = clamp((int)_TiltShiftSampleCount, 1, MAX_SAMPLES);
                    float invCount = 1.0 / count;
                    half4 sum = 0;

                    [loop]
                    for (int i = 0; i < MAX_SAMPLES; i++)
                    {
                        if (i >= count)
                            break;
                        float r = sqrt((i + 0.5) * invCount) * radius;
                        float theta = i * GOLDEN_ANGLE;
                        float2 offset = float2(cos(theta), sin(theta)) * r * texel;
                        sum += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset, 0);
                    }
                    color = sum * invCount;
                }

                half focus = 1.0 - blur;
                half luma = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));
                color.rgb = max(0.0, lerp(luma.xxx, color.rgb, lerp(1.0, _TiltShiftFocusSaturation, focus)));
                return color;
            }
            ENDHLSL
        }
    }
}
