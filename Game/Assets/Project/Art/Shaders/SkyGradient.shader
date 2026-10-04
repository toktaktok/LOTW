Shader "Skybox/LOTW Gradient"
{
    // Skybox gradient driven by normalized screen-space Y, so it does not depend on how Unity
    // projects the skybox mesh for an orthographic camera. Optional posterize bands and
    // hash-based stars (for a night variant).
    Properties
    {
        _TopColor ("Top Color", Color) = (0.549, 0.608, 0.878, 1)
        _HorizonColor ("Horizon Color", Color) = (0.847, 0.824, 0.941, 1)
        _BottomColor ("Bottom Color", Color) = (0.918, 0.851, 0.910, 1)
        _HorizonHeight ("Horizon Height (screen Y)", Range(0, 1)) = 0.35
        _HorizonSharpness ("Horizon Sharpness", Range(0.1, 10)) = 3
        _Bands ("Posterize Bands (0 = off)", Range(0, 32)) = 0
        _StarDensity ("Star Density", Range(0, 0.05)) = 0
        _StarBrightness ("Star Brightness", Range(0, 2)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            Name "SkyGradient"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Star grid cells across the screen height.
            #define STAR_GRID 180.0

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                float _HorizonHeight;
                float _HorizonSharpness;
                float _Bands;
                float _StarDensity;
                float _StarBrightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float Posterize(float t)
            {
                return _Bands > 0.5 ? floor(t * _Bands) / _Bands : t;
            }

            // Ease out from the horizon: higher sharpness = thinner horizon band.
            float Blend(float t)
            {
                return 1.0 - pow(1.0 - saturate(t), _HorizonSharpness);
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // Bottom-up screen UV on every platform / render target.
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float h = max(_HorizonHeight, 1e-4);

                half3 color;
                if (uv.y >= _HorizonHeight)
                {
                    float a = Posterize((uv.y - _HorizonHeight) / max(1.0 - _HorizonHeight, 1e-4));
                    color = lerp(_HorizonColor.rgb, _TopColor.rgb, Blend(a));

                    if (_StarDensity > 0.0)
                    {
                        float aspect = _ScreenParams.x / _ScreenParams.y;
                        float2 cell = floor(uv * float2(aspect, 1.0) * STAR_GRID);
                        float star = step(1.0 - _StarDensity, Hash21(cell));
                        color += star * _StarBrightness * saturate(a * 2.0);
                    }
                }
                else
                {
                    float b = Posterize((_HorizonHeight - uv.y) / h);
                    color = lerp(_HorizonColor.rgb, _BottomColor.rgb, Blend(b));
                }
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
