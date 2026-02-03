Shader "Custom/ScreenSpaceEffect"
{
    Properties
    {
        _Saturation("Saturation", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off
        ZTest Always
        Cull Back

        Pass
        {
            Name "ScreenSpacePass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Saturation;
            static const float BRIGHTNESS_BOOST = 0.005f; // Flat brightness increase

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 screenColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;

                const float3 lumaWeights = float3(0.2126, 0.7152, 0.0722);
                float luminance = dot(screenColor, lumaWeights);

                float3 finalColor = lerp(luminance.xxx, screenColor, _Saturation);

                // Apply flat brightness boost
                finalColor += BRIGHTNESS_BOOST;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}