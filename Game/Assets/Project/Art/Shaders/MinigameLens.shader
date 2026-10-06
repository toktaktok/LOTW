Shader "LOTW/UI/MinigameLens"
{
    // 미니게임 스테이지 RT를 창에 투사. 아날로그 렌즈 느낌(비네팅, 그레인, 깜빡임)만 내고 기하 왜곡은 없음.
    // _IrisX/_IrisY 는 MinigameWindow가 열림/닫힘 애니메이션으로 구동 (0 = 닫힘, 1 = 완전히 열림).
    // 미니게임 전용 셰이더를 만들 때도 _IrisX/_IrisY 를 받으면 같은 조리개 연출이 적용됨.
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.3
        _VignetteRadius ("Vignette Radius", Range(0, 1.5)) = 0.8
        _VignetteSoftness ("Vignette Softness", Range(0.01, 1)) = 0.5
        _GrainStrength ("Grain Strength", Range(0, 0.2)) = 0.035
        _GrainFps ("Grain FPS", Float) = 12
        _FlickerStrength ("Flicker Strength", Range(0, 0.1)) = 0.015

        _IrisX ("Iris Open X", Range(0, 1)) = 1
        _IrisY ("Iris Open Y", Range(0, 1)) = 1
        _IrisEdge ("Iris Edge Darken", Range(0, 1)) = 0.6

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _VignetteStrength;
            float _VignetteRadius;
            float _VignetteSoftness;
            float _GrainStrength;
            float _GrainFps;
            float _FlickerStrength;
            float _IrisX;
            float _IrisY;
            float _IrisEdge;

            // sin 없는 해시 (Dave Hoskins hash12). sin 해시는 입력이 크면 GPU 정밀도 때문에 줄무늬가 생김
            float Hash(float2 p)
            {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 효과는 RT 픽셀 격자에 맞춰 계산해 픽셀 아트와 같은 해상도로 보이게 함
                float2 pixel = floor(i.uv * _MainTex_TexelSize.zw);
                float2 snappedUv = (pixel + 0.5) * _MainTex_TexelSize.xy;

                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                // 스테이지 카메라 배경의 알파와 무관하게 화면은 불투명
                col.a = i.color.a;

                float2 centered = (snappedUv - 0.5) * 2.0;
                float vignette = smoothstep(_VignetteRadius, _VignetteRadius + _VignetteSoftness, length(centered));
                col.rgb *= 1.0 - vignette * _VignetteStrength;

                // 프레임 번호를 작은 범위로 접어 해시 입력이 커지지 않게 함 (커지면 줄무늬가 생김)
                float frame = fmod(floor(_Time.y * _GrainFps), 61.0);
                col.rgb += (Hash(pixel + frame * 17.13) - 0.5) * _GrainStrength;
                col.rgb *= 1.0 + (Hash(float2(frame, 3.7)) - 0.5) * _FlickerStrength;

                // 사각 조리개: 가운데에서 RT 픽셀 단위로 열림. 경계 1픽셀은 어둡게 해 셔터 날 느낌을 줌.
                float2 halfPixels = _MainTex_TexelSize.zw * 0.5;
                float2 fromCenter = abs(pixel + 0.5 - halfPixels);
                float2 open = floor(float2(_IrisX, _IrisY) * halfPixels + 0.5);
                float2 inside = step(fromCenter, open);
                col.a *= inside.x * inside.y;
                float2 onEdge = step(open - 1.0, fromCenter) * step(float2(_IrisX, _IrisY), 0.999);
                col.rgb *= 1.0 - saturate(onEdge.x + onEdge.y) * _IrisEdge;

                return col;
            }
            ENDCG
        }
    }
}
