// This shader takes a flat mesh and uses its UV data to project it
// as a line onto the surface of a sphere.
Shader "Custom/UvProjectedLine"
{
    Properties
    {
        _Color("Line Color", Color) = (1,1,1,1)
        _LineWidth("Line Width", Float) = 10.0
        _GlobeRadius("Globe Radius", Float) = 6371.0
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normal       : NORMAL; // We use the normal channel to pass UV data
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _LineWidth;
                float _GlobeRadius;
            CBUFFER_END

                // Function to convert a UV coordinate to a 3D point on a sphere
                float3 UVToSphere(float2 uv, float radius)
                {
                    float lon = (uv.x - 0.5) * 2 * PI;
                    float lat = (uv.y - 0.5) * -PI;

                    float x = radius * cos(lat) * cos(lon);
                    float y = radius * sin(lat);
                    float z = radius * cos(lat) * sin(lon);

                    return float3(x, y, z);
                }

                Varyings vert(Attributes v)
                {
                    Varyings o;

                    // Get the start and end points of the line segment from the vertex data
                    float2 uv1 = v.normal.xy; // Start UV is in normal
                    float2 uv2 = v.positionOS.xy; // End UV is in position

                    // Convert these 2D UV points to 3D positions on the globe
                    float3 p1_3D = UVToSphere(uv1, _GlobeRadius);
                    float3 p2_3D = UVToSphere(uv2, _GlobeRadius);

                    // Calculate the direction of the line and a "right" vector for thickness
                    float3 lineDir = normalize(p2_3D - p1_3D);
                    float3 right = normalize(cross(lineDir, p1_3D));

                    // Offset the vertex to give the line thickness
                    // v.positionOS.z is either -1 or 1, creating the two sides of the quad
                    float3 offset = right * _LineWidth * v.positionOS.z;

                    // Determine if this vertex is for the start or end of the segment
                    float3 finalPos = lerp(p1_3D, p2_3D, step(0.5, v.normal.z));

                    o.positionHCS = TransformObjectToHClip(finalPos + offset);
                    return o;
                }

                half4 frag(Varyings i) : SV_Target
                {
                    return _Color;
                }
                ENDHLSL
            }
    }
}
