// This shader is designed for the Universal Render Pipeline (URP).
// This version uses a pre-baked border map for high-quality, performant outlines.

Shader "Custom/GlobeMapModeURP_PreBaked"
{
    Properties
    {
        [MainTexture] _MainTex("Albedo (RGB)", 2D) = "white" {}

        [Header(Map Data)]
        _PoliticalMap("Political Map (for country colors)", 2D) = "black" {}
        _BorderMap("Pre-Baked Border Map", 2D) = "black" {}

        [Header(Display Settings)]
        _OverlayOpacity("Overlay Opacity", Range(0, 1)) = 0.75

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
                TEXTURE2D(_BorderMap);
                SAMPLER(sampler_BorderMap);

                CBUFFER_START(UnityPerMaterial)
                    float4 _MainTex_ST;
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
                    if (_ViewMode == 2) // Countries View
                    {
                        half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                        half4 politicalColor = SAMPLE_TEXTURE2D(_PoliticalMap, sampler_PoliticalMap, IN.uv);
                        half4 borderColor = SAMPLE_TEXTURE2D(_BorderMap, sampler_BorderMap, IN.uv);

                        // Start with the base terrain
                        half4 finalColor = mainColor;
                        // Blend the political color overlay
                        finalColor.rgb = lerp(finalColor.rgb, politicalColor.rgb, _OverlayOpacity);
                        // Draw the pre-baked borders on top
                        finalColor.rgb = lerp(finalColor.rgb, borderColor.rgb, borderColor.a);

                        return finalColor;
                    }

                // For other modes, just show the base texture for now
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            }
            ENDHLSL
        }
        }
}
