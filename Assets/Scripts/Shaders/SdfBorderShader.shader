// This shader is designed for the Universal Render Pipeline (URP).
// This is the definitive version that uses a robust SDF rendering technique
// to draw high-quality, dynamically anti-aliased borders.
Shader "Custom/SDFBorderShaderAdvanced"
{
    Properties
    {
        [MainTexture] _MainTex("Albedo (RGB)", 2D) = "white" {}

        [Header(Map Data)]
        _PoliticalMap("Political Map (for country colors)", 2D) = "black" {}
        _SDFMap("SDF Map (for borders)", 2D) = "white" {}

        [Header(Display Settings)]
        _OverlayOpacity("Overlay Opacity", Range(0, 1)) = 0.75
        _BorderColor("Border Color", Color) = (0, 0, 0, 1)
            // A value of 0.0 is the exact center of the border.
            _BorderCenter("Border Center", Range(0.0, 0.5)) = 0.0
            // Width is now a percentage of the SDF spread.
            _BorderWidth("Border Width", Range(0.0, 0.1)) = 0.01

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
                TEXTURE2D(_PoliticalMap);
                SAMPLER(sampler_PoliticalMap);
                TEXTURE2D(_SDFMap);
                SAMPLER(sampler_SDFMap);

                CBUFFER_START(UnityPerMaterial)
                    float4 _MainTex_ST;
                    float4 _BorderColor;
                    float _BorderCenter;
                    float _BorderWidth;
                    float _OverlayOpacity;
                    int _ViewMode;
                CBUFFER_END

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                    return OUT;
                }

                half4 frag(Varyings IN) : SV_Target
                {
                    half4 finalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                    if (_ViewMode == 2) // Countries View
                    {
                        half4 politicalColor = SAMPLE_TEXTURE2D(_PoliticalMap, sampler_PoliticalMap, IN.uv);
                        float sdfValue = SAMPLE_TEXTURE2D(_SDFMap, sampler_SDFMap, IN.uv).r;

                        // This is the correct, robust method for rendering an SDF line.
                        // It calculates the distance from the center and draws a line of a specific width.
                        float dist_from_center = abs(sdfValue - _BorderCenter);
                        float half_width = _BorderWidth / 2.0;
                        float softness = fwidth(sdfValue);
                        float border = 1.0 - smoothstep(half_width - softness, half_width + softness, dist_from_center);

                        // Start with the base terrain
                        finalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                        // Blend the political color overlay
                        finalColor.rgb = lerp(finalColor.rgb, politicalColor.rgb, _OverlayOpacity);
                        // Draw the border on top
                        finalColor.rgb = lerp(finalColor.rgb, _BorderColor.rgb, border);
                    }
                    else if (_ViewMode == 1) // Regions View (for debug)
                    {
                        finalColor = SAMPLE_TEXTURE2D(_SDFMap, sampler_SDFMap, IN.uv);
                    }

                    return finalColor;
                }
                ENDHLSL
            }
        }
}
