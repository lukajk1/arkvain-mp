Shader "Custom/WeaponViewmodels"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tiling ("Tiling", Vector) = (1, 1, 0, 0)
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 1.0
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _SpecularDarknessBoost ("Specular Darkness Weight", Range(0, 1)) = 0.5
        _NoiseScale ("Noise Scale", Float) = 1.0
        _NoiseInfluence ("Noise Influence", Range(0, 1)) = 0.5
        _NoiseSharpness ("Noise Sharpness", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Perlin noise function
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float perlinNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(dot(hash2(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                                 dot(hash2(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                            lerp(dot(hash2(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                                 dot(hash2(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tiling;
                float4 _Offset;
                float _ShadowStrength;
                float _SpecularStrength;
                float _Smoothness;
                float _SpecularDarknessBoost;
                float _NoiseScale;
                float _NoiseInfluence;
                float _NoiseSharpness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv * _Tiling.xy + _Offset.xy;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Get main light
                Light mainLight = GetMainLight();

                // Calculate self-shadowing using shadow coordinates
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                float shadowAttenuation = MainLightRealtimeShadow(shadowCoord);

                // Apply shadow strength - lerp between fully lit (1.0) and shadowed
                float finalShadow = lerp(1.0, shadowAttenuation, _ShadowStrength);

                // Apply lighting with self-shadow
                half3 normalWS = normalize(input.normalWS);
                half NdotL = saturate(dot(normalWS, mainLight.direction));

                half3 lighting = mainLight.color * NdotL * finalShadow;

                // Calculate specular highlights (Blinn-Phong)
                half3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half NdotH = saturate(dot(normalWS, halfDir));

                // Calculate specular power from smoothness
                half specularPower = exp2(10 * _Smoothness + 1);
                half specular = pow(NdotH, specularPower);

                // Sample Perlin noise based on object space position
                float noiseValue = perlinNoise(input.positionOS.xz * _NoiseScale);

                // Apply sharpening: lerp between normal noise and sharpened (1.0 - abs(noise))
                float sharpenedNoise = 1.0 - abs(noiseValue);
                noiseValue = lerp(noiseValue, sharpenedNoise, _NoiseSharpness);

                // Remap noise from [-1, 1] to [0, 1]
                noiseValue = noiseValue * 0.5 + 0.5;

                // Apply noise influence - lerp between full specular (1.0) and noise-modulated specular
                float noiseModulation = lerp(1.0, noiseValue, _NoiseInfluence);

                // Calculate texture brightness (luminance)
                half textureLuminance = dot(texColor.rgb, half3(0.299, 0.587, 0.114));

                // Apply darkness boost - darker textures get stronger specular
                // At _SpecularDarknessBoost = 0.5: white (1.0) gets 0.5x, black (0.0) gets 1.0x
                half darknessMultiplier = lerp(1.0, 1.0 - textureLuminance, _SpecularDarknessBoost);

                // Apply specular strength, darkness boost, and noise modulation
                specular *= _SpecularStrength * darknessMultiplier * noiseModulation * finalShadow;

                half3 specularColor = mainLight.color * specular;

                // Add ambient
                half3 ambient = half3(0.2, 0.2, 0.2);

                half3 finalColor = texColor.rgb * (lighting + ambient) + specularColor;

                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }

        // ShadowCaster pass - required for casting shadows (self-shadowing)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
