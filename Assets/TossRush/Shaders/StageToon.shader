Shader "TossRush/Stage Toon"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _ShadowTint("Shadow tint", Color) = (0.48,0.56,0.68,1)
        _Steps("Light steps", Range(2,6)) = 3
        _Rim("Edge highlight", Range(0,0.5)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _ShadowTint;
            half _Steps, _Rim;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 n=normalize(i.normalWS);
                half diffuse=saturate(dot(n,light.direction)*0.5+0.5);
                half bands=floor(diffuse*_Steps)/max(1,_Steps-1);
                half3 shade=lerp(_ShadowTint.rgb,half3(1,1,1),saturate(bands));
                half rim=pow(1-saturate(dot(n,normalize(GetWorldSpaceViewDir(i.positionWS)))),3)*_Rim;
                return half4(_BaseColor.rgb*(shade*(0.72+0.28*light.color))+rim,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
