Shader "Custom/CrossbowMuzzleFlashFlipbook"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (1, 1, 1, 1)
        _Opacity("Opacity", Range(0, 1)) = 1
        [Enum(Front, 2, Back, 1, Both, 0)] _Cull("Render Faces", Float) = 2
        _FPS("Frames Per Second", Float) = 12
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Color;
                half _Opacity;
                float _FPS;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 2x2 flipbook layout: index 0=top-left, 1=top-right, 2=bottom-left, 3=bottom-right
                int index = (int)fmod(floor(_Time.y * _FPS) * 1.6180339887, 4.0);
                float2 cell = float2(index % 2, index / 2);
                float2 flipbookUV = (input.uv + cell) * 0.5;

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, flipbookUV);
                half4 col = tex * _Color;
                col.a *= _Opacity;
                return col;
            }
            ENDHLSL
        }
    }
}
