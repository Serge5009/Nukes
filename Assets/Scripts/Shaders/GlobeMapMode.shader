// This shader is designed for the Universal Render Pipeline (URP).
// This version displays a political map with distinct borders for regions and countries.
Shader "Custom/GlobeMapModeURP"
{
    Properties
    {
        [MainTexture] _MainTex("Albedo (RGB)", 2D) = "white" {}

        [Header(Map Data)]
        _RegionIDMap("Region ID Map (for region borders)", 2D) = "black" {}
        _PoliticalMap("Political Map (for country colors/borders)", 2D) = "black" {}

        [Header(Border Settings)]
        _RegionBorderColor("Region Border Color", Color) = (0.5, 0.5, 0.5, 1)
        _CountryBorderColor("Country Border Color", Color) = (0, 0, 0, 1)
        _BorderThickness("Border Thickness", Range(1, 5)) = 1.5

            // This property is controlled by C# and hidden from the Inspector.
            [HideInInspector] _ViewMode("View Mode", Int) = 0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

            Pass
            {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct Attributes
                {
                    float4 positionOS   : POSITION;
                    float2 uv           : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionHCS  : SV_POSITION;
                    float2 uv           : TEXCOORD0;
                };

                // Texture and Sampler declarations
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_RegionIDMap);
                SAMPLER(sampler_RegionIDMap);
                TEXTURE2D(_PoliticalMap);
                SAMPLER(sampler_PoliticalMap);

                CBUFFER_START(UnityPerMaterial)
                    float4 _MainTex_ST;
                    float4 _RegionBorderColor;
                    float4 _CountryBorderColor;
                    float _BorderThickness;
                    int _ViewMode;
                    float4 _RegionIDMap_TexelSize;
                CBUFFER_END

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                    return OUT;
                }

                float is_border(TEXTURE2D(tex), SAMPLER(samp), float2 uv, float2 texelSize)
                {
                    half4 center = SAMPLE_TEXTURE2D(tex, samp, uv);
                    half4 top = SAMPLE_TEXTURE2D(tex, samp, uv + float2(0, texelSize.y * _BorderThickness));
                    half4 bottom = SAMPLE_TEXTURE2D(tex, samp, uv - float2(0, texelSize.y * _BorderThickness));
                    half4 left = SAMPLE_TEXTURE2D(tex, samp, uv - float2(texelSize.x * _BorderThickness, 0));
                    half4 right = SAMPLE_TEXTURE2D(tex, samp, uv + float2(texelSize.x * _BorderThickness, 0));

                    if (distance(center, top) > 0.01 || distance(center, bottom) > 0.01 ||
                        distance(center, left) > 0.01 || distance(center, right) > 0.01)
                    {
                        return 1.0;
                    }
                    return 0.0;
                }

                half4 frag(Varyings IN) : SV_Target
                {
                    if (_ViewMode == 1) // Regions View
                    {
                        // Directly show the raw colors from the Region ID Map.
                        return SAMPLE_TEXTURE2D(_RegionIDMap, sampler_RegionIDMap, IN.uv);
                    }
                    else if (_ViewMode == 2) // Countries View
                    {
                        half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                        half4 politicalColor = SAMPLE_TEXTURE2D(_PoliticalMap, sampler_PoliticalMap, IN.uv);
                        half4 finalColor = lerp(mainColor, politicalColor, 0.8);

                        float2 texelSize = _RegionIDMap_TexelSize.xy;

                        float regionBorder = is_border(_RegionIDMap, sampler_RegionIDMap, IN.uv, texelSize);
                        float countryBorder = is_border(_PoliticalMap, sampler_PoliticalMap, IN.uv, texelSize);

                        finalColor.rgb = lerp(finalColor.rgb, _RegionBorderColor.rgb, regionBorder);
                        finalColor.rgb = lerp(finalColor.rgb, _CountryBorderColor.rgb, countryBorder);

                        return finalColor;
                    }

                // Default (ViewMode == 0) is to show the normal texture.
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            }
            ENDHLSL
        }
        }
}
