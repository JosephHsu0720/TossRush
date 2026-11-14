Shader "TossRush/Scenery Net"
{
    Properties
    {
        _BaseColor("Wire color", Color) = (.22,.27,.29,1)
        _Density("Grid density", Float) = 1.6
        _WireWidth("Wire width", Float) = .025
        _Diagonal("Diamond weave", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" }
        Cull Off AlphaToMask On
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Density, _WireWidth, _Diagonal;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings vert(Attributes v)
            { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; return o; }
            half4 frag(Varyings i):SV_Target
            {
                float2 p=lerp(i.uv,float2(i.uv.x+i.uv.y,i.uv.x-i.uv.y),_Diagonal)*_Density;
                float2 distance=abs(frac(p+.5)-.5);
                float wire=min(distance.x,distance.y);
                float aa=max(.001,max(fwidth(p.x),fwidth(p.y))*.45);
                half alpha=1-smoothstep(_WireWidth-aa,_WireWidth+aa,wire);
                clip(alpha-.45);
                return half4(_BaseColor.rgb,alpha);
            }
            ENDHLSL
        }
    }
}
