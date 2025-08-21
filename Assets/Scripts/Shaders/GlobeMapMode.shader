// This shader is designed for the Universal Render Pipeline (URP).
// It replaces the older CGPROGRAM shader to fix compilation errors (pink material).
Shader "Custom/GlobeMapModeURP"
{
    Properties
    {
        [MainTexture] _MainTex("Albedo (RGB)", 2D) = "white" {}
        _OverlayTexture("Overlay Texture", 2D) = "black" {}
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
                TEXTURE2D(_OverlayTexture);
                SAMPLER(sampler_OverlayTexture);

                // CBUFFER contains properties set from C#
                CBUFFER_START(UnityPerMaterial)
                    float4 _MainTex_ST;
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
                    half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                    half4 overlayColor = SAMPLE_TEXTURE2D(_OverlayTexture, sampler_OverlayTexture, IN.uv);

                    half4 finalColor = mainColor;

                    // Switch which texture to display based on the _ViewMode
                    // set by the GlobeDisplayManager script.
                    switch (_ViewMode)
                    {
                        case 1: // Regions View
                            finalColor = overlayColor;
                            break;
                        case 2: // Countries View
                            finalColor = overlayColor;
                            break;
                            // Default is case 0 (Normal View), which is already set
                        }

                        return finalColor;
                    }
                    ENDHLSL
                }
        }
}
