Shader "Hidden/LOTW/PixelOutline"
{
    // Full-screen pass (URP Full Screen Pass Renderer Feature, before transparents, requires
    // Depth + Normal). Runs at the camera target resolution, so with LowResPixelRenderer every
    // line is exactly one low-res pixel wide.
    // - Depth edge: darkens the near side of a silhouette. Uses the second difference of eye
    //   depth so flat surfaces at grazing angles (e.g. the ground) are not outlined.
    // - Normal crease: brightens one side of a convex crease in the derived normals.
    Properties
    {
        _DepthThreshold ("Depth Edge Threshold (units)", Range(0.05, 4)) = 0.4
        _NormalThreshold ("Normal Crease Threshold", Range(0.01, 2)) = 0.4
        _OutlineDarken ("Outline Darken", Range(0, 1)) = 0.45
        _HighlightBrighten ("Crease Highlight", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "PixelOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float _DepthThreshold;
            float _NormalThreshold;
            half _OutlineDarken;
            half _HighlightBrighten;

            bool IsBackground(float rawDepth)
            {
            #if UNITY_REVERSED_Z
                return rawDepth <= 0.0;
            #else
                return rawDepth >= 1.0;
            #endif
            }

            // Linear eye depth for both orthographic and perspective cameras.
            float EyeDepth(float rawDepth)
            {
                if (unity_OrthoParams.w == 1.0)
                {
                #if UNITY_REVERSED_Z
                    rawDepth = 1.0 - rawDepth;
                #endif
                    return lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                }
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);

                int2 p = int2(input.positionCS.xy);
                int2 maxP = int2(_ScreenParams.xy) - 1;
                float raw = LoadSceneDepth(p);
                if (IsBackground(raw))
                    return color;

                float depth = EyeDepth(raw);
                float3 normal = LoadSceneNormals(p);

                const int2 offsets[4] = { int2(1, 0), int2(-1, 0), int2(0, 1), int2(0, -1) };
                float neighborDepth[4];
                float crease = 0.0;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    int2 q = clamp(p + offsets[i], int2(0, 0), maxP);
                    float rawQ = LoadSceneDepth(q);
                    neighborDepth[i] = IsBackground(rawQ) ? _ProjectionParams.z : EyeDepth(rawQ);
                    if (!IsBackground(rawQ))
                    {
                        // Only the side whose normal leans toward (1,1,1) lights up, so the crease is 1px.
                        float3 diff = normal - LoadSceneNormals(q);
                        crease += step(0.0, dot(diff, float3(1, 1, 1))) * dot(diff, diff);
                    }
                }

                // Positive second difference = neighbors on average farther = this pixel is the near side.
                float lapX = neighborDepth[0] + neighborDepth[1] - 2.0 * depth;
                float lapY = neighborDepth[2] + neighborDepth[3] - 2.0 * depth;
                if (max(lapX, lapY) > _DepthThreshold)
                {
                    color.rgb *= 1.0 - _OutlineDarken;
                }
                else if (crease > _NormalThreshold)
                {
                    color.rgb *= 1.0 + _HighlightBrighten;
                }
                return color;
            }
            ENDHLSL
        }
    }
}
