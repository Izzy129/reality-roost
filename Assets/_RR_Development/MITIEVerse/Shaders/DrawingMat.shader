Shader "Custom/Drawing/DrawingMat"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1) // Tint Color
        [MainTexture] _BaseMap("Base Map", 2D) = "white" // Main Texture
        _DrawingTex("Drawing Texture", 2D) = "white" // Drawing RenderTexture
        _InPresentationMode("In Presentation Mode", Float) = 0  // Determines if we are in Presentation mode or Whiteboard mode  
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Main Texture 
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // Drawing RenderTexture
            TEXTURE2D(_DrawingTex);
            SAMPLER(sampler_DrawingTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _InPresentationMode;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                //OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                // Sample Textures
                float4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float4 drawCol = SAMPLE_TEXTURE2D(_DrawingTex, sampler_DrawingTex, IN.uv);

                // Mode 1: Whiteboard mode (Transparent Background)
                if(_InPresentationMode < 0.5)
                {
                    return float4(drawCol.rgb, drawCol.a); // Outputs the drawing texture's color and alpha; background stays transparent
                }

                // Mode 2: Presentation mode (Blended Background)
                float3 blendedRGB = lerp(baseCol.rgb, drawCol.rgb, drawCol.a); // Blends the BaseMap with the DrawingTex (RenderTexture). Allows both textures to be viewable at same time
                return float4(blendedRGB, 1.0); // Outputs the BaseMap and drawing texture blended together
            }
            ENDHLSL
        }
    }
}
