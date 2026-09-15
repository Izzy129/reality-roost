// HLSLPROGRAM only: CGPROGRAM automatically imports incompatible Built-in globals.
#ifndef ROOST_URP_UI_BRIDGE_INCLUDED
#define ROOST_URP_UI_BRIDGE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// Preserve the existing TMP math and property declarations.
#define fixed half
#define fixed2 half2
#define fixed3 half3
#define fixed4 half4
#define UNITY_INITIALIZE_OUTPUT(type, name) name = (type)0
float4 UnityObjectToClipPos(float4 positionOS) { return TransformObjectToHClip(positionOS.xyz); }
float3 UnityObjectToWorldNormal(float3 normalOS) { return TransformObjectToWorldNormal(normalOS); }
float3 WorldSpaceViewDir(float4 positionOS) { return GetWorldSpaceViewDir(TransformObjectToWorld(positionOS.xyz)); }
bool IsGammaSpace()
{
#if defined(UNITY_COLORSPACE_GAMMA)
    return true;
#else
    return false;
#endif
}
// Same UI color conversion as UnityUI.cginc, without Built-in shader globals.
half3 UIGammaToLinear(half3 value)
{
    half3 low = 0.0849710 * value - 0.000163029;
    half3 high = value * (value * (value * 0.265885 + 0.736584) - 0.00980184) + 0.00319697;
    return (value < (half3)0.0725490) ? low : high;
}
#endif
