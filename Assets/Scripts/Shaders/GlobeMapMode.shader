// This shader is designed for the Universal Render Pipeline (URP).
// This version displays political ownership using colored borders instead of a full overlay.
Shader "Custom/GlobeMapModeURP_Borders"
{
    Properties
    {
        [MainTexture] _MainTex("Albedo (RGB)", 2D) = "white" {}

        [Header(Map Data)]
        _RegionIDMap("Region ID Map (for region borders)", 2D) = "black" {}
        _PoliticalMap("Political Map (for country colors/borders)", 2D) = "black" {}

        [Header(Border Settings)]
        _CountryBorderThickness("Country Border Thickness", Range(0.1, 5)) = 2.0
        _RegionBorderThickness("Region Border Thickness", Range(0.1, 3)) = 1.0

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
                    float _CountryBorderThickness;
                    float _RegionBorderThickness;
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

                // Function to detect if a pixel is on a border by checking its neighbors
                float is_border(TEXTURE2D(tex), SAMPLER(samp), float2 uv, float2 texelSize, float thickness)
                {
                    half4 center = SAMPLE_TEXTURE2D(tex, samp, uv);
                    float2 offset_x = float2(texelSize.x * thickness, 0);
                    float2 offset_y = float2(0, texelSize.y * thickness);

                    half4 top = SAMPLE_TEXTURE2D(tex, samp, uv + offset_y);
                    half4 bottom = SAMPLE_TEXTURE2D(tex, samp, uv - offset_y);
                    half4 left = SAMPLE_TEXTURE2D(tex, samp, uv - offset_x);
                    half4 right = SAMPLE_TEXTURE2D(tex, samp, uv + offset_x);

                    if (distance(center.rgb, top.rgb) > 0.01 || distance(center.rgb, bottom.rgb) > 0.01 ||
                        distance(center.rgb, left.rgb) > 0.01 || distance(center.rgb, right.rgb) > 0.01)
                    {
                        return 1.0;
                    }
                    return 0.0;
                }

                half4 frag(Varyings IN) : SV_Target
                {
                    half4 finalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                    if (_ViewMode == 2) // Countries View
                    {
                        half4 politicalColor = SAMPLE_TEXTURE2D(_PoliticalMap, sampler_PoliticalMap, IN.uv);
                        float2 texelSize = _RegionIDMap_TexelSize.xy;

                        // Detect borders with their own thickness settings
                        float countryBorder = is_border(_PoliticalMap, sampler_PoliticalMap, IN.uv, texelSize, _CountryBorderThickness);
                        float regionBorder = is_border(_RegionIDMap, sampler_RegionIDMap, IN.uv, texelSize, _RegionBorderThickness);

                        // Subtract country borders from region borders to prevent overlap
                        regionBorder = saturate(regionBorder - countryBorder);

                        // Draw the borders using the country's color
                        finalColor.rgb = lerp(finalColor.rgb, politicalColor.rgb, regionBorder);
                        finalColor.rgb = lerp(finalColor.rgb, politicalColor.rgb, countryBorder);
                    }
                    else if (_ViewMode == 1) // Regions View
                    {
                        // In region view, just show the raw ID map colors
                        finalColor = SAMPLE_TEXTURE2D(_RegionIDMap, sampler_RegionIDMap, IN.uv);
                    }

                    return finalColor;
                }
                ENDHLSL
            }
        }
}
