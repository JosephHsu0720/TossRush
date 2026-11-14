Shader "TossRush/House Garden Sky"
{
    Properties
    {
        _SkyColor("Upper sky", Color) = (.24,.57,.84,1)
        _HorizonColor("Horizon", Color) = (.77,.89,.92,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _SkyColor, _HorizonColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.direction = v.positionOS.xyz;
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float3 direction = normalize(i.direction);
                half height = pow(saturate(direction.y * 1.7), .55);
                return half4(lerp(_HorizonColor.rgb, _SkyColor.rgb, height), 1);
            }
            ENDHLSL
        }
    }
}
