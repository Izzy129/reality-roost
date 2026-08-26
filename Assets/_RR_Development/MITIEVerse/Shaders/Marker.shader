Shader "Custom/Drawing/Marker"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {} // RenderTexture user is drawing onto
        _BrushCenter ("Brush Center", Vector) = (0.5, 0.5, 0, 0) // UV coordinate of where brush draws
        _BrushRadius ("Brush Radius", Float) = 0.1 // Radius of the drawn circle
        _BrushColor ("Brush Color", Color) = (1,0,0,1) // Brush color
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline"="UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "BrushBlit"
            Blend One Zero
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); // texture we're reading from (AKA RenderTexture)
            SAMPLER(sampler_MainTex);

            // Shader variables
            float4 _BrushCenter;
            float _BrushRadius;
            float4 _BrushColor;

            struct Attributes // Input to Vertex Shader
            {
                float4 positionCS : POSITION; 
                float2 uv : TEXCOORD0;
            };

            struct Varyings // Output to Vertex Shader
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionCS.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                // Get distance from brush center
                float dist = distance(i.uv, _BrushCenter.xy);
                if(dist > _BrushRadius)
                    discard; // Will not output anything
                return float4(_BrushColor.rgb, 1.0); // Returns a circle
            }

            ENDHLSL
        }
    }
}
