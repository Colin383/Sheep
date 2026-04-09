Shader "Custom/Checker Color Swap"
{
    Properties
    {
        [MainTexture] _CheckerTex("Checker Texture (optional)", 2D) = "white" {}
        _CheckerTex_ST("CheckerTex ST", Vector) = (1,1,0,0)

        [MainColor] _ColorA("Color A", Color) = (1,1,1,1)
        _ColorB("Color B", Color) = (0,0,0,1)

        _Scale("Checker Scale", Float) = 8
        _Threshold("Texture Luma Threshold", Range(0,1)) = 0.5

        [Toggle] _UseTexture("Use Checker Texture (otherwise procedural)", Float) = 0
        _Alpha("Alpha", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        HLSLINCLUDE
            // Transparent：走阴影贴图采样，避免与 _MAIN_LIGHT_SHADOWS_SCREEN 的 opaque 路径冲突
            #define _SURFACE_TYPE_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Shadows.hlsl 使用 LerpWhiteTo，定义在 CoreRP CommonMaterial（Core.hlsl 不会自动引入）
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _CheckerTex_ST;
                float4 _ColorA;
                float4 _ColorB;
                float _Scale;
                float _Threshold;
                float _UseTexture;
                float _Alpha;
            CBUFFER_END

            TEXTURE2D(_CheckerTex);
            SAMPLER(sampler_CheckerTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            /// <summary>
            /// Returns 0/1 for a checker pattern, driven by UV.
            /// - If UseTexture=1: sample the checker texture and classify by luminance threshold.
            /// - Otherwise: procedural checker based on UV grid parity.
            /// </summary>
            half Checker01(float2 uv)
            {
                if (_UseTexture > 0.5)
                {
                    half4 t = SAMPLE_TEXTURE2D(_CheckerTex, sampler_CheckerTex, uv);
                    half luma = dot(t.rgb, half3(0.299h, 0.587h, 0.114h));
                    return step(_Threshold, luma);
                }

                float2 suv = uv * max(_Scale, 0.0001);
                float cx = floor(suv.x);
                float cy = floor(suv.y);
                // parity: 0/1 alternating cells
                return fmod(cx + cy, 2.0) < 1.0 ? 0.0h : 1.0h;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs vi = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionHCS = vi.positionCS;
                o.positionWS = vi.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _CheckerTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half mask = Checker01(i.uv);
                half4 col = lerp(_ColorB, _ColorA, mask);
                col.a *= _Alpha;

                #if !defined(_RECEIVE_SHADOWS_OFF)
                    float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                    half shadow = MainLightRealtimeShadow(shadowCoord);
                    col.rgb *= shadow;
                #endif

                return col;
            }
        ENDHLSL

        // URP 2D Renderer uses LightMode="Universal2D".
        // To keep this shader usable in both 2D Renderer and Forward Renderer,
        // we provide two identical passes with different LightMode tags.
        Pass
        {
            Name "Unlit2D"
            Tags { "LightMode"="Universal2D" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            ENDHLSL
        }

        Pass
        {
            Name "UnlitForward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            ENDHLSL
        }
    }
}
